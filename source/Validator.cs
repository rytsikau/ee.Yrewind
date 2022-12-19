using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace yrewind
{
    // Parsing and validating command line input
    class Validator
    {
        // Requested parameters
        public static string Url { get; private set; }           // -url
        public static string Start { get; private set; }         // -start
        public static string Duration { get; private set; }      // -duration
        public static string Resolution { get; private set; }    // -resolution
        public static string Ffmpeg { get; private set; }        // -ffmpeg
        public static string Output { get; private set; }        // -output
        public static string Browser { get; private set; }       // -browser
        public static string Cookie { get; private set; }        // -cookie
        public static bool KeepStreamInfo { get; private set; }  // -keepstreaminfo
        public static bool Log { get; private set; }             // -log
        public static string ExecuteOnExit { get; private set; } // -executeonexit

        // Other variables to determine
        public static string OutputDir { get; private set; }     // folder part of '-output'
        public static string OutputName { get; private set; }    // name part of '-output'
        public static string OutputExt { get; private set; }     // extension part of '-output'
        public static string CookieContent { get; private set; } // cookie data

        #region ParseArgsLine - Get 'key=value' pairs from args line
        public int ParseArgsLine(string argsLine)
        {
            Dictionary<string, string> args;

            // Unify quotes
            argsLine = argsLine.Replace("'", "\"");

            // Because value can contain a hyphen, it shouldn't be confused with an argument hyphen,
            // so replace the argument hyphen with TAB character
            // Also make the arguments case insensitive and replace one-character aliases
            var ic = RegexOptions.IgnoreCase;

            argsLine = Regex.Replace(argsLine, " -u=", " \turl=", ic);
            argsLine = Regex.Replace(argsLine, " -url=", " \turl=", ic);

            argsLine = Regex.Replace(argsLine, " -s=", " \tstart=", ic);
            argsLine = Regex.Replace(argsLine, " -start=", " \tstart=", ic);

            argsLine = Regex.Replace(argsLine, " -d=", " \tduration=", ic);
            argsLine = Regex.Replace(argsLine, " -duration=", " \tduration=", ic);

            argsLine = Regex.Replace(argsLine, " -r=", " \tresolution=", ic);
            argsLine = Regex.Replace(argsLine, " -resolution=", " \tresolution=", ic);

            argsLine = Regex.Replace(argsLine, " -f=", " \tffmpeg=", ic);
            argsLine = Regex.Replace(argsLine, " -ffmpeg=", " \tffmpeg=", ic);

            argsLine = Regex.Replace(argsLine, " -o=", " \toutput=", ic);
            argsLine = Regex.Replace(argsLine, " -output=", " \toutput=", ic);

            argsLine = Regex.Replace(argsLine, " -b=", " \tbrowser=", ic);
            argsLine = Regex.Replace(argsLine, " -browser=", " \tbrowser=", ic);

            argsLine = Regex.Replace(argsLine, " -c=", " \tcookie=", ic);
            argsLine = Regex.Replace(argsLine, " -cookie=", " \tcookie=", ic);

            argsLine = Regex.Replace(argsLine, " -k=", " \tkeepstreaminfo=", ic);
            argsLine = Regex.Replace(argsLine, " -keepstreaminfo=", " \tkeepstreaminfo=", ic);

            argsLine = Regex.Replace(argsLine, " -l=", " \tlog=", ic);
            argsLine = Regex.Replace(argsLine, " -log=", " \tlog=", ic);

            argsLine = Regex.Replace(argsLine, " -e=", " \texecuteonexit=", ic);
            argsLine = Regex.Replace(argsLine, " -executeonexit=", " \texecuteonexit=", ic);

            try
            {
                var r = new Regex("\\s\\\t(?<key>\\w+)=(\"(?<value>[^\"]+)\"|(?<value>[^\\s\\\t]+))*");

                args = r.Matches(argsLine).Cast<Match>().ToDictionary(
                    m => m.Groups["key"].Value,
                    m => m.Groups["value"].Value);
            }
            catch
            {
                // "Check for duplicate arguments on command line input"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9110;
            }

            if (args.Count == 0)
            {
                // "Check command line input"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9112;
            }

            return ParsePairs(args);
        }
        #endregion

        #region ParsePairs - Parse pairs
        int ParsePairs(Dictionary<string, string> args)
        {
            int code = 0;
            KeepStreamInfo = true;

            // If argument is present in command line input
            foreach (var arg in args.Keys)
            {
                switch (arg)
                {
                    case "url": code = Parse_url(args[arg]); break;
                    case "start": code = Parse_start(args[arg]); break;
                    case "duration": code = Parse_duration(args[arg]); break;
                    case "resolution": code = Parse_resolution(args[arg]); break;
                    case "ffmpeg": code = Parse_ffmpeg(args[arg]); break;
                    case "output": code = Parse_output(args[arg]); break;
                    case "browser": code = Parse_browser(args[arg]); break;
                    case "cookie": code = Parse_cookie(args[arg]); break;
                    case "keepstreaminfo": code = Parse_keepstreaminfo(args[arg]); break;
                    case "log": code = Parse_log(args[arg]); break;
                    case "executeonexit": code = Parse_executeonexit(args[arg]); break;
                }

                if (code != 0) return code;
            }

            // If argument was missing in command line input
            if (Url == default)
            {
                // "Required argument '-url' not found"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9113;
            }
            if (Start == default)
            {
                Start = string.Empty;
            }
            if (Duration == default)
            {
                Duration = Constants.DurationDefault.ToString();
            }
            if (Resolution == default)
            {
                Resolution = Constants.ResolutionDefault.ToString();
            }
            if (Ffmpeg == default)
            {
                // Firstly try FFmpeg located in the program folder,
                // then user- and system environment variables
                Ffmpeg = AppDomain.CurrentDomain.BaseDirectory + "ffmpeg.exe";
                if (!File.Exists(Ffmpeg))
                {
                    Ffmpeg = Environment
                        .GetEnvironmentVariable("ffmpeg", EnvironmentVariableTarget.User);
                }
                if (!File.Exists(Ffmpeg))
                {
                    Ffmpeg = Environment
                        .GetEnvironmentVariable("ffmpeg", EnvironmentVariableTarget.Machine);
                }
                if (!File.Exists(Ffmpeg))
                {
                    // "Check '-ffmpeg' argument"
                    Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                    return 9118;
                }
            }
            if (Output == default)
            {
                // Use default subfolder in batch file folder
                Output = Directory.GetCurrentDirectory() + "\\" + Constants.OutputDirDefault + "\\";
                code = Parse_output(Output);
                if (code != 0) return code;
            }
            if (Browser == default)
            {
                Browser = Constants.BrowserDefault;
            }
            if (Browser.ToLower() == "false")
            {
                Browser = default;
            }
            if (Cookie == default)
            {
                // Ignore
            }
            if (KeepStreamInfo == default)
            {
                // Ignore
            }
            if (Log == default)
            {
                // Ignore
            }
            if (ExecuteOnExit == default)
            {
                // Ignore
            }

            if (Validator.Log)
            {
                var logInfo =
                    "\nUrl: " + Url +
                    "\nStart: " + Start +
                    "\nDuration: " + Duration +
                    "\nResolution: " + Resolution +
                    "\nFfmpeg: " + Ffmpeg +
                    "\nOutput: " + Output +
                    "\nBrowser: " + Browser +
                    "\nCookie: " + Cookie +
                    "\nKeepStreamInfo: " + KeepStreamInfo +
                    "\nLog: " + Log +
                    "\nExecuteOnExit: " + ExecuteOnExit +
                    "\nOutputDir: " + OutputDir +
                    "\nOutputName: " + OutputName +
                    "\nOutputExt: " + OutputExt +
                    "\nCookieString: " + CookieContent
                    ;
                Program.Log(logInfo);
            }

            return 0;
        }
        #endregion

        #region Parse_url - Parse '-url' argument
        int Parse_url(string argValue)
        {
            // Cast '-url' to one of the following:
            // "https://www.youtube.com/watch?v=[streamID]"
            // "https://www.youtube.com/channel/[channelID]"
            // "https://www.youtube.com/c/[channelTitle]"
            // "https://www.youtube.com/user/[authorName]"

            argValue = Regex.Replace(argValue, @"\s", "");
            argValue = argValue.Trim('"').Trim('/');

            if (argValue.Length == 11)
            {
                Url = "https://www.youtube.com/watch?v=" + argValue;
                return 0;
            }
            else if (argValue.Length == 24 & !argValue.Contains('.'))
            {
                Url = "https://www.youtube.com/channel/" + argValue;
                return 0;
            }
            else if (argValue.ToLower().Contains("youtube.com/channel/".ToLower()))
            {
                Url = "https://www.youtube.com/channel/" + argValue.Split('/').Last();
                return 0;
            }
            else if (argValue.ToLower().Contains("youtube.com/c/".ToLower()))
            {
                Url = "https://www.youtube.com/c/" + argValue.Split('/').Last();
                return 0;
            }
            else if (argValue.ToLower().Contains("youtube.com/user/".ToLower()))
            {
                Url = "https://www.youtube.com/user/" + argValue.Split('/').Last();
                return 0;
            }

            var match = Regex.Match(argValue,
                @"^.*(?:youtu.be\/|v\/|e\/|u\/\w+\/|embed\/|v=)([^#\&\?]*).*$"
                ).Groups[1].Value;

            if (match.Length == 11)
            {
                Url = "https://www.youtube.com/watch?v=" + match;
                return 0;
            }
            else
            {
                // "Check '-url' argument"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9114;
            }
        }
        #endregion

        #region Parse_start - Parse '-start' argument
        int Parse_start(string argValue)
        {
            // Cast '-start' to one of the following:
            // 'YYYYMMDD:hhmmss'
            // '-[minutesNumber]'
            // '+[minutesNumber]'
            // 'seq[seqNumber]' (internal using in *getnext* mode)

            argValue = Regex.Replace(argValue, @"\s", "");
            argValue = argValue.ToLower();

            if (argValue.StartsWith("y:"))
            {
                argValue = argValue
                    .Replace("y:", Program.Start.AddDays(-1).ToString("yyyyMMdd:"));
            }
            else if (argValue.StartsWith("t:"))
            {
                argValue = argValue
                    .Replace("t:", Program.Start.ToString("yyyyMMdd:"));
            }
            else if ((argValue == "beginning") || (argValue == "b"))
            {
                Start = Constants.StartBeginning;
                return 0;
            }
            else if (Regex.IsMatch(argValue, @"^\-\d{1,3}$"))
            {
                Start = argValue;
                return 0;
            }
            else if (Regex.IsMatch(argValue, @"^\+\d{1,3}$"))
            {
                Start = argValue;
                return 0;
            }
            else if (Regex.IsMatch(argValue, @"^seq\d{1,15}$"))
            {
                Start = argValue;
                return 0;
            }

            if (Regex.IsMatch(argValue, @"^\d{8}:\d{4}$"))
            {
                argValue += "00";
            }

            try
            {
                var start = DateTime.ParseExact(argValue, "yyyyMMdd:HHmmss", null);
                if ((start - Program.Start).TotalDays > 1) throw new Exception();

                Start = start.ToString("yyyyMMdd:HHmmss");
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                // "Check '-start' argument"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9115;
            }

            return 0;
        }
        #endregion

        #region Parse_duration - Parse '-duration' argument
        int Parse_duration(string argValue)
        {
            argValue = Regex.Replace(argValue, @"\s", "");

            if (argValue.ToLower().StartsWith("min"))
            {
                Duration = Constants.DurationMin.ToString();
                return 0;
            }
            else if (argValue.ToLower().StartsWith("max"))
            {
                Duration = Constants.DurationMax.ToString();
                return 0;
            }

            try
            {
                var duration = 0;

                if (Regex.IsMatch(argValue, @"^\d{1,3}$"))
                {
                    duration = int.Parse(argValue) * 60;
                }
                else if (Regex.IsMatch(argValue, @"^\d{1,3}\.\d{1,2}$"))
                {
                    var minutes = int.Parse(argValue.Split('.')[0]);
                    var seconds = int.Parse(argValue.Split('.')[1]);
                    if (seconds > 59) throw new Exception();
                    duration = minutes * 60 + seconds;
                }
                else
                {
                    throw new Exception();
                }

                Duration = duration.ToString();
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                // "Check '-duration' argument"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9116;
            }

            if ((int.Parse(Duration) < Constants.DurationMin) ||
                (int.Parse(Duration) > Constants.DurationMax))
            {
                // "Check '-duration' argument"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9116;
            }

            return 0;
        }
        #endregion

        #region Parse_resolution - Parse '-resolution' argument
        int Parse_resolution(string argValue)
        {
            argValue = Regex.Replace(argValue, @"\s", "");

            if (argValue.ToLower().StartsWith("min"))
                argValue = Constants.ResolutionMin.ToString();
            if (argValue.ToLower().StartsWith("max"))
                argValue = Constants.ResolutionMax.ToString();

            try
            {
                if (!Regex.IsMatch(argValue, @"^\d{1,4}$")) throw new Exception();
                var resolution = int.Parse(argValue);
                Resolution = resolution.ToString();
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                // "Check '-resolution' argument"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9117;
            }

            return 0;
        }
        #endregion

        #region Parse_ffmpeg - Parse '-ffmpeg' argument
        int Parse_ffmpeg(string argValue)
        {
            Ffmpeg = argValue.TrimEnd(' ');
            Ffmpeg = Ffmpeg.Replace("/", "\\");
            Ffmpeg = Environment.ExpandEnvironmentVariables(Ffmpeg);

            if (!Regex.IsMatch(Ffmpeg, @"^[A-Za-z]{1}:\\.*$"))
            {
                // Generate an absolute path relative to batch file
                Ffmpeg = Directory.GetCurrentDirectory() + "\\" + Ffmpeg.Trim('\\');
            }

            try
            {
                if (File.Exists(Ffmpeg))
                {
                    return 0;
                }
                else if (Directory.Exists(Ffmpeg))
                {
                    string[] files = Directory
                        .GetFiles(Ffmpeg, "ffmpeg.exe", SearchOption.AllDirectories);

                    if (files.Count() > 0)
                    {
                        Ffmpeg = files[0];
                        return 0;
                    }
                }
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);
            }

            // "Check '-ffmpeg' argument"
            Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
            return 9118;
        }
        #endregion

        #region Parse_output - Parse '-output' argument and split result
        int Parse_output(string argValue)
        {
            // Also determine
            // OutputDir ('x:\path\to\'), OutputName ('filename'), OutputExt ('.extension')

            Output = argValue.TrimEnd(' ');
            Output = Output.Replace("/", "\\");
            Output = Environment.ExpandEnvironmentVariables(Output);

            OutputName = Output;
            OutputDir = string.Empty;
            OutputExt = string.Empty;

            // Split into folder, filename and extension parts
            if (OutputName.Contains("\\"))
            {
                OutputName = Output.Substring(Output.LastIndexOf("\\") + 1);
                OutputDir = Output.Substring(0, Output.LastIndexOf("\\") + 1);
            }
            if (OutputName.Contains("."))
            {
                OutputExt = OutputName.Substring(OutputName.LastIndexOf("."));
                OutputName = OutputName.Substring(0, OutputName.LastIndexOf("."));
            }

            // Folder
            if (OutputDir.Length == 0)
            {
                OutputDir = Constants.OutputDirDefault;
            }
            if (!Regex.IsMatch(OutputDir, @"^[A-Za-z]{1}:\\.*$"))
            {
                // Generate an absolute path relative to batch file
                OutputDir = Directory.GetCurrentDirectory() + "\\" + OutputDir + "\\";
            }

            // Extension
            if (OutputExt.Length == 0)
            {
                OutputExt = Constants.OutputExtDefault;
            }
            else
            {
                OutputExt = OutputExt.ToLower();
                if (!Regex.IsMatch(OutputExt, @"^(\.[a-z0-9]{1,9})$"))
                {
                    // "Check '-output' argument"
                    Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                    return 9119;
                }
            }

            // Check if path values are correct
            try
            {
                var test =
                    Path.GetFullPath(OutputDir.Replace('*', '_') +
                    OutputName.Replace('*', '_') +
                    OutputExt.Replace('*', '_'));
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                // "Check '-output' argument"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9119;
            }

            return 0;
        }
        #endregion

        #region Parse_browser - Parse '-browser' argument
        int Parse_browser(string argValue)
        {
            Browser = argValue.TrimEnd(' ');
            Browser = Browser.Replace("/", "\\");
            Browser = Environment.ExpandEnvironmentVariables(Browser);

            if (Browser.EndsWith(".exe", true, null) && File.Exists(Browser))
            {
                return 0;
            }
            else if (!Browser.Contains("\\"))
            {
                return 0;
            }
            else
            {
                // "Check '-browser' argument"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9120;
            }
        }
        #endregion

        #region Parse_cookie - Parse '-cookie' argument
        int Parse_cookie(string argValue)
        {
            // Also determine CookieString

            Cookie = argValue.TrimEnd(' ');
            Cookie = Cookie.Replace("/", "\\");
            Cookie = Environment.ExpandEnvironmentVariables(Cookie);

            // Generate an absolute path relative to batch file
            if (!Regex.IsMatch(Cookie, @"^[A-Za-z]{1}:\\.*$"))
            {
                Cookie = Directory.GetCurrentDirectory() + "\\" + Cookie.Trim('\\');
            }

            if (!File.Exists(Cookie))
            {
                // "Check '-cookie' argument"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9121;
            }

            try
            {
                long cookieFileLength = new FileInfo(Cookie).Length;
                if (cookieFileLength > 1000000) throw new Exception();

                var cookieFileContent = File.ReadLines(Cookie);
                if (cookieFileContent.Count(line => !string.IsNullOrWhiteSpace(line)) == 1)
                {
                    // File contains one non-empty line
                    CookieContent = cookieFileContent.First().Trim().Trim('\'').Trim('\"');
                }
                else
                {
                    // Netscape cookie format
                    foreach (var line in cookieFileContent)
                    {
                        if (line.Contains("\t"))
                        {
                            CookieContent += line.Split('\t')[5] + "=" + line.Split('\t')[6] + "; ";
                        }
                    }
                    CookieContent = CookieContent.Substring(0, CookieContent.Length - 2);
                }
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                // "Cannot read cookie file"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9111;
            }

            return 0;
        }
        #endregion

        #region Parse_keepstreaminfo - Parse '-Parse_keepstreaminfo' argument
        int Parse_keepstreaminfo(string argValue)
        {
            argValue = Regex.Replace(argValue, @"\s", "");
            argValue = argValue.ToLower();

            if (argValue == "false")
            {
                KeepStreamInfo = false;
            }

            return 0;
        }
        #endregion

        #region Parse_log - Parse '-log' argument
        int Parse_log(string argValue)
        {
            argValue = Regex.Replace(argValue, @"\s", "");
            argValue = argValue.ToLower();

            if (argValue == "true")
            {
                Log = true;
            }

            return 0;
        }
        #endregion

        #region Parse_executeonexit - Parse '-executeonexit' argument
        int Parse_executeonexit(string argValue)
        {
            ExecuteOnExit = argValue.TrimEnd(' ');
            ExecuteOnExit = ExecuteOnExit.Replace("/", "\\");
            ExecuteOnExit = Environment.ExpandEnvironmentVariables(ExecuteOnExit);

            return 0;
        }
        #endregion
    }
}