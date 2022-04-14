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
        // Time when program started (local time zone)
        public static DateTime Start { get; private set; }

        // Output folder and filename (extension value is taken from class CLInput)
        public static string OutputDir { get; private set; }
        public static string OutputName { get; private set; }

        // TRUE if:
        // FFmpeg process exited correctly AND
        // duration of the saved video is verifiable AND
        // duration of the saved video is as requested AND
        // the start time point is determined sharply (using UTC tag in the server HLS playlist)
        public static bool? ResultСonfirmed;

        // Determined by CheckForUpdates():
        static DateTime StartUtcGithub; // UTC from Github server
        static bool updateChecked;      // Is updates has been checked
        static bool updateExists;       // Is update exists (relative to this version)
        static bool updateCritical;     // Is update critical
        static string updateZipUri;     // Direct URL of new release

        #region Main - Entry point of the program
        static void Main()
        {
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
                ExitProgram(1); // Standard Windows code "Incorrect function"
            }

            // Check for updates and determine the current time
            Start = DateTime.Now;
            CheckForUpdates();
            if (StartUtcGithub != DateTime.MinValue) Start = StartUtcGithub.ToLocalTime();

            // "--> {0} (started at {1})"
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(Constants.Msg[9027], Constants.Name, Start.ToString());
            Console.ResetColor();

            // If the program should work
            if (updateExists & updateCritical)
            {
                ExitProgram(9012); // "Please update the program"
            }

            // Parse the command line input
            var argsLine = Environment.CommandLine;
            if (argsLine.Length > Constants.CommandLineStringLengthLimit)
            {
                ExitProgram(9011); // "Command line input is too long"
            }
            var clinput = new CLInput();
            code = clinput.ParseArgsLine(argsLine);
            if (code != 0) ExitProgram(code);

            // "getting live stream info"
            Console.Write(Constants.Msg[9008] + "...");

            // If specified start point is in future (except 'start=wait')
            if (CLInput.Start > Start)
            {
                if ((CLInput.Start - Start).TotalDays > 1)
                {
                    code = 9018; // "Delayed start is limited to 24 hours"
                    Console.WriteLine();
                    ExitProgram(code);
                }
                CountdownTimer((CLInput.Start - Start).TotalMilliseconds);
                Start = CLInput.Start; // Now in real time mode
            }

            // Get ID info
            var idinfo = new IDInfo();
            if (CLInput.StartWait)
            {
                // "waiting"
                Console.Title = Constants.Name + " - " + Constants.Msg[9025] + "...";
            }
            code = idinfo.Common();
            if (CLInput.StartWait) Console.Title = Constants.Name;
            if (code != 0) ExitProgram(code);
            Console.WriteLine();

            // "stream title"
            Console.WriteLine("    {0,-20}{1}", Constants.Msg[9020], IDInfo.Title);

            // "resolutions"
            Console.WriteLine("    {0,-20}{1}", Constants.Msg[9021], IDInfo.Resolutions);

            // Make various preparations and checks
            var preparer = new Preparer();
            code = preparer.Common();
            if (code != 0) ExitProgram(code);

            // If available start later than requested (difference more than 2 sequences)
            if ((!CLInput.StartBeginning & !CLInput.StartWait) &&
                (Preparer.Start - CLInput.Start) > TimeSpan.FromSeconds(IDInfo.Duration * 2))
            {
                // "requested start" / "actually available"
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("    {0,-20}{1}", Constants.Msg[9023], CLInput.Start);
                Console.WriteLine("    {0,-20}{1}", Constants.Msg[9024], Preparer.Start);
                Console.ResetColor();

                ExitProgram(9014); // "Requested time interval is not available"
            }

            // Determine output directory
            OutputDir = ReplaceWildcards(CLInput.OutputDir);

            // Determine output filename
            if (CLInput.OutputName.Length == 0)
            {
                // set default like '9Auq9mYxFEE_20201231-0730_60m_1080p'
                OutputName = IDInfo.Id + "_" +
                    Preparer.Start.ToString("yyyyMMdd-HHmmss") + "_" +
                    CLInput.Duration.ToString("D3") + "m_" +
                    Preparer.Resolution.ToString("D4") + "p";
            }
            else
            {
                OutputName = ReplaceWildcards(CLInput.OutputName);
            }

            // "output file"
            Console.WriteLine("    {0,-20}{1}", Constants.Msg[9022], OutputName + CLInput.OutputExt);

            // Check if the output file already exists
            if (File.Exists(OutputDir + OutputName + CLInput.OutputExt))
            {
                ExitProgram(9036); // "File already exists"
            }

            // Wait if current sequence is too close
            int timeToWait;
            if (Preparer.Start < Start)
            {
                timeToWait = Constants.RealTimeBufferSeconds -
                    (int)(Start - Preparer.Start).TotalSeconds;
            }
            else
            {
                timeToWait = Constants.RealTimeBufferSeconds -
                    (int)(Preparer.Start - Start).TotalSeconds;
            }
            if (timeToWait > 0)
            {
                Console.WriteLine(Constants.Msg[9035] + "..."); // "waiting data"
                Thread.Sleep(timeToWait * 1000);
            }

            // Download and save the output media
            var getsave = new GetSave();
            Console.Title = Constants.Name + " - " + Constants.Msg[9037] + "..."; // "saving"
            code = getsave.Common();
            Console.Title = Constants.Name;
            if (code != 0) ExitProgram(code);

            // Show warning if result cannot be verified
            if (ResultСonfirmed != true)
            {
                // "unable to verify the saved file is correct"
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(Constants.Msg[9019]);
                Console.ResetColor();
            }

            // Restore console window width
            if (Console.WindowWidth != consoleWidthInit)
            {
                Console.SetWindowSize(consoleWidthInit, Console.WindowHeight);
            }

            ExitProgram(0);
        }
        #endregion

        #region CheckForUpdates - Check for updates
        static int CheckForUpdates()
        {
            var content = string.Empty;
            var startUtcGithubStr = string.Empty;

            try
            {
                using (var wc = new WebClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                        | SecurityProtocolType.Tls11
                        | SecurityProtocolType.Tls12
                        | SecurityProtocolType.Ssl3;
                    content = wc.DownloadString(new Uri(Constants.UpdateInfoUrl)).Trim();
                    startUtcGithubStr = wc.ResponseHeaders.Get("date");

                    updateChecked = true;
                }
            }
            catch
            {
                return 9033; // "Unable to check for updates"
            }

            if (content.StartsWith("_"))
            {
                updateCritical = true;
                content = content.Substring(1);
            }

            try
            {
                var updateDate = int.Parse(content.Split(';')[0]);
                updateZipUri = content.Split(';')[1];
                if (Constants.BuildDate < updateDate) updateExists = true;
                if (!updateZipUri.StartsWith("http")) throw new Exception();
                StartUtcGithub = DateTime.Parse(startUtcGithubStr).ToUniversalTime();
            }
            catch
            {
                return 9038; // "Unable to check for updates"
            }

            return 0;
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
                Console.WriteLine(Constants.Msg[9026], Constants.Name, Constants.Version);
                Console.WriteLine(Constants.Msg[9001]); // "---[64]---"
                Console.WriteLine(Constants.Msg[9002]); // "Press <H> to show help"
                Console.WriteLine(Constants.Msg[9003]); // "Press <U> to get the latest version"
                Console.WriteLine(Constants.Msg[9004]); // "Press <Esc> to exit"
                Console.WriteLine(Constants.Msg[9001]); // "---[64]---"

                tmp1 = Console.ReadKey(true).Key;
                if (tmp1 == ConsoleKey.Escape) break;
                else if (tmp1 == ConsoleKey.H)
                {
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
                    Console.WriteLine(Constants.Msg[9026], Constants.Name, Constants.Version);
                    Console.WriteLine(Constants.Msg[9001]); // "---[64]---"
                    // "Use <Up>, <Down>, <PageUp>, <PageDown> to scroll"
                    Console.WriteLine(Constants.Msg[9006]);
                    Console.WriteLine();
                    Console.WriteLine(Constants.Msg[9004]); // "Press <Esc> to exit"
                    Console.WriteLine(Constants.Msg[9001]); // "---[64]---"

                    //"please see the actual text on the program page"
                    Console.WriteLine("[" + Constants.Msg[9015] + "]");

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
                    Console.Clear();
                    Console.BufferHeight = Console.WindowHeight;
                    // "{0} (version {1})"
                    Console.WriteLine(Constants.Msg[9026], Constants.Name, Constants.Version);
                    Console.WriteLine(Constants.Msg[9001]); // "---[64]---"
                    Console.WriteLine(Constants.Msg[9005]); // "Press <D> to confirm download"
                    Console.WriteLine();
                    Console.WriteLine(Constants.Msg[9004]); // "Press <Esc> to exit"
                    Console.WriteLine(Constants.Msg[9001]); // "---[64]---"
                    Console.WriteLine();
                    Console.Write(Constants.Msg[9013] + "... "); // "Checking"

                    // Check for updates
                    CheckForUpdates();
                    if (updateChecked & !updateExists)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(Constants.Msg[9017]); // "Current version is actual"
                    }
                    else if (updateChecked & updateExists)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(Constants.Msg[9016]); // "New version available"
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(Constants.Msg[9007]); // "Unable to check for updates"
                    }
                    Console.ResetColor();

                    while (true)
                    {
                        tmp3 = Console.ReadKey(true).Key;
                        if (tmp3 == ConsoleKey.Escape) break;
                        else if (tmp3 == ConsoleKey.D)
                        {
                            // "Downloading, please wait a minute"
                            Console.Write(Constants.Msg[9031] + "... ");

                            var zipFullPath =
                                Environment.GetFolderPath(Environment.SpecialFolder.Desktop) +
                                "\\" +
                                Path.GetFileName(updateZipUri);

                            var zipFullPathTmp =
                                zipFullPath.Replace(".zip", "~" + Constants.RandomString + ".zip");

                            if (File.Exists(zipFullPath))
                            {
                                // "Skipped! File already exists"
                                Console.WriteLine(Constants.Msg[9034]);

                                //"see file '{0}' on the desktop"
                                Console.WriteLine("(" + Constants.Msg[9030] + ")",
                                    Path.GetFileName(updateZipUri));

                                Console.ReadKey();
                                break;
                            }
                            else
                            {
                                try
                                {
                                    var uri = updateZipUri;
                                    using (var wc = new WebClient())
                                    {
                                        wc.DownloadFile(new Uri(uri), zipFullPathTmp);
                                    }

                                    File.Move(zipFullPathTmp, zipFullPath);

                                    Console.WriteLine(Constants.Msg[9009] + "!"); // "Ready"

                                    //"see file '{0}' on the desktop"
                                    Console.WriteLine("(" + Constants.Msg[9030] + ")",
                                        Path.GetFileName(updateZipUri));
                                }
                                catch
                                {
                                    // "Error"
                                    Console.WriteLine(Constants.Msg[9032] + "!");
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region CountdownTimer - Show countdown timer, then return
        public static void CountdownTimer(double milliseconds)
        {
            Console.Write(" ");
            Console.CursorVisible = false;
            var dateTime = DateTime.MinValue.AddMilliseconds(milliseconds);
            while (dateTime > DateTime.MinValue)
            {
                Thread.Sleep(1000);
                try
                {
                    dateTime = dateTime.AddSeconds(-1);
                }
                catch
                {
                    // ignore
                }
                Console.Write(dateTime.ToString("HH:mm:ss"));
                Console.SetCursorPosition(Console.CursorLeft - 8, Console.CursorTop);
            }
            Console.CursorVisible = true;
            Console.Write("        ");
            Console.SetCursorPosition(Console.CursorLeft - 9, Console.CursorTop);
        }
        #endregion

        #region ReplaceWildcards - Replace wildcards
        static string ReplaceWildcards(string value)
        {
            // This method does NOT check if replacement values ​​exist

            value = Regex.Replace(value, "~~~asterisk~~~id~~~asterisk~~~",
                IDInfo.Id, RegexOptions.IgnoreCase);

            value = Regex.Replace(value, "~~~asterisk~~~start~~~asterisk~~~",
                Preparer.Start.ToString("yyyyMMdd-HHmmss"), RegexOptions.IgnoreCase);

            value = Regex.Replace(value, "~~~asterisk~~~start\\[(.+)\\]~~~asterisk~~~",
                m => Preparer.Start.ToString(m.Groups[1].Value), RegexOptions.IgnoreCase);

            value = Regex.Replace(value, "~~~asterisk~~~duration~~~asterisk~~~",
                CLInput.Duration.ToString("D2"), RegexOptions.IgnoreCase);

            value = Regex.Replace(value, "~~~asterisk~~~resolution~~~asterisk~~~",
                Preparer.Resolution.ToString("D4"), RegexOptions.IgnoreCase);

            value = Regex.Replace(value, "~~~asterisk~~~author~~~asterisk~~~",
                string.Join("_", IDInfo.Author.Split(Path.GetInvalidFileNameChars())),
                RegexOptions.IgnoreCase);

            value = Regex.Replace(value, "~~~asterisk~~~title~~~asterisk~~~",
                string.Join("_", IDInfo.Title.Split(Path.GetInvalidFileNameChars())),
                RegexOptions.IgnoreCase);

            value = Regex.Replace(value, "~~~asterisk~~~channel_id~~~asterisk~~~",
                IDInfo.ChannelId, RegexOptions.IgnoreCase);

            value = Regex.Replace(value, "~~~asterisk~~~output~~~asterisk~~~",
                OutputDir + OutputName + CLInput.OutputExt, RegexOptions.IgnoreCase);

            value = Regex.Replace(value, "~~~asterisk~~~getnext~~~asterisk~~~",
                "~~~asterisk~~~getnext~~~asterisk~~~", RegexOptions.IgnoreCase);
            if (value.Contains("~~~asterisk~~~getnext~~~asterisk~~~"))
            {
                var getnextSeqStart =
                    Preparer.StartSequence + CLInput.Duration * 60 / IDInfo.Duration;
                var getnextOutput = (CLInput.OutputDir + CLInput.OutputName + CLInput.OutputExt)
                    .Replace("~~~asterisk~~~", "*");
                var getnext = "\"" + "\"" + Assembly.GetEntryAssembly().Location + "\"" + " " +
                    "-u=" + IDInfo.Id + " " +
                    "-s=" + "seq" + getnextSeqStart + " " +
                    "-d=" + CLInput.Duration + " " +
                    "-r=" + Preparer.Resolution + " " +
                    "-f=" + "\"" + CLInput.Ffmpeg + "\"" + " " +
                    "-o=" + "\"" + getnextOutput + "\"" + " " +
                    "-e=*getnext*" + "\"";
                if (CLInput.Cookie != default)
                {
                    getnext = getnext
                        .Replace("*getnext*", "*getnext* -c=\"" + CLInput.Cookie + "\"");
                }
                value = value.Replace("~~~asterisk~~~getnext~~~asterisk~~~", getnext);
            }

            // Still exists '~~~asterisk~~~' after replacing
            if (value.Contains("~~~asterisk~~~"))
            {
                ExitProgram(9010); // "Check '-output' argument"
            }

            return value;
        }
        #endregion

        #region ExitProgram - Show result and exit
        static void ExitProgram(int code)
        {
            if (Console.CursorLeft != 0) Console.WriteLine();

            if (code > 1000)
            {
                //"--> ERROR {0}: {1} ({2})"
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(Constants.Msg[9029],
                    code, Constants.Msg[code], DateTime.Now.ToString());
                Console.ResetColor();
            }
            else if (code == 0)
            {
                // "--> OK (finished at {0})"
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(Constants.Msg[9028], DateTime.Now.ToString());
                Console.ResetColor();

                // Run 'execute on exit' command
                if (CLInput.ExecuteOnExit != null) ExecuteOnExit();
            }

            Environment.Exit(code);
        }
        #endregion

        #region ExecuteOnExit - Run 'execute on exit' command
        static void ExecuteOnExit()
        {
            var executeOnExit = ReplaceWildcards(CLInput.ExecuteOnExit);

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
            catch
            {
                // ignore
            }
        }
        #endregion

        #region Log - Add the value of specified variable to the log
        public static void Log(string mode, string name, string value)
        {
            // Using for (v)iew in console and (s)ave to log file:
            // if (CLInput.Log) Program.Log("v", "title", value);
            // if (CLInput.Log) Program.Log("s", "filename", value);

            name = DateTime.Now.ToString("yyyyMMdd-HHmmss-ffff") + "~~" + name;
            var valueDecoded = HttpUtility.UrlDecode(value);
            valueDecoded = HttpUtility.UrlDecode(valueDecoded);
            valueDecoded = HttpUtility.UrlDecode(valueDecoded);
            valueDecoded = valueDecoded.Replace(@"\u0026", "&");

            if (mode == "v")
            {
                Console.WriteLine("\n" + name + ":\n" + valueDecoded + "\n");
            }
            else if (mode == "s")
            {
                try
                {
                    File.WriteAllText(CLInput.LogDir + name + ".txt", value);
                    File.WriteAllText(CLInput.LogDir + name + "_decoded.txt", valueDecoded);
                }
                catch { }
            }
        }
        #endregion
    }
}