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
        [PrimaryKey]
        // When Guid index is not inuque then insert and "IN (..,..)" clause performances are orders of magnitude better, while select with "LIMIT 1" is as fast
        // remove UNIQUE constraint since it nonsense on Guid column (http://stackoverflow.com/questions/1705008/simple-proof-that-guid-is-not-unique)
        public virtual Guid Id { get; set; }
        // assume this fits memory and avoid IO mess by using AI PK from IDataObject, which is not used anywhere directly for IDistributedDataObject
        // use byte(16) instead of char(32)/(36) in private fork of SS.ORML.MySQL

    }
}