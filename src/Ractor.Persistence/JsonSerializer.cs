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
            // TODO test if JSON.NET does exactly the same thing with nulls
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

        /// <summary>
        /// 
        /// </summary>
        public T DeepClone<T>(T value) {
            return Deserialize<T>(Serialize(value));
        }
    }


    public static class JsonConvertExtensions {

        /// <summary>
        /// 
        /// </summary>
        public static string ToJson<T>(this T value) {
            var json = JsonConvert.SerializeObject(value);
            return json;
        }

        /// <summary>
        /// 
        /// </summary>
        public static T FromJson<T>(this string json) {
            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// 
        /// </summary>
        public static T DeepClone<T>(this T value){
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        }
    }

}