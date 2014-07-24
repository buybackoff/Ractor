using System.Collections.Generic;
using System.IO;

namespace Ractor {
    // TODO how to correctly deal with null? throw here or pass downstream?
    // TODO replace with JSON.NET after all objects are made POCOs
    /// <summary>
    /// 
    /// </summary>
    public class JsonSerializer : ISerializer {

        private readonly Nessos.FsPickler.Json.JsonSerializer _pickler = Nessos.FsPickler.Json.FsPickler.CreateJson();

        /// <summary>
        /// 
        /// </summary>
        public byte[] Serialize<T>(T value) {
            if (!typeof(T).IsValueType && EqualityComparer<T>.Default.Equals(value, default(T))) {
                return null;
            }
            var memoryStream = new MemoryStream();
            _pickler.Serialize(memoryStream, value);
            return memoryStream.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        public T Deserialize<T>(byte[] bytes) {
            return bytes == null
                ? default(T)
                : _pickler.Deserialize<T>(new MemoryStream(bytes));
        }
    }

}