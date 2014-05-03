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
            //r.Set(value); // key= ":String:i:testvalue", value = "testvalue"
            //var result = r.Get<string>(value, false);
            //var result2 = r.Get<string>("String:i:" + value, true);
            //Assert.AreEqual(value, result, "primitive by type");
            //Assert.AreEqual(value, result2, "primitive by key");

            // TODO add null key trick for purepoco without root as well

            // Set and get key explicitly
            //r.Set(key, value);
            //var result = r.Get<string>(key, true);
            //Assert.AreEqual(value, result, "primitive by key 2");

            //var pp = new PurePoco {
            //    Key = key,
            //    Value = value
            //};
            //r.Set(pp);

            //var ppResult = r.Get<PurePoco>(key, false);
            //Assert.AreEqual(ppResult.Value, value, "PP by type");
            //ppResult = r.Get<PurePoco>("pp:i:" + key, true);
            //Assert.AreEqual(ppResult.Value, value, "PP by key");
            //ppResult = r.Get<PurePoco>("pp:i:no_suh_key" + key, true);
            //Assert.AreEqual(ppResult, null, "PP by nonexistent key");

        }

        [Test]
        public void TestEvalNil() {
            var res = GetRedis().Eval<string>("return nil");
            Assert.AreEqual(res, null);

            GetRedis().Del(new[] { "a", "b" });

            var lua = @"
local result = redis.call('RPOP', KEYS[1])
if result ~= nil then
    redis.call('HSET', KEYS[2], ARGV[1], result)
end
return result";
            res = GetRedis().Eval<string>(lua, new[] { "a", "b" }, new[] {"field" });
            Assert.AreEqual(res, null);

            GetRedis().LPush<string>("a", "value");
            res = GetRedis().Eval<string>(lua, new[] { "a", "b" }, new[] { "field" });
            var field = GetRedis().HGet<string>("b", "field");
            Assert.AreEqual(res, "value");
            Assert.AreEqual(field, "value");

        }

    }
}
