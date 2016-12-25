using NUnit.Framework;
using Ractor;
using System;
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
            Assert.AreEqual("PythonEchoActor", actor.Id);
            actor.Start();
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 1; i++)
            {
                var message = new String('a', 10000);
                var result = await actor.PostAndGetResult(message);
                Assert.AreEqual(message, result);
            }
            sw.Stop();
            Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds}");
        }
    }
}
