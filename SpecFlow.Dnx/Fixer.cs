using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SpecFlow.Dnx
{
	internal class Fixer
	{
		private class ConfigInfo
		{
			public string AppConfigPath { get; set; }
			public string TempAppConfigPath { get; set; }
		}

		public readonly string _specFlowExe;

		public Fixer()
		{
			// TODO: Allow for other DNX install locations.
			_specFlowExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".dnx\packages\SpecFlow\1.9.0\tools\specflow.exe");

			if (!File.Exists(_specFlowExe))
				throw new Exception("Can't find SpecFlow: " + _specFlowExe);

			Console.WriteLine("Found: " + _specFlowExe);
		}

		public void Fix(DirectoryInfo directory)
		{
			Console.WriteLine("Current directory: " + directory);
			var xproj = GetXproj(directory);
			var fakeCsproj = SaveFakeCsProj(directory, xproj);
			GenerateSpecFlowGlue(directory, fakeCsproj);
			DeleteFakeCsProj(fakeCsproj);
			FixXunit(directory);
		}

		private void DeleteFakeCsProj(FileInfo fakeCsproj)
		{
			Console.WriteLine("Removing: " + fakeCsproj.FullName);
			fakeCsproj.Delete();
		}

		private void FixXunit(DirectoryInfo directory)
		{
			Console.WriteLine("Fixing SpecFlow generated files for xUnit 2");

			var glueFiles = directory.GetFiles("*.feature.cs", SearchOption.AllDirectories);

			foreach (var glueFile in glueFiles)
			{
				Console.WriteLine("Fixed: " + glueFile.FullName);
				var content = File.ReadAllText(glueFile.FullName);
				content = content.Replace(" : Xunit.IUseFixture<", " : Xunit.IClassFixture<");
				File.WriteAllText(glueFile.FullName, content);
			}
		}

		private string SaveSpecFlowConfig()
		{
			// Target later version of .NET.
			// Credit: http://stackoverflow.com/questions/11363202/specflow-fails-when-trying-to-generate-test-execution-report

			Console.WriteLine("Generating specflow.exe.config.");

			var configPath = _specFlowExe + ".config";
			var content = "<?xml version=\"1.0\" encoding=\"utf-8\" ?><configuration><startup><supportedRuntime version=\"v4.0.30319\" /></startup></configuration>";
			Console.WriteLine(content);

			Console.WriteLine("Saving: " + configPath);
			File.WriteAllText(configPath, content);

			return configPath;
		}

		private ConfigInfo SaveAppConfig(DirectoryInfo directory)
		{
			var info = new ConfigInfo
			{
				AppConfigPath = Path.Combine(directory.FullName, "app.config")
			};

			if (File.Exists(info.AppConfigPath))
			{
				info.TempAppConfigPath = Path.Combine(directory.FullName, $"app.{Guid.NewGuid()}.config");
				Console.WriteLine("Found existing app.config, temporarily moving to: " + info.TempAppConfigPath);
				File.Move(info.AppConfigPath, info.TempAppConfigPath);
			}

			Console.WriteLine("Generating app.config.");

			// TODO: Allow specifying other unit test providers.
			var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
	<configSections>
		<section name=""specFlow"" type=""TechTalk.SpecFlow.Configuration.ConfigurationSectionHandler, TechTalk.SpecFlow"" />
	</configSections>
	<specFlow>
		<unitTestProvider name=""xUnit"" />
	</specFlow>
</configuration>";

			Console.WriteLine(content);

			Console.WriteLine("Saving: " + info.AppConfigPath);
			File.WriteAllText(info.AppConfigPath, content);

			return info;
		}

		private void RunSpecFlow(string csproj)
		{
			// Credit: http://www.marcusoft.net/2010/12/specflowexe-and-mstest.html
			var command = $"{_specFlowExe} generateall {csproj} /force /verbose";
			Console.WriteLine("Calling: " + command);

			var p = new Process
			{
				StartInfo =
				{
					UseShellExecute = false,
					RedirectStandardOutput = true,
					FileName = _specFlowExe,
					Arguments = $"generateall {csproj} /force /verbose"
				}
			};

			p.Start();

			var output = p.StandardOutput.ReadToEnd();
			p.WaitForExit();

			Console.WriteLine(output);

			if (output.Contains("-> test generation failed"))
				throw new Exception("SpecFlow generation failed (review the output).");
		}

		private void DeleteSpecFlowConfig(string configPath)
		{
			Console.WriteLine("Removing the SpecFlow config file.");
			File.Delete(configPath);
		}

		private void DeleteAppConfig(ConfigInfo info)
		{
			Console.WriteLine("Removing the app.config.");
			File.Delete(info.AppConfigPath);

			if (string.IsNullOrWhiteSpace(info.TempAppConfigPath))
				return;

			Console.WriteLine("Moving pre-existing app.config back.");
			File.Move(info.TempAppConfigPath, info.AppConfigPath);
		}

		private void GenerateSpecFlowGlue(DirectoryInfo directory, FileInfo fakeCsproj)
		{
			var specFlowConfigPath = SaveSpecFlowConfig();
			var configInfo = SaveAppConfig(directory);
			RunSpecFlow(fakeCsproj.Name);
			DeleteSpecFlowConfig(specFlowConfigPath);
			DeleteAppConfig(configInfo);
		}

		private FileInfo SaveFakeCsProj(DirectoryInfo directory, FileInfo xproj)
		{
			Console.WriteLine("Generating fake csproj.");

			var featureFiles = directory.GetFiles("*.feature", SearchOption.AllDirectories);
			var sb = new StringBuilder();

			// Set the "ToolsVersion" to VS2013, see: https://github.com/techtalk/SpecFlow/issues/471
			sb.Append(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
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
			Console.WriteLine(content);
			
			var csprojPath = xproj.FullName + ".fake.csproj";
			Console.WriteLine("Saving: " + csprojPath);
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
			Console.WriteLine("Found: " + xproj.FullName);

			return xproj;
		}
	}
}