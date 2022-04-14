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
    // Class for getting the necessary info about the stream
    class IDInfo
    {
        // Stream ID
        public static string Id { get; private set; } = string.Empty;

        // Channel ID
        public static string ChannelId { get; private set; } = string.Empty;

        // Channel name (author)
        public static string Author { get; private set; } = string.Empty;

        // Stream title
        public static string Title { get; private set; } = string.Empty;

        // Sequence duration
        public static int Duration { get; private set; }

        // Direct URLs
        public static string UriAdirect { get; private set; } = string.Empty;
        public static string UriVdirect { get; private set; } = string.Empty;

        // Available resolutions
        public static string Resolutions { get; private set; } = string.Empty;

        // Stream start point
        // (assumes no interruptions caused by network errors,
        // so in practice this point is later than the actual stream start time)
        public static DateTime Start { get; private set; }

        // XML-wrapped JSON object created from stream HTML page
        public static XElement JsonHtml { get; private set; }

        // Other variables
        int curSeq; // Number of current sequence
        DateTime curSeqUtc; // UTC time of current sequence
        string hlsManifestUrl = string.Empty; // URL of HLS manifest

        #region Common - Main method of the class
        public int Common()
        {
            int code;

            // Get stream ID
            if (CLInput.Url.StartsWith("https://www.youtube.com/watch?v="))
            {
                Id = CLInput.Url.Replace("https://www.youtube.com/watch?v=", "");
            }
            else if (!CLInput.StartWait)
            {
                // "Channel URL can only be specified with '-start=wait' option"
                return 9214;
            }
            else
            {
                code = GetChannelId(CLInput.Url);
                if (code != 0) return code;

                code = WaitForStream();
                if (code != 0) return code;
            }

            code = GetHtmlJson();
            if (code != 0) return code;

            try
            {
                var status = JsonHtml.XPathSelectElement("//status").Value;
                if (status != "OK")
                {
                    throw new Exception();
                }
            }
            catch
            {
                code = 9238; // "Video not found"
                return code;
            }

            try
            {
                var isLiveNow = JsonHtml.XPathSelectElement("//isLiveNow").Value;
                if (isLiveNow != "true")
                {
                    throw new Exception();
                } 
            }
            catch
            {
                code = 9239; // "It is not a live stream"
                return code;
            }

            code = ParseHtmlJson();
            if (code != 0) return code;

            if (ChannelId == string.Empty ||
                Author == string.Empty ||
                Title == string.Empty)
            {
                code = GetInfoWithOembed();
                if (code != 0) return code;
            }

            if ((CLInput.Browser != null) & (
                Duration == default ||
                Resolutions == string.Empty ||
                UriAdirect == string.Empty ||
                UriVdirect == string.Empty))
            {
                code = GetBrowserNetlog(out string netlog);
                if (code != 0) return code;

                code = ParseBrowserNetlog(netlog);
                if (code != 0) return code;
            }

            if (hlsManifestUrl != string.Empty) GetInfoWithHls();

            if (Duration == default || Start == default) GetInfoWithAsegment();

            if (Id == string.Empty ||
                ChannelId == string.Empty ||
                Author == string.Empty ||
                Title == string.Empty ||
                Duration == default ||
                UriAdirect == string.Empty ||
                UriVdirect == string.Empty ||
                Resolutions == string.Empty ||
                Start == default ||
                JsonHtml == default)
            {
                return 9241; // "Cannot get live stream information"
            }

            return code;
        }
        #endregion

        #region GetChannelId - Determine channel ID (by checking if channel exists)
        int GetChannelId(string url)
        {
            // Input variants:
            // "https://www.youtube.com/channel/[channelId]"
            // "https://www.youtube.com/c/[channelTitle]"
            // "https://www.youtube.com/user/[authorName]"

            if (url.StartsWith("https://www.youtube.com/channel/"))
            {
                ChannelId = url.Replace("https://www.youtube.com/channel/", "");

                try
                {
                    using (var wc = new WebClient())
                    {
                        var uri = Constants.UrlChannelCheck.Replace("[channel_id]", ChannelId);
                        wc.DownloadString(new Uri(uri));
                    }
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("404")) return 9223; // "URL not found"
                    else return 9213; // "Cannot get channel information"
                }
            }
            else
            {
                var content = string.Empty;

                try
                {
                    using (var wc = new WebClient())
                    {
                        content = wc.DownloadString(new Uri(url));
                    }
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("404"))
                    {
                        // "URL not found. If it contains '%' characters, are they escaped?"
                        return 9221;
                    }
                    else
                    {
                        // "Cannot get channel information"
                        return 9222;
                    }
                }

                ChannelId = Regex.Match(
                            content,
                            ".+?(https://www.youtube.com/channel/)(.{24}).+",
                            RegexOptions.Singleline | RegexOptions.IgnoreCase
                            ).Groups[2].Value;
                if (ChannelId.Length != 24) return 9225; // "Cannot get channel information"
            }

            return 0;
        }
        #endregion

        #region WaitForStream - Wait for a new stream, determine stream ID when it starts
        int WaitForStream()
        {
            // Wait for a new stream on the channel ignoring existing streams:
            // search for strings like 'ytimg.com/vi/[streamID]/*_live.jpg' at each iteration
            // in the html of the page 'https://www.youtube.com/channel/[channel_id]/streams'
            int attempt;
            var streamsOnChannel = Enumerable.Empty<string>();
            var streamsOnChannelPrev = Enumerable.Empty<string>();
            var firstPass = true;
            var r = new Regex(@"ytimg\.com\/vi\/(.{11})\/\w+_live\.jpg");
            var uriStreams = Constants.UrlChannel.Replace("[channel_id]", ChannelId) + "/streams";

            attempt = Constants.NetworkAttemptsNumber * 10000;
            while (attempt-- > 0)
            {
                var content = string.Empty;
                try
                {
                    using (var wc = new WebClient())
                    {
                        content = wc.DownloadString(new Uri(uriStreams));
                    }
                }
                catch
                {
                    // ignore
                }

                streamsOnChannelPrev = streamsOnChannel;
                streamsOnChannel = r.Matches(content).OfType<Match>()
                    .Select(i => i.Groups[1].Value).Distinct();

                if (firstPass)
                {
                    firstPass = false;
                    continue; // On the first pass, only read existing streams
                }

                if (streamsOnChannel.Count() > streamsOnChannelPrev.Count())
                {
                    try
                    {
                        Id = streamsOnChannel.Except(streamsOnChannelPrev).First();
                        return 0;
                    }
                    catch
                    {
                        // ignore
                    }
                }

                Program.CountdownTimer(Constants.NetworkAttemptDelayMilliseconds * 10);
            }

            // The loop ended, a stream not started
            return 9210; // "Cannot get live stream information"
        }
        #endregion

        #region GetHtmlJson - Create JSON object from stream HTML
        int GetHtmlJson()
        {
            var content = string.Empty;

            try
            {
                var uri = Constants.UrlStream.Replace("[stream_id]", Id);

                using (var wc = new WebClient())
                {
                    // YouTube HTML page encoding
                    wc.Encoding = Encoding.UTF8;

                    // Add cookie if it was specified
                    if (CLInput.CookieString != string.Empty)
                    {
                        wc.Headers.Add("Cookie", CLInput.CookieString);
                    }

                    content = wc.DownloadString(new Uri(uri));
                }
            }
            catch (Exception e)
            {
                if (CLInput.Log) Program.Log("v", "Getting stream HTML", e.Message);
                return 0;
            }

            if (CLInput.Log) Program.Log("s", "html", content);

            content = content.Split(new string[] { "var ytInitialPlayerResponse = " },
                StringSplitOptions.None)[1];
            content = Regex.Match(content, @"(.+?);</script>.*",
                RegexOptions.Multiline).Groups[1].Value;

            if (CLInput.Log) Program.Log("s", "json_from_html", content);

            try
            {
                var tmp1 = Encoding.UTF8.GetBytes(HttpUtility.UrlDecode(content));
                var tmp2 = JsonReaderWriterFactory
                    .CreateJsonReader(tmp1, new XmlDictionaryReaderQuotas());
                JsonHtml = XElement.Load(tmp2);
            }
            catch (Exception e)
            {
                if (CLInput.Log) Program.Log("v", "Creating JSON from HTML", e.Message);
                return 0;
            }

            return 0;
        }
        #endregion

        #region ParseHtmlJson - Get info from HTML JSON
        int ParseHtmlJson()
        {
            if (hlsManifestUrl == string.Empty)
            {
                try
                {
                    hlsManifestUrl = JsonHtml.XPathSelectElement("//hlsManifestUrl").Value;
                }
                catch { }
            }
            if (ChannelId == string.Empty)
            {
                try
                {
                    ChannelId = JsonHtml.XPathSelectElement("//channelId").Value;
                }
                catch { }
            }
            if (Author == string.Empty)
            {
                try
                {
                    Author = JsonHtml.XPathSelectElement("//author").Value;
                }
                catch { }
            }
            if (Title == string.Empty)
            { 
                try
                {
                    Title = JsonHtml.XPathSelectElement("//title").Value;
                }
                catch { }
            }
            if (Duration == default)
            {
                try
                {
                    Duration = int.Parse(JsonHtml.XPathSelectElement("//targetDurationSec").Value);
                }
                catch { }
            }
            if (Resolutions == string.Empty)
            {
                try
                {
                    var tmp = JsonHtml.XPathSelectElements
                        ("//adaptiveFormats/*/height").Select(x => x.Value).Distinct();
                    Resolutions = string.Join(",", tmp);
                }
                catch { }
            }
            if (UriAdirect == string.Empty)
            {
                try
                {
                    UriAdirect = JsonHtml.XPathSelectElement
                        ("//adaptiveFormats/*/url[contains(text(),'mime=audio')]").Value;
                }
                catch { }
            }
            if (UriVdirect == string.Empty)
            {
                try
                {
                    UriVdirect = JsonHtml.XPathSelectElement
                        ("//adaptiveFormats/*/url[contains(text(),'mime=video')]").Value;
                }
                catch { }
            }

            return 0;
        }
        #endregion

        #region GetInfoWithOembed - Determine info with oembed page
        int GetInfoWithOembed()
        {
            string content = string.Empty;
            string channelUrl;
            XElement jsonOembed;

            try
            {
                var uri = Constants.UrlStreamOembed.Replace("[stream_id]", Id);

                using (var wc = new WebClient())
                {
                    content = wc.DownloadString(new Uri(uri));
                }
            }
            catch (Exception e)
            {
                if (CLInput.Log) Program.Log("v", "Getting oembed page", e.Message);
                return 9243; // "Cannot get live stream information"
            }

            if (CLInput.Log) Program.Log("s", "oembed", content);

            try
            {
                var tmp1 = Encoding.UTF8.GetBytes(HttpUtility.UrlDecode(content));
                var tmp2 = JsonReaderWriterFactory
                    .CreateJsonReader(tmp1, new XmlDictionaryReaderQuotas());
                jsonOembed = XElement.Load(tmp2);
            }
            catch (Exception e)
            {
                if (CLInput.Log) Program.Log("v", "Creating JSON from oembed", e.Message);
                return 9244; // "Cannot get live stream information"
            }

            try
            {
                channelUrl = jsonOembed.XPathSelectElement("//author_url").Value;
                Author = jsonOembed.XPathSelectElement("//author_name").Value;
                Title = jsonOembed.XPathSelectElement("//title").Value;
            }
            catch
            {
                return 9228; // "Cannot get live stream information"
            }

            if (ChannelId == string.Empty)
            {
                GetChannelId(channelUrl);
            }

            return 0;
        }
        #endregion

        #region GetBrowserNetlog - Get content of browser network log
        int GetBrowserNetlog(out string netlog)
        {
            netlog = string.Empty;
            var browser = CLInput.Browser;
            var pathNetlog = Path.GetTempPath() +
                Constants.Name + "~" + Constants.RandomString + ".tmp";
            var args = Constants.UrlStream.Replace("[stream_id]", Id) + "&autoplay=1" +
                " --headless --disable-extensions --disable-gpu --mute-audio --no-sandbox" +
                " --autoplay-policy=no-user-gesture-required --log-net-log=\"" + pathNetlog + "\"";
            int attempt = Constants.NetworkAttemptsNumber;

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
                catch
                {
                    continue;
                }

                try
                {
                    netlog = File.ReadAllText(pathNetlog);
                    File.Delete(pathNetlog);
                }
                catch
                {
                    continue;
                }

                if (CLInput.Log) Program.Log("s", "netlog_" + attempt, netlog);

                if (netlog.Contains("&sq=") &
                    netlog.Contains("mime=video") &
                    netlog.Contains("mime=audio")) return 0;

                Thread.Sleep(Constants.NetworkAttemptDelayMilliseconds);
            }

            // "Cannot get live stream information with browser"
            return 9240;
        }
        #endregion

        #region ParseBrowserNetlog - Determine info with browser netlog
        int ParseBrowserNetlog(string netlog)

        {
            // Get UriAdirect and UriVdirect
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
                return 9229; // "Cannot get live stream information with browser"
            }

            // Get Resolutions from UriVdirect
            try
            {
                var itags = Regex.Match(UriVdirect, @".*&aitags=([^&]+).*").Groups[1].Value;
                var resolutions = new ArrayList();
                foreach (var itag in itags.Replace("%2C", ",").Split(','))
                {
                    var resolutionStr = Constants.Itag[int.Parse(itag)].Split(';')[2];

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
            catch
            {
                return 9242; // "Cannot get live stream information with browser"
            }

            return 0;
        }
        #endregion

        #region GetInfoWithHls - Determine Duration and Start with HLS manifest and playlist
        int GetInfoWithHls()
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
            catch
            {
                return 0;
            }

            if (CLInput.Log) Program.Log("s", "m3u8_1", content);

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
            catch
            {
                return 0;
            }

            if (CLInput.Log) Program.Log("s", "m3u8_2", content);

            // Parse HLS playlist
            if (Duration == default)
            {
                try
                {
                    var tmp = Regex.Match(content, @".*#EXT-X-TARGETDURATION:(\d+).*").Groups[1].Value;
                    Duration = int.Parse(tmp);
                }
                catch { }
            }
            if (curSeq == default)
            {
                try
                {
                    var tmp = Regex.Match(content, @".*#EXT-X-MEDIA-SEQUENCE:(\d+).*").Groups[1].Value;
                    curSeq = int.Parse(tmp);
                }
                catch { }
            }
            if (curSeqUtc == default)
            {
                try
                {
                    var tmp = Regex.Match(content, @".*#EXT-X-PROGRAM-DATE-TIME:(.+)\+.*").Groups[1].Value;
                    tmp = tmp.Replace("T", "");
                    curSeqUtc = DateTime.ParseExact(tmp, "yyyy-MM-ddHH:mm:ss.fff", null);
                }
                catch { }
            }

            if (Duration == default || curSeq == default || curSeqUtc == default)
            {
                return 0;
            }

            // Sometimes HLS playlist contains incorrect UTC time of the current sequence.
            // Fixing this, we cannot demand an exact match, because the stream is always
            // broadcast with a delay of several tens of seconds.
            // So, we will set an acceptable margin, for example, 3 minutes.
            if ((Program.Start.ToUniversalTime() - curSeqUtc) > TimeSpan.FromMinutes(3))
            {
                curSeqUtc = Program.Start.ToUniversalTime();
                Program.ResultСonfirmed = false;
            }

            Start = curSeqUtc.AddSeconds(-curSeq * Duration).ToLocalTime();

            return 0;
        }
        #endregion

        #region GetInfoWithAsegment - Determine Duration and Start with current audio segment
        int GetInfoWithAsegment()
        {
            string content;

            try
            {
                using (var wc = new WebClient())
                {
                    content = wc.DownloadString(new Uri(UriAdirect));
                }
            }
            catch
            {
                return 0;
            }

            if (CLInput.Log) Program.Log("s", "segmentA", content);

            // Parse audio segment as text - get sequence number, its UTC time and duration
            if (Duration == default)
            {
                try
                {
                    var tmp = Regex.Match(content, @".*Target-Duration-Us: (\d+).*").Groups[1].Value;
                    Duration = int.Parse(tmp) / 1000000;
                }
                catch { }
            }
            if (curSeq == default)
            {
                try
                {
                    var tmp = Regex.Match(content, @".*Sequence-Number: (\d+).*").Groups[1].Value;
                    curSeq = int.Parse(tmp);
                }
                catch { }
            }
            if (curSeqUtc == default)
            {
                try
                {
                    var tmp  = Regex.Match(content, @".*Ingestion-Walltime-Us: (\d+).*").Groups[1].Value;
                    curSeqUtc = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    curSeqUtc = curSeqUtc.AddSeconds(long.Parse(tmp) / 1000000);
                }
                catch { curSeqUtc = default; }
            }

            if (Duration == default || curSeq == default || curSeqUtc == default)
            {
                return 0;
            }

            Start = curSeqUtc.AddSeconds(-curSeq * Duration).ToLocalTime();

            return 0;
        }
        #endregion
    }
}