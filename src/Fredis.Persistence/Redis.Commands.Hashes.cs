using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;
using StackExchange.Redis;

// WIP
//          <T>     <T>Async    key     keyAsycn    Tests
// HDel     x       x           x       x
// HExists  x       x           x       x
// HGet     x       x           x       x
// HGetAll  x       x           x       x
// HIncrBy  x       x           x       x
// HIncrByF x       x           x       x
// HKeys    x       x           x       x
// HLen     x       x           x       x
// HMGet    x       x           x       x
// HMSet    x       x           x       x
// HSet     x
// HVals    x       x           x       x
// HScan

namespace Fredis {

    public partial class Redis {

        #region HDel

        /// <summary>
        /// If hashKey is provided, the hash key is set to "ns:RootFullKey:hashes:hashKey",
        /// else the hash key is set to "ns:RootFullKey:hashes:TValueTypePrefix".
        /// </summary>
        public bool HDel<TRoot, TValue>(TRoot root, string field, string hashKey = null, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().HashDelete(k, field, ff);
            return result;
        }

        /// <summary>
        /// If hashKey is provided, the hash key is set to "ns:RootFullKey:hashes:hashKey",
        /// else the hash key is set to "ns:RootFullKey:hashes:TValueTypePrefix".
        /// </summary>
        public async Task<bool> HDelAsync<TRoot, TValue>(TRoot root, string field, string hashKey = null, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().HashDeleteAsync(k, field, ff);
            return result;
        }

        public bool HDel(string fullKey, string field, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().HashDelete(k, field, ff);
            return result;
        }

        public async Task<bool> HDelAsync(string fullKey, string field, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().HashDeleteAsync(k, field, ff);
            return result;
        }

        #endregion
        
        #region HExists

        public bool HExists<TRoot, TValue>(TRoot root, string field, string hashKey = null) {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var result = GetDb().HashExists(key, field);
            return result;
        }

        public async Task<bool> HExistsAsync<TRoot, TValue>(TRoot root, string field, string hashKey = null) {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var result = await GetDb().HashExistsAsync(key, field);
            return result;
        }

        public bool HExists(string fullKey, string field) {
            var k = _nameSpace + fullKey;
            var result = GetDb().HashExists(k, field);
            return result;
        }

        public async Task<bool> HExistsAsync(string fullKey, string field) {
            var k = _nameSpace + fullKey;
            var result = await GetDb().HashExistsAsync(k, field);
            return result;
        }

        #endregion
        
        #region HGet

        public TValue HGet<TRoot, TValue>(TRoot root, string field, string hashKey = null) {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var result = GetDb().HashGet(key, field);
            return UnpackResultNullable<TValue>(result);
        }

        public async Task<TValue> HGetAsync<TRoot, TValue>(TRoot root, string field, string hashKey = null) {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var result = await GetDb().HashGetAsync(key, field);
            return UnpackResultNullable<TValue>(result);
        }

        public T HGet<T>(string fullKey, string field) {
            var k = _nameSpace + fullKey;
            var result = GetDb().HashGet(k, field);
            return UnpackResultNullable<T>(result);
        }

        public async Task<T> HGetAsync<T>(string fullKey, string field) {
            var k = _nameSpace + fullKey;
            var result = await GetDb().HashGetAsync(k, field);
            return UnpackResultNullable<T>(result);
        }

        #endregion

        #region HGetAll

        // TODO test key conversion to string
        public KeyValuePair<string, TValue>[] HGetAll<TRoot, TValue>(TRoot root, string hashKey = null) {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var result = GetDb().HashGetAll(key)
                .Select(he => ((KeyValuePair<RedisValue, RedisValue>) (he)))
                .Select(
                    kvp => new KeyValuePair<string, TValue>(kvp.Key.ToString(), UnpackResultNullable<TValue>(kvp.Value)))
                .ToArray();
            return result;
        }

        public async Task<KeyValuePair<string, TValue>[]> HGetAllAsync<TRoot, TValue>(TRoot root, string hashKey = null) {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var result = (await GetDb().HashGetAllAsync(key))
                .Select(he => ((KeyValuePair<RedisValue, RedisValue>)(he)))
                .Select(
                    kvp => new KeyValuePair<string, TValue>(kvp.Key.ToString(), UnpackResultNullable<TValue>(kvp.Value)))
                .ToArray();
            return result;
        }

        public KeyValuePair<string, T>[] HGetAll<T>(string fullKey) {
            var k = _nameSpace + fullKey;
            var result = GetDb().HashGetAll(k)
               .Select(he => ((KeyValuePair<RedisValue, RedisValue>)(he)))
               .Select(
                   kvp => new KeyValuePair<string, T>(kvp.Key.ToString(), UnpackResultNullable<T>(kvp.Value)))
               .ToArray();
            return result;
        }

        public async Task<KeyValuePair<string, T>[]> HGetAllAsync<T>(string fullKey) {
            var k = _nameSpace + fullKey;
            var result = (await GetDb().HashGetAllAsync(k))
               .Select(he => ((KeyValuePair<RedisValue, RedisValue>)(he)))
               .Select(
                   kvp => new KeyValuePair<string, T>(kvp.Key.ToString(), UnpackResultNullable<T>(kvp.Value)))
               .ToArray();
            return result;
        }

        #endregion

        #region HIncrBy

        public long HIncrBy<TRoot>(TRoot root, string field, long increment = 1L, string hashKey = null) {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? "counters");
            var result = GetDb().HashIncrement(key, field, increment);
            return result;
        }

        public async Task<long> HIncrByAsync<TRoot>(TRoot root, string field, long increment = 1L, string hashKey = null) {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? "counters");
            var result = await GetDb().HashIncrementAsync(key, field, increment);
            return result;
        }

        public long HIncrBy(string fullKey, string field, long increment = 1L) {
            var k = _nameSpace + fullKey;
            var result = GetDb().HashIncrement(k, field, increment);
            return result;
        }

        public async Task<long> HIncrByAsync(string fullKey, string field, long increment = 1L) {
            var k = _nameSpace + fullKey;
            var result = await GetDb().HashIncrementAsync(k, field, increment);
            return result;
        }

        #endregion

        #region HIncrByFloat

        public double HIncrByFloat<TRoot>(TRoot root, string field, double increment, string hashKey = null) {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? "counters");
            var result = GetDb().HashIncrement(key, field, increment);
            return result;
        }

        public async Task<double> HIncrByFloatAsync<TRoot>(TRoot root, string field, double increment, string hashKey = null) {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? "counters");
            var result = await GetDb().HashIncrementAsync(key, field, increment);
            return result;
        }

        public double HIncrByFloatBy(string fullKey, string field, double increment) {
            var k = _nameSpace + fullKey;
            var result = GetDb().HashIncrement(k, field, increment);
            return result;
        }

        public async Task<double> HIncrByFloatAsync(string fullKey, string field, double increment) {
            var k = _nameSpace + fullKey;
            var result = await GetDb().HashIncrementAsync(k, field, increment);
            return result;
        }

        #endregion

        #region HKeys

        public string[] HKeys<TRoot, TValue>(TRoot root, string hashKey = null) {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var result = GetDb().HashKeys(key).Select(x => (string)x).ToArray();
            return result;
        }

        public async Task<string[]> HKeysAsync<TRoot, TValue>(TRoot root, string field, string hashKey = null) {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var result = (await GetDb().HashKeysAsync(key)).Select(x => (string)x).ToArray();
            return result;
        }

        public string[] HKeys(string fullKey) {
            var k = _nameSpace + fullKey;
            var result = GetDb().HashKeys(k).Select(x => (string)x).ToArray();
            return result;
        }

        public async Task<string[]> HKeysAsync(string fullKey) {
            var k = _nameSpace + fullKey;
            var result = (await GetDb().HashKeysAsync(k)).Select(x => (string)x).ToArray();
            return result;
        }

        #endregion
        
        #region HLen

        public long HLen<TRoot, TValue>(TRoot root, string hashKey = null) {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var result = GetDb().HashLength(key);
            return result;
        }

        public async Task<long> HLenAsync<TRoot, TValue>(TRoot root, string field, string hashKey = null) {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var result = await GetDb().HashLengthAsync(key);
            return result;
        }

        public long HLen(string fullKey) {
            var k = _nameSpace + fullKey;
            var result = GetDb().HashLength(k);
            return result;
        }

        public async Task<long> HLenAsync(string fullKey) {
            var k = _nameSpace + fullKey;
            var result = await GetDb().HashLengthAsync(k);
            return result;
        }

        #endregion
        
        #region HMGet

        public TValue[] HMGet<TRoot, TValue>(TRoot root, string[] fields, string hashKey = null) {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var results = GetDb().HashGet(key, fields.Select(x => (RedisValue)x).ToArray());
            return results.Select(UnpackResultNullable<TValue>).ToArray();
        }

        public async Task<TValue[]> HMGetAsync<TRoot, TValue>(TRoot root, string[] fields, string hashKey = null) {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var results = await GetDb().HashGetAsync(key, fields.Select(x => (RedisValue)x).ToArray());
            return results.Select(UnpackResultNullable<TValue>).ToArray();
        }

        public T[] HMGet<T>(string fullKey, string[] fields) {
            var k = _nameSpace + fullKey;
            var results = GetDb().HashGet(k, fields.Select(x => (RedisValue)x).ToArray());
            return results.Select(UnpackResultNullable<T>).ToArray();
        }

        public async Task<T[]> HMGetAsync<T>(string fullKey, string[] fields) {
            var k = _nameSpace + fullKey;
            var results = await GetDb().HashGetAsync(k, fields.Select(x => (RedisValue)x).ToArray());
            return results.Select(UnpackResultNullable<T>).ToArray();
        }

        #endregion
        
        #region HMSet

        public bool HMSet<TRoot, TValue>(TRoot root, TValue[] valuesWithKey, string hashKey = null,
            bool fireAndForget = false)
            where TValue : IDataObject // where  TRoot : new()  // issue #18
        {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var kvps = valuesWithKey.Select(valueWithKey =>
                (HashEntry)(new KeyValuePair<RedisValue, RedisValue>(_nameSpace + GetItemFullKey(valueWithKey),
                    PackValueNullable(valueWithKey)))).ToArray();

            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            GetDb().HashSet(key, kvps, ff);
            return true;
        }

        public async Task<bool> HMSetAsync<TRoot, TValue>(TRoot root, TValue[] valuesWithKey, string hashKey = null,
            bool fireAndForget = false)
            where TValue : IDataObject // where  TRoot : new()  // issue #18
        {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var kvps = valuesWithKey.Select(valueWithKey =>
                (HashEntry)(new KeyValuePair<RedisValue, RedisValue>(_nameSpace + GetItemFullKey(valueWithKey),
                    PackValueNullable(valueWithKey)))).ToArray();

            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            await GetDb().HashSetAsync(key, kvps, ff);
            return true;
        }

        public bool HMSet<TRoot, TValue>(TRoot root, KeyValuePair<string, TValue>[] valuesWithKey, string hashKey = null,
            bool fireAndForget = false)
            //where TRoot : new() // not a string issue #18
        {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var kvps = valuesWithKey.Select(valueWithKey =>
                (HashEntry)(new KeyValuePair<RedisValue, RedisValue>(_nameSpace + valueWithKey.Key,
                    PackValueNullable(valueWithKey)))).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            GetDb().HashSet(key, kvps, ff);
            return true;
        }

        public async Task<bool> HMSetAsync<TRoot, TValue>(TRoot root, KeyValuePair<string, TValue>[] valuesWithKey, string hashKey = null,
            bool fireAndForget = false)
            //where TRoot : new() // not a string issue #18
        {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var kvps = valuesWithKey.Select(valueWithKey =>
                (HashEntry)(new KeyValuePair<RedisValue, RedisValue>(_nameSpace + valueWithKey.Key,
                    PackValueNullable(valueWithKey)))).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            await GetDb().HashSetAsync(key, kvps, ff);
            return true;
        }

        public bool HMSet<TValue>(string fullKey, KeyValuePair<string, TValue>[] valuesWithKey,
            bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var kvps = valuesWithKey.Select(valueWithKey =>
                (HashEntry)(new KeyValuePair<RedisValue, RedisValue>(_nameSpace + valueWithKey.Key,
                    PackValueNullable(valueWithKey)))).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            GetDb().HashSet(k, kvps, ff);
            return true;
        }

        public async Task<bool> HMSetAsync<TValue>(string fullKey, KeyValuePair<string, TValue>[] valuesWithKey,
            bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var kvps = valuesWithKey.Select(valueWithKey =>
                (HashEntry)(new KeyValuePair<RedisValue, RedisValue>(_nameSpace + valueWithKey.Key,
                    PackValueNullable(valueWithKey)))).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            await GetDb().HashSetAsync(k, kvps, ff);
            return true;
        }


        #endregion

        #region HSet

        public bool HSet<TRoot, TValue>(TRoot root, TValue valueWithKey, string hashKey = null,
            When when = When.Always, bool fireAndForget = false)
            where TValue : IDataObject // where  TRoot : new()  // issue #18
        {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var f = GetItemKey(valueWithKey);
            var v = PackValueNullable(valueWithKey);
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().HashSet(key, f, v, wh, ff);
        }

        public async Task<bool> HSetAsync<TRoot, TValue>(TRoot root, TValue valueWithKey, string hashKey = null,
            When when = When.Always, bool fireAndForget = false)
            where TValue : IDataObject // where  TRoot : new()  // issue #18
        {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var f = GetItemKey(valueWithKey);
            var v = PackValueNullable(valueWithKey);
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().HashSetAsync(key, f, v, wh, ff);
        }

        public bool HSet<TRoot, TValue>(TRoot root, string field, TValue value, string hashKey = null,
            When when = When.Always, bool fireAndForget = false)
            //where TRoot : new() // not a string issue #18
        {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var f = field ?? GetItemKey(value);
            var v = PackValueNullable(value);
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().HashSet(key, f, v, wh, ff);
        }

        public async Task<bool> HSetAsync<TRoot, TValue>(TRoot root, string field, TValue value, string hashKey = null,
            When when = When.Always, bool fireAndForget = false)
            //where TRoot : new() // not a string issue #18
        {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var f = field ?? GetItemKey(value);
            var v = PackValueNullable(value);
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().HashSetAsync(key, f, v, wh, ff);
        }

        public bool HSet<TValue>(string fullKey, string field, TValue value,
            When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var f = field ?? GetItemKey(value);
            var v = PackValueNullable(value);
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().HashSet(k, f, v, wh, ff);
        }

        public async Task<bool> HSetAsync<TValue>(string fullKey, string field, TValue value,
            When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var f = field ?? GetItemKey(value);
            var v = PackValueNullable(value);
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().HashSetAsync(k, f, v, wh, ff);
        }


        #endregion

        #region HVals

        public TValue[] HVals<TRoot, TValue>(TRoot root, string hashKey = null) {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var results = GetDb().HashValues(key);
            return results.Select(UnpackResultNullable<TValue>).ToArray();
        }

        public async Task<TValue[]> HValsAsync<TRoot, TValue>(TRoot root, string field, string hashKey = null) {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var results = await GetDb().HashValuesAsync(key);
            return results.Select(UnpackResultNullable<TValue>).ToArray();
        }

        public T[] HVals<T>(string fullKey) {
            var k = _nameSpace + fullKey;
            var results = GetDb().HashValues(k);
            return results.Select(UnpackResultNullable<T>).ToArray();
        }

        public async Task<T[]> HValsAsync<T>(string fullKey) {
            var k = _nameSpace + fullKey;
            var results = await GetDb().HashValuesAsync(k);
            return results.Select(UnpackResultNullable<T>).ToArray();
        }
        
        #endregion
    
    }


    public class RedisDictionary<TKey, TValue> : IDictionary<TKey, TValue> {
        // need sync channel
        // need cache + cache policy

        private string _key;
        private string _prefix;
        private string _channel;
        // used for local storage during enumerations
        // channel subscriber should add/remove/change it when not null
        // so that default enumerator could throw "colelction changed" as with normal IDictionary
        private Dictionary<TKey, TValue> _dictionary;
        private MemoryCache _cache = Redis.Cache;

        /// <summary>
        /// 
        /// </summary>
        public RedisDictionary(string key) {
            _key = key;
            _prefix = _key + ":";
            _channel = _prefix + "syncChannel";
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() { throw new System.NotImplementedException(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        public void Add(KeyValuePair<TKey, TValue> item) { throw new System.NotImplementedException(); }
        public void Clear() { throw new System.NotImplementedException(); }
        public bool Contains(KeyValuePair<TKey, TValue> item) { throw new System.NotImplementedException(); }
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) { throw new System.NotImplementedException(); }
        public bool Remove(KeyValuePair<TKey, TValue> item) { throw new System.NotImplementedException(); }
        public int Count { get; private set; }
        public bool IsReadOnly { get; private set; }
        public bool ContainsKey(TKey key) { throw new System.NotImplementedException(); }
        public void Add(TKey key, TValue value) { throw new System.NotImplementedException(); }
        public bool Remove(TKey key) { throw new System.NotImplementedException(); }
        public bool TryGetValue(TKey key, out TValue value) { throw new System.NotImplementedException(); }
        public TValue this[TKey key] { get { throw new System.NotImplementedException(); } set { throw new System.NotImplementedException(); } }

        public ICollection<TKey> Keys { get; private set; }
        public ICollection<TValue> Values { get; private set; }
    }

}
