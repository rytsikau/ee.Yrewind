using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace yrewind
{
    // Class for parsing and checking command line input
    class CLInput
    {
        // Requested parameters
        public static string Url { get; set; }                   // -url
        public static DateTime Start { get; private set; }       // -start
        public static int Duration { get; private set; }         // -duration
        public static int Resolution { get; private set; }       // -resolution
        public static string Ffmpeg { get; private set; }        // -ffmpeg
        public static string Output { get; private set; }        // -output
        public static string Cookie { get; private set; }        // -cookie
        public static string ExecuteOnExit { get; private set; } // -executeonexit
        public static bool Browser { get; private set; }         // -browser
        public static bool Log { get; private set; }             // -log

        // Other variables to determine
        public static bool StartBeginning { get; private set; } // flag '-start=beginning'
        public static bool StartWait { get; private set; }      // flag '-start=wait'
        public static int? StartSequence { get; private set; }  // case '-start=[sequence]'
        public static string FfmpegName { get; private set; }   // name.ext part of '-ffmpeg'
        public static string OutputDir { get; private set; }    // folder part of '-output'
        public static string OutputName { get; private set; }   // name part of '-output'
        public static string OutputExt { get; private set; }    // .ext part of '-output'
        public static string CookieString { get; private set; } // ready to use cookie
        public static string LogDir { get; private set; }       // folder to save logs

        #region ParseArgsLine - Get 'key=value' pairs from args line
        public int ParseArgsLine(string argsLine)
        {
            Dictionary<string, string> args = default;

            // Replace asterisks (as part of wildcards) and unify quotes
            argsLine = argsLine.Replace("*", "~~~asterisk~~~").Replace("'", "\"");

            // Since value can contain a hyphen, it should not be confused with argument hyphen
            // So make replace the argument hyphen with an asterisk (which isn't possible here)
            // Also make the arguments case insensitive and replace one-character aliases
            var ic = RegexOptions.IgnoreCase;

            argsLine = Regex.Replace(argsLine, " -u=", " *url=", ic);
            argsLine = Regex.Replace(argsLine, " -url=", " *url=", ic);

            argsLine = Regex.Replace(argsLine, " -s=", " *start=", ic);
            argsLine = Regex.Replace(argsLine, " -start=", " *start=", ic);

            argsLine = Regex.Replace(argsLine, " -d=", " *duration=", ic);
            argsLine = Regex.Replace(argsLine, " -duration=", " *duration=", ic);

            argsLine = Regex.Replace(argsLine, " -r=", " *resolution=", ic);
            argsLine = Regex.Replace(argsLine, " -resolution=", " *resolution=", ic);

            argsLine = Regex.Replace(argsLine, " -f=", " *ffmpeg=", ic);
            argsLine = Regex.Replace(argsLine, " -ffmpeg=", " *ffmpeg=", ic);

            argsLine = Regex.Replace(argsLine, " -o=", " *output=", ic);
            argsLine = Regex.Replace(argsLine, " -output=", " *output=", ic);

            argsLine = Regex.Replace(argsLine, " -c=", " *cookie=", ic);
            argsLine = Regex.Replace(argsLine, " -cookie=", " *cookie=", ic);

            argsLine = Regex.Replace(argsLine, " -e=", " *executeonexit=", ic);
            argsLine = Regex.Replace(argsLine, " -executeonexit=", " *executeonexit=", ic);

            argsLine = Regex.Replace(argsLine, " -b=", " *browser=", ic);
            argsLine = Regex.Replace(argsLine, " -browser=", " *browser=", ic);

            argsLine = Regex.Replace(argsLine, " -l=", " *log=", ic);
            argsLine = Regex.Replace(argsLine, " -log=", " *log=", ic);

            try
            {
                var r = new Regex("\\*(?<key>\\w+)=(\"(?<value>[^\"]+)\"|(?<value>[^\\*]+))\\s*");
                args = r.Matches(argsLine).Cast<Match>().ToDictionary(
                    m => m.Groups["key"].Value,
                    m => m.Groups["value"].Value);
            }
            catch
            {
                return 9113; // "Check for duplicate arguments on command line input"
            }

            if (args.Count == 0)
            {
                return 9119; // "Unknown command line input format"
            }

            return ParsePairs(args);
        }
        #endregion

        #region ParsePairs - Parse pairs
        int ParsePairs(Dictionary<string, string> args)
        {
            int code;

            // If argument(s) are present in command line input
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
                    case "cookie": code = Parse_cookie(args[arg]); break;
                    case "executeonexit": code = Parse_executeonexit(args[arg]); break;
                    case "browser": code = Parse_browser(args[arg]); break;
                    case "log": code = Parse_log(args[arg]); break;

                    // "Command line input contains unknown argument(s)"
                    default: code = 9120; break;
                }

                if (code != 0) return code;
            }

            // If argument(s) was missing in command line input
            if (Url == default) return 9121; // "Required argument '-url' not found"
            if (Start == default) Start = Program.Start;
            if (Duration == default) Duration = Constants.DurationDefault;
            if (Resolution == default) Resolution = Constants.ResolutionDefault;
            if (Ffmpeg == default)
            {
                // Use FFmpeg located in the program folder
                Ffmpeg = AppDomain.CurrentDomain.BaseDirectory + "ffmpeg.exe";
                if (!File.Exists(Ffmpeg)) return 9122; // "FFmpeg not found"
            }
            if (Output == default)
            {
                // Use default subfolder in batch file folder
                Output = Directory.GetCurrentDirectory() + "\\" + Constants.OutputDirDefault + "\\";
                code = Parse_output(Output);
                if (code != 0) return code;
            }
            if (Cookie == default) { } // ignore
            if (ExecuteOnExit == default) { } // ignore
            if (Log == default) { } // ignore

            // if (Browser == default) { } // ignore
            // As UseInfopage way dont works for now, use UseBrowser by default
            Browser = true;

            return 0;
        }
        #endregion

        #region Parse_url - Parse '-url' argument
        int Parse_url(string argValue)
        {
            // Cast the URL to one of the following:
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
                return 9110; // "Check '-url' argument"
            }
        }
        #endregion

        #region Parse_start - Parse '-start' argument
        int Parse_start(string argValue)
        {
            // Also determine StartBeginning, StartWait and StartSequence

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
                StartBeginning = true;
                argValue = "20000101:000000"; // Will be clarified in Preparer class
            }
            else if ((argValue == "wait") || (argValue == "w"))
            {
                StartWait = true;
                argValue = "20000101:000000"; // Will be clarified in Preparer class
            }

            try
            {
                if (Regex.IsMatch(argValue, @"^\d{8}:\d{4}$"))
                {
                    Start = DateTime.ParseExact(argValue, "yyyyMMdd:HHmm", null);
                }
                else if (Regex.IsMatch(argValue, @"^\d{8}:\d{6}$"))
                {
                    Start = DateTime.ParseExact(argValue, "yyyyMMdd:HHmmss", null);
                }
                else if (argValue.StartsWith("-"))
                {
                    argValue = argValue.Replace("-", "");
                    var tmp1 = short.Parse(argValue);
                    Start = Program.Start.AddMinutes(-tmp1);
                }
                else if (argValue.StartsWith("+"))
                {
                    argValue = argValue.Replace("+", "");
                    var tmp1 = short.Parse(argValue);
                    Start = Program.Start.AddMinutes(tmp1);
                }
                else if (argValue.StartsWith("seq"))
                {
                    argValue = argValue.Replace("seq", "");
                    StartSequence = int.Parse(argValue);
                    argValue = "20000101:000000"; // Will be clarified in Preparer class
                }
                else
                {
                    throw new Exception();
                }
            }
            catch
            {
                return 9116; // "Check '-start' argument"
            }

            return 0;
        }
        #endregion

        #region Parse_duration - Parse '-duration' argument
        int Parse_duration(string argValue)
        {
            argValue = Regex.Replace(argValue, @"\s", "");

            int tmp1;
            try
            {
                tmp1 = int.Parse(argValue);
            }
            catch
            {
                return 9114; // "Check '-duration' argument"
            }

            if ((tmp1 >= Constants.DurationMin) & (tmp1 <= Constants.DurationMax))
            {
                Duration = tmp1;
            }
            else
            {
                return 9115; // "Value of '-duration' argument is out of range"
            }

            return 0;
        }
        #endregion

        #region Parse_resolution - Parse '-resolution' argument
        int Parse_resolution(string argValue)
        {
            argValue = Regex.Replace(argValue, @"\s", "");

            int tmp1;
            try
            {
                tmp1 = int.Parse(argValue);
            }
            catch
            {
                return 9111; // "Check '-resolution' argument"
            }

            if ((tmp1 >= Constants.ResolutionMin) & (tmp1 <= Constants.ResolutionMax))
            {
                Resolution = tmp1;
            }
            else
            {
                return 9112; // "Value of '-resolution' argument is out of range"
            }

            return 0;
        }
        #endregion

        #region Parse_ffmpeg - Parse '-ffmpeg' argument
        int Parse_ffmpeg(string argValue)
        {
            // Also determine FfmpegName

            Ffmpeg = argValue.TrimEnd(' ');

            if (!Regex.IsMatch(Ffmpeg, @"^[A-Za-z]{1}:\\.*$"))
            {
                // Generate an absolute path relative to batch file
                Ffmpeg = Directory.GetCurrentDirectory() + "\\" + Ffmpeg.Trim('\\');
            }

            if (Ffmpeg.EndsWith("ffmpeg.exe", true, null) & File.Exists(Ffmpeg))
            {
                FfmpegName = Path.GetFileName(Ffmpeg);
                return 0;
            }
            else if (Directory.Exists(Ffmpeg))
            {
                var files = Directory
                    .GetFiles(Ffmpeg, "ffmpeg.exe", SearchOption.AllDirectories);
                if (files.Count() > 0)
                {
                    Ffmpeg = files[0];
                    FfmpegName = Path.GetFileName(Ffmpeg);
                    return 0;
                }
            }

            return 9117; // "Check '-ffmpeg' argument"
        }
        #endregion

        #region Parse_output - Parse '-output' argument and split result
        int Parse_output(string argValue)
        {
            // Also determine
            // OutputDir ('x:\path\to\'), OutputName ('filename') and OutputExt ('.extension')

            Output = argValue.TrimEnd(' ');
            Output = Output.Replace("/", "\\");

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

            // Prepare folder
            if (OutputDir.Length == 0)
            {
                OutputDir = Constants.OutputDirDefault;
            }
            if (!Regex.IsMatch(OutputDir, @"^[A-Za-z]{1}:\\.*$"))
            {
                // Generate an absolute path relative to batch file
                OutputDir = Directory.GetCurrentDirectory() + "\\" + OutputDir + "\\";
            }
            try
            {
                OutputDir = Path.GetFullPath(OutputDir);
            }
            catch
            {
                return 9123; // "Check '-output' argument"
            }

            // Prepare filename
            if (OutputName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return 9124; // "Check '-output' argument"
            }

            // Prepare extension
            if (OutputExt.Length == 0)
            {
                OutputExt = Constants.OutputExtDefault;
            }
            else
            {
                OutputExt = OutputExt.ToLower();
                if (!Regex.IsMatch(OutputExt, @"^(\.[a-z0-9]{1,9})$"))
                {
                    return 9125; // "Check '-output' argument"
                }
            }

            return 0;
        }
        #endregion

        #region Parse_cookie - Parse '-cookie' argument
        int Parse_cookie(string argValue)
        {
            // Also determine CookieString

            Cookie = argValue.TrimEnd(' ');
            Cookie = Cookie.Replace("/", "\\");

            // Generate an absolute path relative to batch file
            if (!Regex.IsMatch(Cookie, @"^[A-Za-z]{1}:\\.*$"))
            {
                Cookie = Directory.GetCurrentDirectory() + "\\" + Cookie.Trim('\\');
            }

            if (!File.Exists(Cookie))
            {
                return 9126; // "Check '-cookie' argument"
            }

            try
            {
                long cookieFileLength = new FileInfo(Cookie).Length;
                if (cookieFileLength > Constants.CookieFileLengthMax) throw new Exception();

                var cookieFileContent = File.ReadLines(Cookie);
                if (cookieFileContent.Count(line => !string.IsNullOrWhiteSpace(line)) == 1)
                {
                    // If file contains one non-empty line
                    CookieString = cookieFileContent.First().Trim().Trim('\'').Trim('\"');
                }
                else
                {
                    // Otherwise assume it's a Netscape cookie format
                    foreach (var line in cookieFileContent)
                    {
                        if (line.Contains("\t"))
                        {
                            CookieString += line.Split('\t')[5] + "=" + line.Split('\t')[6] + "; ";
                        }
                    }
                    CookieString = CookieString.Substring(0, CookieString.Length - 2);
                }
            }
            catch
            {
                return 9118; // "Cannot read cookie file"
            }

            return 0;
        }
        #endregion

        #region Parse_executeonexit - Parse '-executeonexit' argument
        int Parse_executeonexit(string argValue)
        {
            ExecuteOnExit = argValue.TrimEnd(' ');

            return 0;
        }
        #endregion

        #region Parse_browser - Parse '-browser' argument
        int Parse_browser(string argValue)
        {
            argValue = Regex.Replace(argValue, @"\s", "");

            if (argValue == "test200611") Browser = true;
            else return 9120; // "Command line input contains unknown argument(s)"

            return 0;
        }
        #endregion

        #region Parse_log - Parse '-log' argument
        int Parse_log(string argValue)
        {
            // Also determine LogDir

            argValue = Regex.Replace(argValue, @"\s", "");
            if (argValue == "test200611")
            {
                Log = true;

                LogDir = Directory.GetCurrentDirectory() + "\\" +
                    Program.Start.ToString("yyyyMMdd-HHmmss") + "\\";

                try { Directory.CreateDirectory(LogDir); }
                catch { }
            }
            else
            {
                return 9120; // "Command line input contains unknown argument(s)"
            }

            return 0;
        }
        #endregion
    }
}