using System;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
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
		<" + SpecFlowSectionElement + @" name=""xUnit"" />
	</specFlow>";
		#endregion

		public string Path { get; }

		public AppConfig(string path)
		{
			Path = path;
		}

		public static AppConfig CreateIn(DirectoryInfo directory)
		{
			var config = new AppConfig(System.IO.Path.Combine(directory.FullName, "app.config"));

			if (File.Exists(config.Path))
				return config;

			WriteLine("Generating app.config.");

			// Ignore the strange indentation; it is like that so the final file looks right.
			var content = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
{SpecFlowSectionDefinition}
{SpecFlowSection}
</configuration>";

			WriteLine(content);
			WriteLine("Saving: " + config.Path);
			File.WriteAllText(config.Path, content);

			return config;
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

			var element = file.XPathEvaluate("string(/configuration/specFlow/unitTestProvider/@name)") as string;

			if (string.IsNullOrWhiteSpace(element))
				throw new Exception("Couldn't find required SpecFlow element in app.config. Example:\n" + SpecFlowSection);
		}
	}
}