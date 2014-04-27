using System;

namespace Fredis {
    /// <summary>
    /// Data objects that could be stored as key-value where value is a serizlized object 
    /// </summary>
    public interface IDistributedDataObject : IDataObject {
        [Index(true), Required] 
        Guid Guid { get; set; }

        /// <summary>
        /// Returns default(Guid) for root assets and root guid for dependent assets
        /// 
        /// We guarantee that this.Guid will be in the same shard as the guid that this method returns
        /// </summary>
        Guid GetRootGuid();
    }
}