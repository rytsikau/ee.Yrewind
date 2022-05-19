using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace yrewind
{
    // Downloading and saving the output result
    class Saver
    {
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
                // Full path of media temporary file
                var outputPathTmp =
                    Program.OutputDir +
                    Program.OutputName + "~" + "INCOMPLETE" + Constants.RandomString +
                    Program.OutputExt;

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
                    catch
                    {
                        // Ignore
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
                catch
                {
                    // Ignore
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
                using (var p = new Process())
                {
                    p.StartInfo.FileName = Validator.Ffmpeg;
                    p.StartInfo.Arguments = arguments;
                    p.StartInfo.UseShellExecute = false;
                    if (Validator.Log)
                    {
                        var LogDirAdapted =
                            Program.LogDir.Replace("\\", "\\\\").Replace(":", "\\:");
                        p.StartInfo.EnvironmentVariables["FFREPORT"] =
                            "file=" + LogDirAdapted + "ffmpeg.log:level=40";
                        Program.Log("Starting FFmpeg...\n" +
                            p.StartInfo.FileName + " " + p.StartInfo.Arguments);
                    }
                    p.Start();
                    p.WaitForExit();
                    ffmpegExitCode = p.ExitCode;
                    if (Validator.Log) Program.Log("FFmpeg exit code: " + p.ExitCode);
                }
            }
            catch (Exception e)
            {
                // "FFmpeg not responding"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
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
                          "#EXT-X-STREAM-INF:AUDIO=\"stereo\"\n" +
                          Program.OutputName + "_video" + Program.OutputExt + "\n";

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
                    Program.OutputDir + Program.OutputName + "_audio" + Program.OutputExt,
                    contentA,
                    encoding);
                File.AppendAllText(
                    Program.OutputDir + Program.OutputName + "_video" + Program.OutputExt,
                    contentV,
                    encoding);
                File.AppendAllText(
                    Program.OutputDir + Program.OutputName + Program.OutputExt,
                    content,
                    encoding);
            }
            catch (Exception e)
            {
                // "Output file(s) creating error"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                return 9411;
            }

            if (!File.Exists(Program.OutputDir + Program.OutputName + "_audio" + Program.OutputExt) ||
                !File.Exists(Program.OutputDir + Program.OutputName + "_video" + Program.OutputExt) ||
                !File.Exists(Program.OutputDir + Program.OutputName + Program.OutputExt))
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
            int seqCheck;

            bool realtime = false;
            int seqStart = Preparer.StartSeq;
            int seqStop = seqStart + Preparer.Duration / Preparer.SeqDuration;
            DateTime bufferedTimeOfCurSeq = Preparer.IdStart
                .AddSeconds(seqStart * Preparer.SeqDuration + Constants.RealTimeBufferSeconds);
            int seqNumberA = seqStart;
            int seqNumberV = seqStart;
            string lineA = "#EXTM3U\n#EXT-X-MEDIA-SEQUENCE:[secNumber]\n#EXTINF:-1,\n" +
                Preparer.UriAdirect + "&sq=[secNumber]\n";
            string lineV = "#EXTM3U\n#EXT-X-MEDIA-SEQUENCE:[secNumber]\n#EXTINF:-1,\n" +
                Preparer.UriVdirect + "&sq=[secNumber]\n";

            while (true)
            {
                var context = await listener.GetContextAsync();
                var reqUri = context.Request.RawUrl;

                if (!realtime && (Program.Start + Program.Timer.Elapsed) < bufferedTimeOfCurSeq)
                {
                    // Switch to real time
                    realtime = true;
                    lineA = lineA.Replace("EXTINF:-1", "EXTINF:" + Preparer.SeqDuration);
                    lineV = lineV.Replace("EXTINF:-1", "EXTINF:" + Preparer.SeqDuration);
                }

                // Check if next sequence exists
                seqCheck = Math.Max(seqNumberA + 2, seqNumberV + 2);
                if (!Preparer.FindStart_CheckSeq(seqCheck) && (seqCheck <= seqStop))
                {
                    // Perhaps a network error or the stream has ended,
                    // wait a moment and try again
                    Thread.Sleep(Constants.RealTimeBufferSeconds * 1000);
                    if (!Preparer.FindStart_CheckSeq(seqCheck)) seqStop = seqCheck - 1;
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
                var durationActual = TimeSpan.ParseExact(dStr, @"hh\:mm\:ss", null).TotalSeconds + 1;

                if (durationActual >= durationRequired) return true;
                else return false;
            }
            catch
            {
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
                // "Output folder creating error"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
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
                catch
                {
                    // Ignore
                }
            }
        }
        #endregion
    }
}