using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace ee.yrewind
{
    // Class for getting user input, validating and assigning user' and default input parameters
    class DataInput
    {
        public static string id = string.Empty;                                    // "-url"
        public static int resolution = 9999;                                       // "-resolution"
        public static DateTime dtStart = DateTime.MinValue;                        // "-start"
        public static int duration = 60;                                           // "-duration"
        public static string pathChrome = string.Empty;                            // "-pathchrome"
        public static string pathFfmpeg =
            Directory.GetCurrentDirectory() + "\\ffmpeg.exe";                      // "-pathffmpeg"
        public static string pathSave =
            Directory.GetCurrentDirectory() + "\\saved_streams\\";                 // "-pathsave"

        // Undocumented and unimplemented:
        public static string pathLog = string.Empty;                               // "-pathlog"
        // Undocumented; if it not default, 'Clear cache' option does not work:
        public static string pathTemp = Path.GetTempPath() + Program.title + "\\"; // "-pathtemp"

        #region GetDataInput - Perform the general function of class
        public int GetDataInput(Dictionary<string, string> argsPairs)
        {
            int resultCode = 0;
            string strTmp;
            int intTmp;

            // Parsing and checking argument pairs from cmd input line

            foreach (string arg in argsPairs.Keys)
            {
                if (arg == "url")
                {
                    strTmp = argsPairs[arg];
                    strTmp = strTmp.Trim('"').Trim('\'').TrimEnd('/');
                    strTmp = Regex.Replace(strTmp, @"\s", "");
                    if (strTmp.Length != 11)
                    {
                        strTmp = Regex.Match(
                            strTmp,
                            @"^.*(?:youtu.be\/|v\/|e\/|u\/\w+\/|embed\/|v=)([^#\&\?]*).*$"
                            ).Groups[1].Value;
                    }
                    if (strTmp.Length == 11)
                    {
                        id = strTmp;
                    }
                    else
                    {
                        resultCode = 9110; // "Check '-url' or adjacent parameters"
                        return resultCode;
                    }
                    continue;
                }

                if (arg == "resolution")
                {
                    strTmp = argsPairs[arg];
                    strTmp = strTmp.Trim('"').Trim('\'');
                    try
                    {
                        intTmp = Int32.Parse(strTmp);
                    }
                    catch
                    {
                        resultCode = 9111; // "Check '-resolution' or adjacent parameters"
                        return resultCode;
                    }
                    if (intTmp >= 0 & intTmp <= resolution)
                    {
                        resolution = intTmp;
                    }
                    else
                    {
                        resultCode = 9112; // "Value of '-resolution' parameter is out of range"
                        return resultCode;
                    }
                    continue;
                }

                if (arg == "start")
                {
                    strTmp = argsPairs[arg];
                    strTmp = strTmp.Trim('"').Trim('\'');
                    strTmp = strTmp.Replace("T", Program.dtLocal.ToString("yyyyMMdd"));
                    strTmp = strTmp.Replace("t", Program.dtLocal.ToString("yyyyMMdd"));
                    strTmp = strTmp.Replace("Y", Program.dtLocal.AddDays(-1).ToString("yyyyMMdd"));
                    strTmp = strTmp.Replace("y", Program.dtLocal.AddDays(-1).ToString("yyyyMMdd"));
                    strTmp = Regex.Replace(strTmp, @"\s", "");
                    try
                    {
                        dtStart = DateTime.ParseExact(strTmp, "yyyyMMdd:HHmm", null);
                    }
                    catch
                    {
                        resultCode = 9113; // "Check '-start' or adjacent parameters"
                        return resultCode;
                    }

                    continue;
                }

                if (arg == "duration")
                {
                    strTmp = argsPairs[arg];
                    strTmp = strTmp.Trim('"').Trim('\'');
                    try
                    {
                        intTmp = Int32.Parse(strTmp);
                    }
                    catch
                    {
                        resultCode = 9114; // "Check '-duration' or adjacent parameters"
                        return resultCode;
                    }
                    if (intTmp > 0 & intTmp <= duration)
                    {
                        duration = intTmp;
                    }
                    else
                    {
                        resultCode = 9115; // "Value of '-duration' parameter is out of range"
                        return resultCode;
                    }
                    continue;
                }

                if (arg == "pathchrome")
                {
                    strTmp = argsPairs[arg];
                    strTmp = strTmp.Trim('"').Trim('\'').TrimEnd('\\');
                    if (!strTmp.EndsWith("\\chrome.exe", true, null))
                    {
                        if (Directory.Exists(strTmp))
                        {
                            string[] files = Directory.GetFiles(strTmp, "chrome.exe",
                                SearchOption.AllDirectories);
                            strTmp = files[0];
                        }
                        else
                        {
                            resultCode = 9116; // "Check '-pathchrome' or adjacent parameters"
                            return resultCode;
                        }
                    }
                    pathChrome = strTmp;
                    if (!File.Exists(pathChrome))
                    {
                        resultCode = 9117; // "Check '-pathchrome' or adjacent parameters"
                        return resultCode;
                    }
                    continue;
                }

                if (arg == "pathffmpeg")
                {
                    strTmp = argsPairs[arg];
                    strTmp = strTmp.Trim('"').Trim('\'').TrimEnd('\\');
                    if (!strTmp.EndsWith("\\ffmpeg.exe", true, null))
                    {
                        if (Directory.Exists(strTmp))
                        {
                            string[] files = Directory.GetFiles(strTmp, "ffmpeg.exe",
                                SearchOption.AllDirectories);
                            strTmp = files[0];
                        }
                        else
                        {
                            resultCode = 9118; // "Check '-pathffmpeg' or adjacent parameters"
                            return resultCode;
                        }
                    }
                    pathFfmpeg = strTmp;
                    if (!File.Exists(pathFfmpeg))
                    {
                        resultCode = 9119; // "Check '-pathffmpeg' or adjacent parameters"
                        return resultCode;
                    }
                    continue;
                }

                if (arg == "pathlog")
                {
                    strTmp = argsPairs[arg];
                    strTmp = strTmp.Trim('"').Trim('\'').TrimEnd('\\');
                    pathLog = strTmp + "\\";
                    continue;
                }

                if (arg == "pathsave")
                {
                    strTmp = argsPairs[arg];
                    strTmp = strTmp.Trim('"').Trim('\'').TrimEnd('\\');
                    pathSave = strTmp + "\\";
                    continue;
                }

                if (arg == "pathtemp")
                {
                    strTmp = argsPairs[arg];
                    strTmp = strTmp.Trim('"').Trim('\'').TrimEnd('\\');
                    pathTemp = strTmp + "\\";
                    continue;
                }

                resultCode = 9120; // "Input string contains unknown parameter(s)"
                return resultCode;
            }

            // Parameters that are empty by default and were missing from the command line input

            if (id == string.Empty)
            {
                resultCode = 9121; // "Required parameter '-url' not found"
                return resultCode;
            }

            if (pathChrome == string.Empty)
            {
                string chromeRegistryKey = @"HKEY_CLASSES_ROOT\ChromeHTML\shell\open\command";
                string pathChromeExeInstalled =
                    (string)Registry.GetValue(chromeRegistryKey, null, null);
                if (pathChromeExeInstalled != null)
                {
                    pathChrome = pathChromeExeInstalled.Split('"')[1];
                    if (!File.Exists(pathChrome))
                    {
                        resultCode = 9122; // "Check if Chrome is installed correctly"
                        return resultCode;
                    }
                }
                else
                {
                    resultCode = 9123; // "Install Chrome or provide path to portable version"
                    return resultCode;
                }
            }

            if (pathLog == string.Empty)
            {
                // Do not create log
            }

            // Parameters that depend on each other
            if (dtStart == DateTime.MinValue)
            {
                dtStart = Program.dtLocal.AddMinutes(-duration - 1);
            }

            return resultCode;
        }
        #endregion
    }
}
