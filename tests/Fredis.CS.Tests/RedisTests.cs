using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Fredis.Persistence.Tests {

    [CacheContract(Name = "pp", Compressed = false)]
    public class PurePoco {
        [CacheKey]
        public string Key { get; set; }
        public string Value { get; set; }
    }

    [TestFixture]
    public class RedisTests {

        Redis GetRedis() {
            return new Redis("localhost", "test");
        }

        [Test]
        public void TestStringSetGet() {
            var r = GetRedis();

            var key = "testkey";
            var value = "testvalue";

            // Typed usage, weird for string but expected
            r.Set(value); // key= ":String:i:testvalue", value = "testvalue"
            var result = r.Get<string>(value, false);
            var result2 = r.Get<string>("String:i:" + value, true);
            Assert.AreEqual(value, result, "primitive by type");
            Assert.AreEqual(value, result2, "primitive by key");


            // Set and get key explicitly
            r.Set(key, value);
            result = r.Get<string>(key, true);
            Assert.AreEqual(value, result, "primitive by key 2");

            var pp = new PurePoco {
                Key = key,
                Value = value
            };
            r.Set(pp);

            var ppResult = r.Get<PurePoco>(key, false);
            Assert.AreEqual(ppResult.Value, value, "PP by type");
            ppResult = r.Get<PurePoco>("pp:i:" + key, true);
            Assert.AreEqual(ppResult.Value, value, "PP by key");
            ppResult = r.Get<PurePoco>("pp:i:no_suh_key" + key, true);
            Assert.AreEqual(ppResult, null, "PP by nonexistent key");

        }

    }
}
