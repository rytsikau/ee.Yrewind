using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.XPath;

namespace yrewind
{
    // Class for various preparations and checks using data from CLInput and IDInfo
    class Preparer
    {
        // Start point to download
        public static DateTime Start { get; private set; }

        // Start sequence to download
        public static int StartSequence { get; private set; }

        // Resolution to download
        public static int Resolution { get; private set; }

        // Direct URL of current audio sequence
        public static string UriAdirect { get; private set; }

        // Direct URL of current video sequence
        public static string UriVdirect { get; private set; }

        #region Common - Main method of the class
        public int Common()
        {
            int code;

            // Determine Resolution
            code = GetResolution();
            if (code != 0) return code;

            // Determine direct URLs
            if (CLInput.Browser) code = GetDirectUris_UseBrowser();
            else code = GetDirectUris_UseInfopage();
            if (code != 0) return code;

            // Find start point to download
            if (CLInput.StartSequence.HasValue) code = FindStartUsingSeq();
            else code = FindStartUsingDT();
            if (code != 0) return code;

            return 0;
        }
        #endregion

        #region GetResolution - Determine [Resolution] to download
        int GetResolution()
        {
            // To download audio only
            if ((CLInput.OutputExt == ".aac") ||
                (CLInput.OutputExt == ".m4a") ||
                (CLInput.OutputExt == ".wma"))
            {
                Resolution = 0;
                return 0;
            }

            // Other formats
            try
            {
                var resolutionTmp = 0;
                foreach (var item in IDInfo.Resolutions.Split(','))
                {
                    resolutionTmp = int.Parse(item);
                    if (resolutionTmp > CLInput.Resolution) continue;
                    else break;
                }

                Resolution = resolutionTmp;
            }
            catch
            {
                return 9310; // "Cannot get live stream information"
            }

            // Resolutions above 1080 are only available in VP9 adaptive format ('webm'),
            // but some media containers cannot store VP9 and MP4a data together.
            // Therefore, we will use for them 1080 even if a higher was requested
            if (Resolution > 1080)
            {
                if ((CLInput.OutputExt == ".3gp") ||
                    (CLInput.OutputExt == ".flv") ||
                    (CLInput.OutputExt == ".mov") ||
                    (CLInput.OutputExt == ".ts"))
                {
                    Resolution = 1080;
                }
            }

            return 0;
        }
        #endregion

        #region GetDirectUris_UseInfopage - Determine [UriAdirect] and [UriVdirect]
        int GetDirectUris_UseInfopage()
        {
            try
            {
                UriAdirect = IDInfo.Json.XPathSelectElement
                    ("//adaptiveFormats/*/url[contains(text(),'mime=audio')]").Value;

                // For audio formats determine UriAdirect only
                if (Resolution == 0) return 0;

                var items = IDInfo.Json.XPathSelectElements
                    ("//adaptiveFormats/*/height[contains(text(),'" + Resolution + "')]");
                foreach (var item in items)
                {
                    UriVdirect = item.Parent.Element("url").Value;

                    // For ASF, MKV and WMV containers prefer 'webm' format
                    if ((CLInput.OutputExt == ".asf") ||
                        (CLInput.OutputExt == ".mkv") ||
                        (CLInput.OutputExt == ".wmv"))
                    {
                        if (item.Parent.Element("mimeType").Value.Contains("webm")) break;
                        else continue;
                    }

                    // For others prefer 'mp4'
                    if (item.Parent.Element("mimeType").Value.Contains("mp4")) break;
                }
            }
            catch
            {
                return 9311; // "Cannot get live stream information"
            }

            // Sometimes UriVdirect from 'get_video_info' page is just a link to another URL
            // So we need to find a really direct UriVdirect
            var isReallyDirect = false;
            var attempt = Constants.NetworkAttemptsNumber;
            while (!isReallyDirect && attempt-- > 0)
            {
                try
                {
                    var req = (HttpWebRequest)WebRequest.Create(new Uri(UriVdirect));
                    var res = (HttpWebResponse)req.GetResponse();
                    if (res.ContentType.Contains("video"))
                    {
                        isReallyDirect = true;
                    }
                    else
                    {
                        var reader = new StreamReader(res.GetResponseStream());
                        UriVdirect = reader.ReadToEnd();
                    }
                    res.Close();
                }
                catch
                {
                    continue;
                }
            }
            if (!isReallyDirect) return 9312; // "Cannot get live stream information"

            return 0;
        }
        #endregion

        #region GetDirectUris_UseBrowser - Determine [UriAdirect] and [UriVdirect]
        int GetDirectUris_UseBrowser()
        {
            UriAdirect = IDInfo.UriAdirect;
            UriVdirect = IDInfo.UriVdirect;

            // For audio formats determine UriAdirect only
            if (Resolution == 0) return 0;

            // To get UriVdirect, we need the correct 'itag' parameter,
            // according to the selected resolution and preferred adaptive format
            var itag = string.Empty;
            var itags = Regex.Match(UriVdirect, @".*&aitags=([^&]+).*").Groups[1].Value;
            var preferredAdaptiveFormat = "mp4";
            if ((CLInput.OutputExt == ".asf") ||
                (CLInput.OutputExt == ".mkv") ||
                (CLInput.OutputExt == ".wmv"))
            {
                // For ASF, MKV and WMV containers prefer 'webm' format
                preferredAdaptiveFormat = "webm";
            }
            try
            {
                foreach (var item in itags.Replace("%2C", ",").Split(','))
                {
                    var itemDescription = Constants.Itag[int.Parse(item)];
                    var itemResolutionStr = itemDescription.Split(';')[2];
                    var itemResolution = int.Parse(itemResolutionStr.Split('p')[0]);
                    if (itemResolution == Resolution)
                    {
                        itag = item;
                        if (itemDescription.Contains(preferredAdaptiveFormat)) break;
                    }
                }
            }
            catch
            {
                return 9313; // "Cannot get live stream information"
            }

            // Get UriVdirect
            UriVdirect = Regex.Replace(UriVdirect, @"&itag=\d+", @"&itag=" + itag);

            return 0;
        }
        #endregion

        #region FindStartUsingDT - Determine [Start] and [SeqStart] using CLInput.Start
        int FindStartUsingDT()
        {
            // Incoming CLInput.Start (requested start point):
            // '-start' missing     =Program.Start
            // '-start=+X'          =Program.Start
            // '-start=wait'        =20000101:000000
            // '-start=beginning'   =20000101:000000

            // Find the first actually available point on interval (X,Y)
            // - from requested moment to moment when program started
            Start = CLInput.Start;

            // Correction: If requested point earlier than stream started, move to that point
            if (Start < IDInfo.Start) Start = IDInfo.Start;

            // Correction: If (corrected) point earlier than 167 hours ago, move to that point
            if (Start < Program.Start.AddHours(-168)) Start = Program.Start.AddHours(-168);

            // Calculate sequence of (corrected) requested point
            // (round up to the beginning of next sequence)
            var seqX = (int)Math.Ceiling((Start - IDInfo.Start).TotalSeconds / IDInfo.Duration);

            if (CLInput.Start == Program.Start)
            {
                // If start was in future or missing, do not check,
                // as it current sequence (and unavailable at this moment)
            }
            else if (!CheckSequence(seqX))
            {
                // Test interval in practice
                var seqY = (int)Math.Ceiling
                    ((Program.Start - IDInfo.Start).TotalSeconds / IDInfo.Duration);
                seqX = FindBeginSequence(seqX + 1, seqY);
            }

            Start = IDInfo.Start.AddSeconds(seqX * IDInfo.Duration);
            StartSequence = seqX;

            return 0;
        }
        #endregion

        #region FindStartUsingSeq - Determine [Start] and [SeqStart] using CLInput.StartSequence
        int FindStartUsingSeq()
        {
            StartSequence = (int)CLInput.StartSequence;
            Start = IDInfo.Start.AddSeconds(StartSequence * IDInfo.Duration);

            return 0;
        }
        #endregion

        #region CheckSequence - Check if the specified sequence is available
        public static bool CheckSequence(int seqNum)
        {
            bool code = false;

            var uriA = UriAdirect + "&sq=" + seqNum;
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(new Uri(uriA));
                var res = (HttpWebResponse)req.GetResponse();
                if (res.StatusCode == HttpStatusCode.OK) code = true;

                if (CLInput.Log) Program.Log("v", "checkSeq", seqNum.ToString());
                if (CLInput.Log) Program.Log("v", "checkSeqStatus", res.StatusCode.ToString());

                res.Close();
            }
            catch
            {
                // ignore
            }

            if (code) return code;

            var uriV = UriVdirect + "&sq=" + seqNum;
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(new Uri(uriV));
                var res = (HttpWebResponse)req.GetResponse();
                if (res.StatusCode == HttpStatusCode.OK) code = true;

                if (CLInput.Log) Program.Log("v", "checkSeq2", seqNum.ToString());
                if (CLInput.Log) Program.Log("v", "checkSeqStatus2", res.StatusCode.ToString());

                res.Close();
            }
            catch
            {
                // ignore
            }

            return code;
        }
        #endregion

        #region FindBeginSequence - Find the first available sequence in specified interval
        int FindBeginSequence(int seqBegin, int seqEnd)
        {
            var midpoint = 0;

            var a = seqBegin;
            var b = seqEnd;

            // Bisection method
            while (a != b + 1)
            {
                midpoint = (a + b) / 2;
                if (CheckSequence(midpoint))
                {
                    b = midpoint - 1;
                }
                else
                {
                    a = midpoint + 1;
                }
            }

            return midpoint;
        }
        #endregion
    }
}