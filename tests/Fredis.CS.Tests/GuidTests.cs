using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace Fredis.CS.Tests {
    
    class NativeMethods {
        [DllImport("rpcrt4.dll", SetLastError = true)]
        public static extern int UuidCreateSequential(out Guid guid);
    }

    [TestFixture]
    public class GuidTests {

        public static Guid CreateSequentialGuid() {

            Guid guid;
            int result = NativeMethods.UuidCreateSequential(out guid);
            if (result == 0)
                return guid;
            throw new ApplicationException();
        }

        [Test]
        public void GuidFirstLetterDistribution() {
            const int count = 1000;
            var freq = new Dictionary<string, int>();
            for (int i = 0; i < count; i++) {
                var guidFirstChar = GuidGenerator.NewGuid(4).ToString("N").Substring(0, 2);
                if (freq.ContainsKey(guidFirstChar)) {
                    freq[guidFirstChar] = freq[guidFirstChar] + 1;
                } else {
                    freq[guidFirstChar] = 1;
                }
            }

            var ordered = freq.OrderBy(x => x.Value);

            Console.WriteLine("Number of shards: \n" + freq.Count);
            foreach (var kvp in ordered) {
                Console.WriteLine(kvp.Key + " : " + (double)kvp.Value / count);
                Console.WriteLine();
            }

        }


        [Test]
        public void GuidShardDistribution() {

            for (uint epoch = 0; epoch < 16; epoch++) {
                Console.WriteLine("Epoch: {0}", epoch);
                const int count = 1000;
                var freq = new Dictionary<uint, int>();
                for (int i = 0; i < count; i++) {
                    var shard = GuidGenerator.NewGuid(epoch).Shard();
                    if (freq.ContainsKey(shard)) {
                        freq[shard] = freq[shard] + 1;
                    } else {
                        freq[shard] = 1;
                    }
                }

                var ordered = freq.OrderBy(x => x.Value);

                var diff = ((double)ordered.Last().Value / (double)ordered.First().Value) - 1.0;

                Console.WriteLine("Number of shards: " + freq.Count);
                Console.WriteLine("Error: " + diff);
                
            }
            

        }


        [Test]
        public void GuidInspectByEyes() {
            const int count = 10;
            Console.WriteLine("Mono:");
            for (int i = 0; i < count; i++) {
                Console.WriteLine(GuidGenerator.NewGuid(9).ToString("D"));
            }

            Console.WriteLine("System:");
            for (int i = 0; i < count; i++) {
                Console.WriteLine(Guid.NewGuid().ToString("D"));
            }
        }


        [Test]
        public void GuidEpoch() {
            const int count = 15;
            for (uint i = 0; i <= count; i++) {
                Console.WriteLine("Epoch {0}", i);
                var guid = GuidGenerator.NewGuid(i);
                Assert.AreEqual(i, guid.Epoch());
                Console.WriteLine(guid.ToString("D"));
            }


            Console.WriteLine("Version 4");
            var sguid = Guid.NewGuid();
            Console.WriteLine(sguid.ToString("D"));
            Assert.AreEqual(4u, sguid.Epoch());

        }

        [Test]
        public void ParseGuidWithFakeVersion() {
            string g = "a2fb6ed3-c03a-11e3-b1b3-60c5470c24a5";
            var guid = Guid.Parse(g);
            Console.WriteLine(guid.ToString("D"));

            g = "a2fb6ed3-c03a-21e3-b1b3-60c5470c24a5";
            guid = Guid.Parse(g);
            Console.WriteLine(guid.ToString("D"));

            g = "a2fb6ed3-c03a-31e3-b1b3-60c5470c24a5";
            guid = Guid.Parse(g);
            Console.WriteLine(guid.ToString("D"));

            g = "a2fb6ed3-c03a-41e3-b1b3-60c5470c24a5";
            guid = Guid.Parse(g);
            Console.WriteLine(guid.ToString("D"));

            g = "a2fb6ed3-c03a-51e3-b1b3-60c5470c24a5";
            guid = Guid.Parse(g);
            Console.WriteLine(guid.ToString("D"));

            g = "a2fb6ed3-c03a-61e3-b1b3-60c5470c24a5";
            guid = Guid.Parse(g);
            Console.WriteLine(guid.ToString("D"));

            g = "a2fb6ed3-c03a-71e3-b1b3-60c5470c24a5";
            guid = Guid.Parse(g);
            Console.WriteLine(guid.ToString("D"));

            g = "a2fb6ed3-c03a-81e3-b1b3-60c5470c24a5";
            guid = Guid.Parse(g);
            Console.WriteLine(guid.ToString("D"));

            g = "a2fb6ed3-c03a-91e3-b1b3-60c5470c24a5";
            guid = Guid.Parse(g);
            Console.WriteLine(guid.ToString("D"));

            g = "a2fb6ed3-c03a-01e3-b1b3-60c5470c24a5";
            guid = Guid.Parse(g);
            Console.WriteLine(guid.ToString("D"));

            g = "a2fb6ed3-c03a-a1e3-b1b3-60c5470c24a5";
            guid = Guid.Parse(g);
            Console.WriteLine(guid.ToString("D"));

            g = "a2fb6ed3-c03a-b1e3-b1b3-60c5470c24a5";
            guid = Guid.Parse(g);
            Console.WriteLine(guid.ToString("D"));

            g = "a2fb6ed3-c03a-c1e3-b1b3-60c5470c24a5";
            guid = Guid.Parse(g);
            Console.WriteLine(guid.ToString("D"));

            g = "a2fb6ed3-c03a-d1e3-b1b3-60c5470c24a5";
            guid = Guid.Parse(g);
            Console.WriteLine(guid.ToString("D"));

            g = "a2fb6ed3-c03a-e1e3-b1b3-60c5470c24a5";
            guid = Guid.Parse(g);
            Console.WriteLine(guid.ToString("D"));

            g = "a2fb6ed3-c03a-f1e3-b1b3-60c5470c24a5";
            guid = Guid.Parse(g);
            Console.WriteLine(guid.ToString("D"));

            
        }

    }
}
