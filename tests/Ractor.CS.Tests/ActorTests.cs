using NUnit.Framework;
using Ractor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Fredis.CS.Tests {

    public class DivideByZeroActor : Actor<int, int> {

        public async override Task<int> Computation(int request) {
            return request / 0;
        }
    }

    public class GreeterActor : Actor<string, string> {

        public GreeterActor() : base(priority: 1, maxConcurrencyPerCpu: 10) {
        }

        public async override Task<string> Computation(string name) {
            return $"Hello, {name}";
        }
    }

    [TestFixture]
    public class ActorTests {

        [Test]
        public void CouldDivideByZero() {
            var dz = new DivideByZeroActor();
            dz.Start();
            var result = dz.PostAndGetResult(123);
            Assert.Catch<Exception>(() => {
                result.GetAwaiter().GetResult();
            });
        }

        [Test]
        public async void CouldGreet() {
            var actor = new GreeterActor();
            actor.Start();
            var result = await actor.PostAndGetResult("Ractor");
            Assert.AreEqual("Hello, Ractor", result);
        }

        [Test]
        public async void CouldReceiveEcho() {
            var actor = new EchoActor();
            actor.Start();
            var result = await actor.PostAndGetResult("Ractor");
            Assert.AreEqual("Ractor", result);
        }

        [Test]
        public async void CouldReceiveEchoFromPython() {
            var actor = new PythonEchoActor();
            Assert.AreEqual("PythonEcho", actor.Id);
            actor.Start();
            var sw = new Stopwatch();
            var message = "Hello, Python"; //new String('x', 10000); // "Hello, Python";
            sw.Start();
            List<Task<string>> tasks = new List<Task<string>>();
            for (int i = 0; i < 20000; i++) {
                tasks.Add(actor.PostAndGetResult(i.ToString()));
            }
            await Task.WhenAll(tasks);
            sw.Stop();
            for (int i = 0; i < 20000; i++) {
                Assert.AreEqual((i * 2).ToString(), tasks[i].Result);
            }
            Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds}");
        }

    }
}
