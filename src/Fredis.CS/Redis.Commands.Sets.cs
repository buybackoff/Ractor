using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Common;
using ServiceStack.Text;
using StackExchange.Redis;


// WIP
//          <T>     <T>Async    key     keyAsycn    Tests
// Sadd      x                  x


namespace Fredis {
    public partial class Redis {

        public bool SAdd<TRoot, TValue>(TRoot root, TValue value, string setName = null, bool fireAndForget = false) {
            var key = GetItemFullKey(root) + ":set:" + (setName ?? typeof(TValue).Name);
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



    }
}
