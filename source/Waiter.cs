using System;
using System.Diagnostics;
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
    // Waiting for stream, getting JSON containing its technical info
    class Waiter
    {
        // Stream ID
        public static string Id { get; private set; }

        // Stream status when first checked (ongoing, upcoming, finished)
        public enum Stream { Unknown, Upcoming, Ongoing, Finished };
        public static Stream IdStatus { get; private set; }

        // Channel ID
        public static string ChannelId { get; private set; }

        // XML-wrapped JSON created from stream HTML page, and its text representation
        public static XElement JsonHtml { get; private set; }
        public static string JsonHtmlStr { get; private set; }

        #region Common - Main method of the class
        public int Common()
        {
            int code;
            bool cacheUsed = true;
            Id = string.Empty;
            ChannelId = string.Empty;

            // Get stream ID
            if (Validator.Url.StartsWith("https://www.youtube.com/watch?v="))
            {
                Id = Validator.Url.Replace("https://www.youtube.com/watch?v=", "");
            }
            else
            {
                code = GetChannelId(Validator.Url);
                if (code != 0) return code;

                code = WaitOnChannel();
                if (code != 0) return code;
            }

            // Prepare cache
            var cache = new Cache();

            if (!Validator.KeepStreamInfo)
            {
                cache.Delete();
            }
            else if (cache.Read(Id, out string idStatus, out string channelId, out string jsonHtmlStr))
            {
                if (Enum.TryParse(idStatus, out Stream idStatusParsed)) IdStatus = idStatusParsed;

                ChannelId = channelId;

                code = GetHtmlJson_Convert(jsonHtmlStr);
                if (code != 0) return code;
            }

            if (IdStatus == 0 || ChannelId == string.Empty || JsonHtmlStr == string.Empty)
            {
                cacheUsed = false;
                code = WaitOnId();
                if (code != 0) return code;
            }

            // If stream status still unknown
            if (IdStatus == 0) IdStatus = Stream.Ongoing;

            if (Validator.Log)
            {
                var logInfo =
                    "\nId: " + Id +
                    "\nIdStatus: " + IdStatus +
                    "\nChannelId: " + ChannelId
                    ;
                Program.Log(logInfo);
            }

            if (Id == string.Empty || ChannelId == string.Empty)
            {
                // "Cannot get live stream information"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9210;
            }

            if (Validator.KeepStreamInfo & !cacheUsed)
            {
                cache.Write(ChannelId, Id, IdStatus.ToString(), JsonHtmlStr);
            }

            return 0;
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
                catch (WebException e)
                {
                    Program.ErrInfo =
                        new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                    if (Validator.Log) Program.Log(Program.ErrInfo);

                    // "Cannot get channel information"
                    return 9212;
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
                catch (WebException e)
                {
                    Program.ErrInfo =
                        new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                    if (Validator.Log) Program.Log(Program.ErrInfo);

                    // "Cannot get channel information. If URL contains '%', is it escaped?"
                    return 9213;
                }

                ChannelId = Regex.Match(
                            content,
                            ".+?(https://www.youtube.com/channel/)(.{24}).+",
                            RegexOptions.Singleline | RegexOptions.IgnoreCase
                            ).Groups[2].Value;
                if (ChannelId.Length != 24)
                {
                    // "Cannot get channel information"
                    Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                    return 9212;
                }
            }

            return 0;
        }
        #endregion

        #region WaitOnChannel - Wait for a new stream, determine stream ID when it starts
        int WaitOnChannel()
        {
            // Wait for a new stream on the channel ignoring existing streams:
            // search for strings like 'ytimg.com/vi/[streamID]/*_live.jpg' at each iteration
            // in the html of the page 'https://www.youtube.com/channel/[channel_id]/streams'
            var streamsOnChannel = Enumerable.Empty<string>();
            var streamsOnChannelPrev = Enumerable.Empty<string>();
            var firstPass = true;
            var r = new Regex(@"ytimg\.com\/vi\/(.{11})\/\w+_live\.jpg");
            var uriStreams = Constants.UrlChannel.Replace("[channel_id]", ChannelId) + "/streams";

            IdStatus = Stream.Upcoming;

            while (true)
            {
                var content = string.Empty;

                try
                {
                    using (var wc = new WebClient())
                    {
                        content = wc.DownloadString(new Uri(uriStreams));
                    }
                }
                catch (WebException e)
                {
                    Program.ErrInfo =
                        new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                    if (Validator.Log) Program.Log(Program.ErrInfo);
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
                    catch (Exception e)
                    {
                        Program.ErrInfo =
                            new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                        if (Validator.Log) Program.Log(Program.ErrInfo);
                    }
                }

                Program.CountdownTimer(180);
            }
        }
        #endregion

        #region WaitOnId - Determine if it's downloadable stream, wait if upcoming
        int WaitOnId()
        {
            string streamStatus;
            var code = 0;

            while (true)
            {
                // Try several times in case of incomplete HTML or incorrect JSON data
                var attempt = 10;
                while (attempt-- > 0)
                {
                    code = GetHtmlJson();
                    if (code != 0) Thread.Sleep(5000);
                    else break;
                }
                if (code != 0) return code;

                // This ID unavailable or it's the regular video
                try
                {
                    if (JsonHtml.XPathSelectElement("//isLiveContent").Value == "false")
                    {
                        // "It's not a live stream"
                        Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                        return 9215;
                    }
                }
                catch (Exception e)
                {
                    Program.ErrInfo =
                        new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                    if (Validator.Log) Program.Log(Program.ErrInfo);

                    // "Video unavailable"
                    Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                    return 9214;
                }

                // Copyrighted live stream
                if (JsonHtml.XPathSelectElement("//signatureCipher") != null)
                {
                    // "Saving copyrighted live streams is blocked"
                    Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                    return 9216;
                }

                // Ongoing live stream
                if (JsonHtml.XPathSelectElement("//isLiveNow") == null)
                {
                    // "Cannot get live stream information"
                    Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                    return 9210;
                }
                if (JsonHtml.XPathSelectElement("//isLiveNow").Value == "true")
                {
                    if (JsonHtml.XPathSelectElement("//targetDurationSec") != null)
                    {
                        if (ChannelId == string.Empty)
                        {
                            try
                            {
                                ChannelId = JsonHtml.XPathSelectElement("//channelId").Value;
                            }
                            catch (Exception e)
                            {
                                Program.ErrInfo =
                                    new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                                if (Validator.Log) Program.Log(Program.ErrInfo);

                                // "Cannot get live stream information"
                                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                                return 9210;
                            }
                        }

                        if (IdStatus == 0) IdStatus = Stream.Ongoing;

                        // Sometimes direct video URL from JSON returns HTTP error 403,
                        // so we need to check it.
                        // If this URL is really direct, we are ready to proceed
                        code = CheckDirectUrl();
                        if (code == 0) return 0;
                        else continue;
                    }
                    else
                    {
                        // "Seems to be a restricted live stream, try '-b' or '-c' option"
                        Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                        return 9217;
                    }
                }

                // Get status
                try
                {
                    streamStatus = JsonHtml.XPathSelectElement("//status").Value;
                }
                catch (Exception e)
                {
                    Program.ErrInfo =
                        new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                    if (Validator.Log) Program.Log(Program.ErrInfo);

                    // "Cannot get live stream information"
                    Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                    return 9210;
                }

                // Upcoming live stream
                if (JsonHtml.XPathSelectElement("//isUpcoming") != null)
                {
                    if (streamStatus == "LOGIN_REQUIRED")
                    {
                        // "Seems to be a restricted live stream, try '-b' or '-c' option"
                        Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                        return 9217;
                    }
                    else
                    {
                        if (IdStatus == 0) IdStatus = Stream.Upcoming;

                        // Don't wait if the requested starting point is in the past
                        if (Regex.IsMatch(Validator.Start, @"^\d{8}:\d{6}$"))
                        {
                            var start = DateTime
                                .ParseExact(Validator.Start, "yyyyMMdd:HHmmss", null);
                            if (start < Program.Start)
                            {
                                // "For an upcoming stream, start point cannot be in the past"
                                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                                return 9218;
                            }
                        }
                        else
                        {
                            // Wait
                            Program.CountdownTimer(180);
                            continue;
                        }
                    }
                }

                // Finished live stream
                if (JsonHtml.XPathSelectElement("//targetDurationSec") != null)
                {
                    if (ChannelId == string.Empty)
                    {
                        try
                        {
                            ChannelId = JsonHtml.XPathSelectElement("//channelId").Value;
                        }
                        catch (Exception e)
                        {
                            Program.ErrInfo =
                                new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                            if (Validator.Log) Program.Log(Program.ErrInfo);

                            // "Cannot get live stream information"
                            Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                            return 9210;
                        }
                    }

                    if (IdStatus == 0) IdStatus = Stream.Finished;

                    // Sometimes direct video URL from JSON returns HTTP error 403,
                    // so we need to check it
                    // If this URL is really direct, we are ready to proceed
                    code = CheckDirectUrl();
                    if (code == 0) return 0;
                    else continue;
                }
                else
                {
                    if (streamStatus == "OK")
                    {
                        // "Unavailable, the live stream ended too long ago"
                        Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                        return 9211;
                    }
                    else
                    {
                        // "Seems to be a restricted live stream, try '-b' or '-c' option"
                        Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                        return 9217;
                    }
                }
            }
        }
        #endregion

        #region GetHtmlJson - Get HTML page and JSON text from it
        int GetHtmlJson()
        {
            var content = string.Empty;

            // Download HTML
            while (true)
            {
                try
                {
                    var uri = Constants.UrlStream.Replace("[stream_id]", Id);

                    using (var wc = new WebClient())
                    {
                        wc.Encoding = Encoding.UTF8;

                        // Add cookie if it was specified
                        if (!string.IsNullOrEmpty(Validator.CookieContent))
                        {
                            wc.Headers.Add("Cookie", Validator.CookieContent);
                        }

                        content = wc.DownloadString(new Uri(uri));
                    }

                    break;
                }
                catch (WebException e)
                {
                    Program.ErrInfo =
                        new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                    if (Validator.Log) Program.Log(Program.ErrInfo);

                    if (e.Status == WebExceptionStatus.ProtocolError)
                    {
                        // "Cannot get live stream information"
                        return 9210;
                    }
                    else
                    {
                        // Do not throw an error if the Internet is lost for a while
                        Thread.Sleep(60000);
                    }
                }
            }

            if (Validator.Log) Program.Log(content, "html_full");

            // Find JSON part in HTML
            content = content.Split
                (new string[] { "var ytInitialPlayerResponse = " }, StringSplitOptions.None)[1];
            var c = 0;
            var i = 0;
            do
            {
                if (content[i] == '{') c++;
                else if (content[i] == '}') c--;
                i++;
            }
            while (c != 0 & i < content.Length);
            JsonHtmlStr = content.Substring(0, i);

            if (Validator.Log) Program.Log(JsonHtmlStr, "html_json");

            return GetHtmlJson_Convert(JsonHtmlStr);
        }
        #endregion

        #region GetHtmlJson_Convert - Convert JSON text to object
        int GetHtmlJson_Convert(string jsonHtmlStr)
        {
            try
            {
                var tmp1 = Encoding.UTF8.GetBytes(HttpUtility.UrlDecode(jsonHtmlStr));
                var tmp2 = JsonReaderWriterFactory
                    .CreateJsonReader(tmp1, new XmlDictionaryReaderQuotas());
                JsonHtml = XElement.Load(tmp2);
                JsonHtmlStr = jsonHtmlStr;
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                // "Cannot get live stream information"
                return 9210;
            }

            return 0;
        }
        #endregion

        #region CheckDirectUrl - Check if direct video URL in JSON is really direct
        int CheckDirectUrl()
        {
            try
            {
                var UriVdirect = Waiter.JsonHtml.XPathSelectElement
                    ("//adaptiveFormats/*/url[contains(text(),'mime=video')]").Value;
                var req = (HttpWebRequest)WebRequest.Create(new Uri(UriVdirect));
                var res = (HttpWebResponse)req.GetResponse();
                var UriVdirectOK = res.ContentType.Contains("video");
                res.Close();
                if (!UriVdirectOK) throw new Exception();
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);

                // "Cannot get live stream direct URL"
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + "";
                return 9219;
            }

            return 0;
        }
        #endregion
    }
}