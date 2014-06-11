using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace Fredis.CS.Tests {
    internal class Program {
        private static int _redCount;
        private static long _bestThroughput;


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

            Console.WriteLine("Max Actor Count: {0}", Environment.ProcessorCount * 64);
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
            //yield return 5;
            //yield return 10;
            //yield return 15;
            //for (int i = 20; i < 100; i += 10) {
            //    yield return i;
            //}
            //for (int i = 100; i < 1000; i += 100) {
            //    yield return i;
            //}
        }

        private static async Task<bool> Benchmark(int factor) {
            //const long repeat = 1000L;
            //const long totalMessagesReceived = repeat * 2;
            ////times 2 since the client and the destination both send messages

            //long repeatsPerClient = repeat / factor;

            //var tasks = new List<Task>();

            //var fredis = new Fredis("localhost");

            //var ping = fredis.CreateActor<string, string>("ping", x => {
            //    if (x == "PING") {
            //        var t = new TaskCompletionSource<string>();
            //        t.SetResult("PONG");
            //        return t.Task;
            //    }
            //    throw new ApplicationException();
            //});

            //ping.Start();


            //Stopwatch sw = Stopwatch.StartNew();

            //for (int i = 0; i < factor; i++) {

            //    var t = Task.Run(
            //        () => {
            //            for (int j = 0; j < repeatsPerClient; j++) {
            //                var pong = ping.PostAndGetResultAsync("PING").Result;
            //                if(pong != "PONG") throw new ApplicationException();
            //            }
            //        });
            //    tasks.Add(t);
            //}

            //await Task.WhenAll(tasks.ToArray());
            //sw.Stop();

            //long throughput = totalMessagesReceived / sw.ElapsedMilliseconds * 1000;
            //if (throughput > _bestThroughput) {
            //    Console.ForegroundColor = ConsoleColor.Green;
            //    _bestThroughput = throughput;
            //    _redCount = 0;
            //} else {
            //    _redCount++;
            //    Console.ForegroundColor = ConsoleColor.Red;
            //}

            //Console.WriteLine("{0}, {1} messages/s, {2}", factor, throughput, totalMessagesReceived);

            //if (_redCount > 3)
            //    return false;

            return true;
        }

       

    }
}