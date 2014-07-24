using System;

namespace Ractor {
    /// <summary>
    /// Structured data objects stored in RDBMSs or Redis schema with separate fields and foreign keys
    /// </summary>
    public interface IDataObject {

        /// <summary>
        /// Autoincremented primary key assigned automatically inside DB. Not intended for a direct use, rather for DB PK partitioning.
        /// </summary>
        [AutoIncrement, PrimaryKey]
        long Id { get; set; }

        /// <summary>
        /// Code-generated unique ID. For distributed objects it contains virtual shard and epoch.
        /// </summary>
        [Index(false), Required]
        Guid Guid { get; set; }

        /// <summary>
        /// UTC time when an object was modified.
        /// </summary>
        [Index(false)]
        DateTime UpdatedAt { get; set; }
    }
}