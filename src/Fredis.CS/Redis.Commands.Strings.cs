using System;
using System.Threading.Tasks;
using ServiceStack.Common;
using ServiceStack.Text;
using StackExchange.Redis;


// WIP
//          <T>     <T>Async    key     keyAsycn    Tests
// Set      x       x           x
// Get      n.a.    n.a.        x       x              



namespace Fredis {
    /// <summary>
    /// String commands
    /// </summary>
    public partial class Redis {

        public bool Set<T>(T item, TimeSpan? expiry = null, When when = When.Always, bool fireAndForget = false) {
            var key = _nameSpace + GetItemFullKey(item);
            var value = item.ToJsv();
            var ex = expiry ?? GetTypeExpiry<T>();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return IsTypeCompressed<T>()
                ? GetDb().StringSet(key, value.GZip(), ex, wh, ff)
                : GetDb().StringSet(key, value, ex, wh, ff);
        }

        public async Task<bool> SetAsync<T>(T item, TimeSpan? expiry = null, When when = When.Always, bool fireAndForget = false) {
            var key = _nameSpace + GetItemFullKey(item);
            var value = item.ToJsv();
            var ex = expiry ?? GetTypeExpiry<T>();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return IsTypeCompressed<T>()
                ? await GetDb().StringSetAsync(key, value.GZip(), ex, wh, ff)
                : await GetDb().StringSetAsync(key, value, ex, wh, ff);
        }

        public bool Set<T>(string fullKey, T item, TimeSpan? expiry = null, When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var value = item.ToJsv();
            var ex = expiry ?? GetTypeExpiry<T>();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return IsTypeCompressed<T>()
                ? GetDb().StringSet(k, value.GZip(), ex, wh, ff)
                : GetDb().StringSet(k, value, ex, wh, ff);
        }



        public T Get<T>(string key, bool isFullKey = false) {
            var k = _nameSpace + (!isFullKey
                ? GetTypePrefix<T>() + ":i:" + key
                : key);
            var result = GetDb().StringGet(k);
            return UnpackResultNullable<T>(result);
            return IsTypeCompressed<T>()
                ? ((byte[])GetDb().StringGet(k)).GUnzip().FromJsv<T>()
                : ((string)GetDb().StringGet(k)).FromJsv<T>();
        }

        public async Task<T> GetAsync<T>(string key, bool isFullKey = false) {
            var k = _nameSpace + (!isFullKey
                ? GetTypePrefix<T>() + ":i:" + key
                : key);
            var result = await GetDb().StringGetAsync(k);
            return IsTypeCompressed<T>()
                ? ((byte[])result).GUnzip().FromJsv<T>()
                : ((string)result).FromJsv<T>();
        }



    }
}
