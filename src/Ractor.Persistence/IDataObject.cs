using System;

namespace Ractor {
    /// <summary>
    /// Structured data objects stored in RDBMSs or Redis schema with separate fields and foreign keys
    /// </summary>
    public interface IDataObject {
        /// <summary>
        /// Code-generated unique ID. For distributed objects it contains shard id.
        /// </summary>
        Guid Id { get; set; }

        /// <summary>
        /// Flag for soft-delete
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// Id of the previous version, which is a newly generated timestamped Guid. If this 
        /// property is not null then PreviousId contains last modified time
        /// </summary>
        Guid? PreviousId { get; set; }


        /// <summary>
        /// Guid.GetDateTime(PreviousId ?? Id)
        /// </summary>
        /// <returns></returns>
        DateTimeOffset LastModified();

        // Every data object is an event

        // S3-like versioning
        // Id contains creating timestamp
        // IsActual - bool
        // Version - byte or short

        // PreviousVersion : Guid? - it contains new Guid with time when this (last) version
        // was updated, so we could use GetDateTime(PreviousVersion ?? Id) to get LastChanged 

        // Update
        // Insert 
    }
}