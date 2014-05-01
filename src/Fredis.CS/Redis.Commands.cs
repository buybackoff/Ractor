using System;
using System.Threading.Tasks;
using ServiceStack.Common;
using ServiceStack.Text;
using StackExchange.Redis;

namespace Fredis
{

    // WIP
    // For each write command we need 
    // - fully typed version with implicit key + Async version;
    // - typed version with custom explicit key + Async version;
    // 
    // For each read command we need:
    // - typed version with root/owner object (for collections only) + Async version
    // - typed version with custom explicit key and optional bool to indicate prefixed explicit full key + Async version
    //
    // Need to test each method with primitive, struct(?), pure POCO, CacheContract decorated POCO, IDO, IDDO

    // Lists
    // Strings
    // Hashes
    // Sets
    // SortedSets
    // Keys
    // Other

    // if I do not understand anything in Fredis API without documentation then something is wrong
    // TODO write docs only after extracting interface and on inteface, not implementation

    public partial class Redis {
        // misc commands here
    }
}
