namespace Fredis

open System
open System.IO
open System.Runtime.Serialization.Formatters.Binary
open Nessos.FsPickler
open Fredis

[<ObsoleteAttribute>]
module internal Serialisers = 
    
    let Binary = 
        let formatter = new BinaryFormatter()
        let isEmpty (body:byte[]) = 
            Array.forall (fun b -> b = 0uy) body
        
        let serialise o =
            use ms = new MemoryStream()
            formatter.Serialize(ms, o)
            ms.ToArray()
        
        let deserialise body = 
            if not <| isEmpty body
            then
                use ms = new MemoryStream(body) 
                formatter.Deserialize(ms)
            else null

        { new ISerializer with
              member x.Serialize(payload) = serialise payload
              member x.Deserialize<'a>(body) = deserialise body :?> 'a
        }





