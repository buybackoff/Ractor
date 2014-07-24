// explained in Mono https://github.com/mono/mono/blob/master/mcs/class/corlib/System/Guid.cs


using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using ServiceStack;

namespace Ractor {
    /// <summary>
    ///     Guid generator for sharding
    /// </summary>
    public static class GuidGenerator {
        private static readonly object Locker = new object();
        private static RandomNumberGenerator _rng;

        /// <summary>
        ///     Fibinacci sequence. When increasing epoch the number of shards will grow by c.62%
        ///     which is better than using powers of two (100%) or prime numbers (too small increases)
        ///     Given that load is not only from new root assets but from existing ones, scaling out
        ///     will reduce load on existing shards by only moving new root assets to new shards
        /// </summary>
        internal static readonly IDictionary<ushort, ushort> EpochToShards =
            new SortedList<ushort, ushort> {
                {0, 0}, // zero epoch kept only for shard calculation, means not sharded key
                {1, 1},
                {2, 2},
                {3, 3},
                {4, 5},
                {5, 8},
                {6, 13},
                {7, 21},
                {8, 34},
                {9, 55},
                {10, 89},
                {11, 144},
                {12, 233},
                {13, 377},
                {14, 610},
                {15, 987},
            };

        /// <summary>
        ///     Generate new Guid for an epoch
        /// </summary>
        internal static Guid NewGuid(uint epoch) {
            if (epoch > 15u) throw new ArgumentException("Epoch could be from 0 to 15", "epoch");

            byte[] b = GuidArray(epoch);
            var sg = new Guid(b);
            return sg;
        }

        /// <summary>
        ///     Generate a new Guid that will have the same shard as the root Guid
        /// </summary>
        internal static Guid NewGuid(Guid rootGuid) {
            uint epoch = rootGuid.Epoch();
            byte[] rootBytes = rootGuid.ToByteArray();
            byte[] newBytes = GuidArray(epoch);

            // set the same virtual shard to the new guid
            // still have 2^(8*13) combinations which must be not globally unique but within a virtual shard
            newBytes[0] = rootBytes[0];
            newBytes[1] = rootBytes[1];

            var sg = new Guid(newBytes);
            return sg;
        }

        /// <summary>
        ///     Translate MD5 hash of a string to Guid with zero epoch
        /// </summary>
        public static Guid NewGuid(string uniqueString) {
            byte[] bs = uniqueString.ToUtf8Bytes().ComputeMD5Hash();
            bs[7] = (byte)((bs[7] & 0x0f) | 0 << 4);
            return new Guid(bs);
        }


        internal static byte[] GuidArray(uint epoch) {
            var bytes = new byte[16];

            lock (Locker) {
                if (_rng == null) _rng = RandomNumberGenerator.Create(); // new RNGCryptoServiceProvider(); //
                _rng.GetBytes(bytes);
            }

            // Mask in Variant 1-0 in Bit[7..6]
            bytes[8] = (byte)((bytes[8] & 0x3f) | 0x80);

            // Mask in Version 4 (random based GuidGenerator) in Bits[15..13]
            //guid[7] = (byte)((guid[7] & 0x0f) | 0x40);

            // Mask in epoch instead of Version 4 (random based GuidGenerator) in Bits[15..13]
            bytes[7] = (byte)((bytes[7] & 0x0f) | (byte)(epoch << 4));

            return bytes;
        }


        /// <summary>
        ///     Shard in which the Guid is stored
        /// </summary>
        public static uint Epoch(this Guid guid) {
            byte[] bytes = guid.ToByteArray();
            return (uint)(bytes[7] >> 4);
        }


        /// <summary>
        ///     Returns shard from Guid based on epoch/virtual shard that are stored in Guid
        /// </summary>
        internal static ushort Shard(this Guid guid) {
            byte[] bytes = guid.ToByteArray();
            var epoch = (ushort)(bytes[7] >> 4);
            if (epoch == 0) throw new ArgumentException("Not sharded Guid with zero epoch");
            var virtualShard = (ushort)((bytes[0] << 8) | bytes[1]);

            // if always set to 1, new shards will take a part, not whole write load
            // ReSharper disable once ConvertToConstant.Local
            int firstShardInEpoch = 1; // (ushort) (epoch == 1 ? 1 : EpochToShards[(ushort)(epoch - 1)] + 1);
            ushort lastShardInEpoch = EpochToShards[epoch];

            int numberOfShardInEpoch = lastShardInEpoch - firstShardInEpoch + 1;

            var shard = (ushort)(firstShardInEpoch + ((numberOfShardInEpoch * virtualShard) / 65536) - 1);
            // 6553*6* not 5!

            return shard;
        }
    }
}