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

			// Command line arguments are required.  If absent, exit.
			if (args == null || !args.Any())
			{
				return;
			}

			// establish a dictionary of all good command line variables
			var argDictionary = new Dictionary<string, string>()
			{
				{SpecFlowPathArgName , null},
				{WorkingDirectoryArgName, null },
				{TestFrameworkArgName, null },
				{ToolsVersionArgName, null }
			};


			string lastKey = null;
			// loop through all arguments, attempting to detect and fix bad values
			for (var x = 0; x < args.Length; x++)
			{
				if (argDictionary.ContainsKey(args[x]))
				{
					lastKey = args[x];
				}
				else if (string.IsNullOrEmpty(lastKey))
				{
					throw new Exception("Unknown argument: " + args[x]);
				}
				else
				{
					argDictionary[lastKey] = string.IsNullOrEmpty(argDictionary[lastKey]) ? args[x] : argDictionary[lastKey] + args[x];
				}
			}

			foreach(var key in argDictionary.Keys)
			{
				switch (key)
				{
					case SpecFlowPathArgName:
						SpecFlowPath = argDictionary[key];
						break;

					case WorkingDirectoryArgName:
						if (!Directory.Exists(argDictionary[key]))
							throw new Exception("Working directory doesn't exist: " + argDictionary[key]);
						WorkingDirectory = new DirectoryInfo(argDictionary[key]);
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
		}

		private bool IsOdd(int i)
		{
			return i % 2 != 0;
		}
	}
}