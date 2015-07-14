using System;
using System.Runtime.Serialization;

namespace Ractor {

    /// <summary>
    /// Base implementation of IDistributedDataObject
    /// </summary>
    public abstract class BaseDistributedDataObject : BaseDataObject, IDistributedDataObject {

        /// <summary>
        /// Returns default(Guid) for root assets and root guid for dependent assets
        /// </summary>
        public virtual Guid GetRootGuid() {
            return default(Guid); // e.g. if some object has a property UserGuid, UserGuid.GetRootGuid()
        }
    }
}