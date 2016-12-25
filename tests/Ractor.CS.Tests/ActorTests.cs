using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Ractor;

namespace Fredis.CS.Tests {

    public class DivideByZeroActor : Actor<int, int> {
        public async override Task<int> Computation(int request) {
            return request / 0;
        }
    }

    public class GreeterActor : Actor<string, string> {
        public GreeterActor() : base(priority: 1, maxConcurrencyPerCpu: 10)
        {
            
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
    }
}
