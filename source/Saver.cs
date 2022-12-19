using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace yrewind
{
    // Downloading and saving the output
    class Saver
    {
        // Command to get the rest if FFmpeg hungs
        public static string ExecuteOnExit { get; private set; }

        // Full path of temporary file
        readonly string outputPathTmp =
                    Program.OutputDir +
                    Program.OutputName + "~" + "INCOMPLETE" + Constants.RandomString +
                    Program.OutputExt;

        // Other variables
        Process ffmpeg;
        int seqNumberA;
        int seqNumberV;

        #region Common - Main method of the class
        public int Common()
        {
            int code;

            code = OutputDirCreate(out var outputDirCreated);
            if (code != 0) return code;

            if (Program.OutputExt == ".m3u" || Program.OutputExt == ".m3u8")
            {
                code = CreateM3U();
                OutputDirDelete(outputDirCreated);
                if (code != 0) return code;

                if (!Program.ResultIsOK.HasValue) Program.ResultIsOK = true;
            }
            else
            {
                code = CreateMedia(outputPathTmp, out var ffmpegExitCode);
                OutputDirDelete(outputDirCreated);
                if (code != 0) return code;

                var durationIsOk = DurationCheck(Preparer.Duration, outputPathTmp);

                if (ffmpegExitCode == 0 & durationIsOk != false)
                {
                    try
                    {
                        File.Move(outputPathTmp,
                            Program.OutputDir + Program.OutputName + Program.OutputExt);
                    }
                    catch (Exception e)
                    {
                        Program.ErrInfo =
                            new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                        if (Validator.Log) Program.Log(Program.ErrInfo);
                    }
                }

                if (ffmpegExitCode == 0 && durationIsOk == true && !Program.ResultIsOK.HasValue)
                {
                    Program.ResultIsOK = true;
                }
            }

            return 0;
        }
        #endregion

        #region CreateMedia - Get video/audio with FFmpeg
        int CreateMedia(string outputPath, out int ffmpegExitCode)
        {
            ffmpegExitCode = 0;

            // "Saving"
            Console.Title = Constants.Name + " - " + Constants.Msg[9077].ToLower() + "...";

            // Add video data if required
            var videoSource = string.Empty;
            var videoMapping = string.Empty;
            if (Preparer.Resolution > 0)
            {
                videoSource = "-protocol_whitelist file,http,https,tcp,tls" + " " +
                "-i " + Constants.UrlProxy + Constants.RandomString + "/v.m3u8" + " ";
                videoMapping = "-map 1" + " ";
            }

            // Add cover art if '.mp4' container was selected
            var coverSource = string.Empty;
            var coverMapping = string.Empty;
            if (Program.OutputExt == ".mp4")
            {
                try
                {
                    var uri = Constants.UrlStreamCover.Replace("[stream_id]", Waiter.Id);
                    var req = (HttpWebRequest)WebRequest.Create(new Uri(uri));
                    var res = (HttpWebResponse)req.GetResponse();
                    if (res.StatusCode == HttpStatusCode.OK)
                    {
                        coverSource =
                            "-i \"" +
                            Constants.UrlStreamCover.Replace("[stream_id]", Waiter.Id) +
                            "\"" + " ";
                        coverMapping =
                            "-map 2 -disposition:1 default -disposition:2 attached_pic" + " ";
                    }
                    res.Close();
                }
                catch (WebException e)
                {
                    Program.ErrInfo =
                        new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                    if (Validator.Log) Program.Log(Program.ErrInfo);
                }
            }

            // Create string for the 'Title' metadata field
            var metadataTitle = Preparer.Title +
                " || " + Preparer.Author +
                " || " + Constants.UrlStream.Replace("[stream_id]", Waiter.Id) +
                " || " + Constants.UrlChannel.Replace("[channel_id]", Waiter.ChannelId) +
                " || " + Preparer.Start.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss") + " UTC";

            // Build FFmpeg arguments
            var arguments =
                "-loglevel fatal" + " " +
                "-stats" + " " +
                "-protocol_whitelist file,http,https,tcp,tls" + " " +
                "-i " + Constants.UrlProxy + Constants.RandomString + "/a.m3u8" + " " +
                videoSource +
                coverSource +
                "-map 0" + " " +
                videoMapping +
                coverMapping +
                "-metadata comment=\"Saved with " + Constants.Name + "\"" + " " +
                "-metadata title=\"" + metadataTitle + "\"" + " " +
                "-c copy" + " " +
                "\"" + outputPath + "\"";

            _ = Provider();

            // Run FFmpeg process
            try
            {
                using (ffmpeg = new Process())
                {
                    ffmpeg.StartInfo.FileName = Validator.Ffmpeg;
                    ffmpeg.StartInfo.Arguments = arguments;
                    ffmpeg.StartInfo.UseShellExecute = false;
                    if (Validator.Log)
                    {
                        var LogDirAdapted =
                            Program.LogDir.Replace("\\", "\\\\").Replace(":", "\\:");
                        ffmpeg.StartInfo.EnvironmentVariables["FFREPORT"] =
                            "file=" + LogDirAdapted + "ffmpeg.log:level=40";
                        Program.Log("Starting FFmpeg...\n" +
                            ffmpeg.StartInfo.FileName + " " + ffmpeg.StartInfo.Arguments);
                    }
                    ffmpeg.Start();
                    ffmpeg.WaitForExit();
                    ffmpegExitCode = ffmpeg.ExitCode;
                    if (Validator.Log) Program.Log("FFmpeg exit code: " + ffmpeg.ExitCode);
                }
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                // "FFmpeg not responding"
                return 9410;
            }

            if (!File.Exists(outputPath))
            {
                // "Output file(s) creating error"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9411;
            }

            return 0;
        }
        #endregion

        #region CreateM3U - Create M3U/M3U8 playlists
        int CreateM3U()
        {
            var contentA = string.Empty;
            var contentV = string.Empty;
            int seqStart = Preparer.StartSeq;
            int seqStop = seqStart + Preparer.Duration / Preparer.SeqDuration - 1;
            for (var i = seqStart; i <= seqStop; i++)
            {
                contentA += "#EXTINF:" + Preparer.SeqDuration + ",\n" +
                    Preparer.UriAdirect + "&sq=" + i + "\n";
                contentV += "#EXTINF:" + Preparer.SeqDuration + ",\n" +
                    Preparer.UriVdirect + "&sq=" + i + "\n";
            }
            contentA = "#EXTM3U\n#EXT-X-MEDIA-SEQUENCE:0\n" + contentA + "#EXT-X-ENDLIST\n";
            contentV = "#EXTM3U\n#EXT-X-MEDIA-SEQUENCE:0\n" + contentV + "#EXT-X-ENDLIST\n";

            var content = "#EXTM3U\n" +
                          "#EXT-X-MEDIA:TYPE=AUDIO,GROUP-ID=\"stereo\",URI=" +
                              Program.OutputName + "_audio" + Program.OutputExt + "\n" +
                          "#EXT-X-STREAM-INF:AUDIO=\"stereo\"\n";

            if (Preparer.Resolution > 0)
            {
                content = content + Program.OutputName + "_video" + Program.OutputExt + "\n";
            }

            if (Validator.Log)
            {
                Program.Log(contentA, "playlist_a.m3u8");
                Program.Log(contentV, "playlist_v.m3u8");
                Program.Log(content, "playlist.m3u8");
            }

            var encoding = Encoding.ASCII;
            if (Program.OutputExt == ".m3u8") encoding = Encoding.UTF8;

            try
            {
                File.AppendAllText(
                    Program.OutputDir + Program.OutputName + Program.OutputExt,
                    content,
                    encoding);

                File.AppendAllText(
                    Program.OutputDir + Program.OutputName + "_audio" + Program.OutputExt,
                    contentA,
                    encoding);

                if (Preparer.Resolution > 0)
                {
                    File.AppendAllText(
                        Program.OutputDir + Program.OutputName + "_video" + Program.OutputExt,
                        contentV,
                        encoding);
                }
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                // "Output file(s) creating error"
                return 9411;
            }

            if (!File.Exists(Program.OutputDir + Program.OutputName + Program.OutputExt) ||
                !File.Exists(Program.OutputDir + Program.OutputName + "_audio" + Program.OutputExt))
            {
                // "Output file(s) creating error"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9411;
            }

            if (Preparer.Resolution > 0 &&
                !File.Exists(Program.OutputDir + Program.OutputName + "_video" + Program.OutputExt))
            {
                // "Output file(s) creating error"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9411;
            }

            return 0;
        }
        #endregion

        #region Provider - Local proxy for providing HLS playlists
        async Task Provider()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add(Constants.UrlProxy + Constants.RandomString + "/");
            listener.Start();

            byte[] buffer;
            string content;
            bool realtime = false;
            int seqCheck = 0;
            int seqStart = Preparer.StartSeq;
            int seqStop = seqStart + Preparer.Duration / Preparer.SeqDuration;
            DateTime bufferedTimeOfCurSeq = Preparer.IdStart
                .AddSeconds(seqStart * Preparer.SeqDuration + Constants.RealTimeBuffer);
            int i = seqStart + 1;
            seqNumberA = seqStart;
            seqNumberV = seqStart;
            string lineA = "#EXTM3U\n#EXT-X-MEDIA-SEQUENCE:[secNumber]\n#EXTINF:-1,\n" +
                Preparer.UriAdirect + "&sq=[secNumber]\n";
            string lineV = "#EXTM3U\n#EXT-X-MEDIA-SEQUENCE:[secNumber]\n#EXTINF:-1,\n" +
                Preparer.UriVdirect + "&sq=[secNumber]\n";
            var seqChecks = new Dictionary<int, Task<string>>();

            // Use a timer to see if FFmpeg is working normally
            var timer = new System.Timers.Timer(Constants.FfmpegTimeout * 1000);
            timer.Elapsed += HungsFfmpegHandler;

            while (true)
            {
                timer.Start();

                var context = await listener.GetContextAsync();
                var reqUri = context.Request.RawUrl;

                // Switch to real time if needed
                if (!realtime && (Program.Start + Program.Timer.Elapsed) < bufferedTimeOfCurSeq)
                {
                    realtime = true;
                    lineA = lineA.Replace("EXTINF:-1", "EXTINF:" + Preparer.SeqDuration);
                    lineV = lineV.Replace("EXTINF:-1", "EXTINF:" + Preparer.SeqDuration);
                }

                // Check if next sequence exists and available
                if (seqCheck < Math.Max(seqNumberA + 1, seqNumberV + 1))
                {
                    seqCheck = Math.Max(seqNumberA + 1, seqNumberV + 1);

                    if ((seqCheck < seqStop) && realtime)
                    {
                        var result = Preparer.CheckSeq(seqCheck);
                        if (result != "200")
                        {
                            if (result == "403")
                            {
                                // The stream info may be out of date
                                seqStop = seqCheck;
                                if (Waiter.IdStatus != Waiter.Stream.Finished) TaskRest(seqStop);
                            }
                            else if (result == "404")
                            {
                                // The streamer may experience network outages, wait and try more
                                Thread.Sleep(60000);
                                if (Preparer.CheckSeq(seqCheck) != "200") seqStop = seqCheck;
                            }
                            else
                            {
                                // Unknown error
                                seqStop = seqCheck;
                            }
                        }
                    }
                    else if ((seqCheck < seqStop) && !realtime)
                    {
                        // Check in advance whether the sequence [current+10] is available
                        while ((i < (seqCheck + 10)) & (i < seqStop))
                        {
                            seqChecks.Add(i, Task<string>.Factory.StartNew(Preparer.CheckSeq, i));
                            i++;
                        }

                        var result = seqChecks[seqCheck].Result;
                        if (result != "200")
                        {
                            if (result == "403")
                            {
                                // The stream info may be out of date
                                seqStop = seqCheck;
                                if (Waiter.IdStatus != Waiter.Stream.Finished) TaskRest(seqStop);
                            }
                            else if (result == "404")
                            {
                                // The end of the finished stream
                                seqStop = seqCheck;
                            }
                            else
                            {
                                // Unknown error
                                seqStop = seqCheck;
                            }
                        }
                    }
                }

                // Handling FFmpeg requests
                if (reqUri.EndsWith("a.m3u8"))
                {
                    content = lineA.Replace("[secNumber]", seqNumberA.ToString());

                    if (++seqNumberA == seqStop) content += "#EXT-X-ENDLIST\n";

                    if (!realtime)
                    {
                        bufferedTimeOfCurSeq =
                            bufferedTimeOfCurSeq.AddSeconds(Preparer.SeqDuration);
                    }

                    if (Validator.Log) Program.Log("Get audio segment:\n" + content);
                }
                else if (reqUri.EndsWith("v.m3u8"))
                {
                    content = lineV.Replace("[secNumber]", seqNumberV.ToString());

                    if (++seqNumberV == seqStop) content += "#EXT-X-ENDLIST\n";

                    if (Validator.Log) Program.Log("Get video segment:\n" + content);
                }
                else
                {
                    // Unknown request
                    context.Response.Close();
                    continue;
                }

                buffer = Encoding.ASCII.GetBytes(content);
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();
                context.Response.Close();

                timer.Stop();
            }
        }
        #endregion

        #region DurationCheck - Compares required duration with actual result
        bool? DurationCheck(int durationRequired, string path)
        {
            try
            {
                var t = Type.GetTypeFromProgID("Shell.Application");
                dynamic s = Activator.CreateInstance(t);
                var folder = s.NameSpace(Path.GetDirectoryName(path));
                var item = folder.ParseName(Path.GetFileName(path));
                var dStr = folder.GetDetailsOf(item, 27).ToString();

                // Use simple correction, because shell info contains the number of complete seconds
                // (for example, "4" for video with duration "4.997")
                var durationActual =
                    TimeSpan.ParseExact(dStr, @"hh\:mm\:ss", null).TotalSeconds + 1;

                if (durationActual >= durationRequired) return true;
                else return false;
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                return null;
            }
        }
        #endregion

        #region OutputDirCreate - Create output directory if needed
        int OutputDirCreate(out string outputDirCreatedRoot)
        {
            outputDirCreatedRoot = string.Empty;

            if (Directory.Exists(Program.OutputDir)) return 0;

            // If output directory was created, keep the name of it,
            // if several nested directories was created, keep the name of the topmost one
            outputDirCreatedRoot = Program.OutputDir;
            var parent = Directory.GetParent(Program.OutputDir);
            while (!Directory.Exists(parent.FullName))
            {
                outputDirCreatedRoot = parent.FullName;
                parent = Directory.GetParent(parent.FullName);
            }

            try
            {
                Directory.CreateDirectory(Program.OutputDir);
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                // "Output folder creating error"
                return 9412;
            }

            return 0;
        }
        #endregion

        #region OutputDirDelete - Delete created directory if empty
        void OutputDirDelete(string outputDirCreated)
        {
            if (!string.IsNullOrEmpty(outputDirCreated))
            {
                try
                {
                    var files = Directory
                        .GetFiles(outputDirCreated, "*", SearchOption.AllDirectories);

                    if (files.Length == 0) Directory.Delete(outputDirCreated, true);
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

        #region TaskRest - Create task to get the rest in case of any errors
        void TaskRest(int newStartSeq)
        {
            if (newStartSeq < Preparer.StartSeq) newStartSeq = Preparer.StartSeq;

            // The rest of requested duration
            var newDuration = TimeSpan.FromSeconds
                (Preparer.Duration - Preparer.SeqDuration * (newStartSeq - Preparer.StartSeq));
            var newDurationStr = string.Format
                ("{0:D3}.{1:D2}", (int)newDuration.TotalMinutes, newDuration.Seconds);

            // New output name
            var newOutput = outputPathTmp.Replace(Program.OutputExt, "_2" + Program.OutputExt);

            ExecuteOnExit = "\"" + Assembly.GetEntryAssembly().Location + "\"" + " " +
                "-u=" + Waiter.Id + " " +
                "-s=" + "seq" + newStartSeq + " " +
                "-d=" + newDurationStr + " " +
                "-r=" + Preparer.Resolution + " " +
                "-f=" + "\"" + Validator.Ffmpeg + "\"" + " " +
                "-o=" + "\"" + newOutput + "\"";

            if (!string.IsNullOrEmpty(Validator.Browser))
                ExecuteOnExit += " -b=\"" + Validator.Browser + "\"";

            if (!string.IsNullOrEmpty(Validator.Cookie))
                ExecuteOnExit += " -c=\"" + Validator.Cookie + "\"";

            if (!Validator.KeepStreamInfo)
                ExecuteOnExit += " -k=" + Validator.KeepStreamInfo;

            if (Validator.Log)
                ExecuteOnExit += " -l=" + Validator.Log;

            ExecuteOnExit = "\"" + ExecuteOnExit + "\"";
        }
        #endregion

        #region HungsFfmpegHandler - If FFmpeg hangs
        void HungsFfmpegHandler(Object source, System.Timers.ElapsedEventArgs e)
        {
            if (ffmpeg != null) ffmpeg.Kill();
            var newStartSeq = Math.Min(seqNumberA - 3, seqNumberV - 3);
            TaskRest(newStartSeq);

            // "Output file isn't completed, there will be a retry"
            Program.Exit(9413);
        }
        #endregion
    }
}