using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using System.Collections.Generic;
using System;

// WIP
//          <T>     <T>Async    key     keyAsycn    Tests
// SSAdd       x          x       x         x         x
// 


namespace Ractor
{

    public partial class Redis
    {

        #region SSAdd

        public long SSAdd<TRoot, TValue>(TRoot root, Dictionary<TValue, double> values, string listKey = null,
            bool fireAndForget = false)
        {
            var k = _nameSpace + GetItemFullKey(root) + ":sets:" + (listKey ?? GetTypePrefix<TValue>());
            var v = values.Select(PackValueScoreNullable).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().SortedSetAdd(k, v, ff);
            return result;
        }


        public bool SSAdd<TRoot, TValue>(TRoot root, TValue value,
            double score = 0, string listKey = null, When when = When.Always, bool fireAndForget = false)
        {
            var k = _nameSpace + GetItemFullKey(root) + ":sets:" + (listKey ?? GetTypePrefix<TValue>());
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().SortedSetAdd(k, v, score, ff);
            return result;
        }

        public async Task<long> SSAddAsync<TRoot, TValue>(TRoot root, Dictionary<TValue, double> values, string listKey = null,
            bool fireAndForget = false)
        {
            var k = _nameSpace + GetItemFullKey(root) + ":sets:" + (listKey ?? GetTypePrefix<TValue>());
            var v = values.Select(PackValueScoreNullable).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().SortedSetAddAsync(k, v, ff);
            return result;
        }

        public async Task<bool> SSAddAsync<TRoot, TValue>(TRoot root, TValue value,
            double score = 0, string listKey = null, When when = When.Always, bool fireAndForget = false)
        {
            var k = _nameSpace + GetItemFullKey(root) + ":sets:" + (listKey ?? GetTypePrefix<TValue>());
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().SortedSetAddAsync(k, v, score, ff);
            return result;
        }


        public long SSAdd<TValue>(string fullKey, Dictionary<TValue, double> values,
            bool fireAndForget = false)
        {
            var k = _nameSpace + fullKey;
            var v = values.Select(PackValueScoreNullable).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().SortedSetAdd(k, v, ff);
            return result;
        }

        public bool SSAdd<TValue>(string fullKey, TValue value,
            double score = 0, When when = When.Always, bool fireAndForget = false)
        {
            var k = _nameSpace + fullKey;
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().SortedSetAdd(k, v, score, ff);
            return result;
        }

        public async Task<long> SSAddAsync<TValue>(string fullKey, Dictionary<TValue, double> values,
            bool fireAndForget = false)
        {
            var k = _nameSpace + fullKey;
            var v = values.Select(PackValueScoreNullable).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().SortedSetAddAsync(k, v, ff);
            return result;
        }

        // Redis docs say that SETEX,SETNX,PSETEX could be deprecated, so assume that methods with X's will be replaced by parameterized ones everywhere...
        public async Task<bool> SSAddAsync<TValue>(string fullKey, TValue value,
            double score = 0, When when = When.Always, bool fireAndForget = false)
        {
            var k = _nameSpace + fullKey;
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().SortedSetAddAsync(k, v, score, ff);
            return result;
        }

        #endregion

    }
}
