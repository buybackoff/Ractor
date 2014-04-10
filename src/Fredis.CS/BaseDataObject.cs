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

        /// <summary>
        /// Normaly all data is stored as events or as snapshots of state after a series of events 
        /// so this property means creation time for events and the time when a snapshot of a 
        /// state was taken for denormalized cache of state
        /// </summary>
        [DataMember]
        [Index(false)]
        public virtual DateTimeOffset UpdatedAt { get; set; }
    }
}