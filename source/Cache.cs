using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace yrewind
{
    // Caching the technical information about the stream in a temporary file
    // Line format for each live stream:
    // [dateTimeOfGettingInfo] [id] [idStatus] [channelId] [uriAdirect] [uriVdirect] [jsonHtmlStr]
    class Cache
    {
        #region Read - Read required data from cache
        public void Read(
            string id,
            out string idStatus,
            out string channelId,
            out string uriAdirect,
            out string uriVdirect,
            out string jsonHtmlStr
            )
        {
            idStatus = string.Empty;
            channelId = string.Empty;
            uriAdirect = string.Empty;
            uriVdirect = string.Empty;
            jsonHtmlStr = string.Empty;

            foreach (var line in GetContent())
            {
                if (line == string.Empty) continue;

                var lineId = line.Split('\t')[1];

                if (id.Trim() == lineId.Trim())
                {
                    idStatus = line.Split('\t')[2];
                    channelId = line.Split('\t')[3];
                    uriAdirect = line.Split('\t')[4];
                    uriVdirect = line.Split('\t')[5];
                    jsonHtmlStr = line.Split('\t')[6];
                }
            }
        }
        #endregion

        #region Write - Write cache file
        public void Write(
            string id,
            string idStatus,
            string channelId,
            string uriAdirect,
            string uriVdirect,
            string jsonHtmlStr
            )
        {
            var content = GetContent();
            var newData =
                (Program.Start + Program.Timer.Elapsed).ToString("yyyyMMdd-HHmmss") + '\t' +
                id + '\t' +
                idStatus + '\t' +
                channelId + '\t' +
                uriAdirect + '\t' +
                uriVdirect + '\t' +
                jsonHtmlStr;

            var match = content.FirstOrDefault(x => x.Contains("\t" + id + "\t"));
            if (match == null)
            {
                content.Add(newData);
            }
            else if ((match.Split('\t')[4] == string.Empty) & (uriAdirect != string.Empty))
            {
                // Prefer data containing direct browser URLs
                content.Remove(match);
                content.Add(newData);
            }

            if (Validator.Log)
            {
                Program.Log(string.Join(Environment.NewLine, content), "cache");
            }

            try
            {
                File.WriteAllLines(Constants.PathCache, content);
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);
            }
        }
        #endregion

        #region Delete - Delete cache file
        public void Delete()
        {
            try
            {
                File.Delete(Constants.PathCache);
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);
            }
        }
        #endregion

        #region GetContent - Read unexpired data from cache file
        List<string> GetContent()
        {
            DateTime dtAdded;
            var content = new List<string>();

            try
            {
                if (new FileInfo(Constants.PathCache).Length > 1000000) throw new Exception();

                foreach (var line in File.ReadAllLines(Constants.PathCache))
                {
                    if (line == string.Empty) continue;

                    dtAdded = DateTime.ParseExact(line.Split('\t')[0], "yyyyMMdd-HHmmss", null);
                    var dtExpiration = dtAdded.AddMinutes(Constants.CacheShelflifeMinutes);
                    if (Program.Start + Program.Timer.Elapsed > dtExpiration) continue;

                    content.Add(line);
                }
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);
            }

            return content;
        }
        #endregion
    }
}