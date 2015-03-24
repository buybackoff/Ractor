using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Ractor {

    /// <summary>
    /// Persists POCOs that implement IDataObject and are decorated with Ractor attributes
    /// </summary>
    public interface IPocoPersistor 
    {

        /// <summary>
        /// Insert a new single item of type T and set its primary key
        /// </summary>
        void Insert<T>(T item) where T : class, IData, new();

        /// <summary>
        /// Insert new items of type T and set their primary/guid keys
        /// </summary>
        void Insert<T>(List<T> items) where T : class, IData, new();


        /// <summary>
        /// 
        /// </summary>
        void Update<T>(T item) where T : class, IDataObject, new();
        /// <summary>
        /// 
        /// </summary>
        void Update<T>(List<T> items) where T : class, IDataObject, new();

        /// <summary>
        /// 
        /// </summary>
        void Delete<T>(T item) where T : class, IDataObject, new();
        /// <summary>
        /// 
        /// </summary>
        void Delete<T>(List<T> items) where T : class, IDataObject, new();

        // TODO Select() for select all

        /// <summary>
        /// 
        /// </summary>
        List<T> Select<T>(Expression<Func<T, bool>> predicate = null) where T : class, IDataObject, new();

        /// <summary>
        /// Generate new Guid for an item if Guid was not set, or keep existing.
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


        /// <summary>
        /// Get a new instance of DataContext, which is an EF dynamic context with all IData types that are loaded into current AppDomain
        /// </summary>
        DataContext GetContext();


        DistributedDataContext GetContext(byte bucket);


    }
}