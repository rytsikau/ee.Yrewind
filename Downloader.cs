using System.Diagnostics;
using System.IO;
using System.Net;

namespace ee.yrewind
{
    // Class for downloading/saving the output video and checking result
    class Downloader
    {
        #region GetVideo - Perform the general function of class
        public int GetVideo()
        {
            int resultCode = 0;

            string filenameOutputTmp =
                Preparer.filenameOutput.Replace(".mp4", "~" + Program.randomString + ".mp4");

            string addingEmbedCover = string.Empty;
            if (GetEmbedCover(out string embedCoverFullPath))
            {
                addingEmbedCover = "-i \"" + embedCoverFullPath +
                    "\" -map 1 -map 0 -disposition:v:0 attached_pic ";
            }

            string arguments =
                "-loglevel quiet" + " " +
                "-stats" + " " +
                "-protocol_whitelist file,https,tcp,tls" + " " +
                "-i \"" + Preparer.fullpathMasterPlaylist + "\"" + " " +
                addingEmbedCover +
                "-metadata title=\"" + IDInfo.title + "\"" + " " +
                "-metadata comment=\"saved with " + Program.title + "\"" + " " +
                "-c copy" + " " +
                "\"" + DataInput.pathSave + filenameOutputTmp + "\"";

            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = DataInput.pathFfmpeg;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.Start();
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        File.Move(DataInput.pathSave + filenameOutputTmp,
                            DataInput.pathSave + Preparer.filenameOutput);
                    }
                }
            }
            catch
            {
                resultCode = 9410; // "FFmpeg library not responding"
                return resultCode;
            }

            if (!File.Exists(DataInput.pathSave + Preparer.filenameOutput))
            {
                resultCode = 9411; // "Cannot process livestream with FFmpeg library"
            }

            return resultCode;
        }
        #endregion

        #region GetEmbedCover - Download livestream embed cover
        bool GetEmbedCover(out string embedCoverFullPath)
        {
            embedCoverFullPath = DataInput.pathTemp + Program.randomString + ".jpg";

            if (File.Exists(embedCoverFullPath))
            {
                return true;
            }

            string embedCoverUrl = "https://img.youtube.com/vi/" + DataInput.id + "/0.jpg";
            try
            {
                WebClient stream = new WebClient();
                stream.DownloadFile(embedCoverUrl, embedCoverFullPath);
            }
            catch { }

            if (File.Exists(embedCoverFullPath))
            {
                return true;
            }

            return false;
        }
        #endregion
    }
}
