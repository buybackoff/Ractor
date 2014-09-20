using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Ractor {

    /// <summary>
    /// Persists POCOs that implement IDataObject and are decorated with Ractor attributes
    /// </summary>
    public interface IPocoPersistor {

        /// <summary>
        /// Insert a new single item of type T and set its primary key
        /// </summary>
        void Insert<T>(T item) where T : class, IDataObject, new();

        /// <summary>
        /// Insert new items of type T and set their primary/guid keys
        /// </summary>
        void Insert<T>(List<T> items) where T : class, IDataObject, new();


        /// <summary>
        /// Obsolete warning is a reminder to re-design database as an immutable value
        /// Updates and deletes should never happen (store current state snapshots in cache, not in a DB)
        /// http://www.infoq.com/presentations/Datomic-Database-Value slide at 28:10
        /// </summary>
        [Obsolete]
        void Update<T>(T item) where T : class, IDataObject, new();
        /// <summary>
        /// Obsolete warning is a reminder to re-design database as an immutable value
        /// Updates and deletes should never happen (store current state snapshots in cache, not in a DB)
        /// http://www.infoq.com/presentations/Datomic-Database-Value slide at 28:10
        /// </summary>
        [Obsolete]
        void Update<T>(List<T> items) where T : class, IDataObject, new();

        /// <summary>
        /// Obsolete warning is a reminder to re-design database as an immutable value
        /// Updates and deletes should never happen (store current state snapshots in cache, not in a DB)
        /// http://www.infoq.com/presentations/Datomic-Database-Value slide at 28:10
        /// </summary>
        [Obsolete]
        void Delete<T>(T item) where T : class, IDataObject, new();
        /// <summary>
        /// Obsolete warning is a reminder to re-design database as an immutable value
        /// Updates and deletes should never happen (store current state snapshots in cache, not in a DB)
        /// http://www.infoq.com/presentations/Datomic-Database-Value slide at 28:10
        /// </summary>
        [Obsolete]
        void Delete<T>(List<T> items) where T : class, IDataObject, new();

        // TODO Select() for select all

        /// <summary>
        /// 
        /// </summary>
        List<T> Select<T>(Expression<Func<T, bool>> predicate = null) where T : class, IDataObject, new();

        /// <summary>
        /// Generate new Guid for an item if Guid was not set, or return existing.
        /// </summary>
        void GenerateGuid<T>(ref T item, DateTime? utcDateTime = null, bool replace = false) where T : IDataObject;

        /// <summary>
        /// 
        /// </summary>
        T GetById<T>(Guid guid) where T : class, IDataObject, new();


        /// <summary>
        /// 
        /// </summary>
        List<T> GetByIds<T>(List<Guid> guids) where T : class, IDataObject, new();


    }
}