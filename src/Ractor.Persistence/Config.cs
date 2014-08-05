using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using ServiceStack;

namespace Ractor {
    public static class Config {

        public static bool IsRunningOnAWS() {
            try {
                var wr = WebRequest.Create("http://169.254.169.254/latest/meta-data/");
                wr.Timeout = 250;
                wr.Method = "GET";
                var resp = wr.GetResponse();
                return resp.ContentLength > 0; //!resp.IsErrorResponse();
            } catch {
                return false;
            }
        }

        public static Dictionary<ushort, string> ShardsDictionaryFromConnectionString(string shardConnections) {
            return shardConnections
                .Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries)
                .ToDictionary(line => {
                    var pair = line.Split(new[] { "==" }, StringSplitOptions.RemoveEmptyEntries);
                    return new KeyValuePair<ushort, string>(UInt16.Parse(pair[0].Trim()), pair[1].Trim());
                });
        }

        public static string GetSettingOrDefault(string key, string defaultValue = "") {
            var s = ConfigurationManager.AppSettings[key];
            return !String.IsNullOrEmpty(s) ? s : defaultValue;
        }
    }
}
