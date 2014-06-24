using System.Collections.Generic;
using System.Text;
using ServiceStack;

namespace Fredis {
    // TODO how to correctly deal with null? throw here or pass downstream?
    public class JsonSerializer : ISerializer {
        public byte[] Serialize<T>(T value) {
            if (!typeof(T).IsValueType && EqualityComparer<T>.Default.Equals(value, default(T))) {
                return null;
            }
            return Encoding.UTF8.GetBytes(value.ToJsv());
        }

        public T Deserialize<T>(byte[] bytes) {
            return bytes == null ? default(T) : Encoding.UTF8.GetString(bytes).FromJsv<T>();
        }
    }
}