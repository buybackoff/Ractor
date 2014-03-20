using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Fredis {


    //public interface IHasId<T> : ServiceStack.Model.IHasId<T> where T : IEquatable<T> { 
    //// we already have the "together forever" dependency for ORM, keep it there just
    //// in case it will help to integrate better with SS

    //}


    // TODO Cache - use my extended CacheClient


    // IEncryption with Encrypt/Decrypt and default dummy base class

    /// <summary>
    /// Structured data objects stored in RDBMSs or Redis schema with separate fields and foreign keys
    /// </summary>
    public interface IDataObject {

    }

    /// <summary>
    /// Data objects that could be stored as key-value where value is a serizlized object 
    /// </summary>
    public interface IDistributedDataObject : IDataObject, ServiceStack.Model.IHasId<Guid> {
        
    }



    // Fredis
    //      .Cache - ICacheProvider
    //      .Relational - IRelationalPersistor
    //      .Distributed - IDistributedPersistor
    //      .Blob - IBlobPersistor
    //      .File - IFilePersistor
    // CRUD (+Exist & Create/Drop Table) operations on Fredis instance that act according to attributes and defaults
    // Relationaships at Fredis level, e.g. Relational could have Distributed that in turn 
    // could have Blobs
    //
    // IDistributedProvider should have a method that returns IDP based on GUID
    // GUID is nice to reshard later at any point, all methods must be virtual so that
    // initially we could use on DB, then use some sharding strategy without affecting
    // existing DB and clients
    // 
    // Relational & Distributed should be merged or throw when an Object doesn't implement 
    // relevant base class?


    // Protected - atrributes for class and properties/fields + ICryptoProvider(generic encr/decr)
    // Dynamo - only Distributed
    // RDBMS - both
    // Cache - wrap around SS cache cleints
    // File - do not use SS's - simplified bucket/file_hash which then could be used for implementation of SS's VFS
    // Blob - initially limit to S3

    // Attribute secondary index or composite index
    
    // DB implementation should just work with IDataObject

    // Relational could be just an internal implementation of Distributed - by default, if distributed 
    // is not supplied directly

    // Cache miss for distributed is OK, for relational should be a Data Integrity issue!!!
    // Should lookup on miss as in Distributed case, but log it as a warning

    public interface IRelationalPersistor {

        void CreateTable<T>(bool overwrite = false) where T : new();

        void InsertSingle<T>(T item) where T : IDataObject, new();
        void Insert<T>(List<T> items) where T : IDataObject, new();

        void InsertGetIdSingle<T>(ref T item) where T : IDataObject, new();
        void InsertGetIds<T>(ref List<T> items) where T : IDataObject, new();

        void Update<T>(List<T> items) where T : IDataObject, new();

        void Delete<T>(List<T> items) where T : IDataObject, new();

        List<T> Select<T>(Expression<Func<T, bool>> predicate) where T : IDataObject, new();

        List<T> Select<T>(string sqlFilter, params object[] filterParams) where T : new();

        void ExecuteSql(string sql, bool onShards = false);

        T GetById<T>(long id) where T : IDataObject, new();
        List<T> GetByIds<T>(List<long> ids) where T : IDataObject, new();

    }


    public interface IDistributedPersistor {

        void CreateTable<T>(bool overwrite = false) where T : IDistributedDataObject, new();
        
        void Insert<T>(List<T> items) where T : IDistributedDataObject, new();
        
        T GetById<T>(long id) where T : IDistributedDataObject, new();
        List<T> GetByIds<T>(List<long> ids) where T : IDistributedDataObject, new();

        void Update<T>(List<T> items) where T : IDistributedDataObject, new();

        void Delete<T>(List<T> items) where T : IDistributedDataObject, new();

        List<T> Select<T>(Expression<Func<T, bool>> predicate) where T : IDistributedDataObject, new();

    }

}
