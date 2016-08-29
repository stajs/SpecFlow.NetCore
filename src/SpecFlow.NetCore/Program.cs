using System;
using System.IO;

namespace SpecFlow.NetCore
{
	public class Program
	{
		public static int Main(string[] args)
		{
			try
			{
				var path = args.Length == 1 ? args[0] : Directory.GetCurrentDirectory();
				var directory = new DirectoryInfo(path);

				new Fixer().Fix(directory);

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