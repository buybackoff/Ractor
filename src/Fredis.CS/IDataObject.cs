using System;
using System.Runtime.Serialization;

namespace Fredis {
    /// <summary>
    /// Structured data objects stored in RDBMSs or Redis schema with separate fields and foreign keys
    /// </summary>
    public interface IDataObject {

        [AutoIncrement, PrimaryKey]
        long Id { get; set; }

        /// <summary>
        /// Normaly all data changes are stored as events so this property means creation time for 
        /// events and the time when a snapshot of a state was taken for denormalized cache of state
        /// </summary>
        [Index(false)]
        DateTimeOffset UpdatedAt { get; set; }
    }
}