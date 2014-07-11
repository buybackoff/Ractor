using System.Collections.Generic;
using System.IO;
using Nessos.FsPickler.Json; // Need this for F# types in Actors



namespace Ractor {
    // TODO how to correctly deal with null? throw here or pass downstream?
    public class PicklerJsonSerializer : ISerializer {

        private readonly JsonPickler _pickler = FsPickler.CreateJson();

        public byte[] Serialize<T>(T value) {
            if (!typeof(T).IsValueType && EqualityComparer<T>.Default.Equals(value, default(T))) {
                return null;
            }
            var memoryStream = new MemoryStream();
            _pickler.Serialize(memoryStream, value);
            return memoryStream.ToArray();
        }

        public T Deserialize<T>(byte[] bytes) {
            return bytes == null
                ? default(T)
                : _pickler.Deserialize<T>(new MemoryStream(bytes));
        }
    }

}