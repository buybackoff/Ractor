using System;

namespace Fredis {

    // main difference is that we stick with IDataObjects for CRUD operations and 
    // generate keys from Id or Guid

    // think about related keys:
    // SetAdd<T, U>(T owner, string setId, U value)
    // Key becomes: NameSpace:OwnerType:OwnerId:set:setId

    // Also use hashes for storing I(Distributed)DataObjects as described in Redis manual
    // to reduce memory footprint - but only for values without expiry

    // set key names in such a scheme that key events subscription could be also typed
    // e.g. SubscribeToSet(owner, setId)

    // attributes for namespace, expire


    public enum When {
        Always = 0,
        Exists = 1,
        NotExists = 2
    }

    public interface IRedis {
        ISerializer Serializer { get; set; }
        //bool Set<T>(T item, TimeSpan? expiry = null, When when = When.Always, bool fireAndForget = false);
    }
}
