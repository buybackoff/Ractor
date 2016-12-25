
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
            //for (int i = 0; i < 20; i++)
            //{
            //    Queue();
            //}

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }


        private static void Queue() {
            var redis = new Redis(keyNameSpace: "RedisQueueTests");
            var queue = new RedisQueue<string>(redis, "CouldSendAndReceiveMessages", timeout: 5000);
            const int n = 10000;

            var sw = new Stopwatch();
            sw.Start();
            var producer1 = Task.Run(async () => {
                //await Task.Delay(1000);
                for (var i = 0; i < n; i++) {
                    await queue.TrySendMessage(i.ToString());
                    //await queue.TrySendMessage(new String('x', i*1));

                    //await Task.Delay(50);
                }
            });

            //var producer2 = Task.Run(async () => {
            //    for (var i = n; i < 2*n; i++) {
            //        await queue.TrySendMessage(i.ToString());
            //        //await Task.Delay(50);
            //    }
            //});

            var consumer = Task.Run(async () => {
                var c = 0;
                while (true) {
                    //await Task.Delay(100);
                    QueueReceiveResult<string> message = default(QueueReceiveResult<string>);
                    try {
                        message = await queue.TryReceiveMessage();
                    } catch (Exception e) {
                        Console.WriteLine(e);
                    }
                    c++;
                    //if (message.OK) { Console.WriteLine(message.Value); }
                    if (message.Ok) {
                        await queue.TryDeleteMessage(message.DeleteHandle);
                    }
                    if (message.Ok && c == n) break; // n * 2

                }
            });

            producer1.Wait();
            //producer2.Wait();
            consumer.Wait();
            sw.Stop();
            Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds}");
            queue.Dispose();
            //Thread.Sleep(2000);
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
            yield return 2;
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
            long repeat = 10000L; //* repeatFactor;
            long totalMessagesReceived = repeat;
            //times 2 since the client and the destination both send messages

            long repeatsPerClient = repeat / numberOfClients;

            var clients = new List<Client>();

            for (int i = 0; i < numberOfClients; i++) {
                var destination = new Destination();
                destination.Start();
                var client = new Client(destination, repeatsPerClient);
                client.Start();
                clients.Add(client);
            }

            var sw = Stopwatch.StartNew();
            List<Task<string>> tasks = new List<Task<string>>();
            for (int i = 0; i < numberOfClients; i++)
            {
                tasks.Add(clients[i].PostAndGetResult(Run));
            }

            await Task.WhenAll(tasks);
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

        public class Client : Actor<object, string> {
            private readonly TaskCompletionSource<string> _tcs = new TaskCompletionSource<string>();
            private readonly Actor<object, object> _actor;
            public long Received;
            public long Repeat;
            public long Sent;

            public Client(Actor<object, object> actor, long repeat) {
                _actor = actor;
                Repeat = repeat;
            }

            public override async Task<string> Computation(object message) {
                for (int i = 0; i < Repeat; i++) {
                    Sent++;
                    var res = _actor.PostAndGetResult(Msg);
                    //if (res != Msg) throw new ApplicationException();
                    Received++;
                }
                _tcs.TrySetResult("OK");
                return await _tcs.Task;
            }
        }

        public class Destination : Actor<object, object> {

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



