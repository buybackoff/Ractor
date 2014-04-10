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
        /// Insert a new single item of type T and set its primary key if it was not set
        /// </summary>
        void InsertSetId<T>(ref T item) where T : IDataObject, new();

        /// <summary>
        /// Insert new items of type T and set their primary keys where they were not set
        /// </summary>
        void InsertSetId<T>(ref List<T> items) where T : IDataObject, new();

        /// <summary>
        /// 
        /// </summary>
        void Insert<T>(T item) where T : IDataObject, new();

        /// <summary>
        /// 
        /// </summary>
        void Insert<T>(List<T> items) where T : IDataObject, new();

        /// <summary>
        /// 
        /// </summary>
        void Update<T>(T item) where T : IDataObject, new();
        /// <summary>
        /// 
        /// </summary>
        void Update<T>(List<T> items) where T : IDataObject, new();

        /// <summary>
        /// 
        /// </summary>
        void Delete<T>(T item) where T : IDataObject, new();
        /// <summary>
        /// 
        /// </summary>
        void Delete<T>(List<T> items) where T : IDataObject, new();

        /// <summary>
        /// 
        /// </summary>
        T Single<T>(Expression<Func<T, bool>> predicate) where T : IDataObject, new();
        /// <summary>
        /// 
        /// </summary>
        T Single<T>(string sqlFilter, params object[] filterParams) where T : IDataObject, new();

        /// <summary>
        /// 
        /// </summary>
        List<T> Select<T>(Expression<Func<T, bool>> predicate) where T : IDataObject, new();
        /// <summary>
        /// 
        /// </summary>
        List<T> Select<T>(string sqlFilter, params object[] filterParams) where T : IDataObject, new();

        /// <summary>
        /// 
        /// </summary>
        void ExecuteSql(string sql, bool onShards = false);

        /// <summary>
        /// 
        /// </summary>
        T GetById<T>(object id) where T : IDataObject, new();
        /// <summary>
        /// 
        /// </summary>
        List<T> GetByIds<T>(List<object> ids) where T : IDataObject, new();

    }
}