using System;
using System.Runtime.Serialization;

namespace Fredis {
    /// <summary>
    /// Base implementation of IDataObject
    /// </summary>
    [DataContract]
    public abstract class BaseDataObject : IDataObject {
        [DataMember]
        [AutoIncrement, PrimaryKey]
        public virtual long Id { get; set; }

        [DataMember]
        public virtual DateTime UpdatedAt { get; set; }

    }
}