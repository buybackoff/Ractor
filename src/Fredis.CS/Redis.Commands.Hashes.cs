using ServiceStack.Common;
using ServiceStack.Text;
using StackExchange.Redis;

// WIP
//          <T>     <T>Async    key     keyAsycn    Tests
// HSet     x
// HGet     x                   x


namespace Fredis {



    public partial class Redis {

        public bool HSet<TRoot, TValue>(TRoot root, TValue value, string hashName = null, When when = When.Always, bool fireAndForget = false) {
            var key = GetItemFullKey(root) + ":hash:" + (hashName ?? typeof(TValue).Name);
            var field = GetItemKey(value);
            var val = value.ToJsv();
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return IsTypeCompressed<TValue>()
                ? GetDb().HashSet(key, field, val.GZip(), wh, ff)
                : GetDb().HashSet(key, field, val, wh, ff);
        }


        public TValue HGet<TRoot, TValue>(TRoot root, string field, string hashName = null) {
            var key = GetItemFullKey(root) + ":hash:" + (hashName ?? typeof(TValue).Name);
            return IsTypeCompressed<TValue>()
                ? ((byte[])GetDb().HashGet(key, field)).GUnzip().FromJsv<TValue>()
                : ((string)GetDb().HashGet(key, field)).FromJsv<TValue>();
        }


        public T HGet<T>(string key, string field) {
            //var k = !rootKeyIsPrefixed
            //    ? GetTypePrefix<TRoot>() + ":" + rootKey
            //    : rootKey;
            //var k = rootKey;

            //var key = k;// + ":hash:" + (hashName ?? typeof(TValue).Name);

            return IsTypeCompressed<T>()
                ? ((byte[])GetDb().HashGet(key, field)).GUnzip().FromJsv<T>()
                : ((string)GetDb().HashGet(key, field)).FromJsv<T>();
        }
    }
}
