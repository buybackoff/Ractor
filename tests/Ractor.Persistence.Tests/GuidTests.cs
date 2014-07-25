using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack;

namespace Ractor.CS.Tests {
    
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
                var guidFirstChar = GuidGenerator.NewGuid().ToString("N").Substring(0, 2);
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
        public void GuidToBytesAndBack() {
            var guid = Guid.NewGuid();

            var bs = guid.ToByteArray();

            var g2 = new Guid(bs);
            Console.WriteLine(guid.Equals(g2));
        }

        [Test]
        public void GuidShardDistribution() {

            for (byte bucket = 0; bucket < 255; bucket++) {
                Console.WriteLine("Bucket: {0}", bucket);
                const int count = 10000;
                var freq = new Dictionary<uint, int>();
                for (int i = 0; i < count; i++) {
                    var shard = GuidGenerator.NewRandomBucketGuid().Bucket();
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
            Console.WriteLine("As string:");
            for (int i = 0; i < count; i++) {
                var guid = GuidGenerator.NewRandomBucketGuid(SequentialGuidType.SequentialAsString);
                Console.WriteLine(guid.ToString("D"));
                Console.WriteLine(guid.Timestamp().Ticks);
            }

            Console.WriteLine("As binary:");
            for (int i = 0; i < count; i++) {
                Console.WriteLine(GuidGenerator.NewRandomBucketGuid().ToByteArray().Select(x => x.ToString("x2")).Join(""));
            }
        }


        [Test]
        public void Generate1Million() {
            var sw = new Stopwatch();
            sw.Start();
            
            for (int i = 0; i < 1000000; i++) {
                var guid = GuidGenerator.NewRandomBucketGuid();
            }
            sw.Stop();
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds );
        }

        [Test]
        public void Ticks() {

            for (int i = 0; i < 100; i++) {
                Console.WriteLine("Tick: " + DateTime.UtcNow.Millisecond);
            }
            
        }

        [Test]
        public void Generate1MillionCheckUnique() {
            var sw = new Stopwatch();
            sw.Start();

            var set = new ConcurrentDictionary<Guid, object>();

            var action = new Action(() => {
                for (int i = 0; i < 1000000/4; i++) {
                    var guid = GuidGenerator.NewRandomBucketGuid();
                    if (set.ContainsKey(guid)) throw new ApplicationException("duplicate");
                    if (!set.TryAdd(guid, null)) throw new ApplicationException("cannot add");
                }
            });

            Parallel.Invoke(action, action, action, action);
            
            sw.Stop();
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds);
        }

        [Test]
        public void Generate1MillionCheckOrder() {
            var sw = new Stopwatch();
            sw.Start();

            var previous = GuidGenerator.NewRandomBucketGuid(SequentialGuidType.SequentialAsString);

            for (int i = 0; i < 1000000; i++) {
                var guid = GuidGenerator.NewRandomBucketGuid(SequentialGuidType.SequentialAsString);
                //Console.WriteLine(String.CompareOrdinal(guid.ToString("N"), previous.ToString("N")));
                if (String.CompareOrdinal(guid.ToString("N"), previous.ToString("N")) < 1) throw new ApplicationException();
                previous = guid;
            }
            sw.Stop();
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds);
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
