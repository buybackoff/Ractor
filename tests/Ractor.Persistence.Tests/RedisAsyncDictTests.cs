using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Ractor.Persistence.Tests {

    [TestFixture]
    public class RedisAsyncDictTests {

        [Test]
        public void CouldFillAndTakeValues() {
            var redis = new Redis(keyNameSpace: "RedisAsyncDictTests");
            var rdict = new RedisAsyncDictionary<string>(redis, "CouldFillAndTakeValues", timeout: -1);
            const int n = 100;

            var sw = new Stopwatch();
            sw.Start();
            var producer1 = Task.Run(async () => {
                for (var i = 0; i < n; i++) {
                    var str = i.ToString();
                    var sendResult = await rdict.TryFill(str, str);
                    if (!sendResult) Assert.Fail("Cannot send a message");
                    //await Task.Delay(50);
                }
            });

            var consumer = Task.Run(async () => {
                var c = 0;
                while (true) {
                    var str = c.ToString();
                    var message = await rdict.TryTake(str);
                    Console.WriteLine($"Received: {message}");
                    Assert.AreEqual(str, message);
                    c++;
                    if (c == n) break;
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
        public void CouldFillAndTakeValuesInParallel() {
            var redis = new Redis(keyNameSpace: "RedisAsyncDictTests");
            var rdict = new RedisAsyncDictionary<string>(redis, "CouldFillAndTakeValues", timeout: -1);
            const int n = 100000;

            var sw = new Stopwatch();
            sw.Start();

            var producer1 = Task.Run(async () => {
                for (var i = 0; i < n; i++) {
                    var str = i.ToString();
                    rdict.TryFill(str, str); // do not await
                    //var sendResult = await rdict.TryFill(str, str);
                    //if (!sendResult) Assert.Fail("Cannot send a message");
                    //await Task.Delay(50);
                }
            });
            
            
            var consumer = Task.Run(async () => {
                List<Task<string>> list = new List<Task<string>>();
                for (int i = 0; i < n; i++) {
                    var str = i.ToString();
                    list.Add(rdict.TryTake(str));
                }
                await Task.WhenAll(list);
                sw.Stop();

                for (int i = 0; i < n; i++) {
                    Assert.AreEqual(i.ToString(), list[i].Result);
                }
            });

            producer1.Wait();
            consumer.Wait();

            Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds}");
        }
    }
}
