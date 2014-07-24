using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using ServiceStack;
using StackExchange.Redis;

namespace Ractor {

    public partial class Redis : IRedis {

        /// <summary>
        /// MemoryCache instance for all Redis-related needs with "Redis" config name.
        /// </summary>
        public static MemoryCache Cache = new MemoryCache("Redis");

        /// <summary>
        /// Prefix to all keys created/read by an instance of Redis
        /// </summary>
        public string KeyNameSpace { get; private set; }
        private readonly string _nameSpace;

        /// <summary>
        /// 
        /// </summary>
        public ISerializer Serializer { get; set; }

        /// <summary>
        /// Public constructor for Redis client
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="keyNameSpace">Prefix to all keys created/read by an instance of Redis</param>
        public Redis(string connectionString = "", string keyNameSpace = "") {
            if (string.IsNullOrWhiteSpace(connectionString)) connectionString = "localhost,resolveDns=true";
            ConnectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
            KeyNameSpace = keyNameSpace ?? ""; // just if null is provided
            _nameSpace = KeyNameSpace.IsNullOrEmpty() ? "" : KeyNameSpace + ":";
            Serializer = new JsonSerializer();
        }

        private ConnectionMultiplexer ConnectionMultiplexer { get; set; }

        /// <summary>
        /// Return a full key for an item T, e.g. "ItemType:i:ItemKey"
        /// </summary>
        public string GetItemFullKey<T>(T item) {
            var ci = GetCacheInfo<T>();
            return ci.GetFullKey(item);
        }

        /// <summary>
        /// Return a key for an item T, e.g. "ItemKey"
        /// </summary>
        public string GetItemKey<T>(T item) {
            var ci = GetCacheInfo<T>();
            return ci.GetKey(item);
        }

        /// <summary>
        /// Return a full key for a type T, e.g. "ItemType:t"
        /// </summary>
        public string GetTypeFullKey<T>() {
            var ci = GetCacheInfo<T>();
            return ci.GetTypePrefix() + ":t";
        }

        /// <summary>
        /// Return a prefix for a type T, e.g. "ItemType" (from CacheContract attribute or default to type name)
        /// </summary>
        public string GetTypePrefix<T>() {
            var ci = GetCacheInfo<T>();
            return ci.GetTypePrefix();
        }

        /// <summary>
        /// Return expiry TimeSpan from type attribute
        /// </summary>
        public TimeSpan? GetTypeExpiry<T>() {
            var ci = GetCacheInfo<T>();
            return ci.CacheContract == null ? null : ci.CacheContract.Expiry;
        }

        /// <summary>
        /// Return compressed flag from type attribute
        /// </summary>
        public bool IsTypeCompressed<T>() {
            var ci = GetCacheInfo<T>();
            return ci.CacheContract != null && ci.CacheContract.Compressed;
        }

        private StackExchange.Redis.When MapWhen(When when) {
            switch (when) {
                case When.Always:
                    return StackExchange.Redis.When.Always;
                case When.Exists:
                    return StackExchange.Redis.When.Exists;
                case When.NotExists:
                    return StackExchange.Redis.When.NotExists;
            }
            throw new ApplicationException("wrong When enum");
        }

        private IDatabase GetDb() {
            return ConnectionMultiplexer.GetDatabase();
        }

        private T UnpackResultNullable<T>(RedisValue result) {
            if (result.IsNull) return default(T);
            var bytes =
                IsTypeCompressed<T>()
                ? ((byte[])result).UnGZip()
                : ((byte[])result);
            return Serializer.Deserialize<T>(bytes);
        }

        private RedisValue PackValueNullable<T>(T item) {
            if (!typeof(T).IsValueType && EqualityComparer<T>.Default.Equals(item, default(T))) {
                return RedisValue.Null;
            }
            var bytes = Serializer.Serialize(item);
            return IsTypeCompressed<T>()
                ? (RedisValue)bytes.GZip()
                : (RedisValue)bytes;
        }

        /// <summary>
        /// Stores reflected cash info for each type
        /// </summary>
        private static readonly ConcurrentDictionary<string, CacheInfo> CacheInfos = new ConcurrentDictionary<string, CacheInfo>();

        /// <summary>
        /// Memoized reflection of cache contract
        /// </summary>
        private static CacheInfo GetCacheInfo<T>() {
            var name = typeof(T).FullName;
            CacheInfo ci;
            if (CacheInfos.TryGetValue(name, out ci)) return ci;
            ci = new CacheInfo(typeof(T));
            CacheInfos[name] = ci;
            return ci;
        }

        private class CacheInfo {
            public RedisAttribute CacheContract { get; private set; }

            private PropertyInfo CacheKeyProperty { get; set; }
            private PropertyInfo PrimaryKeyProperty { get; set; }
            public CacheInfo(Type type) {

                CacheContract = type.HasAttribute<RedisAttribute>()
                    ? type.FirstAttribute<RedisAttribute>()
                    : new RedisAttribute {
                        Compressed = false,
                        Expiry = null,
                        Name = type.Name
                    };

                if (CacheContract.Name == null) CacheContract.Name = type.Name;

                CacheKeyProperty = (type).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .SingleOrDefault(p =>
                        p.GetCustomAttributes(typeof(RedisKeyAttribute), false).Count() == 1);

                PrimaryKeyProperty = (type).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .SingleOrDefault(p =>
                        p.GetCustomAttributes(typeof(PrimaryKeyAttribute), false).Count() == 1);
            }

            public string GetTypePrefix() {
                Debug.Assert(CacheContract != null);
                return CacheContract.Name;
            }

            public string GetKey(object obj) {
                if (obj is string) {
                    return obj.ToString();
                }
                if (CacheKeyProperty != null) {
                    return CacheKeyProperty.GetValue(obj, null).ToString();
                }
                var iddo = obj as IDistributedDataObject;
                if (iddo != null) {
                    return iddo.Guid.ToBase64String();
                }
                // for IDataObject put CacheKey attribute on GUID to use it as a key
                var ido = obj as IDataObject;
                if (ido != null) {
                    return ido.Id.ToString(CultureInfo.InvariantCulture);
                }
                if (PrimaryKeyProperty == null) throw new ApplicationException("Cannot determine cache key. Add CacheKey or PrimaryKey attribute to a key property");
                return PrimaryKeyProperty.GetValue(obj, null).ToString();
            }

            public string GetFullKey(object obj) {
                return GetTypePrefix() + ":i:" + GetKey(obj);
            }
        }
    }
}