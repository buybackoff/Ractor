using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;


// WIP
//          <T>     <T>Async    key     keyAsycn    Tests   TestsAsync
// Append   x       x           x       x
// Decr     x       x           x       x
// DecrBy   x       x           x       x
// Get      x       x           x       x           x   
// GetSet   x       x           x       x              
// Incr     x       x           x       x
// IncrBy   x       x           x       x
// IncrByF  x       x           x       x
// MGet     x       x           x       x
// MSet     x       x           x       x
// Set      x       x           x       x           x
// StrLen   x       x           x       x           x   


namespace Fredis {
    /// <summary>
    /// String commands
    /// </summary>
    public partial class Redis {


        #region Append

        public long Append<TRoot>(TRoot root, string stringKey, string value, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + stringKey;
            var v = value;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringAppend(k, v, ff);
        }

        public async Task<long> AppendAsync<TRoot>(TRoot root, string stringKey, string value,
            bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + stringKey;
            var v = value;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringAppendAsync(k, v, ff);
        }

        public long Append(string fullKey, string value, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = value;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringAppend(k, v, ff);
        }

        public async Task<long> AppendAsync(string fullKey, string value, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = value;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringAppendAsync(k, v, ff);
        }

        #endregion


        #region Decr

        public long Decr<TRoot>(TRoot root, string stringKey, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + stringKey;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringDecrement(k, 1L, ff);
        }

        public long DecrBy<TRoot>(TRoot root, string stringKey, long value, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + stringKey;
            var v = value;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringDecrement(k, v, ff);
        }


        public async Task<long> DecrAsync<TRoot>(TRoot root, string stringKey, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + stringKey;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringDecrementAsync(k, 1L, ff);
        }

        public async Task<long> DecrByAsync<TRoot>(TRoot root, string stringKey, long value, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + stringKey;
            var v = value;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringDecrementAsync(k, v, ff);
        }


        public long Decr(string fullKey, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringDecrement(k, 1L, ff);
        }

        public long DecrBy(string fullKey, long value, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = value;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringDecrement(k, v, ff);
        }


        public async Task<long> DecrAsync(string fullKey, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringDecrementAsync(k, 1L, ff);
        }

        public async Task<long> DecrByAsync(string fullKey, long value, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = value;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringDecrementAsync(k, v, ff);
        }


        #endregion


        #region Get

        public TValue Get<TRoot, TValue>(TRoot root, string stringKey) {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + stringKey;
            var result = GetDb().StringGet(k);
            return UnpackResultNullable<TValue>(result);
        }

        public async Task<TValue> GetAsync<TRoot, TValue>(TRoot root, string stringKey) {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + stringKey;
            var result = await GetDb().StringGetAsync(k);
            return UnpackResultNullable<TValue>(result);
        }

        /// <summary>
        /// When useTypePrefix is true, we use a type prefix for T to make a key
        /// like "TypePrefix:i:key"
        /// </summary>
        public T Get<T>(string key, bool useTypePrefix = false) {
            var k = _nameSpace + (useTypePrefix
                ? GetTypePrefix<T>() + ":i:" + key
                : key);
            var result = GetDb().StringGet(k);
            return UnpackResultNullable<T>(result);
        }

        public async Task<T> GetAsync<T>(string key, bool useTypePrefix = false) {
            var k = _nameSpace + (useTypePrefix
                ? GetTypePrefix<T>() + ":i:" + key
                : key);
            var result = await GetDb().StringGetAsync(k);
            return UnpackResultNullable<T>(result);
        }

        #endregion


        #region GetSet

        public TValue GetSet<TValue>(TValue valueWithKey, bool fireAndForget = false)
        where TValue : IDataObject {
            var k = _nameSpace + GetItemFullKey(valueWithKey);
            var v = PackValueNullable(valueWithKey);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return UnpackResultNullable<TValue>(GetDb().StringGetSet(k, v, ff));
        }

        public async Task<TValue> GetSetAsync<TValue>(TValue valueWithKey, bool fireAndForget = false)
        where TValue : IDataObject {
            var k = _nameSpace + GetItemFullKey(valueWithKey);
            var v = PackValueNullable(valueWithKey);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return UnpackResultNullable<TValue>(await GetDb().StringGetSetAsync(k, v, ff));
        }

        public TValue GetSet<TValue>(string fullKey, TValue value, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return UnpackResultNullable<TValue>(GetDb().StringGetSet(k, v, ff));
        }

        public async Task<TValue> GetSetAsync<TValue>(string fullKey, TValue value, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return UnpackResultNullable<TValue>(await GetDb().StringGetSetAsync(k, v, ff));
        }

        public TValue GetSet<TRoot, TValue>(TRoot root, TValue valueWithKey, bool fireAndForget = false)
        where TValue : IDataObject {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + GetItemKey(valueWithKey);
            var v = PackValueNullable(valueWithKey);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return UnpackResultNullable<TValue>(GetDb().StringGetSet(k, v, ff));
        }

        public async Task<TValue> GetSetAsync<TRoot, TValue>(TRoot root, TValue valueWithKey, bool fireAndForget = false)
        where TValue : IDataObject {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + GetItemKey(valueWithKey);
            var v = PackValueNullable(valueWithKey);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return UnpackResultNullable<TValue>(await GetDb().StringGetSetAsync(k, v, ff));
        }

        public TValue GetSet<TRoot, TValue>(TRoot root, string stringKey, TValue value, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + (stringKey ?? GetItemKey(value));
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return UnpackResultNullable<TValue>(GetDb().StringGetSet(k, v, ff));
        }

        public async Task<TValue> GetSetAsync<TRoot, TValue>(TRoot root, string stringKey, TValue value, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + (stringKey ?? GetItemKey(value));
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return UnpackResultNullable<TValue>(await GetDb().StringGetSetAsync(k, v, ff));
        }

        #endregion


        #region Incr

        public long Incr<TRoot>(TRoot root, string stringKey, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + stringKey;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringIncrement(k, 1L, ff);
        }

        public long IncrBy<TRoot>(TRoot root, string stringKey, long value, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + stringKey;
            var v = value;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringIncrement(k, v, ff);
        }

        public double IncrByFloat<TRoot>(TRoot root, string stringKey, double value, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + stringKey;
            var v = value;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringIncrement(k, v, ff);
        }

        public async Task<long> IncrAsync<TRoot>(TRoot root, string stringKey, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + stringKey;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringIncrementAsync(k, 1L, ff);
        }

        public async Task<long> IncrByAsync<TRoot>(TRoot root, string stringKey, long value, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + stringKey;
            var v = value;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringIncrementAsync(k, v, ff);
        }

        public async Task<double> IncrByFloatAsync<TRoot>(TRoot root, string stringKey, double value,
            bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + stringKey;
            var v = value;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringIncrementAsync(k, v, ff);
        }


        public long Incr(string fullKey, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringIncrement(k, 1L, ff);
        }

        public long IncrBy(string fullKey, long value, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = value;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringIncrement(k, v, ff);
        }

        public double IncrByFloat(string fullKey, double value, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = value;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringIncrement(k, v, ff);
        }

        public async Task<long> IncrAsync(string fullKey, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringIncrementAsync(k, 1L, ff);
        }

        public async Task<long> IncrByAsync(string fullKey, long value, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = value;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringIncrementAsync(k, v, ff);
        }

        public async Task<double> IncrByFloatAsync(string fullKey, double value, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = value;
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringIncrementAsync(k, v, ff);
        }

        #endregion


        #region MGet

        public TValue[] MGet<TRoot, TValue>(TRoot root, string[] stringKeys) {
            var k = stringKeys.Select(stringKey => (RedisKey)(_nameSpace + GetItemFullKey(root) + ":strings:" + stringKey)).ToArray();
            var results = GetDb().StringGet(k);
            return results.Select(UnpackResultNullable<TValue>).ToArray();
        }

        public async Task<TValue[]> MGetAsync<TRoot, TValue>(TRoot root, string[] stringKeys) {
            var k = stringKeys.Select(stringKey => (RedisKey)(_nameSpace + GetItemFullKey(root) + ":strings:" + stringKey)).ToArray();
            var results = await GetDb().StringGetAsync(k);
            return results.Select(UnpackResultNullable<TValue>).ToArray();
        }

        /// <summary>
        /// When useTypePrefix is true, we use a type prefix for T to make a key
        /// like "TypePrefix:i:key"
        /// </summary>
        public T[] MGet<T>(string[] keys, bool useTypePrefix = false) {
            var k = keys.Select(key => (RedisKey)(_nameSpace + (useTypePrefix
                ? GetTypePrefix<T>() + ":i:" + key
                : key))).ToArray();
            var results = GetDb().StringGet(k);
            return results.Select(UnpackResultNullable<T>).ToArray();
        }

        public async Task<T[]> MGetAsync<T>(string[] keys, bool useTypePrefix = false) {
            var k = keys.Select(key => (RedisKey)(_nameSpace + (useTypePrefix
                ? GetTypePrefix<T>() + ":i:" + key
                : key))).ToArray();
            var results = await GetDb().StringGetAsync(k);
            return results.Select(UnpackResultNullable<T>).ToArray();
        }

        #endregion


        #region MSet

        public bool MSet<TValue>(TValue[] valuesWithKey,
            When when = When.Always, bool fireAndForget = false)
        where TValue : IDataObject {
            var kvps = valuesWithKey.Select(valueWithKey =>
                new KeyValuePair<RedisKey, RedisValue>(_nameSpace + GetItemFullKey(valueWithKey),
                    PackValueNullable(valueWithKey))).ToArray();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringSet(kvps, wh, ff);
        }

        public async Task<bool> MSetAsync<TValue>(TValue[] valuesWithKey,
            When when = When.Always, bool fireAndForget = false)
        where TValue : IDataObject {
            var kvps = valuesWithKey.Select(valueWithKey =>
                new KeyValuePair<RedisKey, RedisValue>(_nameSpace + GetItemFullKey(valueWithKey),
                    PackValueNullable(valueWithKey))).ToArray();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringSetAsync(kvps, wh, ff);
        }

        public bool MSet<TValue>(KeyValuePair<string, TValue>[] valuesWithKey,
            When when = When.Always, bool fireAndForget = false) {
            var kvps = valuesWithKey.Select(valueWithKey =>
                new KeyValuePair<RedisKey, RedisValue>(_nameSpace + valueWithKey.Key,
                    PackValueNullable(valueWithKey))).ToArray();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringSet(kvps, wh, ff);
        }

        public async Task<bool> MSetAsync<TValue>(KeyValuePair<string, TValue>[] valuesWithKey,
            When when = When.Always, bool fireAndForget = false) {
            var kvps = valuesWithKey.Select(valueWithKey =>
               new KeyValuePair<RedisKey, RedisValue>(_nameSpace + valueWithKey.Key,
                   PackValueNullable(valueWithKey))).ToArray();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringSetAsync(kvps, wh, ff);
        }

        public bool MSet<TRoot, TValue>(TRoot root, TValue[] valuesWithKey,
            When when = When.Always, bool fireAndForget = false)
        where TValue : IDataObject {
            var kvps = valuesWithKey.Select(valueWithKey =>
                new KeyValuePair<RedisKey, RedisValue>(_nameSpace + GetItemFullKey(root) + ":strings:" + GetItemKey(valueWithKey),
                    PackValueNullable(valueWithKey))).ToArray();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringSet(kvps, wh, ff);
        }

        public async Task<bool> SetAsync<TRoot, TValue>(TRoot root, TValue[] valuesWithKey,
            When when = When.Always, bool fireAndForget = false)
        where TValue : IDataObject {
            var kvps = valuesWithKey.Select(valueWithKey =>
                new KeyValuePair<RedisKey, RedisValue>(_nameSpace + GetItemFullKey(root) + ":strings:" + GetItemKey(valueWithKey),
                    PackValueNullable(valueWithKey))).ToArray();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringSetAsync(kvps, wh, ff);
        }

        // leave option to use CacheContract attribute on non-IDataObject but make it very hard to do
        // so by chance (no default values) - stringKey must be set to null then we will try to get item key
        public bool Set<TRoot, TValue>(TRoot root, KeyValuePair<string, TValue>[] valuesWithKey,
            When when = When.Always, bool fireAndForget = false) {
            var kvps = valuesWithKey.Select(valueWithKey =>
                new KeyValuePair<RedisKey, RedisValue>(_nameSpace + GetItemFullKey(root) + ":strings:" + (valueWithKey.Key ?? GetItemKey(valueWithKey.Value)),
                    PackValueNullable(valueWithKey))).ToArray();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringSet(kvps, wh, ff);
        }

        public async Task<bool> SetAsync<TRoot, TValue>(TRoot root, KeyValuePair<string, TValue>[] valuesWithKey,
             When when = When.Always, bool fireAndForget = false) {
            var kvps = valuesWithKey.Select(valueWithKey =>
                new KeyValuePair<RedisKey, RedisValue>(_nameSpace + GetItemFullKey(root) + ":strings:" + (valueWithKey.Key ?? GetItemKey(valueWithKey.Value)),
                    PackValueNullable(valueWithKey))).ToArray();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringSetAsync(kvps, wh, ff);
        }

        #endregion


        #region Set

        public bool Set<TValue>(TValue valueWithKey, TimeSpan? expiry = null,
            When when = When.Always, bool fireAndForget = false)
        where TValue : IDataObject {
            var k = _nameSpace + GetItemFullKey(valueWithKey);
            var v = PackValueNullable(valueWithKey);
            var ex = expiry ?? GetTypeExpiry<TValue>();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringSet(k, v, ex, wh, ff);
        }

        public async Task<bool> SetAsync<TValue>(TValue valueWithKey, TimeSpan? expiry = null,
            When when = When.Always, bool fireAndForget = false)
        where TValue : IDataObject {
            var k = _nameSpace + GetItemFullKey(valueWithKey);
            var v = PackValueNullable(valueWithKey);
            var ex = expiry ?? GetTypeExpiry<TValue>();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringSetAsync(k, v, ex, wh, ff);
        }

        public bool Set<TValue>(string fullKey, TValue value, TimeSpan? expiry = null,
            When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = PackValueNullable(value);
            var ex = expiry ?? GetTypeExpiry<TValue>();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringSet(k, v, ex, wh, ff);
        }

        public async Task<bool> SetAsync<TValue>(string fullKey, TValue value, TimeSpan? expiry = null,
            When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = PackValueNullable(value);
            var ex = expiry ?? GetTypeExpiry<TValue>();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringSetAsync(k, v, ex, wh, ff);
        }

        public bool Set<TRoot, TValue>(TRoot root, TValue valueWithKey,
            TimeSpan? expiry = null, When when = When.Always, bool fireAndForget = false)
        where TValue : IDataObject {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + GetItemKey(valueWithKey);
            var v = PackValueNullable(valueWithKey);
            var ex = (expiry ?? GetTypeExpiry<TValue>()) ?? GetTypeExpiry<TRoot>();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringSet(k, v, ex, wh, ff);
        }

        public async Task<bool> SetAsync<TRoot, TValue>(TRoot root, TValue value,
            TimeSpan? expiry = null, When when = When.Always, bool fireAndForget = false)
        where TValue : IDataObject {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + GetItemKey(value);
            var v = PackValueNullable(value);
            var ex = (expiry ?? GetTypeExpiry<TValue>()) ?? GetTypeExpiry<TRoot>();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringSetAsync(k, v, ex, wh, ff);
        }

        // leave option to use CacheContract attribute on non-IDataObject but make it very hard to do
        // so by chance (no default values) - stringKey must be set to null then we will try to get item key
        public bool Set<TRoot, TValue>(TRoot root, string stringKey, TValue value,
            TimeSpan? expiry = null, When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + (stringKey ?? GetItemKey(value));
            var v = PackValueNullable(value);
            var ex = (expiry ?? GetTypeExpiry<TValue>()) ?? GetTypeExpiry<TRoot>();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringSet(k, v, ex, wh, ff);
        }

        public async Task<bool> SetAsync<TRoot, TValue>(TRoot root, string stringKey, TValue value,
            TimeSpan? expiry = null, When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + (stringKey ?? GetItemKey(value));
            var v = PackValueNullable(value);
            var ex = (expiry ?? GetTypeExpiry<TValue>()) ?? GetTypeExpiry<TRoot>();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringSetAsync(k, v, ex, wh, ff);
        }

        #endregion


        #region StrLen

        public long StrLen<TRoot>(TRoot root, string stringKey) {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + stringKey;
            var result = GetDb().StringLength(k);
            return result;
        }

        public async Task<long> StrLenAsync<TRoot>(TRoot root, string stringKey) {
            var k = _nameSpace + GetItemFullKey(root) + ":strings:" + stringKey;
            var result = await GetDb().StringLengthAsync(k);
            return result;
        }

        public long StrLen<T>(string key, bool isFullKey = false) {
            var k = _nameSpace + (!isFullKey
                ? GetTypePrefix<T>() + ":i:" + key
                : key);
            var result = GetDb().StringLength(k);
            return (result);
        }

        public async Task<long> StrLenAsync<T>(string key, bool isFullKey = false) {
            var k = _nameSpace + (!isFullKey
                ? GetTypePrefix<T>() + ":i:" + key
                : key);
            var result = await GetDb().StringLengthAsync(k);
            return result;
        }

        #endregion


    }
}
