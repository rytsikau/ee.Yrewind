using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace yrewind
{
    // Various preparations based on user request and technical info about the stream
    class Preparer
    {
        // Author name / Channel title
        public static string Author { get; private set; }

        // Stream title
        public static string Title { get; private set; }

        // Available resolutions
        public static string Resolutions { get; private set; }

        // Stream start point
        // (for ongoing streams this point assumes no breaks caused by network errors,
        // so it may be later than the actual/declared stream start)
        public static DateTime IdStart { get; private set; }

        // Start and stop timestamps (used for finished streams)
        public static DateTime IdStartTimeStamp { get; private set; }
        public static DateTime IdStopTimeStamp { get; private set; }

        // Sequence duration
        public static int SeqDuration { get; private set; }

        // Duration to download
        public static int Duration { get; private set; }

        // Resolution to download
        public static int Resolution { get; private set; }

        // Start point to download
        public static DateTime Start { get; private set; }

        // Start sequence to download
        public static int StartSeq { get; private set; }

        // Direct URLs of current sequences
        public static string UriAdirect { get; private set; }
        public static string UriVdirect { get; private set; }

        // Other variables
        string hlsManifestUrl;
        int currentSeq = -1;
        DateTime currentSeqUtc;

        #region Common - Main method of the class
        public int Common()
        {
            int code;
            Author = string.Empty;
            Title = string.Empty;
            Resolutions = string.Empty;
            UriAdirect = string.Empty;
            UriVdirect = string.Empty;

            ParseHtmlJson();

            if (Author == string.Empty || Title == string.Empty)
            {
                code = GetInfoWithOembed();
                if (code != 0) return code;
            }

            if (!string.IsNullOrEmpty(Validator.Browser) & (
                Resolutions == string.Empty ||
                UriAdirect == string.Empty ||
                UriVdirect == string.Empty))
            {
                code = BrowserGetNetlog(out string netlog);
                if (code != 0) return code;

                code = BrowserParseNetlog(netlog);
                if (code != 0) return code;
            }

            GetInfoWithAsegment();

            if (!string.IsNullOrEmpty(hlsManifestUrl) &&
                (SeqDuration == default || IdStart == default)) GetInfoWithHls();

            if (Validator.Log)
            {
                var logInfo =
                    "\nAuthor: " + Author +
                    "\nTitle: " + Title +
                    "\nResolutions: " + Resolutions +
                    "\nUriAdirect: " + UriAdirect +
                    "\nUriVdirect: " + UriVdirect +
                    "\nSeqDuration: " + SeqDuration +
                    "\nIdStart: " + IdStart +
                    "\nIdStartTimeStamp: " + IdStartTimeStamp +
                    "\nIdStopTimeStamp: " + IdStopTimeStamp
                    ;
                Program.Log(logInfo);
            }

            // Just in case, check again
            if (string.IsNullOrEmpty(Author) ||
                string.IsNullOrEmpty(Title) ||
                string.IsNullOrEmpty(Resolutions) ||
                string.IsNullOrEmpty(UriAdirect) ||
                string.IsNullOrEmpty(UriVdirect) ||
                SeqDuration == default ||
                IdStart == default)
            {
                // "Cannot get live stream information"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9310;
            }

            code = GetDuration();
            if (code != 0) return code;

            code = GetResolution();
            if (code != 0) return code;

            if (Resolution > 0)
            {
                code = CorrectUriVdirect();
                if (code != 0) return code;
            }

            code = FindStart();
            if (code != 0) return code;

            if (Validator.Log)
            {
                var logInfo =
                    "\nResolution: " + Resolution +
                    "\nStart: " + Start +
                    "\nStart sequence: " + StartSeq +
                    "\nUriVdirect corrected: " + UriVdirect
                    ;
                Program.Log(logInfo);
            }

            return 0;
        }
        #endregion

        #region ParseHtmlJson - Determine multiply values from HTML JSON
        void ParseHtmlJson()
        {
            try
            {
                Author = Waiter.JsonHtml.XPathSelectElement("//author").Value;
                Author = Replace_InvalidChars(Author);
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);
            }

            try
            {
                Title = Waiter.JsonHtml.XPathSelectElement("//title").Value;
                Title = Replace_InvalidChars(Title);
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);
            }

            try
            {
                var tmp = Waiter.JsonHtml.XPathSelectElements
                    ("//adaptiveFormats/*/height").Select(x => x.Value).Distinct();
                Resolutions = string.Join(",", tmp);
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);
            }

            try
            {
                var tmp = Waiter.JsonHtml.XPathSelectElement("//startTimestamp").Value;
                tmp = tmp.Replace(" 00:00", "Z");
                IdStartTimeStamp = DateTime.Parse(tmp).ToLocalTime();
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);
            }

            try
            {
                var tmp = Waiter.JsonHtml.XPathSelectElement("//endTimestamp").Value;
                tmp = tmp.Replace(" 00:00", "Z");
                IdStopTimeStamp = DateTime.Parse(tmp).ToLocalTime();
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);
            }

            try
            {
                SeqDuration =
                    int.Parse(Waiter.JsonHtml.XPathSelectElement("//targetDurationSec").Value);
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);
            }

            try
            {
                UriAdirect = Waiter.JsonHtml.XPathSelectElement
                    ("//adaptiveFormats/*/url[contains(text(),'mime=audio')]").Value;
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);
            }

            try
            {
                UriVdirect = Waiter.JsonHtml.XPathSelectElement
                    ("//adaptiveFormats/*/url[contains(text(),'mime=video')]").Value;
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);
            }

            try
            {
                hlsManifestUrl = Waiter.JsonHtml.XPathSelectElement("//hlsManifestUrl").Value;
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);
            }
        }
        #endregion

        #region GetInfoWithOembed - Determine [Author] and [Title] with oembed page
        int GetInfoWithOembed()
        {
            var content = string.Empty;
            XElement jsonOembed;

            try
            {
                var uri = Constants.UrlStreamOembed.Replace("[stream_id]", Waiter.Id);

                using (var wc = new WebClient())
                {
                    content = wc.DownloadString(new Uri(uri));
                }
            }
            catch (WebException e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                // "Cannot get live stream information"
                return 9310;
            }

            if (Validator.Log) Program.Log(content, "oembed");

            try
            {
                var tmp1 = Encoding.UTF8.GetBytes(HttpUtility.UrlDecode(content));
                var tmp2 = JsonReaderWriterFactory
                    .CreateJsonReader(tmp1, new XmlDictionaryReaderQuotas());
                jsonOembed = XElement.Load(tmp2);
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                // "Cannot get live stream information"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9310;
            }

            try
            {
                Author = jsonOembed.XPathSelectElement("//author_name").Value;
                Author = Replace_InvalidChars(Author);

                Title = jsonOembed.XPathSelectElement("//title").Value;
                Title = Replace_InvalidChars(Title);
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                // "Cannot get live stream information"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9310;
            }

            return 0;
        }
        #endregion

        #region BrowserGetNetlog - Get content of browser network log
        int BrowserGetNetlog(out string netlog)
        {
            netlog = string.Empty;
            var browser = Validator.Browser;

            var pathNetlog =
                Path.GetTempPath() + Constants.Name + "_netlog_" + Constants.RandomString + ".tmp";
            var args = Constants.UrlStream.Replace("[stream_id]", Waiter.Id) +
                " --headless --disable-extensions --disable-gpu --mute-audio --no-sandbox" +
                " --log-net-log=\"" + pathNetlog + "\"";
            int attempt = 5;

            while (attempt-- > 0)
            {
                try
                {
                    using (var p = new Process())
                    {
                        p.StartInfo.FileName = browser;
                        p.StartInfo.Arguments = args;
                        p.Start();
                        p.WaitForExit();
                    }
                }
                catch (Exception e)
                {
                    Program.ErrInfo =
                        new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                    if (Validator.Log) Program.Log(Program.ErrInfo);

                    continue;
                }

                try
                {
                    netlog = File.ReadAllText(pathNetlog);
                    File.Delete(pathNetlog);
                }
                catch (Exception e)
                {
                    Program.ErrInfo =
                        new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                    if (Validator.Log) Program.Log(Program.ErrInfo);

                    continue;
                }

                if (Validator.Log) Program.Log(netlog, "netlog_" + attempt);

                if (netlog.Contains("&sq=") &
                    netlog.Contains("mime=video") &
                    netlog.Contains("mime=audio")) return 0;

                Thread.Sleep(5000);
            }

            // "Cannot get live stream information with browser"
            Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
            return 9311;
        }
        #endregion

        #region BrowserParseNetlog - Determine [Resolutions] and direct URLs with browser netlog
        int BrowserParseNetlog(string netlog)

        {
            // Get [UriAdirect] and [UriVdirect]
            foreach (var line in netlog.Split('\n'))
            {
                if (UriAdirect == string.Empty)
                {
                    UriAdirect = Regex.Match(
                        line,
                        "^.*?(https[^\"]+mime=audio[^\"]+).*$",
                        RegexOptions.IgnoreCase).Groups[1].Value;
                }

                if (UriVdirect == string.Empty)
                {
                    UriVdirect = Regex.Match(
                        line,
                        "^.*?(https[^\"]+mime=video[^\"]+).*$",
                        RegexOptions.IgnoreCase).Groups[1].Value;
                }

                if ((UriAdirect != string.Empty) & (UriVdirect != string.Empty))
                {
                    UriAdirect = Regex.Replace(UriAdirect, @"&sq=\d+", "");
                    UriVdirect = Regex.Replace(UriVdirect, @"&sq=\d+", "");
                    break;
                }
            }
            if (UriAdirect == string.Empty || UriVdirect == string.Empty)
            {
                // "Cannot get live stream information with browser"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9311;
            }

            // Get Resolutions from [UriVdirect]
            try
            {
                var resolutions = new ArrayList();
                var itags = Regex.Match(UriVdirect, @".*&aitags=([^&]+).*").Groups[1].Value;

                foreach (var itag in itags.Replace("%2C", ",").Split(','))
                {
                    if (!int.TryParse(itag, out var itagNumber)) continue;
                    if (!Constants.Itag.TryGetValue(itagNumber, out var value)) continue;
                    var resolutionStr = value.Split(';')[2];
                    if (resolutionStr.Contains("p"))
                    {
                        var resolution = int.Parse(resolutionStr.Split('p')[0]);
                        if (!resolutions.Contains(resolution)) resolutions.Add(resolution);
                    }
                }
                resolutions.Sort();
                resolutions.Reverse();
                Resolutions = string.Join(",", resolutions.ToArray());
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                // "Cannot get live stream information with browser"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9311;
            }

            return 0;
        }
        #endregion

        #region GetInfoWithAsegment - Determine [SeqDuration] and [IdStart] with audio segment
        void GetInfoWithAsegment()
        {
            string content;

            try
            {
                using (var wc = new WebClient())
                {
                    content = wc.DownloadString(new Uri(UriAdirect));
                }
            }
            catch (WebException e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                return;
            }

            if (Validator.Log) Program.Log(content, "segmentA");

            // Parse audio segment as text - get sequence number, its UTC time and duration
            if (SeqDuration == default)
            {
                try
                {
                    var tmp = Regex.Match(content, @".*Target-Duration-Us: (\d+).*")
                        .Groups[1].Value;
                    SeqDuration = int.Parse(tmp) / 1000000;
                }
                catch (Exception e)
                {
                    Program.ErrInfo =
                        new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                    if (Validator.Log) Program.Log(Program.ErrInfo);

                    SeqDuration = default;
                }
            }
            if (currentSeq == -1)
            {
                try
                {
                    // For finished streams it's number of last sequence
                    var tmp = Regex.Match(content, @".*Sequence-Number: (\d+).*")
                        .Groups[1].Value;
                    currentSeq = int.Parse(tmp);
                }
                catch (Exception e)
                {
                    Program.ErrInfo =
                        new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                    if (Validator.Log) Program.Log(Program.ErrInfo);

                    currentSeq = -1;
                }
            }
            if (currentSeqUtc == default)
            {
                try
                {
                    // For finished streams it's time of last sequence
                    var tmp = Regex.Match(content, @".*Ingestion-Walltime-Us: (\d+).*")
                        .Groups[1].Value;
                    currentSeqUtc = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    currentSeqUtc = currentSeqUtc.AddSeconds(long.Parse(tmp) / 1000000);
                }
                catch (Exception e)
                {
                    Program.ErrInfo =
                        new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                    if (Validator.Log) Program.Log(Program.ErrInfo);

                    currentSeqUtc = default;
                }
            }

            if (SeqDuration == default || currentSeq == -1 || currentSeqUtc == default)
            {
                return;
            }

            IdStart = currentSeqUtc.AddSeconds(-currentSeq * SeqDuration).ToLocalTime();
        }
        #endregion

        #region GetInfoWithHls - Determine [SeqDuration] and [IdStart] with HLS manifest / playlist
        void GetInfoWithHls()
        {
            string content;
            string hlsPlaylistUrl = default;

            // Download HLS manifest
            try
            {
                using (var wc = new WebClient())
                {
                    content = wc.DownloadString(new Uri(hlsManifestUrl));
                }
            }
            catch (WebException e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                return;
            }

            if (Validator.Log) Program.Log(content, "m3u8_1");

            // Find HLS playlist URL in the manifest
            foreach (var line in content.Split('\n'))
            {
                if (Regex.IsMatch(line, @"^https.+m3u8$"))
                {
                    hlsPlaylistUrl = line;
                    break;
                }
            }

            // Download HLS playlist
            try
            {
                using (var wc = new WebClient())
                {
                    content = wc.DownloadString(new Uri(hlsPlaylistUrl));
                }
            }
            catch (WebException e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                return;
            }

            if (Validator.Log) Program.Log(content, "m3u8_2");

            // Parse HLS playlist
            if (SeqDuration == default)
            {
                try
                {
                    var tmp = Regex.Match(content, @".*#EXT-X-TARGETDURATION:(\d+).*")
                        .Groups[1].Value;
                    SeqDuration = int.Parse(tmp);
                }
                catch (Exception e)
                {
                    Program.ErrInfo =
                        new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                    if (Validator.Log) Program.Log(Program.ErrInfo);

                    SeqDuration = default;
                }
            }
            if (currentSeq == -1)
            {
                try
                {
                    // For finished streams it's number of first sequence
                    var tmp = Regex.Match(content, @".*#EXT-X-MEDIA-SEQUENCE:(\d+).*")
                        .Groups[1].Value;
                    currentSeq = int.Parse(tmp);
                }
                catch (Exception e)
                {
                    Program.ErrInfo =
                        new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                    if (Validator.Log) Program.Log(Program.ErrInfo);

                    currentSeq = -1;
                }
            }
            if (currentSeqUtc == default)
            {
                try
                {
                    // For finished streams it's time of first sequence
                    var tmp = Regex.Match(content, @".*#EXT-X-PROGRAM-DATE-TIME:(.+)\+.*")
                        .Groups[1].Value;
                    currentSeqUtc = DateTime.Parse(tmp);
                }
                catch (Exception e)
                {
                    Program.ErrInfo =
                        new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                    if (Validator.Log) Program.Log(Program.ErrInfo);

                    currentSeqUtc = default;
                }
            }

            if (SeqDuration == default || currentSeq == -1 || currentSeqUtc == default)
            {
                return;
            }

            // Sometimes HLS playlist contains incorrect UTC time of the current sequence.
            // Fixing this, we cannot demand an exact match, because the stream is always
            // broadcast with a delay of several tens of seconds.
            // So, we will set an acceptable margin, for example, 3 minutes.
            if ((Waiter.IdStatus != Waiter.Stream.Finished) &&
                ((DateTime.UtcNow - currentSeqUtc) > TimeSpan.FromMinutes(3)))
            {
                currentSeqUtc = Program.Start.ToUniversalTime();
                Program.ResultIsOK = false;
            }

            IdStart = currentSeqUtc.AddSeconds(-currentSeq * SeqDuration).ToLocalTime();
        }
        #endregion

        #region GetDuration - Determine [Duration]
        int GetDuration()
        {
            double numberOfSequences;

            if (Waiter.IdStatus == Waiter.Stream.Finished)
            {
                numberOfSequences =
                    (IdStopTimeStamp - IdStartTimeStamp).TotalSeconds / SeqDuration;
            }
            else
            {
                numberOfSequences =
                    double.Parse(Validator.Duration) / SeqDuration;
            }

            // Round up to the greater number of sequences
            Duration = (int)Math.Ceiling(numberOfSequences) * SeqDuration;

            return 0;
        }
        #endregion

        #region GetResolution - Determine [Resolution] to download
        int GetResolution()
        {
            // To save audio only
            if ((Validator.OutputExt == ".aac") ||
                (Validator.OutputExt == ".m4a") ||
                (Validator.OutputExt == ".wma") ||
                (Validator.Resolution == "0"))
            {
                Resolution = 0;
                return 0;
            }

            // Other formats
            try
            {
                var resolutionTmp = 0;

                foreach (var item in Preparer.Resolutions.Split(','))
                {
                    resolutionTmp = int.Parse(item);
                    if (resolutionTmp > int.Parse(Validator.Resolution)) continue;
                    else break;
                }

                Resolution = resolutionTmp;
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                // "Cannot get live stream information"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9310;
            }

            // Resolutions above 1080 are only available in VP9 adaptive format ('webm'),
            // but for some media containers, FFmpeg cannot store VP9 and MP4a data together.
            // Therefore, we will use for them 1080 even if a higher was requested
            if (Resolution > 1080)
            {
                if ((Validator.OutputExt == ".3gp") ||
                    (Validator.OutputExt == ".flv") ||
                    (Validator.OutputExt == ".mov") ||
                    (Validator.OutputExt == ".ts"))
                {
                    Resolution = 1080;
                }
            }

            return 0;
        }
        #endregion

        #region CorrectUriVdirect - Get [UriVdirect] with required 'itag'
        int CorrectUriVdirect()
        {
            // Get [UriVdirect] with the correct 'itag' parameter,
            // according to the selected resolution and preferred adaptive format

            var code = 0;

            try
            {
                var items = Waiter.JsonHtml.XPathSelectElements(
                    "//adaptiveFormats/*/height[contains(text(),'" + Resolution + "')]");

                foreach (var item in items)
                {
                    if (item.Parent.Element("height").Value != Resolution.ToString()) continue;

                    UriVdirect = item.Parent.Element("url").Value;

                    // For ASF, MKV and WMV containers prefer 'webm' format
                    if ((Validator.OutputExt == ".asf") ||
                        (Validator.OutputExt == ".mkv") ||
                        (Validator.OutputExt == ".wmv"))
                    {
                        if (item.Parent.Element("mimeType").Value.Contains("webm")) break;
                        else continue;
                    }

                    // For others prefer 'mp4'
                    if (item.Parent.Element("mimeType").Value.Contains("mp4")) break;
                }
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                // Possibly direct URLs are taken from browser' netlog
                CorrectUriVdirect_Browser();
                return 0;
            }

            return code;
        }
        #endregion

        #region CorrectUriVdirect_Browser - Get [UriVdirect] with required 'itag' using netlog
        int CorrectUriVdirect_Browser()
        {
            var itag = string.Empty;
            var itags = Regex.Match(UriVdirect, @".*&aitags=([^&]+).*").Groups[1].Value;
            var preferredAdaptiveFormat = "mp4";
            if ((Validator.OutputExt == ".asf") ||
                (Validator.OutputExt == ".mkv") ||
                (Validator.OutputExt == ".wmv"))
            {
                // For ASF, MKV and WMV containers prefer 'webm' format
                preferredAdaptiveFormat = "webm";
            }

            try
            {
                foreach (var item in itags.Replace("%2C", ",").Split(','))
                {
                    if (!int.TryParse(item, out var itemNumber)) continue;
                    if (!Constants.Itag.TryGetValue(itemNumber, out var itemDescr)) continue;
                    var itemResolutionStr = itemDescr.Split(';')[2];
                    var itemResolution = int.Parse(itemResolutionStr.Split('p')[0]);
                    if (itemResolution == Resolution)
                    {
                        itag = item;
                        if (itemDescr.Contains(preferredAdaptiveFormat)) break;
                    }
                }
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                // "Cannot get live stream information with browser"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9311;
            }

            if (itag != string.Empty)
            {
                UriVdirect = Regex.Replace(UriVdirect, @"&itag=\d+", @"&itag=" + itag);
            }
            else
            {
                // "Cannot get live stream information with browser"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9311;
            }

            return 0;
        }
        #endregion

        #region FindStart - Determine [Start] and [StartSeq]
        int FindStart()
        {
            // Internal using in *getnext* mode
            if (Validator.Start.StartsWith("seq"))
            {
                StartSeq = int.Parse(Validator.Start.Replace("seq", ""));

                if (FindStart_CheckSeq(StartSeq))
                {
                    Start = IdStart.AddSeconds(StartSeq * SeqDuration);
                    return 0;
                }
                else
                {
                    // "Further parts of the stream are unavailable"
                    Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                    return 9312;
                }
            }

            // Determine [Start]
            if (string.IsNullOrEmpty(Validator.Start))
            {
                if (Waiter.IdStatus == Waiter.Stream.Ongoing) Start = Program.Start;
                else Start = IdStart;
            }
            else if (Validator.Start == Constants.StartBeginning)
            {
                Start = IdStart;
            }
            else if (Validator.Start.StartsWith("-"))
            {
                if (Waiter.IdStatus == Waiter.Stream.Upcoming)
                {
                    Start = IdStart;
                }
                else if (Waiter.IdStatus == Waiter.Stream.Ongoing)
                {
                    Start = Program.Start.AddMinutes(int.Parse(Validator.Start));
                    if (Start < IdStart) Start = IdStart;
                }
                else if (Waiter.IdStatus == Waiter.Stream.Finished)
                {
                    Start = IdStart;
                }
            }
            else if (Validator.Start.StartsWith("+"))
            {
                if (Waiter.IdStatus == Waiter.Stream.Upcoming)
                {
                    Start = IdStart.AddMinutes(int.Parse(Validator.Start));
                }
                else if (Waiter.IdStatus == Waiter.Stream.Ongoing)
                {
                    Start = Program.Start.AddMinutes(int.Parse(Validator.Start));
                }
                else if (Waiter.IdStatus == Waiter.Stream.Finished)
                {
                    Start = IdStart.AddMinutes(int.Parse(Validator.Start));
                }
            }
            else
            {
                Start = DateTime.ParseExact(Validator.Start, "yyyyMMdd:HHmmss", null);

                // Round up to the beginning of the next sequence
                if (Start > IdStart)
                {
                    var numberOfSequences = (int)((Start - IdStart).TotalSeconds / SeqDuration);
                    Start = IdStart.AddSeconds(numberOfSequences * SeqDuration + SeqDuration);
                }
            }

            // Some checks and corrections
            if (Start < IdStart)
            {
                // "Requested time interval isn't available"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() +
                    " - requested " + Start + " / stream started " + IdStart;
                return 9313;
            }
            else if (Waiter.IdStatus == Waiter.Stream.Ongoing)
            {
                // For long ongoing streams, sequences 'aged' 167-168 hours
                // become unavailable randomly (not in order),
                // so we should to trim the unstable interval with a margin
                var ago167 = (Program.Start + Program.Timer.Elapsed).AddHours(-167);
                if (Start < ago167)
                {
                    if (Validator.Start == Constants.StartBeginning)
                    {
                        Start = ago167;
                    }
                    else
                    {
                        // "Requested time interval isn't available"
                        Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() +
                            " - requested " + Start + " / available " + ago167;
                        return 9313;
                    }
                }
            }

            // Determine [StartSeq] (round up to the beginning of the next sequence)
            StartSeq = (int)Math.Ceiling((Start - IdStart).TotalSeconds / SeqDuration);

            // If the sequence is not YET available, just return to Program
            if ((Program.Start + Program.Timer.Elapsed) <
                Start.AddSeconds(Constants.RealTimeBufferSeconds))
            {
                return 0;
            }
            else if (!FindStart_CheckSeq(StartSeq))
            {
                // "Requested time interval isn't available"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9313;
            }

            return 0;
        }
        #endregion

        #region FindStart_CheckSeq - Check if the specified sequence available
        public static bool FindStart_CheckSeq(int seqNum)
        {
            var code = false;
            string result;

            // Check if audio sequence available
            var uriA = UriAdirect + "&sq=" + seqNum;
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(new Uri(uriA));
                var res = (HttpWebResponse)req.GetResponse();
                if (res.StatusCode == HttpStatusCode.OK) code = true;
                result = res.StatusCode.ToString();
                res.Close();
            }
            catch (WebException e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                result = e.Message;
            }

            if (Validator.Log)
            {
                Program.Log("Checking audio sequence " + seqNum + "... " + result);
            }

            if (code) return code;

            // For a case of any error, try to check also video sequence
            var uriV = UriVdirect + "&sq=" + seqNum;
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(new Uri(uriV));
                var res = (HttpWebResponse)req.GetResponse();
                if (res.StatusCode == HttpStatusCode.OK) code = true;
                result = res.StatusCode.ToString();
                res.Close();
            }
            catch (WebException e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                result = e.Message;
            }

            if (Validator.Log)
            {
                Program.Log("Checking video sequence " + seqNum + "... " + result);
            }

            return code;
        }
        #endregion

        #region Replace_InvalidChars - Replace characters that illegal in paths and filenames
        string Replace_InvalidChars(string input)
        {
            string output = input;

            output = string.Join("_", output.Split(Path.GetInvalidFileNameChars()));
            output = string.Join("_", output.Split(Path.GetInvalidPathChars()));

            return output;
        }
        #endregion
    }
}