using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using ServiceStack.Text;
using StackExchange.Redis;

namespace Fredis {
    

    public partial class Redis {

        // non-commands

        public Redis(string connectionString) {
            ConnectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);

        }

        public ConnectionMultiplexer ConnectionMultiplexer { get; private set; }

        /// <summary>
        /// Return a key for an item T, e.g. NameSpace:ItemType:ItemId
        /// </summary>
        public string GetItemFullKey<T>(T item) {
            var ci = GetCacheInfo<T>();
            return ci.GetFullKey(item);
        }

        public string GetItemKey<T>(T item) {
            var ci = GetCacheInfo<T>();
            return ci.GetKey(item);
        }

        public string GetTypePrefix<T>() {
            var ci = GetCacheInfo<T>();
            return ci.GetTypePrefix();
        }

        private TimeSpan? GetTypeExpiry<T>() {
            var ci = GetCacheInfo<T>();
            return ci.CacheContract == null ? null : ci.CacheContract.Expiry;
        }

        private bool IsTypeCompressed<T>() {
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
                default:
                    return StackExchange.Redis.When.NotExists;
            }
        }

        private IDatabase GetDb() {
            return ConnectionMultiplexer.GetDatabase();
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
            public CacheContractAttribute CacheContract { get; set; }
            
            private PropertyInfo CacheKeyProperty { get; set; }

            private PropertyInfo PrimaryKeyProperty { get; set; }

            public CacheInfo(Type type) {
                CacheContract = type.HasAttribute<CacheContractAttribute>()
                    ? type.FirstAttribute<CacheContractAttribute>()
                    : null;

                if (type.HasAttribute<CacheKeyAttribute>()) {
                    CacheKeyProperty = (type).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Single(p =>
                            p.GetCustomAttributes(typeof (CacheKeyAttribute), false).Count() == 1);
                } else {
                    CacheKeyProperty = null;
                }

                if (type.HasAttribute<PrimaryKeyAttribute>()) {
                    PrimaryKeyProperty = (type).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Single(p =>
                            p.GetCustomAttributes(typeof(PrimaryKeyAttribute), false).Count() == 1);
                } else {
                    PrimaryKeyProperty = null;
                }
            }

            public string GetTypePrefix() {
                return CacheContract.NameSpace + ":" + CacheContract.Name;
            }

            public string GetKey(object obj) {

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

                if (PrimaryKeyProperty == null) throw new ApplicationException("Missing PrimaryKeyAttribute");

                return PrimaryKeyProperty.GetValue(obj, null).ToString();
            }

            /// <summary>
            /// e.g. ns:n:key
            /// </summary>
            public string GetFullKey(object obj) {
                return GetTypePrefix() + ":" + GetKey(obj);
            }
        }
    
    }
}