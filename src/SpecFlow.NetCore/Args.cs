using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.Console;

namespace Specflow.NetCore
{
	internal class Args
	{
		public const string SpecFlowPathArgName = "--specflow-path";
		public const string WorkingDirectoryArgName = "--working-directory";
		public const string TestFrameworkArgName = "--test-framework";
		public const string ToolsVersionArgName = "--tools-version";

		public string SpecFlowPath { get; }
		public DirectoryInfo WorkingDirectory { get; }
		public string TestFramework { get; }
		public string ToolsVersion { get; }

		public Args(string[] args)
		{
			WorkingDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

			if (args == null || !args.Any())
				return;

			// establish a dictionary of all good command line variables
			var argDictionary = new Dictionary<string, string>
			{
 				{ SpecFlowPathArgName, null },
 				{ WorkingDirectoryArgName, null },
 				{ TestFrameworkArgName, null },
 				{ ToolsVersionArgName, null }
 			};

			string lastKey = null;

			foreach (var arg in args)
			{
				if (argDictionary.ContainsKey(arg))
				{
					lastKey = arg;
				}
				else if (arg.StartsWith("--") || string.IsNullOrEmpty(lastKey))
				{
					// We are making the assumption that anything starting with -- is intentionally an argument key.
					// If that argument key is not in the dictionary, we know it is a bad argument.
					// Additionally, if the first argument key is not in our dictionary, the arguments are bad.
					throw new Exception("Unknown argument: " + arg);
				}
				else
				{
					argDictionary[lastKey] = string.IsNullOrEmpty(argDictionary[lastKey]) ? arg : argDictionary[lastKey] + " " + arg;
				}
			}

			foreach (var key in argDictionary.Keys)
			{
				switch (key)
				{
					case SpecFlowPathArgName:
						SpecFlowPath = argDictionary[key];
						break;

					case WorkingDirectoryArgName:
						var path = string.IsNullOrEmpty(argDictionary[key]) ? Directory.GetCurrentDirectory() : argDictionary[key];
						if (!Directory.Exists(path))
							throw new Exception("Working directory doesn't exist: " + path);
						WorkingDirectory = new DirectoryInfo(path);
						break;

					case TestFrameworkArgName:
						TestFramework = argDictionary[key];
						break;

					case ToolsVersionArgName:
						ToolsVersion = argDictionary[key];
						break;
				}
			}

			WriteLine("SpecFlowPath: " + SpecFlowPath);
			WriteLine("WorkingDirectory: " + WorkingDirectory.FullName);
			WriteLine("TestFramework: " + TestFramework);
			WriteLine("ToolsVersion: " + ToolsVersion);
		}
	}
}