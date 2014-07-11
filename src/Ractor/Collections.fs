namespace Ractor

open System
open System.Collections
open System.Collections.Generic
open System.Runtime.Caching

// Distributed synchronized collections

[<AbstractClassAttribute>]
type DistributedCollectionBase
    internal() =
    let syncRoot = Object()
    static member Cache = new MemoryCache("DistributedCollections")
    member this.SyncRoot = syncRoot

[<AbstractClassAttribute>]
type DistributedCollection<'T>
    internal(redis:Redis, key:string) =
    inherit DistributedCollectionBase()
    
    let prefix = key + ":"
    let channel = prefix + "syncChannel"

    abstract GetEnumerator : unit -> IEnumerator<'T>
    abstract Count : int with get

    interface IEnumerable with
        member this.GetEnumerator() = this.GetEnumerator() :> IEnumerator

    interface IEnumerable<'T> with
        member this.GetEnumerator() = this.GetEnumerator()

    interface ICollection  with
        member this.SyncRoot = this.SyncRoot
        member this.CopyTo(array, arrayIndex) =
            if array = null then raise (ArgumentNullException("array"))
            if arrayIndex < 0 || arrayIndex > array.Length then raise (ArgumentOutOfRangeException("arrayIndex"))
            if array.Length - arrayIndex < this.Count then raise (ArgumentException("ArrayPlusOffTooSmall"))
            let mutable index = 0
            for value in this do // using enumerator
                array.SetValue(value, arrayIndex + index)
                index <- index + 1
        member this.Count = this.Count
        member this.IsSynchronized with get() = false
