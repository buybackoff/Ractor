using System;
using System.Runtime.Serialization;

namespace Ractor {
    /// <summary>
    /// Base implementation of IDataObject
    /// </summary>
    public abstract class BaseDataObject : IDataObject {

        // When Guid index is not inuque then insert and "IN (..,..)" clause performances are orders of magnitude better, while select with "LIMIT 1" is as fast
        // remove UNIQUE constraint since it nonsense on Guid column (http://stackoverflow.com/questions/1705008/simple-proof-that-guid-is-not-unique)
        public virtual Guid Id { get; set; }

        public bool IsDeleted { get; set; }
        public Guid PreviousId { get; set; }

        public DateTimeOffset LastModified() {
            return (PreviousId != Guid.Empty ? PreviousId : Id).Timestamp();
        }
        // assume this fits memory and avoid IO mess by using AI PK from IDataObject, which is not used anywhere directly for IDistributedDataObject
        // use byte(16) instead of char(32)/(36) in private fork of SS.ORML.MySQL
    }
}