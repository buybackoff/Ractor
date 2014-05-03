using System.Threading.Tasks;
using ServiceStack.Common;
using ServiceStack.Text;
using StackExchange.Redis;

// WIP
//          <T>     <T>Async    key     keyAsycn    Tests
// HSet     x
// HGet     x                   x


namespace Fredis {

    public partial class Redis {

        public bool HSet<TRoot, TValue>(TRoot root, TValue valueWithKey, string hashKey = null,
            When when = When.Always, bool fireAndForget = false)
        where TValue : IDataObject {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var f = GetItemKey(valueWithKey);
            var v = PackValueNullable(valueWithKey);
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().HashSet(key, f, v, wh, ff);
        }

        public bool HSet<TRoot, TValue>(TRoot root, string field, TValue value, string hashKey = null,
            When when = When.Always, bool fireAndForget = false) {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            var f = field ?? GetItemKey(value);
            var v = PackValueNullable(value);
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return GetDb().HashSet(key, f, v, wh, ff);
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

        public TValue HGet<TRoot, TValue>(TRoot root, string field, string hashKey = null) {
            var key = _nameSpace + GetItemFullKey(root) + ":hashes:" + (hashKey ?? GetTypePrefix<TValue>());
            return IsTypeCompressed<TValue>()
                ? ((byte[])GetDb().HashGet(key, field)).GUnzip().FromJsv<TValue>()
                : ((string)GetDb().HashGet(key, field)).FromJsv<TValue>();
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



        public bool HDel(string fullKey, string field) {
            var k = _nameSpace + fullKey;
            var result = GetDb().HashDelete(k, field);
            return result;
        }

        public async Task<bool> HDelAsync(string fullKey, string field) {
            var k = _nameSpace + fullKey;
            var result = await GetDb().HashDeleteAsync(k, field);
            return result;
        }

    }
}
