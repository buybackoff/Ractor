using System;
using System.Runtime.Serialization;

namespace Fredis {

    /// <summary>
    /// Base implementation of IDistributedDataObject
    /// </summary>
    [DataContract]
    public abstract class BaseDistributedDataObject : BaseDataObject, IDistributedDataObject {

        [DataMember]
        [Index(false), Required]
        // When Guid index is not inuque then insert and "IN (..,..)" clause performances are orders of magnitude better, while select with "LIMIT 1" is as fast
        // remove UNIQUE constraint since it nonsense on Guid column (http://stackoverflow.com/questions/1705008/simple-proof-that-guid-is-not-unique)
        public Guid Guid { get; set; }
        // assume this fits memory and avoid IO mess by using AI PK from IDataObject, which is not used anywhere directly for IDistributedDataObject
        // use byte(16) instead of char(32)/(36) in private fork of SS.ORML.MySQL


        /// <summary>
        /// Returns default(Guid) for root assets and root guid for dependent assets
        /// </summary>
        public virtual Guid GetRootGuid() {
            return default(Guid); // e.g. if some object has a property UserGuid, UserGuid.ShardingKey()
        }

    }
}