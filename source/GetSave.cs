using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace yrewind
{
    // Class for downloading and saving the output media
    class GetSave
    {
        // If output directory was created, keep the name of it
        // (if several nested directories was created, keep the name of the topmost one)
        string outputDirCreatedRoot;

        // Temp output filename
        readonly string outputNameTmp =
            Program.OutputName + "~" + "INCOMPLETE" + Constants.RandomString;

        // Is save process exited correctly
        bool saveProcessExitedOk;

        #region Common - Main method of the class
        public int Common()
        {
            int code;

            // Create output folder
            code = CreateOutputDir();
            if (code != 0) return code;

            // Create M3U8 playlists
            if (CLInput.Log) CreateM3u8();

            // Get and save the output file
            code = GetMedia();
            if (code != 0) return code;

            // Check and rename the output file
            if (File.Exists(Program.OutputDir + outputNameTmp + CLInput.OutputExt))
            {
                CheckDuration(Program.OutputDir + outputNameTmp + CLInput.OutputExt,
                    out bool durationChecked, out bool durationErrorFound);

                // Rename to normal name even if cannot check result duration
                if (saveProcessExitedOk & !durationErrorFound)
                {
                    try
                    {
                        File.Move(Program.OutputDir + outputNameTmp + CLInput.OutputExt,
                            Program.OutputDir + Program.OutputName + CLInput.OutputExt);
                    }
                    catch
                    {
                        return 9412; // "Unable to access the saved file"
                    }
                }

                // But set status as confirmed only if duration checked
                if (saveProcessExitedOk & !durationErrorFound & durationChecked
                    & !Program.ResultСonfirmed.HasValue)
                {
                    Program.ResultСonfirmed = true;
                }
            }
            else
            {
                DeleteOutputDir();
                return 9411; // "Output file not created"
            }

            return 0;
        }
        #endregion

        #region GetMedia - Get video/audio with FFmpeg
        int GetMedia()
        {
            // FFmpeg logging mode
            string ffmpegLogMode = "quiet";
            if (CLInput.Log) ffmpegLogMode = "verbose";

            // Add video data (except cases when audio container is requested)
            var videoSource = string.Empty;
            var videoMapping = string.Empty;
            if (Preparer.Resolution > 0)
            {
                videoSource = "-protocol_whitelist file,http,https,tcp,tls" + " " +
                "-i " + Constants.UrlProxy + Constants.RandomString + "/v.m3u8" + " ";
                videoMapping = "-map 1" + " ";
            }

            // Add cover image if MP4 format is requested
            var coverSource = string.Empty;
            var coverMapping = string.Empty;
            if (CLInput.OutputExt == ".mp4")
            {
                coverSource = "-i \"" +
                    Constants.UrlStreamCover.Replace("[stream_id]", IDInfo.Id) + "\"" + " ";
                coverMapping = "-map 2 -disposition:1 default -disposition:2 attached_pic" + " ";
            }

            // Create metadata string for 'Title' field
            var metadataTitle = IDInfo.Title +
                " || " + IDInfo.Author +
                " || " + Constants.UrlStream.Replace("[stream_id]", IDInfo.Id) +
                " || " + Constants.UrlChannel.Replace("[channel_id]", IDInfo.ChannelId) +
                " || " + Preparer.Start.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss") + " UTC";

            // Build FFmpeg arguments
            var arguments =
                "-loglevel" + " " + ffmpegLogMode + " " +
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
                "\"" + Program.OutputDir + outputNameTmp + CLInput.OutputExt + "\"";

            _ = Provider();

            // Run FFmpeg process
            try
            {
                using (var p = new Process())
                {
                    p.StartInfo.FileName = CLInput.Ffmpeg;
                    p.StartInfo.Arguments = arguments;
                    p.StartInfo.UseShellExecute = false;
                    p.Start();
                    p.WaitForExit();
                    if (p.ExitCode == 0) saveProcessExitedOk = true;
                }
            }
            catch
            {
                return 9410; // "FFmpeg not responding"
            }

            return 0;
        }
        #endregion

        #region CreateM3u8 - Create and save M3U8 playlists
        void CreateM3u8()
        {
            var contentA = string.Empty;
            var contentV = string.Empty;
            int seqStart = Preparer.StartSequence;
            int seqStop = seqStart + CLInput.Duration * 60 / IDInfo.Duration - 1;
            for (var i = seqStart; i <= seqStop; i++)
            {
                contentA += "#EXTINF:" + IDInfo.Duration + ",\n" +
                    Preparer.UriAdirect + "&sq=" + i + "\n";
                contentV += "#EXTINF:" + IDInfo.Duration + ",\n" +
                    Preparer.UriVdirect + "&sq=" + i + "\n";
            }
            contentA = "#EXTM3U\n#EXT-X-MEDIA-SEQUENCE:0\n" + contentA + "#EXT-X-ENDLIST\n";
            contentV = "#EXTM3U\n#EXT-X-MEDIA-SEQUENCE:0\n" + contentV + "#EXT-X-ENDLIST\n";

            var content = "#EXTM3U\n" +
                          "#EXT-X-MEDIA:TYPE=AUDIO,GROUP-ID=\"stereo\",URI=" +
                          Program.OutputName + "_A.m3u8\n" +
                          "#EXT-X-STREAM-INF:AUDIO=\"stereo\"" + "\n" +
                          Program.OutputName + "_V.m3u8\n";

            try
            {
                File.WriteAllText(CLInput.LogDir + Program.OutputName + "_A.m3u8",
                    contentA, Encoding.ASCII);

                File.WriteAllText(CLInput.LogDir + Program.OutputName + "_V.m3u8",
                    contentV, Encoding.ASCII);

                File.WriteAllText(CLInput.LogDir + Program.OutputName + ".m3u8",
                    content, Encoding.ASCII);
            }
            catch
            {
                // ignore
            }
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
            int seqStart = Preparer.StartSequence;
            int seqStop = seqStart + CLInput.Duration * 60 / IDInfo.Duration;
            DateTime bufferedTimeOfCurSeq = IDInfo.Start
                .AddSeconds(seqStart * IDInfo.Duration + Constants.RealTimeBufferSeconds);
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

                if (!realtime && DateTime.Now < bufferedTimeOfCurSeq)
                {
                    // Switch to real time
                    realtime = true;
                    lineA = lineA.Replace("EXTINF:-1", "EXTINF:" + IDInfo.Duration);
                    lineV = lineV.Replace("EXTINF:-1", "EXTINF:" + IDInfo.Duration);
                }

                if (realtime)
                {
                    // Check if next sequence exists
                    seqCheck = Math.Max(seqNumberA + 2, seqNumberV + 2);
                    if ((seqCheck <= seqStop) && !Preparer.CheckSequence(seqCheck))
                    {
                        seqStop = seqCheck - 1;
                    }
                }

                // Handling FFmpeg requests
                if (reqUri.EndsWith("a.m3u8"))
                {
                    content = lineA.Replace("[secNumber]", seqNumberA.ToString());
                    if (++seqNumberA == seqStop) content += "#EXT-X-ENDLIST\n";

                    if (!realtime)
                    {
                        bufferedTimeOfCurSeq = bufferedTimeOfCurSeq.AddSeconds(IDInfo.Duration);
                    }

                    if (CLInput.Log) Program.Log("v", "contentA", content.Replace("\n", "~"));
                }
                else if (reqUri.EndsWith("v.m3u8"))
                {
                    content = lineV.Replace("[secNumber]", seqNumberV.ToString());
                    if (++seqNumberV == seqStop) content += "#EXT-X-ENDLIST\n";

                    if (CLInput.Log) Program.Log("v", "contentV", content.Replace("\n", "~"));
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

        #region CheckDuration - Compare the requested duration and duration of the output file
        void CheckDuration(string videoFullPath,
            out bool durationChecked, out bool durationErrorFound)
        {
            durationChecked = false;
            durationErrorFound = false;

            var t = Type.GetTypeFromProgID("Shell.Application");
            dynamic s = Activator.CreateInstance(t);
            var folder = s.NameSpace(Path.GetDirectoryName(videoFullPath));
            var item = folder.ParseName(Path.GetFileName(videoFullPath));
            string dStr = folder.GetDetailsOf(item, 27).ToString();

            if (TimeSpan.TryParseExact(dStr, @"hh\:mm\:ss", null, out var dTs))
            {
                durationChecked = true;

                var durationReq = CLInput.Duration *60;
                var durationActual = (int)dTs.TotalSeconds;
                var durationDeviation = Math.Abs(durationReq - durationActual);
                if (durationDeviation > IDInfo.Duration *0.75)
                {
                    durationErrorFound = true;
                    if (IDInfo.Duration == 1 & durationDeviation == 1) durationErrorFound = false;
                }
            }
        }
        #endregion

        #region CreateOutputDir - Create output directory
        int CreateOutputDir()
        {
            if (!Directory.Exists(Program.OutputDir))
            {
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
                catch
                {
                    return 9413; // "Cannot create output folder"
                }
            }

            return 0;
        }
        #endregion

        #region DeleteOutputDir - Delete created directory if empty
        void DeleteOutputDir()
        {
            if ((outputDirCreatedRoot != string.Empty) && Directory.Exists(outputDirCreatedRoot))
            {
                try
                {
                    var files = Directory
                        .GetFiles(outputDirCreatedRoot, "*", SearchOption.AllDirectories);
                    if (files.Length == 0) Directory.Delete(outputDirCreatedRoot, true);
                }
                catch
                {
                    // ignored
                }
            }
        }
        #endregion
    }
}