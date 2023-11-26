//
// Copyright 2023 - Jeffrey "botman" Broome
//

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

#pragma warning disable CS0649

namespace P4Util
{
	internal class Program
	{
		public struct RegExMatchStruct
		{
			public List<string> Matches;  // list of RegEx match strings
			public int ArgIndex;  // this keeps track of which argument in args was used to create a regex (so it can be removed from the "stack" when unwinding)
		};

		public static int Count;

		// RegExMatches is global so that args further down the arg list can reference regex's from previous earlier args
		public static List<RegExMatchStruct> RegExMatches = new List<RegExMatchStruct>();

		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("P4Util Usage:");
				Console.WriteLine("");
				Console.WriteLine("  commandline arguments are:");
				Console.WriteLine("    command=, commandl=, count, regex=, regexm=, replace=, output, output=, outputf=,");
				Console.WriteLine("    greaterthan=, lessthan=");
				Console.WriteLine("");
				Console.WriteLine("  See the P4Util_ReadMe.txt file for details");

				return;
			}

			ConsoleCommand command = new ConsoleCommand();

			// check that P4.exe exists ("p4 info" should always return some output)...
			bool result = command.Run("p4.exe", "info", "", out string stdout, out string stderr);

			if (result == false || stdout == "")
			{
				Console.WriteLine("p4.exe not found.");
				Console.WriteLine("Did you install Helix Visual Client (P4V) and check the 'Command-Line Client(P4)' option?");
				return;
			}

			// if there are "regex" commands in the args, verify that the regex expression is valid before doing anything else...
			foreach(string arg in args)
			{
				if (arg.StartsWith("regex=", StringComparison.CurrentCultureIgnoreCase))
				{
					string regular_expression = arg.Substring(6, arg.Length - 6);
					regular_expression = regular_expression.Trim(new char[] {'"'});

					try
					{
						RegexOptions options = RegexOptions.Multiline;
						Regex SearchRegEx = new Regex(regular_expression, options);
					}
					catch(Exception ex)
					{
						Console.WriteLine("Regular Expression error in args: {0}", arg);
						Console.WriteLine(ex.Message);
						Console.WriteLine("An error occurred while processing the arguments");
						return;
					}
				}
			}

			int arg_index = 0;

			Processor processor = new Processor();
			if (!processor.ProcessArgs(ref args, arg_index))
			{
				Console.WriteLine("An error occurred while processing the arguments");
			}
		}
	}
}