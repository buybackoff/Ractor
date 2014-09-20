using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Ractor {

    /// <summary>
    /// Bytes layout that affects sorting. With SSDs phisical order shouldn't matter that much,
    /// so default is Binary to save space.
    /// </summary>
    public enum SequentialGuidType : byte {
        /// <summary>
        /// Guid.ToByteArray() will be sequential
        /// </summary>
        SequentialAsBinary = 0,
        /// <summary>
        /// Guid.ToString() will be sequential
        /// </summary>
        SequentialAsString = 1,
        /// <summary>
        /// Use for MSSQL only
        /// </summary>
        SequentialAtEnd = 2
    }

    /// <summary>
    ///     Guid generator
    ///     0 bucket - main DB
    /// </summary>
    internal static class GuidGenerator {
        private static readonly RandomNumberGenerator Rng = new RNGCryptoServiceProvider();

        public static Guid NewGuid(SequentialGuidType guidType = SequentialGuidType.SequentialAsString, DateTime? utcDateTime = null) {
            return new Guid(GuidSequentialArray(0, guidType, utcDateTime));
        }

        public static Guid NewRandomBucketGuid(SequentialGuidType guidType = SequentialGuidType.SequentialAsString, DateTime? utcDateTime = null) {
            var bs = new byte[1];
            bs[0] = 0;
            while (bs[0] == 0) { Rng.GetBytes(bs); } // 1-255
            return new Guid(GuidSequentialArray(bs[0], guidType, utcDateTime));
        }

        /// <summary>
        ///     Generate new Guid for a bucket
        /// </summary>
        public static Guid NewBucketGuid(byte bucket, SequentialGuidType guidType = SequentialGuidType.SequentialAsString, DateTime? utcDateTime = null) {
            return new Guid(GuidSequentialArray(bucket, guidType,utcDateTime));
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
        
        internal static byte[] GuidSequentialArray(byte bucket, SequentialGuidType guidType, DateTime? utcDateTime = null) {
            if (bucket > 63) throw new ArgumentOutOfRangeException("bucket", "Bucket is too large! 64 buckets ought to be enough for anybody!");
            
            var bytes = new byte[16];
            Rng.GetBytes(bytes);

            long ticks = utcDateTime.HasValue? utcDateTime.Value.Ticks : GetTicks();

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


            var guidTypeByte = (byte) guidType;
            // first two bits for sequence type, other 6 bits for bucket
            bytes[8] = (byte)(( (guidTypeByte & 3) << 6 ) | (bucket & 63) );
            return bytes;
        }


        /// <summary>
        ///     Shard in which the Guid is stored
        /// </summary>
        public static byte Bucket(this Guid guid) {
            byte[] bytes = guid.ToByteArray();
            return (byte)(bytes[8] & 63);
        }

        /// <summary>
        ///     Shard in which the Guid is stored
        /// </summary>
        internal static SequentialGuidType SequentialType(this Guid guid) {
            byte[] bytes = guid.ToByteArray();
            return (SequentialGuidType)(bytes[8] >> 6);
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
            byte[] bs = Encoding.UTF8.GetBytes(uniqueString).ComputeMD5Hash();
            bs[8] = 0;
            return new Guid(bs);
        }

        internal static DateTime Timestamp(this Guid guid) {
            var tickBytes = new byte[8];
            var gbs = guid.ToByteArray();
            var guidType = guid.SequentialType();
            
            switch (guidType) {
                case SequentialGuidType.SequentialAsString:
                case SequentialGuidType.SequentialAsBinary:
                    
                    if (guidType == SequentialGuidType.SequentialAsString && BitConverter.IsLittleEndian) {
                        Array.Reverse(gbs, 0, 4);
                        Array.Reverse(gbs, 4, 2);
                        Array.Reverse(gbs, 6, 2);
                    }
                    Array.Copy(gbs, 0, tickBytes, 1, 7);
                    break;
                case SequentialGuidType.SequentialAtEnd:
                    Buffer.BlockCopy(gbs, 1, tickBytes, 9, 7);
                    break;
            }
            if (BitConverter.IsLittleEndian) {
                Array.Reverse(tickBytes);
            }
            var ticks = BitConverter.ToInt64(tickBytes, 0);
            return new DateTime(ticks, DateTimeKind.Utc);
        }

    }
}