using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

// WIP TODO tests
//          <T>     <T>Async    key     keyAsycn    Tests
// LIndex   x       x           x       x
// LInsert  x       x           x       x
// LLen     x       x           x       x
// LPop     x       x           x       x
// LPush    x       x           x       x
// LRange   x       x           x       x
// LRem     x       x           x       x
// LSet     x       x           x       x
// LTrim    x       x           x       x
// RPop     x       x           x       x
// RPopLPush x      x           x       x
// RPush    x       x           x       x


namespace Fredis {

    public partial class Redis {

        #region LIndex

        /// <summary>
        /// Returns the element at index index in the list stored at key. The index is 
        /// zero-based, so 0 means the first element, 1 the second element and so on. 
        /// Negative indices can be used to designate elements starting at the tail of 
        /// the list. Here, -1 means the last element, -2 means the penultimate and so forth.
        /// 
        /// When the value at key is not a list, an error is returned.
        /// </summary>
        /// <typeparam name="TRoot">Type of root object</typeparam>
        /// <typeparam name="TValue">Type of value stored in the list</typeparam>
        /// <param name="root">Root object (owner) of the list</param>
        /// <param name="index">Index of value</param>
        /// <param name="listKey">Optional list name that is appended to root schema instead of 
        /// TValue type name </param>
        /// <returns>The requested element, or default(TValue) when index is out of range.</returns>
        public TValue LIndex<TRoot, TValue>(TRoot root, long index, string listKey = null) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var result = GetDb().ListGetByIndex(k, index);
            return UnpackResultNullable<TValue>(result);
        }

        public async Task<TValue> LIndexAsync<TRoot, TValue>(TRoot root, long index, string listKey = null) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var result = await GetDb().ListGetByIndexAsync(k, index);
            return UnpackResultNullable<TValue>(result);
        }

        public T LIndex<T>(string fullKey, long index) {
            var k = _nameSpace + fullKey;
            var result = GetDb().ListGetByIndex(k, index);
            return UnpackResultNullable<T>(result);
        }

        public async Task<T> LIndexAsync<T>(string fullKey, long index) {
            var k = _nameSpace + fullKey;
            var result = await GetDb().ListGetByIndexAsync(k, index);
            return UnpackResultNullable<T>(result);
        }

        #endregion

        #region LInsert

        public long LInsert<TRoot, TValue>(TRoot root, bool after, TValue pivot, TValue value, string listKey = null,
            bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var p = PackValueNullable(pivot);
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = after
                ? GetDb().ListInsertAfter(k, p, v, ff)
                : GetDb().ListInsertBefore(k, p, v, ff);
            return result;
        }

        public async Task<long> LInsertAsync<TRoot, TValue>(TRoot root, bool after, TValue pivot, TValue value,
            string listKey = null, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var p = PackValueNullable(pivot);
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = after
                ? await GetDb().ListInsertAfterAsync(k, p, v, ff)
                : await GetDb().ListInsertBeforeAsync(k, p, v, ff);
            return result;
        }

        public long LInsert<TValue>(string fullKey, bool after, TValue pivot, TValue value, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var p = PackValueNullable(pivot);
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = after
                ? GetDb().ListInsertAfter(k, p, v, ff)
                : GetDb().ListInsertBefore(k, p, v, ff);
            return result;
        }

        public async Task<long> LInsertAsync<TValue>(string fullKey, bool after, TValue pivot, TValue value,
            bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var p = PackValueNullable(pivot);
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = after
                ? await GetDb().ListInsertAfterAsync(k, p, v, ff)
                : await GetDb().ListInsertBeforeAsync(k, p, v, ff);
            return result;
        }

        #endregion

        #region LLen

        public long LLen<TRoot, TValue>(TRoot root, string listKey = null) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var result = GetDb().ListLength(k);
            return result;
        }

        public async Task<long> LLenAsync<TRoot, TValue>(TRoot root, string listKey = null) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var result = await GetDb().ListLengthAsync(k);
            return result;
        }

        public long LLen(string fullKey) {
            var k = _nameSpace + fullKey;
            var result = GetDb().ListLength(k);
            return result;
        }

        public async Task<long> LLenAsync(string fullKey) {
            var k = _nameSpace + fullKey;
            var result = await GetDb().ListLengthAsync(k);
            return result;
        }

        #endregion

        #region LPop

        public TValue LPop<TRoot, TValue>(TRoot root, string listKey = null) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var result = GetDb().ListLeftPop(k);
            return UnpackResultNullable<TValue>(result);
        }

        public async Task<TValue> LPopAsync<TRoot, TValue>(TRoot root, long index, string listKey = null) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var result = await GetDb().ListLeftPopAsync(k);
            return UnpackResultNullable<TValue>(result);
        }

        public T LPop<T>(string fullKey) {
            var k = _nameSpace + fullKey;
            var result = GetDb().ListLeftPop(k);
            return UnpackResultNullable<T>(result);
        }

        public async Task<T> LPopAsync<T>(string fullKey) {
            var k = _nameSpace + fullKey;
            var result = await GetDb().ListLeftPopAsync(k);
            return UnpackResultNullable<T>(result);
        }

        #endregion

        #region LPush

        public long LPush<TRoot, TValue>(TRoot root, TValue[] values, string listKey = null,
            bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var v = values.Select(PackValueNullable).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().ListLeftPush(k, v, ff);
            return result;
        }

        // Redis docs say that SETEX,SETNX,PSETEX could be deprecated, so assume that methods with X's will be replaced by parameterized ones everywhere...
        public long LPush<TRoot, TValue>(TRoot root, TValue value, string listKey = null,
            When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var v = PackValueNullable(value);
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().ListLeftPush(k, v, wh, ff);
            return result;
        }

        public async Task<long> LPushAsync<TRoot, TValue>(TRoot root, TValue[] values, string listKey = null,
            bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var v = values.Select(PackValueNullable).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().ListLeftPushAsync(k, v, ff);
            return result;
        }

        // Redis docs say that SETEX,SETNX,PSETEX could be deprecated, so assume that methods with X's will be replaced by parameterized ones everywhere...
        public async Task<long> LPushAsync<TRoot, TValue>(TRoot root, TValue value, string listKey = null,
            When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var v = PackValueNullable(value);
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().ListLeftPushAsync(k, v, wh, ff);
            return result;
        }


        public long LPush<TValue>(string fullKey, TValue[] values,
            bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = values.Select(PackValueNullable).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().ListLeftPush(k, v, ff);
            return result;
        }

        // Redis docs say that SETEX,SETNX,PSETEX could be deprecated, so assume that methods with X's will be replaced by parameterized ones everywhere...
        public long LPush<TValue>(string fullKey, TValue value,
            When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = PackValueNullable(value);
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().ListLeftPush(k, v, wh, ff);
            return result;
        }

        public async Task<long> LPushAsync<TValue>(string fullKey, TValue[] values,
            bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = values.Select(PackValueNullable).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().ListLeftPushAsync(k, v, ff);
            return result;
        }

        // Redis docs say that SETEX,SETNX,PSETEX could be deprecated, so assume that methods with X's will be replaced by parameterized ones everywhere...
        public async Task<long> LPushAsync<TValue>(string fullKey, TValue value,
            When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = PackValueNullable(value);
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().ListLeftPushAsync(k, v, wh, ff);
            return result;
        }

        #endregion

        #region LRange

        public TValue[] LRange<TRoot, TValue>(TRoot root, long start, long stop, string listKey = null) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var result = GetDb().ListRange(k, start, stop);
            return result.Select(UnpackResultNullable<TValue>).ToArray();
        }

        public async Task<TValue[]> LRangeAsync<TRoot, TValue>(TRoot root, long start, long stop, string listKey = null) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var result = await GetDb().ListRangeAsync(k, start, stop);
            return result.Select(UnpackResultNullable<TValue>).ToArray();
        }

        public TValue[] LRange<TValue>(string fullKey, long start, long stop) {
            var k = _nameSpace + fullKey;
            var result = GetDb().ListRange(k, start, stop);
            return result.Select(UnpackResultNullable<TValue>).ToArray();
        }

        public async Task<TValue[]> LRangeAsync<TValue>(string fullKey, long start, long stop) {
            var k = _nameSpace + fullKey;
            var result = await GetDb().ListRangeAsync(k, start, stop);
            return result.Select(UnpackResultNullable<TValue>).ToArray();
        }

        #endregion

        #region LRem

        public long LRem<TRoot, TValue>(TRoot root, long count, TValue value, string listKey = null,
            bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().ListRemove(k, v, count, ff);
            return result;
        }

        public async Task<long> LRemAsync<TRoot, TValue>(TRoot root, long count, TValue value,
            string listKey = null, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().ListRemoveAsync(k, v, count, ff);
            return result;
        }

        public long LRem<TValue>(string fullKey, long count, TValue value, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().ListRemove(k, v, count, ff);
            return result;
        }

        public async Task<long> LRemAsync<TValue>(string fullKey, long count, TValue value,
            bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().ListRemoveAsync(k, v, count, ff);
            return result;
        }

        #endregion

        #region LSet

        public void LSet<TRoot, TValue>(TRoot root, long index, TValue value, string listKey = null,
            bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            GetDb().ListSetByIndex(k, index, v, ff);
        }

        public async Task LSetAsync<TRoot, TValue>(TRoot root, long index, TValue value,
            string listKey = null, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            await GetDb().ListSetByIndexAsync(k, index, v, ff);
        }

        public void LSet<TValue>(string fullKey, long index, TValue value, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            GetDb().ListSetByIndex(k, index, v, ff);
        }

        public async Task LSetAsync<TValue>(string fullKey, long index, TValue value,
            bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            await GetDb().ListSetByIndexAsync(k, index, v, ff);

        }

        #endregion

        #region LTrim

        public void LTrim<TRoot, TValue>(TRoot root, long start, long stop, string listKey = null) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            GetDb().ListTrim(k, start, stop);
        }

        public async Task LTrimAsync<TRoot, TValue>(TRoot root, long start, long stop, string listKey = null) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            await GetDb().ListTrimAsync(k, start, stop);
        }

        public void LTrim(string fullKey, long start, long stop) {
            var k = _nameSpace + fullKey;
            GetDb().ListTrim(k, start, stop);
        }

        public async Task LTrimAsync(string fullKey, long start, long stop) {
            var k = _nameSpace + fullKey;
            await GetDb().ListTrimAsync(k, start, stop);
        }

        #endregion

        #region RPop

        public TValue RPop<TRoot, TValue>(TRoot root, string listKey = null) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var result = GetDb().ListRightPop(k);
            return UnpackResultNullable<TValue>(result);
        }

        public async Task<TValue> RPopAsync<TRoot, TValue>(TRoot root, string listKey = null) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var result = await GetDb().ListRightPopAsync(k);
            return UnpackResultNullable<TValue>(result);
        }

        public T RPop<T>(string fullKey) {
            var k = _nameSpace + fullKey;
            var result = GetDb().ListRightPop(k);
            return UnpackResultNullable<T>(result);
        }

        public async Task<T> RPopAsync<T>(string fullKey) {
            var k = _nameSpace + fullKey;
            var result = await GetDb().ListRightPopAsync(k);
            return UnpackResultNullable<T>(result);
        }

        #endregion

        #region RPopLPush

        public TValue RPopLPush<TSource, TDesctination, TValue>(TSource source,
            TDesctination destination, string sourceListName = null, string destinationListName = null) {
            var kSource = _nameSpace + GetItemFullKey(source) + ":lists:" + (sourceListName ?? GetTypePrefix<TValue>());
            var kDestination = _nameSpace + GetItemFullKey(destination) + ":lists:" + (destinationListName ?? GetTypePrefix<TValue>());
            var result = GetDb().ListRightPopLeftPush(kSource, kDestination);
            return UnpackResultNullable<TValue>(result);
        }

        public async Task<TValue> RPopLPushAsync<TSource, TDesctination, TValue>(TSource source,
            TDesctination destination, string sourceListName = null, string destinationListName = null) {
            var kSource = _nameSpace + GetItemFullKey(source) + ":lists:" + (sourceListName ?? GetTypePrefix<TValue>());
            var kDestination = _nameSpace + GetItemFullKey(destination) + ":lists:" + (destinationListName ?? GetTypePrefix<TValue>());
            var result = await GetDb().ListRightPopLeftPushAsync(kSource, kDestination);
            return UnpackResultNullable<TValue>(result);
        }

        public TValue RPopLPush<TValue>(string sourceFullKey, string destinationFullKey) {
            var kSource = _nameSpace + sourceFullKey;
            var kDestination = _nameSpace + destinationFullKey;
            var result = GetDb().ListRightPopLeftPush(kSource, kDestination);
            return UnpackResultNullable<TValue>(result);
        }

        public async Task<TValue> RPopLPushAsync<TValue>(string sourceFullKey, string destinationFullKey) {
            var kSource = _nameSpace + sourceFullKey;
            var kDestination = _nameSpace + destinationFullKey;
            var result = await GetDb().ListRightPopLeftPushAsync(kSource, kDestination);
            return UnpackResultNullable<TValue>(result);
        }

        #endregion

        #region RPush

        public long RPush<TRoot, TValue>(TRoot root, TValue[] values, string listKey = null,
            bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var v = values.Select(PackValueNullable).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().ListRightPush(k, v, ff);
            return result;
        }

        // Redis docs say that SETEX,SETNX,PSETEX could be deprecated, so assume that methods with X's will be replaced by parameterized ones everywhere...
        public long RPush<TRoot, TValue>(TRoot root, TValue value, string listKey = null,
            When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var v = PackValueNullable(value);
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().ListRightPush(k, v, wh, ff);
            return result;
        }

        public async Task<long> RPushAsync<TRoot, TValue>(TRoot root, TValue[] values, string listKey = null,
            bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var v = values.Select(PackValueNullable).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().ListRightPushAsync(k, v, ff);
            return result;
        }

        // Redis docs say that SETEX,SETNX,PSETEX could be deprecated, so assume that methods with X's will be replaced by parameterized ones everywhere...
        public async Task<long> RPushAsync<TRoot, TValue>(TRoot root, TValue value, string listKey = null,
            When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var v = PackValueNullable(value);
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().ListRightPushAsync(k, v, wh, ff);
            return result;
        }


        public long RPush<TValue>(string fullKey, TValue[] values,
            bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = values.Select(PackValueNullable).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().ListRightPush(k, v, ff);
            return result;
        }

        // Redis docs say that SETEX,SETNX,PSETEX could be deprecated, so assume that methods with X's will be replaced by parameterized ones everywhere...
        public long RPush<TValue>(string fullKey, TValue value,
            When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = PackValueNullable(value);
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().ListRightPush(k, v, wh, ff);
            return result;
        }

        public async Task<long> RPushAsync<TValue>(string fullKey, TValue[] values,
            bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = values.Select(PackValueNullable).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().ListRightPushAsync(k, v, ff);
            return result;
        }

        // Redis docs say that SETEX,SETNX,PSETEX could be deprecated, so assume that methods with X's will be replaced by parameterized ones everywhere...
        public async Task<long> RPushAsync<TValue>(string fullKey, TValue value,
            When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = PackValueNullable(value);
            var wh = MapWhen(when);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().ListRightPushAsync(k, v, wh, ff);
            return result;
        }

        #endregion

    }
}
