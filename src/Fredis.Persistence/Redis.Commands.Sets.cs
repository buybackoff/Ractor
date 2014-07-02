using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;


// WIP
//          <T>     <T>Async    key     keyAsycn    Tests
// SAdd       x          x       x         x
// SCard      x          x       x         x
// SDiff      x          x       x         x
// SDiffStore x          x       x         x
// SInter     x          x       x         x
// SInterStore x          x       x         x
// SIsMember  x          x       x         x
// SMembers  x          x       x         x
// SUnion      x          x       x         x
// SUnionStore x          x       x         x


namespace Fredis {
    public partial class Redis {

        #region SAdd

        public long SAdd<TRoot, TValue>(TRoot root, TValue[] values, string listKey = null,
            bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":sets:" + (listKey ?? GetTypePrefix<TValue>());
            var v = values.Select(PackValueNullable).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().SetAdd(k, v, ff);
            return result;
        }

        public bool SAdd<TRoot, TValue>(TRoot root, TValue value, string listKey = null,
            When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":sets:" + (listKey ?? GetTypePrefix<TValue>());
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().SetAdd(k, v, ff);
            return result;
        }

        public async Task<long> SAddAsync<TRoot, TValue>(TRoot root, TValue[] values, string listKey = null,
            bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":sets:" + (listKey ?? GetTypePrefix<TValue>());
            var v = values.Select(PackValueNullable).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().SetAddAsync(k, v, ff);
            return result;
        }

        public async Task<bool> SAddAsync<TRoot, TValue>(TRoot root, TValue value, string listKey = null,
            When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + GetItemFullKey(root) + ":sets:" + (listKey ?? GetTypePrefix<TValue>());
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().SetAddAsync(k, v, ff);
            return result;
        }


        public long SAdd<TValue>(string fullKey, TValue[] values,
            bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = values.Select(PackValueNullable).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().SetAdd(k, v, ff);
            return result;
        }

        public bool SAdd<TValue>(string fullKey, TValue value,
            When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().SetAdd(k, v, ff);
            return result;
        }

        public async Task<long> SAddAsync<TValue>(string fullKey, TValue[] values,
            bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = values.Select(PackValueNullable).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().SetAddAsync(k, v, ff);
            return result;
        }

        // Redis docs say that SETEX,SETNX,PSETEX could be deprecated, so assume that methods with X's will be replaced by parameterized ones everywhere...
        public async Task<bool> SAddAsync<TValue>(string fullKey, TValue value,
            When when = When.Always, bool fireAndForget = false) {
            var k = _nameSpace + fullKey;
            var v = PackValueNullable(value);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().SetAddAsync(k, v, ff);
            return result;
        }

        #endregion

        #region SCard

        public long SCard<TRoot, TValue>(TRoot root, string listKey = null) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var result = GetDb().SetLength(k);
            return result;
        }

        public async Task<long> SCardAsync<TRoot, TValue>(TRoot root, string listKey = null) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var result = await GetDb().SetLengthAsync(k);
            return result;
        }

        public long SCard(string fullKey) {
            var k = _nameSpace + fullKey;
            var result = GetDb().SetLength(k);
            return result;
        }

        public async Task<long> SCardAsync(string fullKey) {
            var k = _nameSpace + fullKey;
            var result = await GetDb().SetLengthAsync(k);
            return result;
        }

        #endregion

        #region SDiff

        public TValue[] SDiff<TRoot, TValue>(TRoot root, string[] listKeys) {
            var k = listKeys.Select(listKey => (RedisKey)(_nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>()))).ToArray();
            var results = GetDb().SetCombine(SetOperation.Difference, k);
            return results.Select(UnpackResultNullable<TValue>).ToArray();
        }

        public async Task<TValue[]> SDiffAsync<TRoot, TValue>(TRoot root, string[] listKeys) {
            var k = listKeys.Select(listKey => (RedisKey)(_nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>()))).ToArray();
            var results = await GetDb().SetCombineAsync(SetOperation.Difference, k);
            return results.Select(UnpackResultNullable<TValue>).ToArray();
        }


        public T[] SDiff<T>(string[] fullKeys) {
            var k = fullKeys.Select(fullKey => (RedisKey)(_nameSpace + fullKey)).ToArray();
            var results = GetDb().SetCombine(SetOperation.Difference, k);
            return results.Select(UnpackResultNullable<T>).ToArray();
        }

        public async Task<T[]> SDiffAsync<T>(string[] fullKeys) {
            var k = fullKeys.Select(fullKey => (RedisKey)(_nameSpace + fullKey)).ToArray();
            var results = await GetDb().SetCombineAsync(SetOperation.Difference, k);
            return results.Select(UnpackResultNullable<T>).ToArray();
        }

        #endregion

        #region SDiffStore

        public long SDiffStore<TRoot, TValue>(TRoot root, string destinationKey, string[] listKeys,
            bool fireAndForget = false) {
            var k = listKeys.Select(listKey => (RedisKey)(_nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>()))).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().SetCombineAndStore(SetOperation.Difference, destinationKey, k, ff);
            return result;
        }

        public async Task<long> SDiffStoreAsync<TRoot, TValue>(TRoot root, string destinationKey, string[] listKeys,
            bool fireAndForget = false) {
            var k = listKeys.Select(listKey => (RedisKey)(_nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>()))).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().SetCombineAndStoreAsync(SetOperation.Difference, destinationKey, k, ff);
            return result;
        }


        public long SDiffStore(string destinationKey, string[] fullKeys, bool fireAndForget = false) {
            var k = fullKeys.Select(fullKey => (RedisKey)(_nameSpace + fullKey)).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().SetCombineAndStore(SetOperation.Difference, destinationKey, k, ff);
            return result;
        }

        public async Task<long> SDiffStoreAsync(string destinationKey, string[] fullKeys, bool fireAndForget = false) {
            var k = fullKeys.Select(fullKey => (RedisKey)(_nameSpace + fullKey)).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().SetCombineAndStoreAsync(SetOperation.Difference, destinationKey, k, ff);
            return result;
        }
        #endregion

        #region SInter

        public TValue[] SInter<TRoot, TValue>(TRoot root, string[] listKeys) {
            var k = listKeys.Select(listKey => (RedisKey)(_nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>()))).ToArray();
            var results = GetDb().SetCombine(SetOperation.Intersect, k);
            return results.Select(UnpackResultNullable<TValue>).ToArray();
        }

        public async Task<TValue[]> SInterAsync<TRoot, TValue>(TRoot root, string[] listKeys) {
            var k = listKeys.Select(listKey => (RedisKey)(_nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>()))).ToArray();
            var results = await GetDb().SetCombineAsync(SetOperation.Intersect, k);
            return results.Select(UnpackResultNullable<TValue>).ToArray();
        }


        public T[] SInter<T>(string[] fullKeys) {
            var k = fullKeys.Select(fullKey => (RedisKey)(_nameSpace + fullKey)).ToArray();
            var results = GetDb().SetCombine(SetOperation.Intersect, k);
            return results.Select(UnpackResultNullable<T>).ToArray();
        }

        public async Task<T[]> SInterAsync<T>(string[] fullKeys) {
            var k = fullKeys.Select(fullKey => (RedisKey)(_nameSpace + fullKey)).ToArray();
            var results = await GetDb().SetCombineAsync(SetOperation.Intersect, k);
            return results.Select(UnpackResultNullable<T>).ToArray();
        }

        #endregion

        #region SInterStore

        public long SInterStore<TRoot, TValue>(TRoot root, string destinationKey, string[] listKeys,
            bool fireAndForget = false) {
            var k = listKeys.Select(listKey => (RedisKey)(_nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>()))).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().SetCombineAndStore(SetOperation.Intersect, destinationKey, k, ff);
            return result;
        }

        public async Task<long> SInterStoreAsync<TRoot, TValue>(TRoot root, string destinationKey, string[] listKeys,
            bool fireAndForget = false) {
            var k = listKeys.Select(listKey => (RedisKey)(_nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>()))).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().SetCombineAndStoreAsync(SetOperation.Intersect, destinationKey, k, ff);
            return result;
        }


        public long SInterStore(string destinationKey, string[] fullKeys, bool fireAndForget = false) {
            var k = fullKeys.Select(fullKey => (RedisKey)(_nameSpace + fullKey)).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().SetCombineAndStore(SetOperation.Intersect, destinationKey, k, ff);
            return result;
        }

        public async Task<long> SInterStoreAsync(string destinationKey, string[] fullKeys, bool fireAndForget = false) {
            var k = fullKeys.Select(fullKey => (RedisKey)(_nameSpace + fullKey)).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().SetCombineAndStoreAsync(SetOperation.Intersect, destinationKey, k, ff);
            return result;
        }
        #endregion

        #region SIsMember

        public bool SIsMember<TRoot, TValue>(TRoot root, TValue value, string listKey = null) {
            var k = _nameSpace + GetItemFullKey(root) + ":sets:" + (listKey ?? GetTypePrefix<TValue>());
            var v = PackValueNullable(value);
            var result = GetDb().SetContains(k, v);
            return result;
        }

        public async Task<bool> SIsMemberAsync<TRoot, TValue>(TRoot root, TValue value, string listKey = null) {
            var k = _nameSpace + GetItemFullKey(root) + ":sets:" + (listKey ?? GetTypePrefix<TValue>());
            var v = PackValueNullable(value);
            var result = await GetDb().SetContainsAsync(k, v);
            return result;
        }


        public bool SIsMember<TValue>(string fullKey, TValue value) {
            var k = _nameSpace + fullKey;
            var v = PackValueNullable(value);
            var result = GetDb().SetContains(k, v);
            return result;
        }

        public async Task<bool> SIsMemberAsync<TValue>(string fullKey, TValue value) {
            var k = _nameSpace + fullKey;
            var v = PackValueNullable(value);
            var result = await GetDb().SetContainsAsync(k, v);
            return result;
        }

        #endregion

        #region SMembers

        public TValue[] SMembers<TRoot, TValue>(TRoot root, string listKey) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var results = GetDb().SetMembers(k);
            return results.Select(UnpackResultNullable<TValue>).ToArray();
        }

        public async Task<TValue[]> SMembersAsync<TRoot, TValue>(TRoot root, string listKey) {
            var k = _nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>());
            var results = await GetDb().SetMembersAsync(k);
            return results.Select(UnpackResultNullable<TValue>).ToArray();
        }


        public T[] SMembers<T>(string fullKey) {
            var k = _nameSpace + fullKey;
            var results = GetDb().SetMembers(k);
            return results.Select(UnpackResultNullable<T>).ToArray();
        }

        public async Task<T[]> SMembersAsync<T>(string fullKey) {
            var k = _nameSpace + fullKey;
            var results = await GetDb().SetMembersAsync(k);
            return results.Select(UnpackResultNullable<T>).ToArray();
        }

        #endregion


        #region SUnion

        public TValue[] SUnion<TRoot, TValue>(TRoot root, string[] listKeys) {
            var k = listKeys.Select(listKey => (RedisKey)(_nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>()))).ToArray();
            var results = GetDb().SetCombine(SetOperation.Union, k);
            return results.Select(UnpackResultNullable<TValue>).ToArray();
        }

        public async Task<TValue[]> SUnionAsync<TRoot, TValue>(TRoot root, string[] listKeys) {
            var k = listKeys.Select(listKey => (RedisKey)(_nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>()))).ToArray();
            var results = await GetDb().SetCombineAsync(SetOperation.Union, k);
            return results.Select(UnpackResultNullable<TValue>).ToArray();
        }


        public T[] SUnion<T>(string[] fullKeys) {
            var k = fullKeys.Select(fullKey => (RedisKey)(_nameSpace + fullKey)).ToArray();
            var results = GetDb().SetCombine(SetOperation.Union, k);
            return results.Select(UnpackResultNullable<T>).ToArray();
        }

        public async Task<T[]> SUnionAsync<T>(string[] fullKeys) {
            var k = fullKeys.Select(fullKey => (RedisKey)(_nameSpace + fullKey)).ToArray();
            var results = await GetDb().SetCombineAsync(SetOperation.Union, k);
            return results.Select(UnpackResultNullable<T>).ToArray();
        }

        #endregion

        #region SUnionStore

        public long SUnionStore<TRoot, TValue>(TRoot root, string destinationKey, string[] listKeys,
            bool fireAndForget = false) {
            var k = listKeys.Select(listKey => (RedisKey)(_nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>()))).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().SetCombineAndStore(SetOperation.Union, destinationKey, k, ff);
            return result;
        }

        public async Task<long> SUnionStoreAsync<TRoot, TValue>(TRoot root, string destinationKey, string[] listKeys,
            bool fireAndForget = false) {
            var k = listKeys.Select(listKey => (RedisKey)(_nameSpace + GetItemFullKey(root) + ":lists:" + (listKey ?? GetTypePrefix<TValue>()))).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().SetCombineAndStoreAsync(SetOperation.Union, destinationKey, k, ff);
            return result;
        }


        public long SUnionStore(string destinationKey, string[] fullKeys, bool fireAndForget = false) {
            var k = fullKeys.Select(fullKey => (RedisKey)(_nameSpace + fullKey)).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = GetDb().SetCombineAndStore(SetOperation.Union, destinationKey, k, ff);
            return result;
        }

        public async Task<long> SUnionStoreAsync(string destinationKey, string[] fullKeys, bool fireAndForget = false) {
            var k = fullKeys.Select(fullKey => (RedisKey)(_nameSpace + fullKey)).ToArray();
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            var result = await GetDb().SetCombineAndStoreAsync(SetOperation.Union, destinationKey, k, ff);
            return result;
        }
        #endregion

    }
}
