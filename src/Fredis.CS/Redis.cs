using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using ServiceStack.Common;
using ServiceStack.Text;
using StackExchange.Redis;

namespace Fredis {

    public partial class Redis : IRedis {

        // all non-commands stuff here

        public string KeyNameSpace { get; private set; }
        private readonly string _nameSpace;

        public Redis(string connectionString, string keyNameSpace = "") {
            ConnectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
            KeyNameSpace = keyNameSpace ?? ""; // just if null is provided
            _nameSpace = KeyNameSpace.IsNullOrEmpty() ? "" : KeyNameSpace + ":";
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
            return IsTypeCompressed<T>()
                ? ((byte[])result).GUnzip().FromJsv<T>()
                : ((string)result).FromJsv<T>();
        }

        private RedisValue PackResultNullable<T>(T item) {
            // TODO null check for only reference types
            return IsTypeCompressed<T>()
                ? (RedisValue)item.ToJsv().GZip()
                : (RedisValue)item.ToJsv();
        }

        /// <summary>
        /// Stores reflected cash info for each type
        /// </summary>
        private static readonly Dictionary<string, CacheInfo> CacheInfos = new Dictionary<string, CacheInfo>();

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
            public CacheContractAttribute CacheContract { get; private set; }

            private PropertyInfo CacheKeyProperty { get; set; }
            private PropertyInfo PrimaryKeyProperty { get; set; }
            public CacheInfo(Type type) {

                CacheContract = type.HasAttribute<CacheContractAttribute>()
                    ? type.FirstAttribute<CacheContractAttribute>()
                    : new CacheContractAttribute {
                        Compressed = false,
                        Expiry = null,
                        Name = type.Name
                    };

                CacheKeyProperty = (type).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .SingleOrDefault(p =>
                        p.GetCustomAttributes(typeof(CacheKeyAttribute), false).Count() == 1);

                PrimaryKeyProperty = (type).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .SingleOrDefault(p =>
                        p.GetCustomAttributes(typeof(PrimaryKeyAttribute), false).Count() == 1);
            }

            public string GetTypePrefix() {
                Debug.Assert(CacheContract != null);
                return CacheContract.Name;
            }

            public string GetKey(object obj) {
                // TODO keys of primitive types, add other tyeps
                if (obj is string || obj.GetType().IsPrimitive) {
                    return obj.ToString();
                }
                if (CacheKeyProperty != null) {
                    return CacheKeyProperty.GetValue(obj, null).ToString();
                }
                var iddo = obj as IDistributedDataObject;
                if (iddo != null) {
                    return iddo.Guid.ToString("N");
                }
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