using System.Diagnostics;
using System.IO;

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

            string arguments =
                "-loglevel quiet -stats -protocol_whitelist file,https,tcp,tls -i \"" +
                Preparer.fullpathMasterPlaylist + "\" -c copy \"" +
                DataInput.pathSave + filenameOutputTmp + "\"";

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
    }
}
