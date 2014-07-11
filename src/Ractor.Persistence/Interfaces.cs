using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ractor {
    // TODO Cache - use my extended CacheClient
    // TODO IEncryption with Encrypt/Decrypt and default dummy base class


    // Ractor
    //      .Cache - ICacheProvider
    //      .Relational - IRelationalPersistor
    //      .Distributed - IDistributedPersistor
    //      .Blob - IBlobPersistor
    //      .File - IFilePersistor
    // CRUD (+Exist & Create/Drop Table) operations on Ractor instance that act according to attributes and defaults
    // Relationaships at Ractor level, e.g. Relational could have Distributed that in turn 
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


    // I alos need an event store - each action, especially DELETE, must be stored to be able
    // to repeat the history

}
