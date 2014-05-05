using System;
using System.Threading.Tasks;
using StackExchange.Redis;


// WIP
//          <T>     <T>Async    key     keyAsycn    Tests   TestsAsync
// Set      x       x           x       x           x
// Get      x       x           x       x           x   
// StrLen   x       x           x       x           x   


namespace Fredis {
    /// <summary>
    /// String commands
    /// </summary>
    public partial class Redis {

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
        // so by change (no default values) - stringKey must be set to null then we will try to get item key
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

        public T Get<T>(string key, bool isFullKey = false) {
            var k = _nameSpace + (!isFullKey
                ? GetTypePrefix<T>() + ":i:" + key
                : key);
            var result = GetDb().StringGet(k);
            return UnpackResultNullable<T>(result);
        }

        public async Task<T> GetAsync<T>(string key, bool isFullKey = false) {
            var k = _nameSpace + (!isFullKey
                ? GetTypePrefix<T>() + ":i:" + key
                : key);
            var result = await GetDb().StringGetAsync(k);
            return UnpackResultNullable<T>(result);
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
