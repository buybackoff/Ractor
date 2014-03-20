using System;
using System.Runtime.Serialization;

namespace Fredis {
    /// <summary>
    /// Base implementation of IDataObject
    /// </summary>
    [DataContract]
    public abstract class BaseDistributedDataObject : IDistributedDataObject {
        [DataMember]
        [PrimaryKey]
        public virtual Guid Id { get; set; }

        [DataMember]
        public virtual DateTime UpdatedAt { get; set; }

    }
}