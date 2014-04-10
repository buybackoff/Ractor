// explained in Mono https://github.com/mono/mono/blob/master/mcs/class/corlib/System/Guid.cs


using System;
using System.Security.Cryptography;


namespace Fredis {

    public static class GuidGenerator {

        private static readonly object Locker = new object();
        private static RandomNumberGenerator _rng;

        public static Guid NewGuid(uint epoch = 4u) {
            if (epoch > 15u) throw new ArgumentException("Epoch could be from 0 to 15", "epoch");

            var b = GuidArray(epoch);
            var sg = new Guid(b);
            return sg;
        }


        internal static byte[] GuidArray(uint epoch = 4u) {
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

        public static uint Epoch(this Guid guid) {

            var bytes = guid.ToByteArray();
            return (uint)(bytes[7] >> 4);
        }


        public static uint Shard(this Guid guid) {
            var bytes = guid.ToByteArray();
            var epoch = (uint)(bytes[7] >> 4);
            uint shardingKey = ((uint)bytes[0] << 8) | ((uint)bytes[1]); // 65536 possible combinations

            var numberOfShardInEpoch = (uint)Math.Pow(2, (epoch)); // epoch 0 = 1 shard

            var shard = (numberOfShardInEpoch * shardingKey) / 65536;

            if (shard == 16) {
                shard = shard;
            }

            return shard;
        }

    }

}