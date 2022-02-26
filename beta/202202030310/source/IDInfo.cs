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
    // Class for getting the necessary technical information about the stream
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
        public static int Duration { get; private set; } = 0;

        // Available resolutions
        public static string Resolutions { get; private set; } = string.Empty;

        // Stream start point
        // (assumes no interruptions caused by network errors,
        // so in practice this point is later than the actual stream start time)
        public static DateTime Start { get; private set; } = DateTime.MinValue;

        // XML-wrapped JSON object (UseInfopage way)
        public static XElement Json { get; private set; } = default;

        // Preliminary direct audio and video URLs (UseBrowser way)
        public static string UriAdirect { get; private set; } = string.Empty;
        public static string UriVdirect { get; private set; } = string.Empty;

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

            // Get stream info by selected way
            if (CLInput.Browser) code = UseBrowser();
            else code = UseInfopage();

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

                Program.CountdownTimer(Constants.NetworkAttemptDelaySeconds * 20000);
            }

            // The loop ended, a stream not started
            return 9210; // "Cannot get live stream information"
        }
        #endregion

        #region UseInfopage - Get ID info from 'get_video_info' page and HLS playlist
        int UseInfopage()
        {
            int code;

            code = UseInfopage_Json(out string hlsManifestUrl);
            if (code != 0) return code;

            try
            {
                var c = Json.XPathSelectElement("//adaptiveFormats/*/signatureCipher");
                if (c.ToString() != string.Empty)
                {
                    return 9226; // "Saving copyrighted live streams is blocked"
                }
            }
            catch
            {
                // ignore
            }

            try
            {
                ChannelId = Json.XPathSelectElement("//videoDetails/channelId").Value;

                Author = Json.XPathSelectElement("//videoDetails/author").Value;

                Title = Json.XPathSelectElement("//videoDetails/title").Value;

                var tmp1 = Json.XPathSelectElement("//targetDurationSec").Value;
                Duration = int.Parse(Regex.Replace(tmp1, @"\..*", ""));

                var tmp2 = Json.XPathSelectElements("//adaptiveFormats/*/height")
                    .Select(x => x.Value).Distinct();
                Resolutions = string.Join(",", tmp2);
            }
            catch
            {
                return 9220; // "Cannot get live stream information"
            }

            code = UseInfopage_Hls(hlsManifestUrl, out string hlsPlaylist);
            if (code != 0) return code;

            // To determine Start, firstly parse current sequence number and it's UTC time
            var curSeq = 0;
            var curSeqUtc = DateTime.MinValue;
            try
            {
                var tmp1 = Regex.Match(hlsPlaylist,
                    @".*#EXT-X-MEDIA-SEQUENCE:(\d+).*").Groups[1].Value;
                curSeq = int.Parse(tmp1);

                var tmp2 = Regex.Match(hlsPlaylist,
                    @".*#EXT-X-PROGRAM-DATE-TIME:(.+)\+.*").Groups[1].Value;
                curSeqUtc = DateTime.ParseExact(tmp2.Replace("T", ""),
                    "yyyy-MM-ddHH:mm:ss.fff", null);
            }
            catch
            {
                return 9217; // "Cannot get live stream information"
            }

            // Sometimes there is an error in HLS playlist - incorrect UTC time
            // of the current sequence, so we need to fix this if the parsed value
            // differs significantly from the current time. At the same time,
            // we cannot demand an exact match, since the stream is always broadcast
            // with a delay of several tens of seconds. Therefore, we will set
            // an acceptable margin, for example, 3 minutes.
            if ((Program.Start.ToUniversalTime() - curSeqUtc) > TimeSpan.FromMinutes(3))
            {
                curSeqUtc = Program.Start.ToUniversalTime();
                Program.ResultСonfirmed = false;
            }

            // Calculate when the stream started
            Start = curSeqUtc.AddSeconds(-curSeq * Duration).ToLocalTime();

            return 0;
        }
        #endregion

        #region UseInfopage_Json - Create JSON object from 'get_video_info' page
        int UseInfopage_Json(out string hlsManifestUrl)
        {
            hlsManifestUrl = string.Empty;
            int attempt;
            var uri = string.Empty;
            var urlInfoPageType = string.Empty;
            var content = string.Empty;
            var jsonText = string.Empty;

            attempt = Constants.NetworkAttemptsNumber;
            while (attempt-- > 0)
            {
                try
                {
                    if (urlInfoPageType == "embedded") urlInfoPageType = "detailpage";
                    else urlInfoPageType = "embedded";

                    uri = Constants.UrlStreamInfo.Replace("[stream_id]", Id) + urlInfoPageType;
                    using (var wc = new WebClient())
                    {
                        if (CLInput.CookieString != string.Empty)
                        {
                            wc.Headers.Add("Cookie", CLInput.CookieString);
                        }
                        content = wc.DownloadString(new Uri(uri));
                    }

                    jsonText = Regex
                        .Match(content, @"^.+player_response=([^&]+%7D).*$")
                        .Groups[1].Value;

                    if (CLInput.Log) Program.Log("s", urlInfoPageType, content);
                    if (CLInput.Log) Program.Log("s", urlInfoPageType + "_json", jsonText);

                    var tmp1 = Encoding.UTF8.GetBytes(HttpUtility.UrlDecode(jsonText));
                    var tmp2 = JsonReaderWriterFactory
                        .CreateJsonReader(tmp1, new XmlDictionaryReaderQuotas());
                    Json = XElement.Load(tmp2);

                    hlsManifestUrl = Json.XPathSelectElement("//hlsManifestUrl").Value;

                    return 0;
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("429"))
                    {
                        // "Server displays captcha page. Use '-cookie' parameter"
                        return 9224;
                    }
                    else if ((Json != default) & (hlsManifestUrl == string.Empty))
                    {
                        // "Cannot get live stream information. Is it a live stream?"
                        return 9212;
                    }

                    if (CLInput.Log) Program.Log("s", urlInfoPageType, content);
                    if (CLInput.Log) Program.Log("s", urlInfoPageType + "_json", jsonText);

                    Program.CountdownTimer(Constants.NetworkAttemptDelaySeconds * 1000);
                }
            }

            if (content == string.Empty)
            {
                // "Cannot get live stream information"
                return 9211;
            }
            else if (jsonText == string.Empty)
            {
                // "Cannot get live stream information"
                return 9215;
            }
            else if (Json == default)
            {
                // "Cannot get live stream information"
                return 9216;
            }
            else if (hlsManifestUrl == string.Empty)
            {
                // "Cannot get live stream information. Is it a live stream?"
                return 9227;
            }

            return 0;
        }
        #endregion

        #region UseInfopage_Hls - Get HLS playlist
        int UseInfopage_Hls(string hlsManifestUrl, out string hlsPlaylist)
        {
            hlsPlaylist = string.Empty;
            var hlsPlaylistUrl = string.Empty;
            var content = string.Empty;

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
                return 9218; // "Cannot get live stream information"
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
                    hlsPlaylist = wc.DownloadString(new Uri(hlsPlaylistUrl));
                }
            }
            catch
            {
                return 9219; // "Cannot get live stream information"
            }

            if (CLInput.Log) Program.Log("s", "m3u8_2", hlsPlaylist);

            return 0;
        }
        #endregion

        #region UseBrowser - Get ID info from 'oembed' page and browser network log
        int UseBrowser()
        {
            // Alternative way to get info about the live stream -
            // using Google Chrome or Microsoft Edge if Chrome is not installed.
            // This 'browser method' a little slower and more buggy,
            // but in case of a captcha error, it does not require manipulations with cookie -
            // you just need to solve the captcha in the browser.
            // Also, this way is more robust against various server side changes.
            // This way doesn't indicate and doesn't interfere with the download of
            // copyrighted (with 'cipher signature') live streams,
            // so we have to prevent it manually.

            int code;

            code = UseBrowser_Html(out string html);
            if (code != 0) return code;

            if (html.Contains("\"signatureCipher\":\""))
            {
                return 9237; // "Saving copyrighted live streams is blocked"
            }
            else if (!Regex.IsMatch(html, "\"itag\":\\d+,\"url\":\"https://"))
            {
                return 9238; // "Cannot get live stream information"
            }

            code = UseBrowser_Oembed(out var jsonOembed);
            if (code != 0) return code;

            string channelUrl;
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
                code = GetChannelId(channelUrl);
                if (code != 0) return code;
            }

            code = UseBrowser_Netlog(out string netlog);
            if (code != 0) return code;

            // Get UriAdirect and UriVdirect from netlog
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
                return 9229; // "Cannot get live stream information"
            }

            // Get Resolutions from UriVdirect
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

            code = UseBrowser_AudioSegment(out string segment);
            if (code != 0) return code;

            // Parse audio segment as text - get sequence number, duration and UTC time
            int curSeq;
            var curSeqUtc = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            try
            {
                var curSeqStr = Regex.Match(segment,
                    @".*Sequence-Number: (\d+).*").Groups[1].Value;
                curSeq = int.Parse(curSeqStr);

                var curSeqUtcStr = Regex.Match(segment,
                    @".*Ingestion-Walltime-Us: (\d+).*").Groups[1].Value;
                var unixTimestamp = long.Parse(curSeqUtcStr) / 1000000;
                curSeqUtc = curSeqUtc.AddSeconds(unixTimestamp);

                var durationStr = Regex.Match(segment,
                    @".*Target-Duration-Us: (\d+).*").Groups[1].Value;
                Duration = int.Parse(durationStr) / 1000000;
            }
            catch
            {
                return 9230; // "Cannot get live stream information"
            }

            // Calculate when the stream started
            Start = curSeqUtc.AddSeconds(-curSeq * Duration).ToLocalTime();

            return 0;
        }
        #endregion

        #region UseBrowser_Html - Get main page of stream as HTML text
        int UseBrowser_Html(out string content)
        {
            content = string.Empty;

            try
            {
                var uri = Constants.UrlStream.Replace("[stream_id]", Id);
                using (var wc = new WebClient())
                {
                    content = wc.DownloadString(new Uri(uri));
                }

                if (CLInput.Log) Program.Log("s", "html", content);
            }
            catch
            {
                return 9236; // "Cannot get live stream information"
            }

            return 0;
        }
        #endregion

        #region UseBrowser_Oembed - Create JSON object from 'oembed' page
        int UseBrowser_Oembed(out XElement jsonOembed)
        {
            jsonOembed = default;
            var content = string.Empty;

            try
            {
                var uri = Constants.UrlStreamInfoOembed.Replace("[stream_id]", Id);
                using (var wc = new WebClient())
                {
                    content = wc.DownloadString(new Uri(uri));
                }

                if (CLInput.Log) Program.Log("s", "oembed", content);

                var tmp1 = Encoding.UTF8.GetBytes(HttpUtility.UrlDecode(content));
                var tmp2 = JsonReaderWriterFactory
                    .CreateJsonReader(tmp1, new XmlDictionaryReaderQuotas());
                jsonOembed = XElement.Load(tmp2);
            }
            catch
            {
                return 9231; // "Cannot get live stream information"
            }

            return 0;
        }
        #endregion

        #region UseBrowser_Netlog - Get content of browser network log
        int UseBrowser_Netlog(out string netlog)
        {
            netlog = string.Empty;
            var browser = "chrome";
            var pathNetlog = Path.GetTempPath() +
                Constants.Name + "~" + Constants.RandomString + ".tmp";
            var args = Constants.UrlStream.Replace("[stream_id]", Id) + "&autoplay=1" +
                " --headless --disable-extensions --disable-gpu --mute-audio --no-sandbox" +
                " --autoplay-policy=no-user-gesture-required --log-net-log=\"" + pathNetlog + "\"";
            int attempt = Constants.NetworkAttemptsNumber * 2;

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
                    if (browser == "chrome")
                    {
                        browser = "msedge";
                        continue;
                    }
                    else
                    {
                        // "Cannot launch browser to get live stream information"
                        return 9232;
                    }
                }

                try
                {
                    netlog = File.ReadAllText(pathNetlog);
                    File.Delete(pathNetlog);
                }
                catch
                {
                    // "Cannot get live stream information"
                    return 9233;
                }

                if (CLInput.Log) Program.Log("s", "netlog_" + attempt, netlog);

                if (netlog.Contains("&sq=") &
                    netlog.Contains("mime=video") &
                    netlog.Contains("mime=audio")) break;

                Thread.Sleep(Constants.NetworkAttemptDelaySeconds * 1000);
            }

            return 0;
        }
        #endregion

        #region UseBrowser_AudioSegment - Get content of audio segment
        int UseBrowser_AudioSegment(out string segment)
        {
            segment = string.Empty;
            string segmentPath = Path.GetTempPath() +
                Constants.Name + "~" + Constants.RandomString + ".tmp";

            try
            {
                using (var wc = new WebClient())
                {
                    wc.DownloadFile(new Uri(UriAdirect), segmentPath);
                }
            }
            catch
            {
                return 9234; // "Cannot get live stream information"
            }

            try
            {
                segment = File.ReadAllText(segmentPath);
                File.Delete(segmentPath);
            }
            catch
            {
                return 9235; // "Unable to access temporary data"
            }

            if (CLInput.Log) Program.Log("s", "segmentA", segment);

            return 0;
        }
        #endregion
    }
}