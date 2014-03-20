namespace Fredis

open System
open System.Collections.Generic

/// Global unique key of a data structure and Redis key
type Key = string

// For now limite structures to lists and hashes

// Redis structures

type RStructure<'T> =
| RValue of 'T
| RList of IList<'T>
| RDictionary of IDictionary<string,'T>
// ... set, sorted set

type VersionedStructure<'T> = Key * int64 * RStructure<'T>
 

 // 