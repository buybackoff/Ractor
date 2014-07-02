using System.Collections.Generic;
using System.IO;
using Nessos.FsPickler;

// TODO replace SS with FsPickler

namespace Fredis {
    // TODO how to correctly deal with null? throw here or pass downstream?
    
    public class PicklerBinarySerializer : ISerializer {

        private readonly BinaryPickler _pickler = FsPickler.CreateBinary();

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