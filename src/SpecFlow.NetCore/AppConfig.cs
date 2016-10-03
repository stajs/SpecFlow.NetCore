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

		public DirectoryInfo Directory { get; set; }
		public string Path => System.IO.Path.Combine(Directory.FullName, "app.config");
		public string TestFramework { get; }

		public AppConfig(DirectoryInfo directory, string testFramework)
		{
			Directory = directory;
			TestFramework = testFramework;
		}

		public static AppConfig CreateIn(DirectoryInfo directory, string testFramework = null)
		{
			if (string.IsNullOrWhiteSpace(testFramework))
				testFramework = GetProjectJsonTestRunner(directory);

			var config = new AppConfig(directory, testFramework);

			if (File.Exists(config.Path))
				return config;

			Content = string.Format(Content, testFramework);

			// fixes SpecFlow Scenario Outline scenarios appearing under the project they belong to.
			// see https://github.com/stajs/SpecFlow.NetCore/issues/34 and https://github.com/techtalk/SpecFlow/issues/275
			if (testFramework.ToLower() == "mstest")
			{
				Content = Content.Replace("</specFlow>", "  <generator allowDebugGeneratedFiles=\"true\" />\r\n  </specFlow>");
			}

			WriteLine("Generating app.config");
			WriteLine(Content);
			WriteLine("Saving: " + config.Path);
			File.WriteAllText(config.Path, Content);

			return config;
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

			var projectJsonTestRunner = GetProjectJsonTestRunner(Directory);

			if (string.IsNullOrWhiteSpace(projectJsonTestRunner))
				return;

			if (!TestFramework.Equals(projectJsonTestRunner, StringComparison.OrdinalIgnoreCase))
				throw new Exception($"App.config test provider doesn't match the project.json test runner: {TestFramework} vs {projectJsonTestRunner}");
		}

		private static string GetProjectJsonTestRunner(DirectoryInfo directory)
		{
			var foundProjectJson = directory.GetFiles().SingleOrDefault(f => f.Name == "project.json");

			if (foundProjectJson == null)
				throw new Exception("Can't find project.json in directory: " + directory.FullName);

			var projectJson = JsonConvert.DeserializeObject<ProjectJson>(File.ReadAllText(foundProjectJson.FullName));

			if (string.IsNullOrWhiteSpace(projectJson.TestRunner))
				throw new Exception("project.json doesn't have a testRunner.");

			return projectJson.TestRunner;
		}
	}
}
