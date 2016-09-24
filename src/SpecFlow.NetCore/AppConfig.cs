using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json;
using Specflow.NetCore;
using static System.Console;

namespace SpecFlow.NetCore
{
	internal class AppConfig
	{
		#region Ignore the strange indentation; it is like that so the final file looks right.
		public const string SpecFlowSectionDefinitionType = "TechTalk.SpecFlow.Configuration.ConfigurationSectionHandler, TechTalk.SpecFlow";

		public const string SpecFlowSectionDefinition = @"	<configSections>
		<section name=""specFlow"" type=""" + SpecFlowSectionDefinitionType + @""" />
	</configSections>";

		public const string SpecFlowSectionElement = "unitTestProvider";

		public const string SpecFlowSection = @"	<specFlow>
		<" + SpecFlowSectionElement + @" name=""{0}"" />
	</specFlow>";

		public static string Content = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
{SpecFlowSectionDefinition}
{SpecFlowSection}
</configuration>";
		#endregion

		public string Path { get; }

		public string TestFramework { get; }

		public AppConfig(string path, string testFramework)
		{
			Path = path;
			TestFramework = testFramework;
		}

		public static AppConfig CreateIn(DirectoryInfo directory, out string usedTestRunner, string testRunner = null)
		{
			if (string.IsNullOrWhiteSpace(testRunner))
			{
				testRunner = GuessUnitTestProvider(directory);
			}
			var config = new AppConfig(System.IO.Path.Combine(directory.FullName, "app.config"), testRunner);

			WriteLine($@"Using test runner ""{testRunner}""");
			usedTestRunner = testRunner;
			if (File.Exists(config.Path))
			{
				config.ChangeTestProviderIfNeeded(testRunner);
				return config;
			}

			Content = string.Format(Content, testRunner);
			WriteLine("Generating app.config");
			WriteLine(Content);
			WriteLine("Saving: " + config.Path);
			File.WriteAllText(config.Path, Content);

			return config;
		}

		public void ChangeTestProviderIfNeeded(string testRunner)
		{
			var file = XDocument.Load(Path);

			var currentTestRunner = GetTestProvider(file);
			if (!string.IsNullOrWhiteSpace(currentTestRunner) && testRunner == currentTestRunner)
				return;

			var configXml = file.Descendants("configuration").FirstOrDefault();
			var specFlowXml = configXml?.Descendants("specFlow").FirstOrDefault();
			var unitTestProviderXml = specFlowXml?.Descendants("unitTestProvider").FirstOrDefault();
			if (unitTestProviderXml != null)
			{
				unitTestProviderXml.Attribute("name").Value = testRunner;
				WriteLine("unitTestProviderXml Name attribute = " + unitTestProviderXml.Attribute("name").Value);
			}
			using (var writer = File.CreateText(Path))
			{
				file.Save(writer);
				writer.Flush();
			}
		}

		private static string GetTestProvider(XDocument file)
		{
			return file.XPathEvaluate("string(/configuration/specFlow/unitTestProvider/@name)") as string;
		}

		public void Validate()
		{
			WriteLine("Validating app.config.");

			// I would rather use the ConfigurationBuilder, but as of beta8 it fails to read an element without
			// a value, e.g.: <unitTestProvider name="xUnit" />
			//var config = new ConfigurationBuilder()
			//	.AddXmlFile(path)
			//	.Build();

			var file = XDocument.Load(Path);

			var definitionType = file.XPathEvaluate("string(/configuration/configSections/section[@name='specFlow']/@type)") as string;

			if (definitionType != SpecFlowSectionDefinitionType)
				throw new Exception("Couldn't find required SpecFlow section handler in app.config. Example:\n" + SpecFlowSectionDefinition);

			var testProvider = GetTestProvider(file);

			if (string.IsNullOrWhiteSpace(testProvider))
				throw new Exception("Couldn't find required SpecFlow element in app.config. Example:\n" + SpecFlowSection);
		}

		private static string GuessUnitTestProvider(DirectoryInfo directory)
		{
			WriteLine($"Guessing test runner from project.json. You can always force a specific test runner using {Args.TestRunnerArgName}");
			var foundProjectJson = directory.GetFiles().SingleOrDefault(f => f.Name == "project.json");
			if (foundProjectJson != null)
			{
				var projectJson = JsonConvert.DeserializeObject<ProjectJson>(File.ReadAllText(foundProjectJson.FullName));
				if (projectJson.TestRunner != null)
				{
					return projectJson.TestRunner;
				}
				WriteLine($@"No ""testRunner"" element found in { foundProjectJson.FullName }. Defaulting to xUnit.");
				return "xUnit";
			}
			WriteLine("No project json found. Defaulting to xUnit.");
			return "xUnit";
		}
	}
}
