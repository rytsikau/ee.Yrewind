using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace yrewind
{
    // Various preparations based on command line request and technical info about the stream
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

            if (!string.IsNullOrEmpty(Waiter.UriAdirect) & !string.IsNullOrEmpty(Waiter.UriVdirect))
            {
                ParseBrowserUris();
            }

            ParseHtmlJson();

            if (Author == string.Empty || Title == string.Empty)
            {
                code = GetInfoWithOembed();
                if (code != 0) return code;
            }

            GetInfoWithAsegment();

            if (!string.IsNullOrEmpty(hlsManifestUrl) &&
                (SeqDuration == default || IdStart == default)) GetInfoWithHls();

            if (Validator.Log)
            {
                var logInfo =
                    "\nPreparer 1:" +
                    "\nAuthor: " + Author +
                    "\nDuration: " + Duration +
                    "\nIdStart: " + IdStart +
                    "\nIdStartTimeStamp: " + IdStartTimeStamp +
                    "\nIdStopTimeStamp: " + IdStopTimeStamp +
                    "\nResolution: " + Resolution +
                    "\nResolutions: " + Resolutions +
                    "\nSeqDuration: " + SeqDuration +
                    "\nStart: " + Start +
                    "\nStartSeq: " + StartSeq +
                    "\nTitle: " + Title +
                    "\nUriAdirect: " + UriAdirect +
                    "\nUriVdirect: " + UriVdirect;
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
                    "\nPreparer 2:" +
                    "\nAuthor: " + Author +
                    "\nDuration: " + Duration +
                    "\nIdStart: " + IdStart +
                    "\nIdStartTimeStamp: " + IdStartTimeStamp +
                    "\nIdStopTimeStamp: " + IdStopTimeStamp +
                    "\nResolution: " + Resolution +
                    "\nResolutions: " + Resolutions +
                    "\nSeqDuration: " + SeqDuration +
                    "\nStart: " + Start +
                    "\nStartSeq: " + StartSeq +
                    "\nTitle: " + Title +
                    "\nUriAdirect: " + UriAdirect +
                    "\nUriVdirect: " + UriVdirect;
                Program.Log(logInfo);
            }

            return 0;
        }
        #endregion

        #region ParseBrowserUris - Get [Resolutions], [UriAdirect], [UriVdirect] from browser netlog
        int ParseBrowserUris()
        {
            // Get [UriAdirect] and [UriVdirect]
            UriAdirect = Regex.Replace(Waiter.UriAdirect, @"&sq=\d+", "");
            UriVdirect = Regex.Replace(Waiter.UriVdirect, @"&sq=\d+", "");

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

        #region ParseHtmlJson - Get various values from HTML JSON
        void ParseHtmlJson()
        {
            if (Author == string.Empty)
            {
                try
                {
                    Author = Waiter.JsonHtml.XPathSelectElement("//author").Value;
                    Author = Program.Replace_InvalidChars(Author);
                }
                catch (Exception e)
                {
                    Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                    if (Validator.Log) Program.Log(Program.ErrInfo);
                }
            }

            if (Title == string.Empty)
            {
                try
                {
                    Title = Waiter.JsonHtml.XPathSelectElement("//title").Value;
                    Title = Program.Replace_InvalidChars(Title);
                }
                catch (Exception e)
                {
                    Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                    if (Validator.Log) Program.Log(Program.ErrInfo);
                }

            }

            if (Resolutions == string.Empty)
            {
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

            }

            if (IdStartTimeStamp == default)
            {
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

            }

            if (IdStopTimeStamp == default)
            {
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

            }

            if (SeqDuration == default)
            {
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

            }

            if (UriAdirect == string.Empty)
            {
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

            }

            if (UriVdirect == string.Empty)
            {
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

            }

            if (hlsManifestUrl == string.Empty)
            {
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
        }
        #endregion

        #region GetInfoWithOembed - Get [Author] and [Title] from oembed page
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
                Author = Program.Replace_InvalidChars(Author);

                Title = jsonOembed.XPathSelectElement("//title").Value;
                Title = Program.Replace_InvalidChars(Title);
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

        #region GetInfoWithAsegment - Get [SeqDuration] and [IdStart] from audio segment
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

        #region GetInfoWithHls - Get [SeqDuration] and [IdStart] from HLS manifest and playlist
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
            // broadcast with delay of several tens of seconds.
            // So, we will set an acceptable margin, for example, 3 minutes.
            if ((Waiter.IdStatus != Waiter.Stream.Finished) &&
                ((DateTime.UtcNow - currentSeqUtc) > TimeSpan.FromMinutes(3)))
            {
                currentSeqUtc = Program.Start.ToUniversalTime() + Program.Timer.Elapsed;
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
            if (Duration > Constants.DurationMax) Duration -= SeqDuration;

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

                // "Cannot get live stream information"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9310;
            }

            if (itag != string.Empty)
            {
                UriVdirect = Regex.Replace(UriVdirect, @"&itag=\d+", @"&itag=" + itag);
            }
            else
            {
                // "Cannot get live stream information"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9310;
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

                if (CheckSeq(StartSeq) == "200")
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
                // become unavailable randomly (not in order), so we trim the unstable interval
                var earliestStable = (Program.Start + Program.Timer.Elapsed).AddHours(-Constants.RewindMaxHours);
                if (Start < earliestStable)
                {
                    if (Validator.Start == Constants.StartBeginning)
                    {
                        Start = earliestStable;
                    }
                    else
                    {
                        // "Requested time interval isn't available"
                        Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() +
                            " - requested " + Start + " / available " + earliestStable;
                        return 9313;
                    }
                }
            }

            // Determine [StartSeq] (round up to the beginning of the next sequence)
            StartSeq = (int)Math.Ceiling((Start - IdStart).TotalSeconds / SeqDuration);

            // If the sequence is not YET available, just return to Program
            if ((Program.Start + Program.Timer.Elapsed) <
                Start.AddSeconds(Constants.RealTimeBuffer))
            {
                return 0;
            }
            else if (CheckSeq(StartSeq) != "200")
            {
                // "Requested time interval isn't available"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9313;
            }

            return 0;
        }
        #endregion

        #region CheckSeq - Check availability of the specified sequence
        public static string CheckSeq(object arg)
        {
            // Input argument as <object> to be able to use this method as a task
            int seqNum = (int)arg;

            string result;

            // Check audio sequence
            var uriA = UriAdirect + "&sq=" + seqNum;
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(new Uri(uriA));
                var res = (HttpWebResponse)req.GetResponse();
                result = ((int)res.StatusCode).ToString();
                res.Close();
            }
            catch (WebException e)
            {
                result = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
            }

            if (Validator.Log)
            {
                Program.Log("Check status of audio sequence " + seqNum + "... " + result);
            }

            if (result == "200") return result;

            // If any error, check also video sequence
            var uriV = UriVdirect + "&sq=" + seqNum;
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(new Uri(uriV));
                var res = (HttpWebResponse)req.GetResponse();
                result = ((int)res.StatusCode).ToString();
                res.Close();
            }
            catch (WebException e)
            {
                result = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
            }

            if (Validator.Log)
            {
                Program.Log("Check status of video sequence " + seqNum + "... " + result);
            }

            return result;
        }
        #endregion
    }
}