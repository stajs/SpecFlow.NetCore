using System;
using System.IO;

namespace SpecFlow.Dnx
{
	public class Program
	{
		public static int Main(string[] args)
		{
			try
			{
				var path = args.Length == 1 ? args[0] : Environment.CurrentDirectory;
				var directory = new DirectoryInfo(path);

				new Fixer().Fix(directory);
				return 0;
			}
			catch (Exception e)
			{
				var color = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Error: " + e.Message);
				Console.ForegroundColor = color;

				return -1;
			}
		}
	}
}