
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using System;
using System.Threading.Tasks;


namespace Ractor.CS.Tests {
    internal class Program {
        private static int _redCount;
        private static long _bestThroughput;

        private static readonly object Msg = new object();
        private static readonly object Run = new object();

        public static uint CpuSpeed() {
#if !mono
            var mo = new ManagementObject("Win32_Processor.DeviceID='CPU0'");
            var sp = (uint)(mo["CurrentClockSpeed"]);
            mo.Dispose();
            return sp;
#else
            return 0;
#endif
        }

        private static void Main() {
            Start();
            Console.ReadKey();
        }

        private static async void Start() {
            int workerThreads;
            int completionPortThreads;
            ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);

            Console.WriteLine("Worker threads: {0}", workerThreads);
            Console.WriteLine("OSVersion: {0}", Environment.OSVersion);
            Console.WriteLine("ProcessorCount: {0}", Environment.ProcessorCount);
            Console.WriteLine("ClockSpeed: {0} MHZ", CpuSpeed());

            Console.WriteLine("");
            Console.WriteLine("Throughput Setting, Messages/sec");

            foreach (var t in GetThroughputSettings()) {
                await Benchmark(t);
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Done..");
        }

        public static IEnumerable<int> GetThroughputSettings() {
            yield return 1;
            yield return 5;
            yield return 10;
            yield return 15;
            yield return 30;
            yield return 50;
            //for (int i = 20; i < 100; i += 10) {
            //    yield return i;
            //}
            //for (int i = 100; i < 1000; i += 100) {
            //    yield return i;
            //}
        }

        private static async Task<bool> Benchmark(int numberOfClients) {
            //const int repeatFactor = 500;
            long repeat = 1000L; //* repeatFactor;
            long totalMessagesReceived = repeat * 2;
            //times 2 since the client and the destination both send messages


            long repeatsPerClient = repeat / numberOfClients;

            var clients = new List<Client>();

            for (int i = 0; i < numberOfClients; i++) {
                var destination = new Destination();
                var client = new Client(destination, repeatsPerClient);
                clients.Add(client);
            }

            var sw = Stopwatch.StartNew();
            var tks = clients.AsParallel()
                .WithDegreeOfParallelism(numberOfClients)
                .Select(c => c.PostAndGetResultAsync(Run));

            await Task.WhenAll(tks);
            //await Task.WhenAll(tasks.ToArray());
            sw.Stop();

            long throughput = totalMessagesReceived * 1000 / sw.ElapsedMilliseconds;
            if (throughput > _bestThroughput) {
                Console.ForegroundColor = ConsoleColor.Green;
                _bestThroughput = throughput;
                _redCount = 0;
            } else {
                _redCount++;
                Console.ForegroundColor = ConsoleColor.Red;
            }

            Console.WriteLine("{0}, {1} messages/s", numberOfClients, throughput);

            if (_redCount > 3)
                return false;

            return true;
        }

        public class Client : Actor<object, bool> {
            private readonly TaskCompletionSource<bool> _latch = new TaskCompletionSource<bool>();
            private readonly Actor<object, object> _actor;
            public long Received;
            public long Repeat;
            public long Sent;

            public Client(Actor<object, object> actor, long repeat) {
                _actor = actor;
                Repeat = repeat;
            }

            public override async Task<bool> Computation(object message) {
                for (int i = 0; i < Repeat; i++) {
                    Sent++;
                    var res = _actor.PostAndGetResult(Msg);
                    //if (res != Msg) throw new ApplicationException();
                    Received++;
                }
                _latch.TrySetResult(true);
                return await _latch.Task;
            }
        }

        public class Destination : Actor<object, object> {
            public override bool Optimistic { get { return false; } }

            public override async Task<object> Computation(object input) {
                //var i = 0;
                //for (int j = 0; j < 100000000; j++) { i = i + 1; }
                //i = i + 1;
                var tcs = new TaskCompletionSource<object>();
                tcs.SetResult(input);
                return await tcs.Task;
            }
        }
    }
}



