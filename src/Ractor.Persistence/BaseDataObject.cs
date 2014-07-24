using System;
using System.Runtime.Serialization;
using Nest;

namespace Ractor {
    /// <summary>
    /// Base implementation of IDataObject
    /// </summary>
    [DataContract]
    [ElasticType(IdProperty = "Guid")]
    public abstract class BaseDataObject : IDataObject {
        [DataMember]
        [AutoIncrement, PrimaryKey]
        public virtual long Id { get; set; }

        [DataMember]
        [Index(false), Required]
        // When Guid index is not inuque then insert and "IN (..,..)" clause performances are orders of magnitude better, while select with "LIMIT 1" is as fast
        // remove UNIQUE constraint since it nonsense on Guid column (http://stackoverflow.com/questions/1705008/simple-proof-that-guid-is-not-unique)
        public Guid Guid { get; set; }
        // assume this fits memory and avoid IO mess by using AI PK from IDataObject, which is not used anywhere directly for IDistributedDataObject
        // use byte(16) instead of char(32)/(36) in private fork of SS.ORML.MySQL


        /// <summary>
        /// Normaly all data is stored as events or as snapshots of state after a series of events 
        /// so this property means creation time for events and the time when a snapshot of a 
        /// state was taken for denormalized cache of state
        /// </summary>
        [DataMember]
        [Index(false)]
        public virtual DateTime UpdatedAt { get; set; }
    }
}