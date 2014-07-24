using System;

namespace Ractor {
    /// <summary>
    /// Data objects distributes accross shards 
    /// </summary>
    public interface IDistributedDataObject : IDataObject {
        
        /// <summary>
        /// Returns default(Guid) for root assets and root guid for dependent assets
        /// 
        /// We guarantee that this.Guid will be in the same shard as the guid that this method returns
        /// </summary>
        Guid GetRootGuid();
    }
}