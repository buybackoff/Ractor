using System;
using System.Configuration;

namespace Ractor {

    /// <summary>
    /// Immutable data. Marker interface for EF context automigration
    /// </summary>
    public interface IData
    {

    }

    /// <summary>
    /// Mutable data with history. Structured data objects stored in RDBMSs or Redis schema with separate fields and foreign keys.
    /// </summary>
    public interface IDataObject : IData {
        /// <summary>
        /// Code-generated unique ID for each DB record. For distributed objects it contains shard id as a part of Guid.
        /// </summary>
        Guid Id { get; set; }

        /// <summary>
        /// Flag for soft-delete
        /// </summary>
        bool IsDeleted { get; set; }

        /// <summary>
        /// Guid of the previous version, which is a newly generated timestamped Guid. If this 
        /// property is not null then PreviousGuid contains last modified time
        /// </summary>
        Guid PreviousId { get; set; }


        /// <summary>
        /// Guid.GetDateTime(PreviousGuid ?? Guid)
        /// </summary>
        /// <returns></returns>
        DateTimeOffset LastModified();

        // Every data object is an event

        // S3-like versioning
        // Guid contains creating timestamp
        // IsActual - bool
        // Version - byte or short

        // PreviousVersion : Guid? - it contains new Guid with time when this (last) version
        // was updated, so we could use GetDateTime(PreviousVersion ?? Guid) to get LastChanged 

        // Update
        // Insert 
    }
}