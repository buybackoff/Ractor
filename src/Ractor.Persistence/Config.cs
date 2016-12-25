using System;
using System.Collections.Generic;
using System.Linq;
#if NET451
using System.Configuration;
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

        [Obsolete]
        public static string GetSettingOrDefault(string key, string defaultValue = "") {
            var s = ConfigurationManager.AppSettings[key];
            return !String.IsNullOrEmpty(s) ? s : defaultValue;
        }

        /// <summary>
        /// Get name of the main connection string
        /// </summary>
        public static string DataConnectionName(string name) {
            var css = ConfigurationManager.ConnectionStrings;
            for (var i = 0; i < css.Count; i++) {
                var cs = css[i];
                if (cs.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) {
                    return cs.Name;
                }
            }
            throw new Exception("No main Ractor connection string in app config.");
        }

        /// <summary>
        /// Get name of the main connection string
        /// </summary>
        public static IEnumerable<KeyValuePair<byte, string>> DistibutedDataConnectionNames(string prefix) {
            var css = ConfigurationManager.ConnectionStrings;
            for (var i = 0; i < css.Count; i++) {
                var cs = css[i];
                if (!cs.Name.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase)) continue;
                byte n;
                if (byte.TryParse(cs.Name.Split('.')[1], out n)) {
                    yield return new KeyValuePair<byte, string>(n, cs.Name);
                } else {
                    throw new Exception("Wrong format of connection string: " + cs.Name);
                }
            }
        }

    }



}
#endif