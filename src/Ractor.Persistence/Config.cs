using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;

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

        public static Dictionary<byte, string> ShardsDictionaryFromConnectionString(string shardConnections) {
            var kvps =
             shardConnections
                .Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => {
                    var pair = line.Split(new[] { "==" }, StringSplitOptions.RemoveEmptyEntries);
                    return new KeyValuePair<byte, string>(byte.Parse(pair[0].Trim()), pair[1].Trim());
                });
            return kvps.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static string GetSettingOrDefault(string key, string defaultValue = "") {
            var s = ConfigurationManager.AppSettings[key];
            return !String.IsNullOrEmpty(s) ? s : defaultValue;
        }
    }
}
