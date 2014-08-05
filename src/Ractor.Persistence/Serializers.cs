using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Ractor {
    // TODO how to correctly deal with null? throw here or pass downstream?
    /// <summary>
    /// 
    /// </summary>
    public class JsonSerializer : ISerializer {

        /// <summary>
        /// 
        /// </summary>
        public byte[] Serialize<T>(T value) {
            if (!typeof(T).IsValueType && EqualityComparer<T>.Default.Equals(value, default(T))) {
                return null;
            }
            var json = JsonConvert.SerializeObject(value);
            return Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// 
        /// </summary>
        public T Deserialize<T>(byte[] bytes) {
            if (bytes == null) return default(T);
            var json = Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }

}