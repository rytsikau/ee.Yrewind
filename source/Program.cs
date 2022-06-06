using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace yrewind
{
    // Main class
    static class Program
    {
        // Exact time when program started (local time zone) and program timer
        public static DateTime Start { get; private set; }
        public static Stopwatch Timer = new Stopwatch();

        // Output paths
        public static string OutputDir { get; private set; }
        public static string OutputName { get; private set; }
        public static string OutputExt { get; private set; }

        // TRUE if:
        // FFmpeg process exited correctly AND
        // duration of the saved video is verifiable AND
        // duration of the saved video is as requested AND
        // the start time point is determined accurately
        // (using UTC tags in the stream technical info)
        public static bool? ResultIsOK;

        // Output folder for log files
        public static string LogDir { get; private set; }

        // Additional info about the error
        public static string ErrInfo;

        // Other variables
        static bool youTubeAvailable;
        static bool? isUpdateExists;
        static string updateDirectUrl;

        #region Main - Entry point of the program
        static void Main()
        {
            Start = DateTime.Now;
            Timer.Start();
            int code;

            // Console settings
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = Constants.Name;
            if (Console.CursorLeft != 0) Console.WriteLine();
            int consoleWidthInit = Console.WindowWidth;
            if (Console.WindowWidth < Constants.FfmpegConsoleWidthMin)
            {
                // To prevent FFmpeg from printing a new 'stats' line every second
                Console.SetWindowSize(Constants.FfmpegConsoleWidthMin, Console.WindowHeight);
            }

            // If the program was launched without args
            if (Environment.GetCommandLineArgs().Length == 1)
            {
                WithoutArgs();

                // Standard Windows code "Incorrect function"
                Exit(1);
            }

            // Determine exact current time and YouTube availability
            // (to get server response without bulk content just send wrong URL)
            Downloader(Constants.UrlMain + "~", 1, 1, out string resultMsg);
            if (resultMsg.Contains(" // "))
            {
                try
                {
                    var utcStr =
                        resultMsg.Split(new string[] { " // " }, StringSplitOptions.None)[0];
                    Start = DateTime.Parse(utcStr) - Program.Timer.Elapsed;
                    youTubeAvailable = true;
                }
                catch (Exception e)
                {
                    Program.ErrInfo =
                        new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                    if (Validator.Log) Program.Log(Program.ErrInfo);
                }
            }
            else
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + resultMsg;
                if (Validator.Log) Program.Log(Program.ErrInfo);
            }

            // "--> {0} (started at {1})"
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(Constants.Msg[9052], Constants.Name, Start.ToString());
            Console.ResetColor();

            // If the program should work
            if (!youTubeAvailable)
            {
                // "YouTube not responding"
                Exit(9085);
            }

            // Output folder for log files
            LogDir = Directory.GetCurrentDirectory() + "\\" +
                Start.ToString("yyyyMMdd-HHmmss") + "\\";

            // Parse the command line input
            var argsLine = Environment.CommandLine;
            if (argsLine.Length > Constants.CommandLineStringLengthLimit)
            {
                // "Command line input is too long"
                Exit(9059);
            }
            var validator = new Validator();
            code = validator.ParseArgsLine(argsLine);
            if (code != 0) Exit(code);

            // "Getting live stream info"
            Console.Write(Constants.Msg[9065].ToLower() + "...");

            // Wait for stream, get HTML JSON
            var waiter = new Waiter();
            code = waiter.Common();
            if (code != 0) Exit(code);

            // Get ID info and make various preparations and checks
            var preparer = new Preparer();
            while (true)
            {
                code = preparer.Common();
                if (code != 0) Exit(code);

                // If start point is not yet available
                var secondsToWait =
                    (Preparer.Start - (Program.Start + Program.Timer.Elapsed)).TotalSeconds +
                    Constants.RealTimeBufferSeconds;
                if (secondsToWait > 0) Program.CountdownTimer(secondsToWait);

                // If the waiting time was long, run Preparer again
                if (secondsToWait < 100) break;
            }

            // New line after "Getting live stream info" and possible сountdown timer
            Console.WriteLine();

            // "Stream title"
            Console.WriteLine("    {0,-20}{1}", Constants.Msg[9080].ToLower(), Preparer.Title);

            // "Resolutions"
            Console.WriteLine("    {0,-20}{1}", Constants.Msg[9076].ToLower(), Preparer.Resolutions);

            // Determine output directory
            OutputDir = ReplaceWildcards(Validator.OutputDir);

            // Determine output filename
            if (string.IsNullOrEmpty(Validator.OutputName))
            {
                // Set default like '9Auq9mYxFEE_20201231-073000_060m00s_1080p'
                var d = TimeSpan.FromSeconds(Preparer.Duration);
                OutputName =
                    Waiter.Id + "_" +
                    Preparer.Start.ToString("yyyyMMdd-HHmmss") + "_" +
                    d.TotalMinutes.ToString("000") + "m" + d.Seconds.ToString("00")+ "s" + "_" +
                    Preparer.Resolution.ToString("0000") + "p";
            }
            else
            {
                OutputName = ReplaceWildcards(Validator.OutputName);
            }

            // Determine output extension
            OutputExt = Validator.OutputExt;

            // "Output file"
            Console.WriteLine("    {0,-20}{1}", Constants.Msg[9067].ToLower(),
                OutputName + OutputExt);

            if (Validator.Log)
            {
                var logInfo =
                    "\nOutput folder: " + OutputDir +
                    "\nOutput file: " + OutputName + OutputExt
                    ;
                Program.Log(logInfo);
            }

            // Check if the output file already exists
            if (File.Exists(OutputDir + OutputName + OutputExt))
            {
                // "File already exists"
                Exit(9064);
            }

            // Download and save the output media
            var getsave = new Saver();
            code = getsave.Common();
            if (code != 0) Exit(code);

            // Show warning if result cannot be verified
            if (ResultIsOK != true)
            {
                // "Unable to verify the saved file is correct"
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(Constants.Msg[9082].ToLower());
                Console.ResetColor();
            }

            // Restore console window width
            if (Console.WindowWidth != consoleWidthInit)
            {
                Console.SetWindowSize(consoleWidthInit, Console.WindowHeight);
            }

            // Normal exit point
            Exit(0);
        }
        #endregion

        #region WithoutArgs - If the program started without arguments
        static void WithoutArgs()
        {
            Console.CursorVisible = false;
            ConsoleKey tmp1, tmp2, tmp3;

            while (true)
            {
                Console.Clear();
                Console.BufferHeight = Console.WindowHeight;

                // "{0} (version {1})"
                Console.WriteLine(Constants.Msg[9051], Constants.Name, Constants.Version);
                // "---[64]---"
                Console.WriteLine(Constants.Msg[9050]);
                // "Press <H> to show help"
                Console.WriteLine(Constants.Msg[9071]);
                // "Press <U> to get the latest version"
                Console.WriteLine(Constants.Msg[9072]);
                // "Press <Esc> to exit"
                Console.WriteLine(Constants.Msg[9070]);
                // "---[64]---"
                Console.WriteLine(Constants.Msg[9050]);

                tmp1 = Console.ReadKey(true).Key;
                if (tmp1 == ConsoleKey.Escape)
                {
                    // Exit

                    break;
                }
                else if (tmp1 == ConsoleKey.H)
                {
                    // Show Help

                    Console.Clear();
                    var BufferHeightExtra = 10;
                    foreach (var line in Constants.Help.Split('\n'))
                    {
                        if (line.Length > Console.WindowWidth)
                        {
                            BufferHeightExtra +=
                                (int)Math.Ceiling((double)line.Length / Console.WindowWidth) - 1;
                        }
                    }
                    Console.BufferHeight = Constants.Help.Split('\n').Length + BufferHeightExtra;

                    // "{0} (version {1})"
                    Console.WriteLine(Constants.Msg[9051], Constants.Name, Constants.Version);
                    // "---[64]---"
                    Console.WriteLine(Constants.Msg[9050]);
                    // "Use <Up>, <Down>, <PageUp>, <PageDown> to scroll"
                    Console.WriteLine(Constants.Msg[9083]);
                    // ""
                    Console.WriteLine();
                    // "Press <Esc> to exit"
                    Console.WriteLine(Constants.Msg[9070]);
                    // "---[64]---"
                    Console.WriteLine(Constants.Msg[9050]);
                    // "Please see the actual text on the program page"
                    Console.WriteLine("[" + Constants.Msg[9068] + "]");
                    // Show help content
                    Console.WriteLine(Constants.Help);

                    Console.SetWindowPosition(0, 0);

                    while (true)
                    {
                        tmp2 = Console.ReadKey(true).Key;
                        if (tmp2 == ConsoleKey.Escape) break;
                        else if (tmp2 == ConsoleKey.DownArrow)
                        {
                            if (Console.WindowTop + Console.WindowHeight < Console.BufferHeight)
                                Console.WindowTop += 1;
                        }
                        else if (tmp2 == ConsoleKey.UpArrow)
                        {
                            if (Console.WindowTop > 1)
                                Console.WindowTop -= 1;
                            else Console.WindowTop = 0;
                        }
                        else if (tmp2 == ConsoleKey.PageDown)
                        {
                            if (Console.WindowTop + 2 * Console.WindowHeight < Console.BufferHeight)
                                Console.WindowTop += Console.WindowHeight;
                            else Console.WindowTop = Console.BufferHeight - Console.WindowHeight;
                        }
                        else if (tmp2 == ConsoleKey.PageUp)
                        {
                            if (Console.WindowTop > Console.WindowHeight)
                                Console.WindowTop -= Console.WindowHeight;
                            else Console.WindowTop = 0;
                        }
                    }
                }
                else if (tmp1 == ConsoleKey.U)
                {
                    // Check for updates

                    Console.Clear();
                    Console.BufferHeight = Console.WindowHeight;

                    // "{0} (version {1})"
                    Console.WriteLine(Constants.Msg[9051], Constants.Name, Constants.Version);
                    // "---[64]---"
                    Console.WriteLine(Constants.Msg[9050]);
                    // "Press <D> to confirm download"
                    Console.WriteLine(Constants.Msg[9069]);
                    // ""
                    Console.WriteLine();
                    // "Press <Esc> to exit"
                    Console.WriteLine(Constants.Msg[9070]);
                    // "---[64]---"
                    Console.WriteLine(Constants.Msg[9050]);
                    // ""
                    Console.WriteLine();
                    // "Checking"
                    Console.Write(Constants.Msg[9058] + "... ");
                    var content = Downloader(Constants.UpdateInfoUrl, 3, 5, out _);
                    try
                    {
                        var updateDate = int.Parse(content.Split(';')[0].Trim());
                        if (Constants.BuildDate < updateDate) isUpdateExists = true;
                        else isUpdateExists = false;
                        updateDirectUrl = content.Split(';')[1].Trim();
                    }
                    catch (Exception e)
                    {
                        Program.ErrInfo =
                            new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                        if (Validator.Log) Program.Log(Program.ErrInfo);
                    }

                    if (!isUpdateExists.HasValue)
                    {
                        // "Unable to check for updates"
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(Constants.Msg[9081]);
                    }
                    else if ((bool)isUpdateExists)
                    {
                        // "New version available"
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(Constants.Msg[9066]);
                    }
                    else
                    {
                        // "Current version is actual"
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(Constants.Msg[9060]);
                    }
                    Console.ResetColor();

                    while (true)
                    {
                        tmp3 = Console.ReadKey(true).Key;
                        if (tmp3 == ConsoleKey.Escape) break;
                        else if (tmp3 == ConsoleKey.D)
                        {
                            // "Downloading, please wait a minute"
                            Console.Write(Constants.Msg[9062] + "... ");

                            var zipFullPath =
                                Environment.GetFolderPath(Environment.SpecialFolder.Desktop) +
                                "\\" +
                                Path.GetFileName(updateDirectUrl);

                            var zipFullPathTmp =
                                zipFullPath.Replace(".zip", "~" + Constants.RandomString + ".zip");

                            if (File.Exists(zipFullPath))
                            {
                                // "Skipped! File already exists"
                                Console.WriteLine(Constants.Msg[9079]);
                                //"See file '{0}' on the desktop"
                                Console.WriteLine("(" + Constants.Msg[9078].ToLower() + ")",
                                    Path.GetFileName(updateDirectUrl));

                                Console.ReadKey();
                                break;
                            }
                            else
                            {
                                try
                                {
                                    var uri = updateDirectUrl;
                                    using (var wc = new WebClient())
                                    {
                                        wc.DownloadFile(new Uri(uri), zipFullPathTmp);
                                    }

                                    File.Move(zipFullPathTmp, zipFullPath);

                                    // "Ready"
                                    Console.WriteLine(Constants.Msg[9073] + "!");
                                    //"See file '{0}' on the desktop"
                                    Console.WriteLine("(" + Constants.Msg[9078].ToLower() + ")",
                                        Path.GetFileName(updateDirectUrl));
                                }
                                catch (WebException e)
                                {
                                    Program.ErrInfo =
                                        new StackFrame(0, true).GetFileLineNumber() +
                                        " - " + e.Message;
                                    if (Validator.Log) Program.Log(Program.ErrInfo);

                                    // "Error"
                                    Console.WriteLine(Constants.Msg[9063] + "!");
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region CountdownTimer - Show countdown timer, then return
        public static void CountdownTimer(double seconds)
        {
            if (seconds < 0) return;

            // "Waiting"
            var consoleTitle = Console.Title;
            Console.Title = Constants.Name + " - " + Constants.Msg[9084].ToLower() + "...";

            Console.Write(" ");
            Console.CursorVisible = false;
            var dateTime = DateTime.MinValue.AddSeconds(seconds);

            while (dateTime > DateTime.MinValue)
            {
                Thread.Sleep(1000);
                try
                {
                    dateTime = dateTime.AddSeconds(-1);
                }
                catch
                {
                    break;
                }
                Console.Write(dateTime.ToString("HH:mm:ss"));
                Console.SetCursorPosition(Console.CursorLeft - 8, Console.CursorTop);
            }
            Console.CursorVisible = true;
            Console.Write("        ");
            Console.SetCursorPosition(Console.CursorLeft - 9, Console.CursorTop);
            Console.Title = consoleTitle;
        }
        #endregion

        #region ReplaceWildcards - Replace wildcards
        static string ReplaceWildcards(string value)
        {
            // This method does NOT check if replacement values ​​exist

            value = Regex.Replace(value, "~~~asterisk~~~id~~~asterisk~~~",
                Waiter.Id, RegexOptions.IgnoreCase);

            value = Regex.Replace(value, "~~~asterisk~~~start~~~asterisk~~~",
                Preparer.Start.ToString("yyyyMMdd-HHmmss"), RegexOptions.IgnoreCase);

            value = Regex.Replace(value, "~~~asterisk~~~start\\[(.+)\\]~~~asterisk~~~",
                m => Preparer.Start.ToString(m.Groups[1].Value), RegexOptions.IgnoreCase);

            var duration = TimeSpan.FromSeconds(Preparer.Duration);
            var durationStr = string
                .Format("{0:D3}m{1:D2}s", (int)duration.TotalMinutes, duration.Seconds);
            value = Regex.Replace(value, "~~~asterisk~~~duration~~~asterisk~~~",
                durationStr, RegexOptions.IgnoreCase);

            value = Regex.Replace(value, "~~~asterisk~~~resolution~~~asterisk~~~",
                Preparer.Resolution.ToString(), RegexOptions.IgnoreCase);

            value = Regex.Replace(value, "~~~asterisk~~~author~~~asterisk~~~",
                string.Join("_", Preparer.Author.Split(Path.GetInvalidFileNameChars())),
                RegexOptions.IgnoreCase);

            value = Regex.Replace(value, "~~~asterisk~~~title~~~asterisk~~~",
                string.Join("_", Preparer.Title.Split(Path.GetInvalidFileNameChars())),
                RegexOptions.IgnoreCase);

            value = Regex.Replace(value, "~~~asterisk~~~channel_id~~~asterisk~~~",
                Waiter.ChannelId, RegexOptions.IgnoreCase);

            value = Regex.Replace(value, "~~~asterisk~~~output~~~asterisk~~~",
                OutputDir + OutputName + Validator.OutputExt, RegexOptions.IgnoreCase);

            if (value.Contains("~~~asterisk~~~getnext~~~asterisk~~~".ToLower()))
            {
                var getnextSeqStart =
                    Preparer.StartSeq + Preparer.Duration / Preparer.SeqDuration;
                var getnextOutput =
                    (Validator.OutputDir + Validator.OutputName + Validator.OutputExt)
                    .Replace("~~~asterisk~~~", "*");
                var getnext = "\"" + "\"" + Assembly.GetEntryAssembly().Location + "\"" + " " +
                    "-u=" + Waiter.Id + " " +
                    "-s=" + "seq" + getnextSeqStart + " " +
                    "-d=" + durationStr + " " +
                    "-r=" + Preparer.Resolution + " " +
                    "-f=" + "\"" + Validator.Ffmpeg + "\"" + " " +
                    "-o=" + "\"" + getnextOutput + "\"" + " " +
                    "-e=*getnext*" + "\"";
                if (!string.IsNullOrEmpty(Validator.Browser))
                {
                    getnext = getnext
                        .Replace("*getnext*", "*getnext* -b=\"" + Validator.Browser + "\"");
                }
                if (!string.IsNullOrEmpty(Validator.Cookie))
                {
                    getnext = getnext
                        .Replace("*getnext*", "*getnext* -c=\"" + Validator.Cookie + "\"");
                }
                if (!Validator.KeepStreamInfo)
                {
                    getnext = getnext
                        .Replace("*getnext*", "*getnext* -k=\"" + "false" + "\"");
                }
                if (Validator.Log)
                {
                    getnext = getnext
                        .Replace("*getnext*", "*getnext* -l=\"" + "true" + "\"");
                }
                value = value.Replace("~~~asterisk~~~getnext~~~asterisk~~~", getnext);
            }

            // Still exists '~~~asterisk~~~' after replacing
            if (value.Contains("~~~asterisk~~~"))
            {
                // "Check '-output' argument"
                Exit(9057);
            }

            return value;
        }
        #endregion

        #region ExecuteOnExit - Run 'execute on exit' command
        static void ExecuteOnExit()
        {
            var executeOnExit = ReplaceWildcards(Validator.ExecuteOnExit);

            try
            {
                using (var p = new Process())
                {
                    p.StartInfo.FileName = "cmd.exe";
                    p.StartInfo.Arguments = @"/c " + executeOnExit;
                    p.StartInfo.UseShellExecute = false;
                    p.Start();
                }
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);
            }
        }
        #endregion

        #region Downloader - Get data as string at the specified URL
        public static string Downloader
            (string url, int attemptsNumber, int intervalSeconds, out string resultMsg)
        {
            // Call example: var content = Downloader(uri, 3, 10, out string resultMsg);
            // resultMsg content: [datetime] // 'OK' or [ErrorMessage]

            resultMsg = string.Empty;
            var content = string.Empty;

            while (attemptsNumber-- > 0)
            {
                try
                {
                    using (var wc = new WebClient())
                    {
                        ServicePointManager.SecurityProtocol =
                            SecurityProtocolType.Tls12 |
                            SecurityProtocolType.Tls11 |
                            SecurityProtocolType.Tls |
                            SecurityProtocolType.Ssl3;
                        content = wc.DownloadString(new Uri(url.Trim()));
                        resultMsg = wc.ResponseHeaders.Get("date") + " // " + "OK";
                    }

                    break;
                }
                catch (WebException e)
                {
                    Program.ErrInfo =
                        new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                    if (Validator.Log) Program.Log(Program.ErrInfo);

                    if (attemptsNumber > 0)
                    {
                        Thread.Sleep(intervalSeconds * 1000);
                        continue;
                    }

                    resultMsg = e.Message;
                    if (e.Status == WebExceptionStatus.ProtocolError)
                        resultMsg = e.Response.Headers.Get("date") + " // " + resultMsg;
                    content = string.Empty;
                }
            }

            return content;
        }
        #endregion

        #region Log - Save technical info to the current folder
        public static void Log(string value, string name = "")
        {
            // Add value to common log file:
            // if (Validator.Log) Program.Log(valueToSave);
            // To save value as a separate file:
            // if (Validator.Log) Program.Log(valueToSave, "fileName");

            if (!Directory.Exists(LogDir))
            {
                try
                {
                    Directory.CreateDirectory(LogDir);
                }
                catch
                {
                    return;
                }
            }

            if (name == "")
            {
                name = Start.ToString("yyyyMMdd-HHmmss") + ".log";
            }
            else
            {
                name = (Program.Start + Program.Timer.Elapsed)
                    .ToString("yyyyMMdd-HHmmss-ffff") + "~~" + name + ".log";
            }

            value = HttpUtility.UrlDecode(value);
            value = HttpUtility.UrlDecode(value);
            value = HttpUtility.UrlDecode(value);
            value = value.Replace(@"\u0026", "&");

            try
            {
                File.AppendAllText(
                    LogDir + name,
                    "[" + (Program.Start + Program.Timer.Elapsed).ToString() + "]\n" +
                    value + "\n\n\n\n\n");
            }
            catch
            {
                return;
            }
        }
        #endregion

        #region LogClean - Determine user IP and delete it from log files
        public static void LogClean()
        {
            var userIp = string.Empty;
            var userCookies = string.Empty;

            try
            {
                userIp = Regex.Match(
                    Preparer.UriAdirect,
                    @"^.*ip=(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}).*$",
                    RegexOptions.IgnoreCase
                    ).Groups[1].Value;

                userCookies = Validator.CookieContent;
                userCookies = HttpUtility.UrlDecode(userCookies);
                userCookies = HttpUtility.UrlDecode(userCookies);
                userCookies = HttpUtility.UrlDecode(userCookies);
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);
            }

            foreach (var logFile in Directory.GetFiles(Program.LogDir))
            {
                try
                {
                    var content = File.ReadAllText(logFile);

                    if (!string.IsNullOrEmpty(userIp))
                        content = content.Replace(userIp, "[not_logged]");
                    if (!string.IsNullOrEmpty(userCookies))
                        content = content.Replace(userCookies, "[not_logged]");

                    File.WriteAllText(logFile, content);
                }
                catch (Exception e)
                {
                    Program.ErrInfo =
                        new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                    if (Validator.Log) Program.Log(Program.ErrInfo);
                }
            }
        }
        #endregion

        #region Exit - Show result and exit
        public static void Exit(int code)
        {
            // Format of 'ErrInfo' string:
            // Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
            // Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + message;

            Console.Title = Constants.Name;
            if (Console.CursorLeft != 0) Console.WriteLine();
            var msg = string.Empty;

            if (code == 0)
            {
                // "--> OK ({0})"
                msg = string.Format(
                    Constants.Msg[9053],
                    (Program.Start + Program.Timer.Elapsed).ToString());
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(msg);
                Console.ResetColor();
            }
            else if (code > 1000)
            {
                // "--> ERROR {0}: {1} ({2})"
                msg = Constants.Msg[code];
                if (!string.IsNullOrEmpty(ErrInfo))
                {
                    msg = msg + " (line " + ErrInfo.TrimEnd('.') + ")";
                }
                msg = string.Format(
                    Constants.Msg[9054],
                    code,
                    msg,
                    (Program.Start + Program.Timer.Elapsed).ToString());
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(msg);
                Console.ResetColor();
            }

            if (Validator.Log) Program.Log(msg);

            if (Validator.Log) LogClean();

            // Run 'execute on exit' command
            if (!string.IsNullOrEmpty(Validator.ExecuteOnExit)) ExecuteOnExit();

            Environment.Exit(code);
        }
        #endregion
    }
}