//
// Copyright 2023 - Jeffrey "botman" Broome
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P4Util
{
	internal class ConsoleCommand
	{
        private StringBuilder stdoutBuilder = new StringBuilder();
        private StringBuilder stderrBuilder = new StringBuilder();

		private bool StdOutDone;  // wait until OnOutputDataReceived receives e.Data == null to know that stdout has terminated
		private bool StdErrDone;  // wait until OnErrorDataReceived receives e.Data == null to know that stderr has terminated

		// run a commandline command with arguments and return a bool result (true if ran without failure, false otherwise)
		public bool Run(string command, string arguments, string stdin, out string stdout, out string stderr, int timeout_in_seconds = 5)
		{
			stdout = "";
			stderr = "";

			bool bTimedOut = false;

			try
			{
				Process proc = new Process();

				StdOutDone = false;
				StdErrDone = false;

				ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe");

				startInfo.UseShellExecute = false;
				startInfo.CreateNoWindow = true;

				if (stdin != null && stdin != "")
				{
					startInfo.RedirectStandardInput = true;
				}
				else
				{
					startInfo.RedirectStandardInput = false;
				}
				startInfo.RedirectStandardOutput = true;
				startInfo.RedirectStandardError = true;

				startInfo.Arguments = "/C " + command + " " + arguments;

				proc.StartInfo = startInfo;
				proc.EnableRaisingEvents = true;

				proc.OutputDataReceived += OnOutputDataReceived;
				proc.ErrorDataReceived += OnErrorDataReceived;

				proc.Start();

				proc.BeginOutputReadLine();
				proc.BeginErrorReadLine();

				if (startInfo.RedirectStandardInput)
				{
					proc.StandardInput.WriteLine(stdin);
					proc.StandardInput.Close();
				}

				if (!proc.WaitForExit(timeout_in_seconds * 1000))  // wait with a timeout (in milliseconds)
				{
					// process timed out, kill the process...
					bTimedOut = true;
					proc.Kill();
				}

				// wait for stdout and stderr streams to flush (loop 500 times with 10ms delay each time for a total of 5 seconds)
				int output_timeout = 500;  // number of loops
				while (!StdOutDone || !StdErrDone)  // wait until both stdout and stderr have closed
				{
					Thread.Sleep(10);

					if (--output_timeout == 0)
					{
						// took too long to close stdout or stderr, bail out anyway
						bTimedOut = true;
						break;
					}
				}

				proc.Close();
			}
			catch(Exception ex)
			{
				string message = String.Format("P4Util.RunExecutable.Run exception: {0}", ex.Message);
				System.Console.WriteLine(message);
			}

			stdout = stdoutBuilder.ToString();
			stderr = stderrBuilder.ToString();

			return !bTimedOut;
		}

		private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != null)
			{
				stdoutBuilder.AppendLine(e.Data);
			}
			else
			{
				StdOutDone = true;
			}
		}

		private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != null)
			{
				stderrBuilder.AppendLine(e.Data);
			}
			else
			{
				StdErrDone = true;
			}
		}
	}
}
