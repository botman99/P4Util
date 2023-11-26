//
// Copyright 2023 - Jeffrey "botman" Broome
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace P4Util
{
	internal class Processor
	{
		public bool ProcessArgs(ref string[] args, int arg_index, string input = "")
		{
			bool ProcessArgsResult = true;

			if ((args[arg_index].StartsWith("command=", StringComparison.CurrentCultureIgnoreCase)) ||
				(args[arg_index].StartsWith("commandl=", StringComparison.CurrentCultureIgnoreCase)))
			{
				string p4command = "";
				bool bIsCommandL = false;

				if (args[arg_index].StartsWith("commandl=", StringComparison.CurrentCultureIgnoreCase))
				{
					p4command = args[arg_index].Substring(9, args[arg_index].Length - 9);
					bIsCommandL = true;
				}
				else
				{
					p4command = args[arg_index].Substring(8, args[arg_index].Length - 8);
				}

				p4command = p4command.Trim(new char[] {'"'});

				// handle command that contains "< %i" or "<%i"
				string stdin = "";
				if (p4command.Contains("< %i"))
				{
					p4command = p4command.Replace("< %i", "").Trim();
					stdin = input;
				}
				else if (p4command.Contains("<%i"))
				{
					p4command = p4command.Replace("<%i", "").Trim();
					stdin = input;
				}

				string new_p4command = "";
				if (!ScanArgumentForFormatStrings(p4command, input, ref new_p4command))
				{
					return false;
				}

				if (p4command.StartsWith("p4"))
				{
					Console.WriteLine("Error: '{0}', don't put \"P4\" in the command.", args[arg_index]);
					return false;
				}

				ConsoleCommand command = new ConsoleCommand();
				bool RunResult = command.Run("p4.exe", new_p4command, stdin, out string stdout, out string stderr);

				if (!RunResult || (stderr != ""))
				{
					if (stderr != "")
					{
						Console.WriteLine("Error: \"command=\" P4 failed on '{0}', stderr = {1}", new_p4command, stderr);
					}
					else
					{
						Console.WriteLine("Error: \"command=\" P4 failed on '{0}'", new_p4command);
					}
					return false;
				}

				arg_index++;

				if (arg_index == args.Length)  // if we've reached the end of the args list, then return success
				{
					return true;
				}

				stdout = stdout.Replace("\r", "");  // remove any embedded carriage returns and leave only the newlines (for regex '^' and '$' match)

				// do we want to process things line by line from the output?
				if (bIsCommandL)
				{
					// for each line of output from the P4 command, pass the args and the output of the p4 command as 'input'
					string[] lines = stdout.Split('\n');

					foreach(string line in lines)
					{
						if (!ProcessArgs(ref args, arg_index, line))
						{
							ProcessArgsResult = false;
						}

						int top_index = Program.RegExMatches.Count-1;
						if ((top_index >= 0) && (arg_index == Program.RegExMatches[top_index].ArgIndex))
						{
							Program.RegExMatches.RemoveAt(top_index);
						}
					}
				}
				else
				{
					if (!ProcessArgs(ref args, arg_index, stdout))
					{
						ProcessArgsResult = false;
					}

					int top_index = Program.RegExMatches.Count-1;
					if ((top_index >= 0) && (arg_index == Program.RegExMatches[top_index].ArgIndex))
					{
						Program.RegExMatches.RemoveAt(top_index);
					}
				}

				return true;
			}
			else if (args[arg_index].StartsWith("count", StringComparison.CurrentCultureIgnoreCase))
			{
				string[] lines = input.Split('\n');

				int count = 0;

				// count the lines, ignoring blank lines of output
				foreach(string line in lines)
				{
					string new_line = line.Replace("\r", "").Trim();  // remove any embedded carriage returns and leave only the newline

					if (new_line != "\n" && new_line != "")
					{
						count++;
					}
				}

				Program.Count = count;
			}
			else if (args[arg_index].StartsWith("greaterthan=", StringComparison.CurrentCultureIgnoreCase))
			{
				string greaterthan_string = args[arg_index].Substring(12, args[arg_index].Length - 12);
				greaterthan_string = greaterthan_string.Trim(new char[] {'"'});

				string new_greaterthan = "";
				if (!ScanArgumentForFormatStrings(greaterthan_string, input, ref new_greaterthan))
				{
					return false;
				}

				string[] values = new_greaterthan.Split(',');

				if (values.Count() != 2)
				{
					Console.WriteLine("Error: '{0}' does not contain 2 values to compare.", args[arg_index]);
					return false;
				}

				if (int.TryParse(values[0], out int lvalue) && int.TryParse(values[1], out int rvalue))
				{
					ProcessArgsResult = lvalue > rvalue;
				}
				else  // just do a string comparison (this should work when comparing "full" timestamps (date and time in YYYY/MM/DD HH:MM:SS format)
				{
					ProcessArgsResult = String.Compare(values[0], values[1]) > 0;
				}
			}
			else if (args[arg_index].StartsWith("lessthan=", StringComparison.CurrentCultureIgnoreCase))
			{
				string lessthan_string = args[arg_index].Substring(9, args[arg_index].Length - 9);
				lessthan_string = lessthan_string.Trim(new char[] {'"'});

				string new_lessthan = "";
				if (!ScanArgumentForFormatStrings(lessthan_string, input, ref new_lessthan))
				{
					return false;
				}

				string[] values = new_lessthan.Split(',');

				if (values.Count() != 2)
				{
					Console.WriteLine("Error: '{0}' does not contain 2 values to compare.", args[arg_index]);
					return false;
				}

				if (int.TryParse(values[0], out int lvalue) && int.TryParse(values[1], out int rvalue))
				{
					ProcessArgsResult = lvalue < rvalue;
				}
				else  // just do a string comparison (this should work when comparing "full" timestamps (date and time in YYYY/MM/DD HH:MM:SS format)
				{
					ProcessArgsResult = String.Compare(values[0], values[1]) < 0;
				}
			}
			else if (args[arg_index].StartsWith("output=", StringComparison.CurrentCultureIgnoreCase))
			{
				string output_string = args[arg_index].Substring(7, args[arg_index].Length - 7);
				output_string = output_string.Trim(new char[] {'"'});

				string new_output = "";
				if (!ScanArgumentForFormatStrings(output_string, input, ref new_output))
				{
					return false;
				}

				Console.WriteLine(new_output);
			}
			else if (args[arg_index].StartsWith("outputf=", StringComparison.CurrentCultureIgnoreCase))
			{
				try
				{
					string output_string = args[arg_index].Substring(8, args[arg_index].Length - 8);
					output_string = output_string.Trim(new char[] {'"'});

					string filename = "";
					if (!ScanArgumentForFormatStrings(output_string, input, ref filename))
					{
						return false;
					}

					if (filename == null || filename == "")
					{
						Console.WriteLine("Error: '{0}' produced an empty filename string.", args[arg_index]);
						return false;
					}

					string dir = Path.GetDirectoryName(filename);
					if (dir != null && dir != "")
					{
						Directory.CreateDirectory(dir);
					}

					StreamWriter sw = new StreamWriter(filename);
					sw.Write(input);
					sw.Close();
				}
				catch (Exception ex)
				{
					Console.WriteLine(string.Format("Exception in '{0}' command, {1}", args[arg_index], ex.Message));
					return false;
				}
			}
			else if (args[arg_index].StartsWith("output", StringComparison.CurrentCultureIgnoreCase))
			{
				Console.WriteLine(input);
			}
			else if ((args[arg_index].StartsWith("regex=", StringComparison.CurrentCultureIgnoreCase)) ||
					 (args[arg_index].StartsWith("regexm=", StringComparison.CurrentCultureIgnoreCase)))
			{
				string regular_expression = "";

				if (args[arg_index].StartsWith("regexm=", StringComparison.CurrentCultureIgnoreCase))
				{
					regular_expression = args[arg_index].Substring(7, args[arg_index].Length - 7);
				}
				else
				{
					regular_expression = args[arg_index].Substring(6, args[arg_index].Length - 6);
				}

				regular_expression = regular_expression.Trim(new char[] {'"'});

				try
				{
					RegexOptions options = RegexOptions.None;

					if (args[arg_index].StartsWith("regexm=", StringComparison.CurrentCultureIgnoreCase))
					{
						options = RegexOptions.Multiline;
					}

					Regex SearchRegEx = new Regex(regular_expression, options);

					if (SearchRegEx == null)
					{
						Console.WriteLine("Failed to create Regular Expression for: {0}", args[arg_index]);
						return false;
					}

					MatchCollection matches = SearchRegEx.Matches(input);

					if (matches.Count == 0)  // if the regex didn't match an expression, return false
					{
						return false;
					}

					Program.RegExMatchStruct regexmatch = new Program.RegExMatchStruct();

					regexmatch.Matches = new List<string>();
					regexmatch.ArgIndex = arg_index;

					foreach(Match match in matches)
					{
						for(int index = 1; index < match.Groups.Count; ++index)  // skip the first group since it is the input to the RegEx
						{
							regexmatch.Matches.Add(match.Groups[index].Value);
						}
					}

					if (regexmatch.Matches.Count == 0)
					{
						Console.WriteLine("Regular Expression for: '{0}' matched, but nothing was being captured!  Are you missing '(' and ')'?", args[arg_index]);
						return false;
					}

					Program.RegExMatches.Add(regexmatch);
				}
				catch(Exception ex)
				{
					// we shouldn't get here if the regex validate in Main succeeded, but just in case...
					Console.WriteLine("Regular Expression error in args: {0}", args[arg_index]);
					Console.WriteLine(ex.Message);
					return false;
				}
			}
			else if (args[arg_index].StartsWith("replace=", StringComparison.CurrentCultureIgnoreCase))
			{
				string replace = args[arg_index].Substring(8, args[arg_index].Length - 8);

				string[] replacements = replace.Split(',');

				if (replacements.Count() != 2)
				{
					Console.WriteLine("Error: \"replace=\" failed on '{0}'", replace);
					return false;
				}

				input = input.Replace(replacements[0], replacements[1]);
			}
			else
			{
				if (input == "")
				{
					Console.WriteLine("Error: ProcessArgs unknown argument on index {0}, '{1}'", arg_index, args[arg_index]);
				}
				else
				{
					Console.WriteLine("Error: ProcessArgs unknown argument on index {0}, '{1}' when processing input: {2}", arg_index, args[arg_index], input);
				}
			}

			// everything above except "command=" falls through to here to continue processing original args
			arg_index++;

			if (arg_index == args.Length)  // if we've reached the end of the args list, then return success
			{
				return true;
			}

			if (ProcessArgsResult)
			{
				ProcessArgsResult = ProcessArgs(ref args, arg_index, input);  // NOTE: we pass the original input line through to the next command unmodified
			}

			{
				int top_index = Program.RegExMatches.Count-1;
				if ((top_index >= 0) && (arg_index == Program.RegExMatches[top_index].ArgIndex))
				{
					Program.RegExMatches.RemoveAt(top_index);
				}
			}

			return ProcessArgsResult;
		}

		// in_argument is from the stage's "=" argument (with the formatting strings), input is from the previous stage, out_argument is the formatted argument
		private bool ScanArgumentForFormatStrings(string in_argument, string input, ref string out_argument)
		{
			// In the formatting string:
			// %c gets replaced with the current integer value of count,
			// %i gets replaced with the input string,
			// %rN_M gets replaced with regex number N match number M (1 relative, not zero relative)

			out_argument = in_argument;
			out_argument = out_argument.Replace("%c", Program.Count.ToString());
			out_argument = out_argument.Replace("%i", input);

			string percent_r_string = "";
			if (!ScanForPercentR(out_argument, ref percent_r_string))
			{
				// %r scan failed
				return false;
			}

			string percent_m_string = "";
			if (!ScanForPercentM(percent_r_string, ref percent_m_string))
			{
				// %m scan failed
				return false;
			}

			out_argument = percent_m_string;

			return true;
		}

		private bool ScanForPercentR(string output, ref string percent_r_string)  // 'output' is the argument of the 'output=' command
		{
			percent_r_string = output;

			// scan the input string for "%r" to replace that text with matches[N].Groups[M+1] (Groups[0] is the whole input string and Groups[1] is the first captured group)
			RegexOptions options = RegexOptions.Multiline;
			Regex SearchRegEx = new Regex(@"\%r(\d+)_(\d+)", options);

			MatchCollection percent_r_matches = SearchRegEx.Matches(output);

			foreach(Match percent_r_match in percent_r_matches.Cast<Match>())
			{
				if (percent_r_match.Groups.Count != 3)  // the regex "\%r(\d+)_(\d+)" should produce 3 groups (the whole match string plus 2 captured groups)
				{
					Console.WriteLine("Error: %r error when scanning for regex number and group number, string = '{0}'", output);
					return false;
				}

				int regex_index = Int32.Parse(percent_r_match.Groups[1].Value);
				int group_index = Int32.Parse(percent_r_match.Groups[2].Value);

				if (regex_index < 1)
				{
					Console.WriteLine("Error: %r error, regex index must be 1 or greater, string = '{0}'", output);
					return false;
				}

				if (group_index < 1)
				{
					Console.WriteLine("Error: %r error, group index must be 1 or greater, string = '{0}'", output);
					return false;
				}

				if (regex_index > Program.RegExMatches.Count())
				{
					Console.WriteLine("Error: %r error, regex index '{0}' is greater than number of regex groups, string = '{1}'", regex_index, output);
					return false;
				}

				Program.RegExMatchStruct match = Program.RegExMatches[regex_index-1];

				if (match.Matches.Count != 0)
				{
					regex_index--;  // make zero relative
					group_index--;  // make zero relative

					if (group_index >= match.Matches.Count)
					{
						Console.WriteLine("Error: %r error, group index '{0}' is greater than number of groups in regex '{1}', string = '{2}'", group_index, regex_index, output);
						return false;
					}

					percent_r_string = percent_r_string.Replace(percent_r_match.Value, match.Matches[group_index]);
				}
				else  // there were no regex matches to process, skip this output
				{
					return false;
				}
			}

			return true;
		}

		private bool ScanForPercentM(string output, ref string percent_m_string)  // 'output' is the argument of the 'output=' command
		{
			percent_m_string = output;

			// scan the input string for "%m" to replace that text with matches[N].Count
			RegexOptions options = RegexOptions.Multiline;
			Regex SearchRegEx = new Regex(@"\%m(\d+)", options);

			MatchCollection percent_m_matches = SearchRegEx.Matches(output);

			foreach(Match percent_m_match in percent_m_matches.Cast<Match>())
			{
				if (percent_m_match.Groups.Count != 2)  // the regex "\%m(\d+)" should produce 2 groups (the whole match string plus 1 captured group)
				{
					Console.WriteLine("Error: %r error when scanning for regex number and group number, string = '{0}'", output);
					return false;
				}

				int regex_index = Int32.Parse(percent_m_match.Groups[1].Value);

				if (regex_index < 1)
				{
					Console.WriteLine("Error: %r error, regex index must be 1 or greater, string = '{0}'", output);
					return false;
				}

				if (regex_index > Program.RegExMatches.Count())
				{
					Console.WriteLine("Error: %r error, regex index '{0}' is greater than number of regex groups, string = '{1}'", regex_index, output);
					return false;
				}

				Program.RegExMatchStruct match = Program.RegExMatches[regex_index-1];

				percent_m_string = percent_m_string.Replace(percent_m_match.Value, match.Matches.Count.ToString());
			}

			return true;
		}
	}
}
