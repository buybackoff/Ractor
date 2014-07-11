using System;

namespace Ractor {
    /// <summary>
    /// Sets default expiration time span, name and compression option for a type
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class RedisAttribute : Attribute {
        public TimeSpan? Expiry { get; set; }
        public string Name { get; set; }
        public bool Compressed { get; set; }
        //public bool StoreAsHash { get; set; } // TODO??

        public RedisAttribute() {
            Expiry = null;
            Name = null;
            Compressed = false;
            //StoreAsHash = false; // TODO
        }
    }

    /// <summary>
    /// Use this property as cache key. Takes precedence over other options (IDataObject keys and PrimaryKey attributes)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RedisKeyAttribute : Attribute {
    }


}