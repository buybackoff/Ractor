using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Ractor {

    // WIP
    // For each write command we need 
    // - fully typed version with implicit key + Async version;
    // - typed version with custom explicit key + Async version;
    // 
    // For each read command we need:
    // - typed version with root/owner object (for collections only) + Async version
    // - typed version with custom explicit key and optional bool to indicate prefixed explicit full key + Async version
    //
    // Need to test each method with primitive, struct(?), pure POCO, CacheContract decorated POCO, IDO, IDDO

    // Lists
    // Strings
    // Hashes
    // Sets
    // SortedSets
    // Keys
    // Other

    // if I do not understand anything in Ractor API without documentation then something is wrong
    // TODO write docs only after extracting interface and on inteface, not implementation

    public partial class Redis {
        // misc commands here

        public bool Del(string key) {
            var k = _nameSpace + key;
            return GetDb().KeyDelete(k);
        }

        public async Task<bool> DelAsync(string key) {
            var k = _nameSpace + key;
            return await GetDb().KeyDeleteAsync(k);
        }

        public long Del(string[] keys) {
            var ks = keys.Select(k => (RedisKey) (_nameSpace + k)).ToArray();
            return GetDb().KeyDelete(ks);
        }

        public async Task<long> DelAsync(string[] keys) {
            var ks = keys.Select(k => (RedisKey)(_nameSpace + k)).ToArray();
            return await GetDb().KeyDeleteAsync(ks);
        }


        /// <summary>
        /// Runs lua script and returns a deserialized result (int, long, double, bool, string are not deserialized).
        /// Eval doesn't prefixes keys, should use redis.KeyNameSpace + ":" + key to access a key.
        /// </summary>
        public TResult Eval<TResult>(string script, string[] fullKeysWithPrefix = null, object[] values = null) {
            var result = GetDb().ScriptEvaluate(script,
                fullKeysWithPrefix == null ? null : fullKeysWithPrefix.Select(k => (RedisKey)(k)).ToArray(),
                values == null ? null : values.Select(PackValueNullable).ToArray());
            var type = typeof (TResult);
            if (type == typeof (int)) {
                return (TResult)((object)(int)(RedisValue)result);
            }
            if (type == typeof(long)) {
                return (TResult)((object)(long)(RedisValue)result);
            }
            if (type == typeof(double)) {
                return (TResult)((object)(double)(RedisValue)result);
            }
            if (type == typeof(bool)) {
                return (TResult)((object)(bool)(RedisValue)result);
            }
            if (type == typeof(string)) {
                return (TResult)((object)(string)(RedisValue)result);
            }
            return UnpackResultNullable<TResult>((RedisValue)result);
        }

        public void Eval(string script, string[] keys = null, object[] values = null) {
            GetDb().ScriptEvaluate(script,
                keys == null ? null : keys.Select(k => (RedisKey)(k)).ToArray(),
                values == null ? null : values.Select(PackValueNullable).ToArray());
        }

        /// <summary>
        /// Runs lua script and returns a deserialized result (int, long, double, bool, string are not deserialized).
        /// Eval doesn't prefixes keys, should use redis.KeyNameSpace + ":" + key to access a key.
        /// </summary>
        public async Task<TResult> EvalAsync<TResult>(string script, string[] fullKeysWithPrefix = null, object[] values = null) {
            var result = await GetDb().ScriptEvaluateAsync(script,
                fullKeysWithPrefix == null ? null : fullKeysWithPrefix.Select(k => (RedisKey)(k)).ToArray(),
                values == null ? null : values.Select(PackValueNullable).ToArray());
            var type = typeof(TResult);
            if (type == typeof(int)) {
                return (TResult)((object)(int)(RedisValue)result);
            }
            if (type == typeof(long)) {
                return (TResult)((object)(long)(RedisValue)result);
            }
            if (type == typeof(double)) {
                return (TResult)((object)(double)(RedisValue)result);
            }
            if (type == typeof(bool)) {
                return (TResult)((object)(bool)(RedisValue)result);
            }
            if (type == typeof(string)) {
                return (TResult)((object)(string)(RedisValue)result);
            }
            return UnpackResultNullable<TResult>((RedisValue)result);
        }


    }
}
