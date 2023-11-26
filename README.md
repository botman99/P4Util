# P4Util - Perforce commandline utility

A Perforce commandline utility that allows you to gather information about Perforce users, workspaces, streams and branchspecs.  You can do complex queries like finding out how many users have more than 10 workspaces, which streamspecs are locked and who has them locked, which workspaces were modified after a specific date.  You can also create a backup text file of a user's workspace specifications or of stream specifications, or of branchspec specifications.  You can also use P4Util to automatically modify workspaces, streams or branchspecs.  For example, you can edit all workspaces for a user to change 'submitunchanged' to 'revertunchanged' so that unmodified files that were checked out, don't get submitted with files that have been modified.

This utility runs Perforce P4 commands and processes the output using "stages" where the output of a P4 command is used as	input to the next stage(s).  So you have something like:

>	command -> stage one -> stage two -> stage three -> stage four

If a stage returns no results (fails), then the next stage is not executed.  So you can think of each stage as an "if" statement.  If a stage fails, then execution returns back to the previous stage to continue	processing input.

The input from one stage is passed as input into the next stage.  The only thing that modifies what 'input' contains is running another 'command' as an argument to P4Util, for example, if you had this hypothetical example:

>	P4Util command="something" stageone stagetwo command="otherthing" stagethree

The output of 'P4 something' would be passed into stageone and stagetwo AND into command="otherthing" but at that point, the output from 'P4 something' is replaced with the output of 'P4 otherthing' as the input to stagethree.  Only stageone and stagetwo have access to the output of 'P4 something'. This way you can use the output of a P4 command as the arguments into another P4 command.  In most cases, you would want to use regex to search for some substring of the output of a P4 command and then use those substrings as arguments into some other P4 command.  For example, you may want to get a list of users and then for each user, find the workspaces they have on the server and output the username followed by the name of the workspace.  Or you may just want to count how may workspaces each user has	and output the username followed by the number of workspaces, etc.


Stage commands:

The following commands are used for the stage' mentioned above.  These command will execute and, if they do not fail, the command for the next stage will be executed.  An example of a command failing would be if you were using the 'regex' command and that returned no matches, then it is treated as a failure.

>	command= 	- Pass a command to P4 and process the resulting output as a single string (all of the output is in one string of text).
>
>	commandl=	- Pass a command to P4 and process each line of the output as individual strings one after the other.
>
>	regex=		- Take the stage input and run a Regular Expression search on the input string using the specific regex string.
>
>	regexm=		- Take the stage input and run a Regular Expression search on the input string using "Multiline Mode".
>
>	Multiline mode for regexm will treat each line of text in the input string as a single line.  This allows '^' (beginning of line) and '$' (end of line) to work properly when searching for text in the input.  Multiline mode is usually only necessary if you use the 'command=' stage format to execute a Perforce command and store all of the output of that command in a single text string.  When doing a regular expression search in that text string, you usually want to look for beginning and end of line markers to search for specific text.
>
>	replace=	- Replace one string in the input to the stage with a different string (this modifies the input to the next stage).
>
>	output		- Take the stage input string and output that to standard out.
>
>	output=		- Output a formatted string to standard out.  This allows you to output matches from previous regex stages.  See "Output" below.
>
>	outputf=	- Output the stage input to a file by specifying a filename (like outputf="D:\Perforce\output.txt")
>
>	count		- Take the stage input and count the number of non-blank lines and store that count.
>
>	greaterthan= - Compare two integers or two strings and continue to the next stage if the first is greater than the second.
>
>	lessthan=	- Compare two integers or two strings and continue to the next stage if the first is less than the second.
>
>	(Note: You can do 'equals' by doing both greaterthan and lessthan, like X > 9, X < 11, for checking if X equals 10).


Text substitution in arguments:

For things like "command=", or "output=", it is useful to be able to substitute control strings with other text. For example, you may want to run the Perforce command for getting a workspace and specify a user or workspace name	at runtime.  

>	%i		- replace "%i" in the stage argument with the current 'input' string.

>	%c		- replace "%c" in the stage argument with the integer result of the previous 'count' stage.

>	%rN_M	- replace "%rN_M" in the stage argument with the regular expression capture group from regex 'N', group 'M'
	(where N and M are integer values 1 or greater).

>	%mN		- replace "%mN" in the stage argument with the number of capture groups from regex 'N'
				(where N is an integer value 1 or greater).

For example, if you have the following P4Util arguments:

>	commandl="streams" output="Perforce Stream=%i"

P4Util might output something like this:

	Perforce Stream=Stream //streams/MyStream mainline none 'MyStream'
	Perforce Stream=Stream //streams/TestStream mainline none 'TestStream'
	Perforce Stream=Stream //streams/UtilityStream mainline none 'UtilityStream'

The commandl will run "p4 streams" and will pass the output, line by line, on to the output command as input. The output command will replace '%i' with the input passed to it before outputting the text.

You can save the matches from a Regular Expression by capturing the matches using '(' and ')' in the regular expression and these will be saved on a 'stack' in P4Util so that you can reference matches from previous regular expressions that appeared earlier in the sequence of commands, for example:

The following will run "p4 streams"  and will use regular expressions to capture the stream specification, the type, the parent stream, and the stream name so that they can be used in subsequent stages.  Here, we select just the 4th group captured (the stream name) and the 1st group captured (the stream specification) and output those.

>	commandl="streams" regex="^Stream (.*?) (.*?) (.*?) '(.*?)'$" output="%r1_4 %r1_1"

Whenever you have a regex or regexm stage command, you must include the '(' and ')' somewhere in that regular expression to capture text even if you don't plan to use that text later.  This is due to the way the regular expression results are saved as each stage executes and to allow this list of saved regular expression results to "unwind" properly as a stage returns back to the previous stage.

See this Microsoft webpage for details of Regular Expression handling:
	https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference

For Regular Expression Multiline Mode, see the following Microsoft webpage:
	https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-options


See this following webpage about Perforce time/date format:

https://portal.perforce.com/s/article/2964

Examples:

Runs "p4 workspaces" and outputs the results, same as running "p4 workspaces" from the commandline:

>	command="workspaces" output

Runs "p4 streams" and outputs the results, same as running "p4 streams" from the commandline:

>	command="streams" output

Runs "p4 streams" and outputs the results line by line, same as running "p4 streams" from the commandline:

>	commandl="streams" output

Runs "p4 users" and counts the number of non-blank lines in the output giving the total number of users on the Perforce server:

>	command="users" count output="Total users: %c"

Runs "p4 streams" and outputs stream name and depot path (stream specification) then sorts that list (by stream name):

>	commandl="streams" regex="^Stream (.*?) (.*?) (.*?) '(.*?)'$" output="%r1_4 %r1_1" | sort

Runs "p4 streams" and gets the stream specification from regex and then runs "p4 stream -o <stream_spec_here>" and then looks at that output for "Options:" that contains "locked" and outputs the name of the stream and the depot path showing you only the streams that are locked by some owner:

(Note that here we are using the second regex to check for the string "locked" but even though we don't need the match from	this regular expression, we still need to capture something with '(' and ')' for P4Util to handle regular expression stack properly.)

>	commandl="streams" regex="^Stream (.*?) (.*?) (.*?) '(.*?)'$" command="stream -o %r1_1" regexm="^Options:.+ (locked) " output="%r1_4 %r1_1"

Runs "p4 workspace -o" on a workspace (Workspace) and replaces 'submitunchanged' with 'revertunchanged' in the SubmitOptions and then
	writes that modified workspace back to the Perforce server:

>	command="workspace -o JunkWork" replace="submitunchanged,revertunchanged" command="workspace -i < %i" output

Go through all workspaces for the specified user and output the workspace specification to a file in D:\Perforce named "workspace_<workspace_name>.txt":

>	commandl="workspaces -u someuser" regex="^Client (.*?) (.*?) (.*?) (.*?) '(.*?)'$" command="workspace -o %r1_1" outputf="D:\Perforce\Workspace_%r1_1.txt" output="%r1_1"

Go through all users and count the number of workspaces they have:

>	commandl="users" regex="^(.*?) .+$" command="workspaces -a -u %r1_1" count output="User: %r1_1 has %c workspaces"

Go through all users and count the number of workspaces they have and if they have 10 or more workspaces, output the username and count:

>	commandl="users" regex="^(.*?) .+$" command="workspaces -a -u %r1_1" count greaterthan="%c,9" output="User: %r1_1 has %c workspaces"

Go through the lines of a stream spec and count the number of lines that start with "# ":

>	command="stream -o //streams/P4Util" regexm="^\# (.+)$" output="%m1"


The following demonstrates using "OR" in a regular expression to capture groups that may occur in different lines of a multiline input.

Note that the order that the captured groups appear in is the order they appear in within the multiline input and NOT the order they appear in within the regular expression string.  For example if you use "p4 stream <some_stream_spec>" and look at the output, "Update" appears before "Owner" and "Owner" appears before "Type".  Outputs a stream spec and extracts 3 fields (Update timestamp, Owner name, Stream type:

>	command="stream -o //streams/P4Util" regexm="(^Owner:|^Update:|^Type:)[ \t]+(.+$)" output="%r1_1 %r1_2, %r1_3 %r1_4, %r1_5 %r1_6"
