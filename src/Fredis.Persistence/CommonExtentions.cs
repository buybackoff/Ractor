using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using ServiceStack.Text;

namespace Fredis {
    public static class CommonExtentions {

        public static List<T> ItemAsList<T>(this T o) {
            return new List<T>
            {
                           o
                       };
        }

        public static T[] ItemAsArray<T>(this T o) {
            return new[] {
                           o
                       };
        }

        public static byte[] Zip(this byte[] bytes) {
            using (var inStream = new MemoryStream(bytes)) {
                using (var outStream = new MemoryStream()) {
                    using (var compress = new GZipStream(outStream, CompressionMode.Compress)) {
                        inStream.CopyTo(compress);
                    }
                    return outStream.ToArray();
                }
            }
        }


        public static byte[] UnZip(this byte[] bytes) {
            byte[] outBytes;
            using (var inStream = new MemoryStream(bytes)) {
                using (var outStream = new MemoryStream()) {
                    using (var deCompress = new GZipStream(inStream, CompressionMode.Decompress)) {
                        deCompress.CopyTo(outStream);
                    }
                    outBytes = outStream.ToArray();
                }
            }
            return outBytes;
        }
        
    }

}
