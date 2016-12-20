using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Specflow.NetCore;

using static System.Console;

namespace SpecFlow.NetCore
{
	internal class Fixer
	{
		private readonly string _specFlowExe;
		private string _testFramework;
		private FileInfo[] _featureFiles;

		public Fixer(string specFlowPath = null, string testFramework = null)
		{
			_specFlowExe = FindSpecFlow(specFlowPath);
			WriteLine("Found: " + _specFlowExe);

			_testFramework = testFramework;
		}

		private string FindSpecFlow(string path)
		{
			if (!string.IsNullOrWhiteSpace(path))
			{
				if (File.Exists(path))
					return path;

				throw new Exception("Path to SpecFlow was supplied as an argument, but doesn't exist: " + path);
			}

			const string nuGetPackagesVariable = "NUGET_PACKAGES";
			var nugetPackagesPath = Environment.GetEnvironmentVariable(nuGetPackagesVariable);
			if (!string.IsNullOrWhiteSpace(nugetPackagesPath))
			{
				path = ResolveSpecFlowApplication(nuGetPackagesVariable, nugetPackagesPath);

				if (File.Exists(path))
					return path;

				throw new Exception("NUGET_PACKAGES environment variable found, but SpecFlow doesn't exist: " + path);
			}

			// For full .NET Framework, you can get the user profile with: Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
			// This isn't available yet in .NET Core, so rely on the environment variable for now.
			const string userProfileVariable = "USERPROFILE";
			var userProfile = Environment.GetEnvironmentVariable(userProfileVariable);
			if (!string.IsNullOrWhiteSpace(userProfile))
				path = ResolveSpecFlowApplication(userProfileVariable, Path.Combine(userProfile, ".nuget", "packages"));

			if (File.Exists(path))
				return path;

			throw new Exception($"Can't find SpecFlow: {path}\nTry specifying the path with {Args.SpecFlowPathArgName}.");
		}

		const string RelativePathToSpecFlowApplication = @"tools\specflow.exe";
		private static string ResolveSpecFlowApplication(string environmentVariable, string rootPath)
		{
			var specflowVersionsDirectory = Path.Combine(rootPath, "SpecFlow");
			if (!Directory.Exists(specflowVersionsDirectory))
				throw new Exception($"{environmentVariable} environment variable found, but no versions of SpecFlow found: {specflowVersionsDirectory}");
			var specflowVersions = Directory.GetDirectories(specflowVersionsDirectory);
			var latestSpecflowVersion = specflowVersions.Max();
			if (specflowVersions.Length > 1)
				WriteLine("Found multiple versions of SpecFlow in the NUGET_PACKAGES directory, using '{0}'.", latestSpecflowVersion);
			return Path.Combine(specflowVersionsDirectory, latestSpecflowVersion, RelativePathToSpecFlowApplication);
		}

		public void Fix(DirectoryInfo directory)
		{
			WriteLine("Current directory: " + directory.FullName);
			_featureFiles = directory.GetFiles("*.feature", SearchOption.AllDirectories);
			var missingGeneratedFiles = _featureFiles.Where(f => !File.Exists(f.FullName + ".cs")).ToList();

			var xproj = GetXproj(directory);
			var fakeCsproj = SaveFakeCsProj(directory, xproj);
			GenerateSpecFlowGlue(directory, fakeCsproj);
			DeleteFakeCsProj(fakeCsproj);
			FixTests(directory);

			if (missingGeneratedFiles.Any())
			{
				missingGeneratedFiles.ForEach(WarnNotExists);
				WriteLine("Rebuild to make the above files discoverable, see https://github.com/stajs/SpecFlow.NetCore/issues/22.");
			}
		}

		private void WarnNotExists(FileInfo featureFile)
		{
			WriteLine($@"New file generated: {featureFile.FullName}.cs. No tests in {featureFile.Name} will be discovered by 'dotnet test'");
		}

		private void DeleteFakeCsProj(FileInfo fakeCsproj)
		{
			WriteLine("Removing: " + fakeCsproj.FullName);
			fakeCsproj.Delete();
		}

		private void FixTests(DirectoryInfo directory)
		{
			WriteLine("Fixing SpecFlow generated files for xUnit 2");

			var glueFiles = directory.GetFiles("*.feature.cs", SearchOption.AllDirectories);

			foreach (var glueFile in glueFiles)
			{
				WriteLine("Fixing: " + glueFile.FullName);
				var content = File.ReadAllText(glueFile.FullName);

				if (_testFramework.Equals("xunit", StringComparison.OrdinalIgnoreCase))
					content = FixXunit(content);
				else if (_testFramework.Equals("mstest", StringComparison.OrdinalIgnoreCase))
					content = FixMsTest(content);

				File.WriteAllText(glueFile.FullName, content);
			}
		}

		private static string FixMsTest(string content)
		{
			content = Regex.Replace(content, ".*Microsoft.VisualStudio.TestTools.UnitTesting.Description.*", "");
			return content;
		}

		private static string FixXunit(string content)
		{
			content = content.Replace(" : Xunit.IUseFixture<", " : Xunit.IClassFixture<");
			content = content.Replace("[Xunit.Extensions", "[Xunit");
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

		private void GenerateSpecFlowGlue(DirectoryInfo directory, FileInfo fakeCsproj)
		{
			var appConfig = AppConfig.CreateIn(directory);
			appConfig.Validate();
			_testFramework = appConfig.TestFramework;
			RunSpecFlow(fakeCsproj.FullName);
		}

		private FileInfo SaveFakeCsProj(DirectoryInfo directory, FileInfo xproj)
		{
			WriteLine("Generating fake csproj.");

			var sb = new StringBuilder();

			// Set the "ToolsVersion" to VS2013, see: https://github.com/techtalk/SpecFlow/issues/471
			sb.Append(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
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
			<LastGenOutput>{featureFile.Name}.cs</LastGenOutput>
		</None>");
			}

			sb.Append(@"
	</ItemGroup>
</Project>");

			var content = sb.ToString();
			WriteLine(content);

			var csprojPath = xproj.FullName + ".fake.csproj";
			WriteLine("Saving: " + csprojPath);
			File.WriteAllText(csprojPath, content);

			return new FileInfo(csprojPath);
		}

		private FileInfo GetXproj(DirectoryInfo directory)
		{
			var xprojs = directory.GetFiles("*.xproj", SearchOption.TopDirectoryOnly);

			if (xprojs.Length == 0)
				throw new Exception("Could not find '.xproj'.");

			if (xprojs.Length > 1)
				throw new Exception("More than one '.xproj' found.");

			var xproj = xprojs.Single();
			WriteLine("Found: " + xproj.FullName);

			return xproj;
		}
	}
}
