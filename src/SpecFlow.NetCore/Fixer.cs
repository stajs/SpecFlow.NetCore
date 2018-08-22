using Specflow.NetCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using static System.Console;

namespace SpecFlow.NetCore
{
	internal class Fixer
	{
		private string _specFlowExe;
		private string _testFramework;
		private FileInfo[] _featureFiles;
		private readonly string _toolsVersion;

		public Fixer(string specFlowPath = null, string testFramework = null, string toolsVersion = "14.0")
		{
			_specFlowExe = specFlowPath;
			_testFramework = testFramework;
			_toolsVersion = toolsVersion;
		}

		private static string FindSpecFlow(string version)
		{
			string path;
			var relativePathToSpecFlow = Path.Combine("specflow", version, "tools", "specflow.exe");
			var nugetPackagesPath = Environment.GetEnvironmentVariable("NUGET_PACKAGES");

			if (!string.IsNullOrWhiteSpace(nugetPackagesPath))
			{
				path = Path.Combine(nugetPackagesPath, relativePathToSpecFlow);

				if (File.Exists(path))
					return path;

				throw new FileNotFoundException("NUGET_PACKAGES environment variable found, but SpecFlow doesn't exist: " + path);
			}
			
			var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");

			if (string.IsNullOrWhiteSpace(userProfile))
			{
				userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			}

			path = Path.Combine(userProfile, ".nuget", "packages", relativePathToSpecFlow);

			if (File.Exists(path))
				return path;

			throw new FileNotFoundException($"Can't find SpecFlow: {path}\nTry specifying the path with {Args.SpecFlowPathArgName}.");
		}

		private static bool TryGetSpecFlowVersion(FileInfo csproj, out string version)
		{
			var doc = new XmlDocument();
			doc.Load(csproj.FullName);

			var root = doc.DocumentElement;
			var node = root.SelectSingleNode("//ItemGroup/PackageReference[translate(@Include, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')='specflow']"); // case-insensitive for XPath version 1.0
			if (node == null)
			{
				if (TryGetSpecflowVersionFromImports(csproj, root, out version))
					return true;
				
				version = default(string);
				return false;
			}
			version = node.Attributes["Version"].Value;
			return true;
		}

		private static bool TryGetSpecflowVersionFromImports(FileInfo csproj, XmlElement root, out string version)
		{
			var importNodes = root.SelectNodes("//Import");
			foreach (XmlNode import in importNodes)
			{
				var relativePath = import.Attributes["Project"].Value;
				var fullPath = Path.Combine(csproj.DirectoryName, relativePath);
				if (!File.Exists(fullPath))
					continue;
				var importInfo = new FileInfo(fullPath);
				if (TryGetSpecFlowVersion(importInfo, out version))
					return true;
			}

			version = default(string);
			return false;
		}

		public IEnumerable<FileInfo> GetFeatureFromLinks(FileInfo csproj)
		{
			var doc = new XmlDocument();
			doc.Load(csproj.FullName);

			var nodes = doc.DocumentElement.SelectNodes("//ItemGroup/*[string(@Include) and string(@Link)]");

			foreach (XmlNode node in nodes)
			{
				var include = node.Attributes["Include"].Value;

				if (File.Exists(include) && Path.GetExtension(include).Equals(".feature", StringComparison.OrdinalIgnoreCase))
				{
					yield return new FileInfo(include);
				}
			}
		}

		public void Fix(DirectoryInfo directory)
		{
			WriteLine("Current directory: " + directory.FullName);

			var csproj = GetCsProj(directory);

			_featureFiles = directory.GetFiles("*.feature", SearchOption.AllDirectories)
				.Concat(GetFeatureFromLinks(csproj))
				.ToArray();

			var missingGeneratedFiles = _featureFiles.Where(f => !File.Exists(f.FullName + ".cs")).ToList();

			EnsureSpecFlow(csproj);

			var fakeCsproj = SaveFakeCsProj(directory, csproj);
			try
			{
				GenerateSpecFlowGlue(directory, fakeCsproj, csproj);
			}
			finally
			{
				DeleteFakeCsProj(fakeCsproj);
			}
			FixTests(directory);

			if (missingGeneratedFiles.Any())
			{
				missingGeneratedFiles.ForEach(WarnNotExists);
				WriteLine("Rebuild to make the above files discoverable, see https://github.com/stajs/SpecFlow.NetCore/issues/22.");
			}
		}

		private void EnsureSpecFlow(FileInfo csproj)
		{
			if (string.IsNullOrWhiteSpace(_specFlowExe))
			{
				if (!TryGetSpecFlowVersion(csproj, out string specFlowVersion))
				{
					throw new XmlException("Could not get SpecFlow version from: " + csproj.FullName);
				}

				_specFlowExe = FindSpecFlow(specFlowVersion);
				WriteLine("Found: " + _specFlowExe);
				return;
			}

			if (File.Exists(_specFlowExe))
			{
				return;
			}
			throw new FileNotFoundException("Path to SpecFlow was supplied as an argument, but doesn't exist: " + _specFlowExe);
		}

		private void WarnNotExists(FileInfo featureFile)
		{
			WriteLine($"New file generated: {featureFile.FullName}.cs. No tests in {featureFile.Name} will be discovered by 'dotnet test'");
		}

		private void DeleteFakeCsProj(FileInfo fakeCsproj)
		{
			WriteLine("Removing: " + fakeCsproj.FullName);
			fakeCsproj.Delete();
		}

		private void FixTests(DirectoryInfo directory)
		{
			WriteLine("Fixing SpecFlow generated files");

			var glueFiles = directory.GetFiles("*.feature.cs", SearchOption.AllDirectories);

			foreach (var glueFile in glueFiles)
			{
				WriteLine("Fixing: " + glueFile.FullName);
				var content = File.ReadAllText(glueFile.FullName);

				if (_testFramework.Equals("xunit", StringComparison.OrdinalIgnoreCase))
					content = FixXunit(content);
				else if (_testFramework.Equals("mstest", StringComparison.OrdinalIgnoreCase))
					content = FixMsTest(content);
				else
					content = FixNunit(content);

				File.WriteAllText(glueFile.FullName, content);
			}
		}

		private static string FixMsTest(string content)
		{
			content = Regex.Replace(content, @"\[Microsoft\.VisualStudio\.TestTools\.UnitTesting\.Description.*?\)]", "", RegexOptions.Singleline);
			return content;
		}

		private static string FixXunit(string content)
		{
			content = content.Replace(" : Xunit.IUseFixture<", " : Xunit.IClassFixture<");
			content = content.Replace("[Xunit.Extensions", "[Xunit");
			return content;
		}

		private static string FixNunit(string content)
		{
			content = content.Replace("[NUnit.Framework.TestFixtureSetUpAttribute()]", "[NUnit.Framework.OneTimeSetUp()]");
			content = content.Replace("[NUnit.Framework.TestFixtureTearDownAttribute()]", "[NUnit.Framework.OneTimeTearDown()]");
			return content;
		}

		private void RunSpecFlow(string csproj)
		{
			// Credit: http://www.marcusoft.net/2010/12/specflowexe-and-mstest.html
			var arguments = $@"generateall ""{csproj}"" /force /verbose";
			WriteLine($"Calling: {_specFlowExe} {arguments}");

			var p = new Process
			{
				StartInfo =
				{
					UseShellExecute = false,
					RedirectStandardOutput = true,
					FileName = _specFlowExe,
					Arguments = arguments
				}
			};

			p.Start();

			var output = p.StandardOutput.ReadToEnd();
			p.WaitForExit();

			WriteLine(output);

			if (output.Contains("-> test generation failed"))
				throw new Exception("SpecFlow generation failed (review the output).");
		}

		private void GenerateSpecFlowGlue(DirectoryInfo directory, FileInfo fakeCsproj, FileInfo csproj)
		{
			var appConfig = AppConfig.CreateIn(directory, csproj);
			appConfig.Validate(csproj);
			_testFramework = appConfig.TestFramework;
			RunSpecFlow(fakeCsproj.FullName);
		}

		private FileInfo SaveFakeCsProj(DirectoryInfo directory, FileInfo csproj)
		{
			WriteLine("Generating fake csproj.");

			var sb = new StringBuilder();

			// Set the "ToolsVersion" to VS2013, see: https://github.com/techtalk/SpecFlow/issues/471
			sb.Append($@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""{_toolsVersion}"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
	<PropertyGroup>
		<RootNamespace>SpecFlow.GeneratedTests</RootNamespace>
		<AssemblyName>SpecFlow.GeneratedTests</AssemblyName>
	</PropertyGroup>
	<ItemGroup>
		<None Include=""app.config"">
			<SubType>Designer</SubType>
		</None>");

			foreach (var featureFile in _featureFiles)
			{
				sb.Append($@"
		<None Include=""{featureFile.FullName.Replace(directory.FullName + Path.DirectorySeparatorChar, "")}"">
			<Generator>SpecFlowSingleFileGenerator</Generator>
			<LastGenOutput>{featureFile.FullName}.cs</LastGenOutput>
		</None>");
			}

			sb.Append(@"
	</ItemGroup>
</Project>");

			var content = sb.ToString();
			WriteLine(content);

			var fakecsprojPath = csproj.FullName + ".fake";
			WriteLine("Saving: " + fakecsprojPath);
			File.WriteAllText(fakecsprojPath, content);

			return new FileInfo(fakecsprojPath);
		}

		private FileInfo GetCsProj(DirectoryInfo directory)
		{
			var csprojs = directory.GetFiles("*.csproj", SearchOption.TopDirectoryOnly);

			if (csprojs.Length == 0)
				throw new FileNotFoundException("Could not find '.csproj'.");

			if (csprojs.Length > 1)
				throw new Exception("More than one '.csproj' found.");

			var csproj = csprojs.Single();
			WriteLine("Found: " + csproj.FullName);

			return csproj;
		}
	}
}