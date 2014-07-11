// explained in Mono https://github.com/mono/mono/blob/master/mcs/class/corlib/System/Guid.cs


using System;
using System.Collections.Generic;
using System.Security.Cryptography;


namespace Ractor {

    public static class GuidGenerator {

        private static readonly object Locker = new object();
        private static RandomNumberGenerator _rng;

        /// <summary>
        /// Fibinacci sequence. When increasing epoch the number of shards will grow by c.62%
        /// which is better than using powers of two (100%) or prime numbers (too small increases)
        /// 
        /// Given that load is not only from new root assets but from existing ones, scaling out 
        /// will reduce load on existing shards by only moving new root assets to new shards
        /// </summary>
        public static readonly IDictionary<ushort, ushort> EpochToShards =
            new SortedList<ushort, ushort> {
                {0,1},
                {1,2},
                {2,3},
                {3,5},
                {4,8},
                {5,13},
                {6,21},
                {7,34},
                {8,55},
                {9,89},
                {10,144},
                {11,233},
                {12,377},
                {13,610},
                {14,987},
                {15,1597}
            };

        /// <summary>
        /// Generate new Guid for an epoch
        /// </summary>
        public static Guid NewGuid(uint epoch) {
            if (epoch > 15u) throw new ArgumentException("Epoch could be from 0 to 15", "epoch");

            var b = GuidArray(epoch);
            var sg = new Guid(b);
            return sg;
        }

        /// <summary>
        /// Generate a new Guid that will have the same virtual shard and epoch that the root Guid
        /// </summary>
        public static Guid NewGuid(Guid rootGuid) {
            var epoch = rootGuid.Epoch();
            var rootBytes = rootGuid.ToByteArray();
            var newBytes = GuidArray(epoch);

            // set the same virtual shard to the new guid
            // still have 2^(8*13) combinations which must be not globally unique but within a virtual shard
            newBytes[0] = rootBytes[0];
            newBytes[1] = rootBytes[1];

            var sg = new Guid(newBytes);
            return sg;
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
            bytes[7] = (byte)((bytes[7] & 0x0f) | (epoch << 4));

            return bytes;
        }

        /// <summary>
        /// Shard in which the Guid is stored
        /// </summary>
        public static uint Epoch(this Guid guid) {
            var bytes = guid.ToByteArray();
            return (uint)(bytes[7] >> 4);
        }


        //public static ushort VirtualShard(this Guid guid) {
        //    var bytes = guid.ToByteArray();
        //    return (ushort)((bytes[0] << 8) | bytes[1]);
        //}


        /// <summary>
        /// Returns shard from Guid based on epoch/virtual shard that are stored in Guid
        /// </summary>
        public static ushort Shard(this Guid guid) {
            var bytes = guid.ToByteArray();
            var epoch = (ushort)(bytes[7] >> 4);

            // each epoch has its own shards, no shared shards

            // alternatively old comment below, where on new epoch new root assets are distributed among all shards not incremental shards only

            // shard must be stable for each guid for different epoch
            // epoch 0 - all guids go to single shard
            // epoch 1 - shardingKey divided b/w 2 shards, but all shardingKey from epoch 0 direct to the first shard
            // that means we could offload work to new shard only for new users or root sharding keys
            // scaling out should be balanced with scaling up, should never scale up to a limit (e.g. maximum AWS instance until great engineers are there to fix all DB issue)
            // remeber, this is only done to avoid choking with a single instance until more resources are there,
            // not to solve all DB problems in the world
            // The rule should be: first scale up from 1:x1 to 1:x2, then add another 2:x1, then
            // scale up 1:x2 to 1:x4 and 2:x1 to 2:x2, then scale out, etc. (c.80% load as a trigger)
            // in case of AWS we will still need some shutdown (c.10 mins) but that way we could 
            // avoid moving data, changing connection strings, etc. Much simpler and faster than moving entire
            // shards to another phisical server

            // and remember, hardware costs less then time and money costs less than time!

            var virtualShard = (ushort)((bytes[0] << 8) | bytes[1]);

            var firstShardInEpoch = (ushort) (epoch == 0 ? 1 : EpochToShards[(ushort)(epoch - 1)] + 1);
            var lastShardInEpoch = EpochToShards[epoch];

            var numberOfShardInEpoch = lastShardInEpoch - firstShardInEpoch + 1;

            var shard = (ushort)(firstShardInEpoch + ((numberOfShardInEpoch * virtualShard) / 65536) - 1); // 6553*6* not 5!

            return shard;
        }

    }

}