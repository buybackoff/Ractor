using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Fredis {

    /// <summary>
    /// Persists POCOs that implement IDataObject and are decorated with Fredis attributes
    /// </summary>
    public interface IPocoPersistor {

        /// <summary>
        /// Create a table for type T
        /// </summary>
        void CreateTable<T>(bool overwrite = false) where T : IDataObject, new();

        /// <summary>
        /// Insert a new single item of type T and set its primary key
        /// </summary>
        void Insert<T>(T item) where T : IDataObject, new();

        /// <summary>
        /// Insert new items of type T and set their primary/guid keys
        /// </summary>
        void Insert<T>(List<T> items) where T : IDataObject, new();


        /// <summary>
        /// Obsolete warning is a reminder to re-design database as an immutable value
        /// Updates and deletes should never happen (store current state snapshots in cache, not in a DB)
        /// http://www.infoq.com/presentations/Datomic-Database-Value slide at 28:10
        /// </summary>
        [Obsolete]
        void Update<T>(T item) where T : IDataObject, new();
        /// <summary>
        /// Obsolete warning is a reminder to re-design database as an immutable value
        /// Updates and deletes should never happen (store current state snapshots in cache, not in a DB)
        /// http://www.infoq.com/presentations/Datomic-Database-Value slide at 28:10
        /// </summary>
        [Obsolete]
        void Update<T>(List<T> items) where T : IDataObject, new();

        /// <summary>
        /// Obsolete warning is a reminder to re-design database as an immutable value
        /// Updates and deletes should never happen (store current state snapshots in cache, not in a DB)
        /// http://www.infoq.com/presentations/Datomic-Database-Value slide at 28:10
        /// </summary>
        [Obsolete]
        void Delete<T>(T item) where T : IDataObject, new();
        /// <summary>
        /// Obsolete warning is a reminder to re-design database as an immutable value
        /// Updates and deletes should never happen (store current state snapshots in cache, not in a DB)
        /// http://www.infoq.com/presentations/Datomic-Database-Value slide at 28:10
        /// </summary>
        [Obsolete]
        void Delete<T>(List<T> items) where T : IDataObject, new();

        /// <summary>
        /// 
        /// </summary>
        List<T> Select<T>(Expression<Func<T, bool>> predicate) where T : IDataObject, new();
        /// <summary>
        /// 
        /// </summary>
        List<T> Select<T>(string sqlFilter, params object[] filterParams) where T : IDataObject, new();

        /// <summary>
        /// Execute custom SQL and discard results
        /// </summary>
        void ExecuteSql(string sql, bool onShards = false);
        // TODO add method to execute any SQL that returns results. And other methods that make sense at this abstraction level without being too SS.ORML-specific
        // count

        /// <summary>
        /// 
        /// </summary>
        T GetById<T>(long id) where T : IDataObject, new();

        /// <summary>
        /// 
        /// </summary>
        T GetById<T>(Guid guid) where T : IDistributedDataObject, new();

        /// <summary>
        /// 
        /// </summary>
        List<T> GetByIds<T>(List<long> ids) where T : IDataObject, new();

        /// <summary>
        /// 
        /// </summary>
        List<T> GetByIds<T>(List<Guid> guids) where T : IDistributedDataObject, new();

        

    }
}