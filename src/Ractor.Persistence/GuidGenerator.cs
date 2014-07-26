using System;
using System.Security.Cryptography;
using System.Threading;
using ServiceStack;

namespace Ractor {

    public enum SequentialGuidType {
        /// <summary>
        /// Guid.ToString() will be sequential
        /// </summary>
        SequentialAsString,
        /// <summary>
        /// Guid.ToByteArray() will be sequential
        /// </summary>
        SequentialAsBinary,
        /// <summary>
        /// Use for MSSQL only
        /// </summary>
        SequentialAtEnd
    }

    /// <summary>
    ///     Guid generator
    ///     0 bucket - main DB
    /// </summary>
    internal static class GuidGenerator {
        private static readonly RandomNumberGenerator Rng = new RNGCryptoServiceProvider();

        public static Guid NewGuid(SequentialGuidType guidType = SequentialGuidType.SequentialAsString) {
            return new Guid(GuidSequentialArray(0, guidType));
        }

        public static Guid NewRandomBucketGuid(SequentialGuidType guidType = SequentialGuidType.SequentialAsString) {
            var bs = new byte[1];
            bs[0] = 0;
            while (bs[0] == 0) { Rng.GetBytes(bs); } // 1-255
            return new Guid(GuidSequentialArray(bs[0], guidType));
        }

        /// <summary>
        ///     Generate new Guid for a bucket
        /// </summary>
        public static Guid NewBucketGuid(byte bucket, SequentialGuidType guidType = SequentialGuidType.SequentialAsString) {
            return new Guid(GuidSequentialArray(bucket, guidType));
        }

        /// <summary>
        ///     Generate a new Guid that will have the same bucket as the root Guid
        /// </summary>
        public static Guid NewBucketGuid(Guid rootGuid, SequentialGuidType guidType = SequentialGuidType.SequentialAsString) {
            return new Guid(GuidSequentialArray(rootGuid.ToByteArray()[8], guidType));
        }


        private static readonly long BaseTicks = new DateTime(2000, 1, 1).Ticks;
        private static long _previousTicks = DateTime.UtcNow.Ticks - BaseTicks;

        private static long GetTicks() {
            long orig, newval;
            do {
                orig = _previousTicks;
                var now = DateTime.UtcNow.Ticks - BaseTicks;
                newval = Math.Max(now, orig + 1);
            } while (Interlocked.CompareExchange
                         (ref _previousTicks, newval, orig) != orig);
            return newval;
        }
        
        
        internal static byte[] GuidSequentialArray(byte bucket, SequentialGuidType guidType) {
            var bytes = new byte[16];
            Rng.GetBytes(bytes);
            long ticks = GetTicks();

            // Convert to a byte array 
            byte[] ticksArray = BitConverter.GetBytes(ticks);
            if (BitConverter.IsLittleEndian) {
                Array.Reverse(ticksArray);
            }

            // Copy the bytes into the guid 

            switch (guidType) {
                case SequentialGuidType.SequentialAsString:
                case SequentialGuidType.SequentialAsBinary:
                    Array.Copy(ticksArray, 1, bytes, 0, 7); // 7 bytes for ticks ~ 228 years

                    // If formatting as a string, we have to reverse the order
                    // of the Data1 and Data2 blocks on little-endian systems.
                    if (guidType == SequentialGuidType.SequentialAsString && BitConverter.IsLittleEndian) {
                        Array.Reverse(bytes, 0, 4);
                        Array.Reverse(bytes, 4, 2);
                        Array.Reverse(bytes, 6, 2);
                    }
                    break;

                case SequentialGuidType.SequentialAtEnd:
                    Buffer.BlockCopy(ticksArray, 1, bytes, 9, 7);
                    break;
            }

            bytes[8] = bucket;
            return bytes;
        }


        /// <summary>
        ///     Shard in which the Guid is stored
        /// </summary>
        public static byte Bucket(this Guid guid) {
            byte[] bytes = guid.ToByteArray();
            return bytes[8];
        }
    }

    public static class GuidExtensions {
        public static Guid GuidFromBase64String(this string shortGuid) {
            Guid guid;
            shortGuid = shortGuid.Replace("-", "/").Replace("_", "+") + "==";
            try {
                guid = new Guid(Convert.FromBase64String(shortGuid));
            } catch (Exception ex) {
                throw new ArgumentException("Wrong Base64 fomat for GUID", ex);
            }
            return guid;
        }

        public static string ToBase64String(this Guid guid) {
            return Convert.ToBase64String(guid.ToByteArray()).Replace("/", "-").Replace("+", "_").Replace("=", "");
        }

        /// <summary>
        ///     Translate MD5 hash of a string to Guid with zero epoch
        /// </summary>
        public static Guid MD5Guid(this string uniqueString) {
            byte[] bs = uniqueString.ToUtf8Bytes().ComputeMD5Hash();
            bs[8] = 0;
            return new Guid(bs);
        }

        internal static DateTime Timestamp(this Guid guid, SequentialGuidType guidType = SequentialGuidType.SequentialAsString) {
            var bytes = new byte[8];
            var gbs = guid.ToByteArray();

            
            switch (guidType) {
                case SequentialGuidType.SequentialAsString:
                case SequentialGuidType.SequentialAsBinary:
                    
                    if (guidType == SequentialGuidType.SequentialAsString && BitConverter.IsLittleEndian) {
                        Array.Reverse(gbs, 0, 4);
                        Array.Reverse(gbs, 4, 2);
                        Array.Reverse(gbs, 6, 2);
                    }
                    Array.Copy(gbs, 0, bytes, 1, 7);
                    break;
                case SequentialGuidType.SequentialAtEnd:
                    Buffer.BlockCopy(gbs, 1, bytes, 9, 7);
                    break;
            }
            if (BitConverter.IsLittleEndian) {
                Array.Reverse(bytes);
            }
            var ticks = BitConverter.ToInt64(bytes, 0);
            return new DateTime(ticks, DateTimeKind.Utc);
        }

    }
}