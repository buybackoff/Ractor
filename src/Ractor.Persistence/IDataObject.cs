using System;
using System.Runtime.Serialization;

namespace Ractor {
    /// <summary>
    /// Structured data objects stored in RDBMSs or Redis schema with separate fields and foreign keys
    /// </summary>
    public interface IDataObject {

        [AutoIncrement, PrimaryKey]
        long Id { get; set; }

        /// <summary>
        /// Normally all data changes are stored as events so this property means creation time for 
        /// events and the time when a snapshot of a state was taken for denormalized cache of state
        /// </summary>
        [Index(false)]
        DateTime UpdatedAt { get; set; }
    }
}