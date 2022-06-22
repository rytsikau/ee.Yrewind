using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace yrewind
{
    // Caching the technical information about the stream in a temporary file
    // Line format: '[YYYYMMDD-hhmmss]\t[Id]\t[IdStatus]\t[ChannelId]\t[JsonHtml]'
    class Cache
    {
        string[] content = default;

        #region Read - Read required data from cache content
        public bool Read(string id, out string idStatus, out string channelId, out string jsonHtmlStr)
        {
            bool success;
            idStatus = string.Empty;
            channelId = string.Empty;
            jsonHtmlStr = string.Empty;

            if (content == default) content = GetContent();

            foreach (var line in content)
            {
                var lineId = line.Split('\t')[1];

                if (id.Trim() == lineId.Trim())
                {
                    idStatus = line.Split('\t')[2];
                    channelId = line.Split('\t')[3];
                    jsonHtmlStr = line.Split('\t')[4];
                }
            }

            if (idStatus == string.Empty || channelId == string.Empty || jsonHtmlStr == string.Empty)
            {
                success = false;
            }
            else
            {
                success = true;
            }

            return success;
        }
        #endregion

        #region Write - Write cache file
        public bool Write(string channelId, string id, string idStatus, string jsonHtmlStr)
        {
            bool success;

            if (content == default) content = GetContent();

            var contentUpdated = new List<string>(content);
            var line =
                (Program.Start + Program.Timer.Elapsed).ToString("yyyyMMdd-HHmmss") + '\t' +
                id + '\t' +
                idStatus + '\t' +
                channelId + '\t' +
                jsonHtmlStr;
            contentUpdated.Add(line);

            if (Validator.Log)
            {
                Program.Log(string.Join(Environment.NewLine, contentUpdated), "cache");
            }

            try
            {
                File.WriteAllLines(Constants.PathCache, contentUpdated);
                success = true;
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);
                success = false;
            }

            return success;
        }
        #endregion

        #region Delete - Delete cache file
        public bool Delete()
        {
            bool success;

            try
            {
                File.Delete(Constants.PathCache);
                success = true;
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);
                success = false;
            }

            return success;
        }
        #endregion

        #region GetContent - Read unexpired cache data from file
        string[] GetContent()
        {
            DateTime dtAdded;
            List<string> content = new List<string>();

            try
            {
                if (new FileInfo(Constants.PathCache).Length > 1000000) throw new Exception();

                foreach (var line in File.ReadAllLines(Constants.PathCache))
                {
                    if (line.Trim() == string.Empty) continue;

                    var value = line.Split('\t')[0];
                    dtAdded = DateTime.ParseExact(value, "yyyyMMdd-HHmmss", null);
                    var dtExpiration = dtAdded.AddMinutes(Constants.CacheShelflifeMinutes);

                    if (Program.Start + Program.Timer.Elapsed < dtExpiration) content.Add(line);
                }
            }
            catch (Exception e)
            {
                Program.ErrInfo = new StackFrame(0, true).GetFileLineNumber() + " - " + e.Message;
                if (Validator.Log) Program.Log(Program.ErrInfo);
            }

            return content.ToArray();
        }
        #endregion
    }
}