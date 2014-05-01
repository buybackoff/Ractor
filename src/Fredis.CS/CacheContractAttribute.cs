using System;

namespace Fredis {
    /// <summary>
    /// Mark a class cacheable and sets expiration span and naming convention
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class CacheContractAttribute : Attribute {
        public TimeSpan? Expiry { get; set; }
        //public string NameSpace { get; set; }
        public string Name { get; set; }
        public bool Compressed { get; set; }
        //public bool StoreAsHash { get; set; } // TODO

        public CacheContractAttribute() {
            Expiry = null; //TimeSpan.FromSeconds(60 * 60 * 24 * 365); // 1 year default could be unexpected
            //NameSpace = ""; // default will start from :ItemType:i:ItemId
            Name = "";
            Compressed = false;
            //StoreAsHash = false; // TODO
        }
    }


    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class CacheKeyAttribute : Attribute {
    }


}