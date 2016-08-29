using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SpecFlow.Dnx
{
    internal class Fixer
    {
        public readonly string SpecFlowExe;

        private const string AppConfigSpecFlowSectionDefinitionType = "TechTalk.SpecFlow.Configuration.ConfigurationSectionHandler, TechTalk.SpecFlow";

        private const string AppConfigSpecFlowSectionDefinition = @"	<configSections>
		<section name=""specFlow"" type=""" + AppConfigSpecFlowSectionDefinitionType + @""" />
	</configSections>";

        private const string AppConfigSpecFlowSectionElement = "unitTestProvider";

        // TODO: Allow specifying other unit test providers.
        private const string AppConfigSpecFlowSection = @"	<specFlow>
		<" + AppConfigSpecFlowSectionElement + @" name=""xUnit"" />
	</specFlow>";

        public Fixer()
        {
            // TODO: Allow for other DNX install locations.
            var users = Directory.GetDirectories(@"C:\Users");

            var dnxUser = users.SingleOrDefault(u =>
            {
                try
                {
                    return Directory.GetDirectories(u).Any(d => d.Contains(".dnx"));
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }
            });
            if (dnxUser == null)
            {
                throw new Exception("Can't find a user with a .dnx folder in his/her home directory");
            }

            var SpecFlowPaths = new List<string>
            {
                Path.Combine(dnxUser, @".nuget\packages\SpecFlow\2.1.0\tools\specflow.exe"),
                Path.Combine(dnxUser, @".dnx\packages\SpecFlow\2.1.0\tools\specflow.exe"),
                Path.Combine(dnxUser, @".nuget\packages\SpecFlow\2.0.0\tools\specflow.exe"),
                Path.Combine(dnxUser, @".dnx\packages\SpecFlow\2.0.0\tools\specflow.exe"),
                Path.Combine(dnxUser, @".nuget\packages\SpecFlow\1.9.0\tools\specflow.exe"),
                Path.Combine(dnxUser, @".dnx\packages\SpecFlow\1.9.0\tools\specflow.exe")
            };

            for(var i = 0; i < SpecFlowPaths.Count && !File.Exists(SpecFlowExe); i++)
            {
                SpecFlowExe = SpecFlowPaths[i];
            }

            if(!File.Exists(SpecFlowExe))
            {
                throw new Exception("Can't find SpecFlow: " + SpecFlowExe);
            }

            Console.WriteLine("Found: " + SpecFlowExe);
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
                content = content.Replace("[Xunit.Extensions", "[Xunit");
                File.WriteAllText(glueFile.FullName, content);
            }
        }

        private string SaveSpecFlowConfig()
        {
            // Target later version of .NET.
            // Credit: http://stackoverflow.com/questions/11363202/specflow-fails-when-trying-to-generate-test-execution-report

            Console.WriteLine("Generating specflow.exe.config.");

            var configPath = SpecFlowExe + ".config";
            var content = "<?xml version=\"1.0\" encoding=\"utf-8\" ?><configuration><startup><supportedRuntime version=\"v4.0.30319\" /></startup></configuration>";
            Console.WriteLine(content);

            Console.WriteLine("Saving: " + configPath);
            File.WriteAllText(configPath, content);

            return configPath;
        }

        private string EnsureAppConfig(DirectoryInfo directory)
        {
            var path = Path.Combine(directory.FullName, "app.config");

            if (File.Exists(path))
                return path;

            Console.WriteLine("Generating app.config.");

            var content = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
{AppConfigSpecFlowSectionDefinition}
{AppConfigSpecFlowSection}
</configuration>";

            Console.WriteLine(content);

            Console.WriteLine("Saving: " + path);
            File.WriteAllText(path, content);

            return path;
        }

        private void ValidateAppConfig(string path)
        {
            Console.WriteLine("Validating app.config.");

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
            Console.WriteLine($"Calling: {SpecFlowExe} {arguments}");

            var p = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    FileName = SpecFlowExe,
                    Arguments = arguments
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
            Console.WriteLine("Generating fake csproj.");

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