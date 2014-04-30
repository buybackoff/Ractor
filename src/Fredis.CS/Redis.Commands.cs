using System;
using System.Threading.Tasks;
using ServiceStack.Common;
using ServiceStack.Text;
using StackExchange.Redis;

namespace Fredis
{

	// each command must have three versions
	// Type item only

    public partial class Redis : IRedis {

        public bool Set<T>(T item, TimeSpan? expiry = null, When when = When.Always, bool fireAndForget = false) {
            var key = GetItemFullKey(item);
            var value = item.ToJsv();
            var ex = expiry ?? GetTypeExpiry<T>();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
			return IsTypeCompressed<T>() 
                ? GetDb().StringSet(key, value.GZip(), ex, wh, ff)
                : GetDb().StringSet(key, value, ex, wh, ff);
        }

        public bool Set<T>(string key, T item, TimeSpan? expiry = null, When when = When.Always, bool fireAndForget = false) {
            var value = item.ToJsv();
            var ex = expiry ?? GetTypeExpiry<T>();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return IsTypeCompressed<T>()
                ? GetDb().StringSet(key, value.GZip(), ex, wh, ff)
                : GetDb().StringSet(key, value, ex, wh, ff);
        }

        public async Task<bool> SetAsync<T>(T item, TimeSpan? expiry = null, When when = When.Always, bool fireAndForget = false) {
            var key = GetItemFullKey(item);
            var value = item.ToJsv();
            var ex = expiry ?? GetTypeExpiry<T>();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return  IsTypeCompressed<T>()
                ? await GetDb().StringSetAsync(key, value.GZip(), ex, wh, ff)
                : await GetDb().StringSetAsync(key, value, ex, wh, ff);
        }


        public T Get<T>(string key, bool fullKey = false) {
            var k = !fullKey
                ? GetTypePrefix<T>() + ":" + key
                : key;
            return IsTypeCompressed<T>()
                ? ((byte[])GetDb().StringGet(k)).GUnzip().FromJsv<T>()
                : ((string)GetDb().StringGet(k)).FromJsv<T>();
        }

        public async Task<T> GetAsync<T>(string key, bool fullKey = false) {
            var k = !fullKey
                ? GetTypePrefix<T>() + ":" + key
                : key;
            var result = await GetDb().StringGetAsync(k);
            return IsTypeCompressed<T>()
                ? ((byte[])result).GUnzip().FromJsv<T>()
                : ((string)result).FromJsv<T>();
        }


        public bool SAdd<TRoot, TValue>(TRoot root, TValue value, string setKey = null, bool fireAndForget = false) {
            var key = GetItemFullKey(root) + ":set:" + (setKey ?? typeof(TValue).Name);
            var val = value.ToJsv();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return IsTypeCompressed<TValue>()
                ? GetDb().SetAdd(key, val.GZip(), ff)
                : GetDb().SetAdd(key, val, ff);
        }

        public bool SAdd<TValue>(string key, TValue value, bool fireAndForget = false) {
            var val = value.ToJsv();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return IsTypeCompressed<TValue>()
                ? GetDb().SetAdd(key, val.GZip(), ff)
                : GetDb().SetAdd(key, val, ff);
        }



        public bool HSet<TRoot, TValue>(TRoot root, TValue value, string hashKey = null, bool fireAndForget = false) {
            var key = GetItemFullKey(root) + ":hash:" + (hashKey ?? typeof(TValue).Name);
            var field = GetItemKey(value);
            var val = value.ToJsv();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return IsTypeCompressed<TValue>()
                ? GetDb().HashSet(key, field, val.GZip(), StackExchange.Redis.When.Always , ff)
                : GetDb().HashSet(key, field, val, StackExchange.Redis.When.Always, ff);
        }


        public TValue HGet<TRoot, TValue>(TRoot root, string field, string hashKey = null) {
            var key = GetItemFullKey(root) + ":hash:" + (hashKey ?? typeof(TValue).Name);

            return IsTypeCompressed<TValue>()
                ? ((byte[])GetDb().HashGet(key, field)).GUnzip().FromJsv<TValue>()
                : ((string)GetDb().HashGet(key, field)).FromJsv<TValue>();
        }


        public TValue HGet<TRoot, TValue>(string rootKey, string field, bool fullKey = false, string hashKey = null) {
            var k = !fullKey
                ? GetTypePrefix<TRoot>() + ":" + rootKey
                : rootKey;

            var key = GetItemFullKey(k) + ":hash:" + (hashKey ?? typeof(TValue).Name);

            return IsTypeCompressed<TValue>()
                ? ((byte[])GetDb().HashGet(key, field)).GUnzip().FromJsv<TValue>()
                : ((string)GetDb().HashGet(key, field)).FromJsv<TValue>();
        }


    }
}
