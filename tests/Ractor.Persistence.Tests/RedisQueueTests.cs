using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Ractor.Persistence.Tests {

    [TestFixture]
    public class RedisQueueTests {

        [Test]
        public void CouldSendAndReceiveMessages() {
            var redis = new Redis(keyNameSpace: "RedisQueueTests");
            var queue = new RedisQueue<string>(redis, "CouldSendAndReceiveMessages", timeout: 5000);
            const int n = 100000;

            var sw = new Stopwatch();
            sw.Start();
            var producer1 = Task.Run(async () => {
                for (var i = 0; i < n; i++) {
                    await queue.TrySendMessage(i.ToString());
                    //await Task.Delay(50);
                }
            });

            //var producer2 = Task.Run(async () => {
            //    for (var i = n; i < 2*n; i++) {
            //        await queue.TrySendMessage(i.ToString());
            //        //await Task.Delay(50);
            //    }
            //});

            var consumer = Task.Run(async () =>
            {
                var c = 0;
                while (true) {
                    var message = await queue.TryReceiveMessage();
                    c++;
                    //if (message.OK) { Console.WriteLine(message.Value); }
                    if (message.OK) {
                       await queue.TryDeleteMessage(message.DeleteHandle);
                    }
                    if (message.OK && c == n ) break; // n * 2
                }
            });

            producer1.Wait();
            //producer2.Wait();
            consumer.Wait();
            sw.Stop();
            Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds}");
            //Thread.Sleep(2000);
        }



        [Test]
        public void ProcessInbox() {
            var redis = new Redis(keyNameSpace: "RedisQueueTests");
            var queue = new RedisQueue<string>(redis, "CouldSendAndReceiveMessages", timeout: 5000);
            const int n = 10000;

            var sw = new Stopwatch();
            sw.Start();
            

            var consumer = Task.Run(async () => {
                var c = 0;
                while (true) {
                    var message = await queue.TryReceiveMessage();
                    c++;
                    if(c % 10000 == 0) Console.WriteLine(c);
                    //if (message.OK) { Console.WriteLine(message.Value); }
                    if (message.OK) {
                        await queue.TryDeleteMessage(message.DeleteHandle);
                    }
                    
                }
            });

            
            consumer.Wait();
            sw.Stop();
            Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds}");
            //Thread.Sleep(2000);
        }
    }
}
