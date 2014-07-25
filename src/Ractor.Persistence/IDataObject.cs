using System;

namespace Ractor {
    /// <summary>
    /// Structured data objects stored in RDBMSs or Redis schema with separate fields and foreign keys
    /// </summary>
    public interface IDataObject {

        /// <summary>
        /// Code-generated unique ID. For distributed objects it contains shard id.
        /// </summary>
        [PrimaryKey]
        Guid Id { get; set; }

        /// <summary>
        /// UTC time when an object was modified.
        /// </summary>
        [Index(false)]
        DateTime UpdatedAt { get; set; }
    }
}