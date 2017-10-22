using System;
using Specflow.NetCore;

namespace SpecFlow.NetCore
{
	public class Program
	{
		public static int Main(string[] args)
		{
			try
			{
				var a = new Args(args);
				var fixer = new Fixer(a.SpecFlowPath, a.TestFramework, a.ToolsVersion);
				fixer.Fix(a.WorkingDirectory);

				PrintUsingColor("SpecFlow fixed.", ConsoleColor.Green);
				return 0;
			}
			catch (Exception e)
			{
				PrintUsingColor("Error: " + e.Message, ConsoleColor.Red);
				return -1;
			}
		}

		private static void PrintUsingColor(string message, ConsoleColor newColor)
		{
			var oldColor = Console.ForegroundColor;
			Console.ForegroundColor = newColor;
			Console.WriteLine(message);
			Console.ForegroundColor = oldColor;
		}
	}
}