using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SpecFlow.Dnx
{
	internal class Fixer
	{
		public readonly string _specFlowExe;

		public Fixer()
		{
			_specFlowExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".dnx\packages\SpecFlow\1.9.0\tools\specflow.exe");

			if (!File.Exists(_specFlowExe))
				throw new Exception("Can't find SpecFlow: " + _specFlowExe);

			Console.WriteLine("Found SpecFlow: " + _specFlowExe);
		}

		public void Fix(DirectoryInfo directory)
		{
			Console.WriteLine("Current directory: " + directory);
			var xproj = GetXproj(directory);
			var fakeCsproj = GenerateFakeCsProj(directory, xproj);
			GenerateSpecFlowGlue(fakeCsproj);
			FixXunit(directory);
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

		private void GenerateSpecFlowGlue(FileInfo fakeCsproj)
		{
			// Target later version of .NET.
			// Credit: http://stackoverflow.com/questions/11363202/specflow-fails-when-trying-to-generate-test-execution-report
			var configPath = _specFlowExe + ".config";
			var configFileContents = "<?xml version=\"1.0\" encoding=\"utf-8\" ?><configuration><startup><supportedRuntime version=\"v4.0.30319\" /></startup></configuration>";
			File.WriteAllText(configPath, configFileContents);
			Console.WriteLine("Created SpecFlow config file.");

			// Credit: http://www.marcusoft.net/2010/12/specflowexe-and-mstest.html
			var command = $"{_specFlowExe} generateall {fakeCsproj.Name} /force /verbose";
			Console.WriteLine("Calling: " + command);

			var p = new Process
			{
				StartInfo =
				{
					UseShellExecute = false,
					RedirectStandardOutput = true,
					FileName = _specFlowExe,
					Arguments = $"generateall {fakeCsproj.Name} /force /verbose"
				}
			};

			p.Start();

			var output = p.StandardOutput.ReadToEnd();
			p.WaitForExit();

			Console.WriteLine(output);

			Console.WriteLine("Removing the SpecFlow config file.");
			File.Delete(configPath);
		}

		private FileInfo GenerateFakeCsProj(DirectoryInfo directory, FileInfo xproj)
		{
			Console.WriteLine("Generating fake csproj...");

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
			Console.WriteLine("Found xproj: " + xproj.Name);

			return xproj;
		}
	}
}