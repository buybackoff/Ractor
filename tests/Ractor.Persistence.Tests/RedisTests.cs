using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Ractor.Persistence.Tests {

    [Redis(Name = "pp", Compressed = false)]
    public class PurePoco {
        [RedisKey]
        public string Key { get; set; }
        public string Value { get; set; }
    }

    [TestFixture]
    public class RedisTests {

        Redis GetRedis() {
            return new Redis("localhost", "test");
        }

        [Test]
        public void TestSortedSetAdd()
        {
            var r = GetRedis();

            var root = "testroot";
            var fullRoot = "String:i:testroot:sets:String";

            var dict = new Dictionary<string, double>();

            for (int i = 0; i < 10; i++)
            {
                dict.Add(i.ToString(), (double)i);
            }
            var value1 = 10;
            var result1 = r.SSAdd<string, string>(root, dict);
            Console.WriteLine("SortedSetAdd1 " + result1);
            r.Del(fullRoot);

            var value2 = 10;
            var result2 = r.SSAddAsync<string, string>(root, dict).Result;
            Console.WriteLine("SortedSetAdd2 " + result2);
            r.Del(fullRoot);

            var val = "10";
            var score = 0.0;
            var result3 = r.SSAdd<string, string>(root, val, score);
            Console.WriteLine("SortedSetAdd3 " + result3);
            r.Del(fullRoot);

            var result4 = r.SSAddAsync<string, string>(root, val, score).Result;
            Console.WriteLine("SortedSetAdd4 " + result4);
            r.Del(fullRoot);


            var value5 = 10;
            var result5 = r.SSAdd<string>(root, dict);
            Console.WriteLine("SortedSetAdd5 " + result5);
            r.Del(root);

            var value6 = 10;
            var result6 = r.SSAddAsync<string>(root, dict).Result;
            Console.WriteLine("SortedSetAdd6 " + result6);
            r.Del(root);

            var result7 = r.SSAdd<string>(root, val, score);
            Console.WriteLine("SortedSetAdd7 " + result7);
            r.Del(root);

            var result8 = r.SSAddAsync<string>(root, val, score).Result;
            Console.WriteLine("SortedSetAdd8 " + result8);
            r.Del(root);

            Assert.AreEqual(result1, value1);
            Assert.AreEqual(result2, value2);
            Assert.True(result3);
            Assert.True(result4);

            Assert.AreEqual(result5, value5);
            Assert.AreEqual(result6, value6);
            Assert.True(result7);
            Assert.True(result8);
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
            var r = GetRedis();

            var res = r.Eval<string>("return nil");
            Assert.AreEqual(res, null);

            GetRedis().Del(new[] { "a", "b" });

            var lua = @"
local result = redis.call('RPOP', KEYS[1])
if result ~= nil then
    redis.call('HSET', KEYS[2], KEYS[3], result)
end
return result";

            res = r.Eval<string>(lua, new[] { r.KeyNameSpace + "a", r.KeyNameSpace + "b", "field" });
            Assert.AreEqual(res, null);

            r.LPush<string>("a", "value");
            res = GetRedis().Eval<string>(lua, new[] { r.KeyNameSpace + "a", r.KeyNameSpace + "b", "field" });
            var field = GetRedis().HGet<string>("b", "field");
            Assert.AreEqual(res, "value");
            Assert.AreEqual(field, "value");

        }

        [Test]
        public void TestEval() {
            var r = GetRedis();

            var lua = @"
local result = redis.call('RPOP', KEYS[1])
if result ~= nil then
    redis.call('HSET', KEYS[2], KEYS[3], result)
end
return result";
            r.LPush<string>("greeter:Mailbox:inbox", "value");


            var res = r.Eval<string>(lua, new[] { r.KeyNameSpace + "greeter:Mailbox:inbox", r.KeyNameSpace + "test:greeter:Mailbox:pipeline", "test" });

            Assert.AreEqual("value", res);

            Console.WriteLine(res);
        }


        [Test]
        public void TestSyncCallFromNestedAsyncs() {
            // Have seen multiple timeout errors when a sync call to Redis
            // was done from async Services and Hubs
            var r = GetRedis();
            Assert.True(First(r).Result);
        }

        private async Task<bool> First(Redis r) { return await Second(r); }

        private async Task<bool> Second(Redis r) {
            return await Third(r);
        }

        private async Task<bool> Third(Redis r) {

            for (int i = 0; i < 10000; i++) {
                r.HSet<string>("testKey", "testField", "testValue");
                r.HGet<string>("testKey", "testField");
                r.Del("testKey");
            }
            await Task.Delay(1);
            return true;
        }

    }
}
