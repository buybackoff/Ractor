using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Ractor {
    /// <summary>
    /// 
    /// </summary>
    public static class CommonExtentions {

        /// <summary>
        /// 
        /// </summary>
        public static List<T> ItemAsList<T>(this T o) {
            return new List<T>
            {
                           o
                       };
        }

        /// <summary>
        /// 
        /// </summary>
        public static T[] ItemAsArray<T>(this T o) {
            return new[] {
                           o
                       };
        }

        /// <summary>
        /// In-memory compress
        /// </summary>
        public static byte[] GZip(this byte[] bytes) {
            using (var inStream = new MemoryStream(bytes)) {
                using (var outStream = new MemoryStream()) {
                    using (var compress = new GZipStream(outStream, CompressionMode.Compress)) {
                        inStream.CopyTo(compress);
                    }
                    return outStream.ToArray();
                }
            }
        }


        /// <summary>
        /// In-memory uncompress
        /// </summary>
        public static byte[] UnGZip(this byte[] bytes) {
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
