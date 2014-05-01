using System;
using System.Threading.Tasks;
using StackExchange.Redis;


// WIP
//          <T>     <T>Async    key     keyAsycn    Tests   TestsAsync
// Set      x       x           x       x           x
// Get      n.a.    n.a.        x       x           x   




namespace Fredis {
    /// <summary>
    /// String commands
    /// </summary>
    public partial class Redis {

        public bool Set<T>(T item, TimeSpan? expiry = null, When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(item);
            var v = PackResultNullable(item);
            var ex = expiry ?? GetTypeExpiry<T>();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringSet(k, v, ex, wh, ff);
        }

        public async Task<bool> SetAsync<T>(T item, TimeSpan? expiry = null, When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(item);
            var v = PackResultNullable(item);
            var ex = expiry ?? GetTypeExpiry<T>();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringSetAsync(k, v, ex, wh, ff);
        }

        public bool Set<T>(string fullKey, T item, TimeSpan? expiry = null, When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = PackResultNullable(item);
            var ex = expiry ?? GetTypeExpiry<T>();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().StringSet(k, v, ex, wh, ff);
        }

        public async Task<bool> SetAsync<T>(string fullKey, T item, TimeSpan? expiry = null, When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = PackResultNullable(item);
            var ex = expiry ?? GetTypeExpiry<T>();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await GetDb().StringSetAsync(k, v, ex, wh, ff);
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



    }
}
