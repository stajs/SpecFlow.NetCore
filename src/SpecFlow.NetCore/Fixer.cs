using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using static System.Console;

namespace SpecFlow.NetCore
{
	internal class Fixer
	{
		private const string AppConfigSpecFlowSectionDefinitionType = "TechTalk.SpecFlow.Configuration.ConfigurationSectionHandler, TechTalk.SpecFlow";

		private const string AppConfigSpecFlowSectionDefinition = @"	<configSections>
		<section name=""specFlow"" type=""" + AppConfigSpecFlowSectionDefinitionType + @""" />
	</configSections>";

		private const string AppConfigSpecFlowSectionElement = "unitTestProvider";

		// TODO: Allow specifying other unit test providers.
		private const string AppConfigSpecFlowSection = @"	<specFlow>
		<" + AppConfigSpecFlowSectionElement + @" name=""xUnit"" />
	</specFlow>";

		private readonly string _specFlowExe;

		public Fixer()
		{
			// For full .NET Framework, you can get the user profile with: Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
			// This isn't available yet in .NET Core, so rely on the environment variable for now.
			var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");

			_specFlowExe = Path.Combine(userProfile, @".nuget\packages\SpecFlow\2.1.0\tools\specflow.exe");

			if (!File.Exists(_specFlowExe))
				throw new Exception("Can't find SpecFlow: " + _specFlowExe);

			WriteLine("Found: " + _specFlowExe);
		}

		public void Fix(DirectoryInfo directory)
		{
			WriteLine("Current directory: " + directory);
			var xproj = GetXproj(directory);
			var fakeCsproj = SaveFakeCsProj(directory, xproj);
			GenerateSpecFlowGlue(directory, fakeCsproj);
			DeleteFakeCsProj(fakeCsproj);
			FixXunit(directory);
		}

		private void DeleteFakeCsProj(FileInfo fakeCsproj)
		{
			WriteLine("Removing: " + fakeCsproj.FullName);
			fakeCsproj.Delete();
		}

		private void FixXunit(DirectoryInfo directory)
		{
			WriteLine("Fixing SpecFlow generated files for xUnit 2");

			var glueFiles = directory.GetFiles("*.feature.cs", SearchOption.AllDirectories);

			foreach (var glueFile in glueFiles)
			{
				WriteLine("Fixed: " + glueFile.FullName);
				var content = File.ReadAllText(glueFile.FullName);
				content = content.Replace(" : Xunit.IUseFixture<", " : Xunit.IClassFixture<");
				content = content.Replace("[Xunit.Extensions", "[Xunit");
				File.WriteAllText(glueFile.FullName, content);
			}
		}

		private string SaveSpecFlowConfig()
		{
			// Target later version of .NET.
			// Credit: http://stackoverflow.com/questions/11363202/specflow-fails-when-trying-to-generate-test-execution-report

			WriteLine("Generating specflow.exe.config.");

			var configPath = _specFlowExe + ".config";
			var content = "<?xml version=\"1.0\" encoding=\"utf-8\" ?><configuration><startup><supportedRuntime version=\"v4.0.30319\" /></startup></configuration>";
			WriteLine(content);

			WriteLine("Saving: " + configPath);
			File.WriteAllText(configPath, content);

			return configPath;
		}

		private string EnsureAppConfig(DirectoryInfo directory)
		{
			var path = Path.Combine(directory.FullName, "app.config");

			if (File.Exists(path))
				return path;

			WriteLine("Generating app.config.");

			var content = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
{AppConfigSpecFlowSectionDefinition}
{AppConfigSpecFlowSection}
</configuration>";

			WriteLine(content);

			WriteLine("Saving: " + path);
			File.WriteAllText(path, content);

			return path;
		}

		private void ValidateAppConfig(string path)
		{
			WriteLine("Validating app.config.");

			// I would rather use the ConfigurationBuilder, but as of beta8 it fails to read an element without
			// a value, e.g.: <unitTestProvider name="xUnit" />
			//var config = new ConfigurationBuilder()
			//	.AddXmlFile(path)
			//	.Build();

			var file = XDocument.Load(path);

			var definitionType = file.XPathEvaluate("string(/configuration/configSections/section[@name='specFlow']/@type)") as string;

			if (definitionType != AppConfigSpecFlowSectionDefinitionType)
				throw new Exception("Couldn't find required SpecFlow section handler in app.config. Example:\n" + AppConfigSpecFlowSectionDefinition);

			var element = file.XPathEvaluate("string(/configuration/specFlow/unitTestProvider/@name)") as string;

			if (string.IsNullOrWhiteSpace(element))
				throw new Exception("Couldn't find required SpecFlow element in app.config. Example:\n" + AppConfigSpecFlowSection);
		}

		private void RunSpecFlow(string csproj)
		{
			// Credit: http://www.marcusoft.net/2010/12/specflowexe-and-mstest.html
			var arguments = $"generateall {csproj} /force /verbose";
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

		private void DeleteSpecFlowConfig(string configPath)
		{
			WriteLine("Removing the SpecFlow config file.");
			File.Delete(configPath);
		}

		private void GenerateSpecFlowGlue(DirectoryInfo directory, FileInfo fakeCsproj)
		{
			var appConfigPath = EnsureAppConfig(directory);
			ValidateAppConfig(appConfigPath);
			var specFlowConfigPath = SaveSpecFlowConfig();
			RunSpecFlow(fakeCsproj.Name);
			DeleteSpecFlowConfig(specFlowConfigPath);
		}

		private FileInfo SaveFakeCsProj(DirectoryInfo directory, FileInfo xproj)
		{
			WriteLine("Generating fake csproj.");

			var featureFiles = directory.GetFiles("*.feature", SearchOption.AllDirectories);
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

			foreach (var featureFile in featureFiles)
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