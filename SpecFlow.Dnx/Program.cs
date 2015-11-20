using System;
using System.IO;

namespace SpecFlow.Dnx
{
	public class Program
	{
		public Program()
		{
		}

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
				Console.WriteLine("Error: " + e.Message);
				return -1;
			}
		}
	}
}