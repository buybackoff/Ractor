#I "../../bin"
#r "Ractor.dll"
#r "Ractor.Persistence.dll"

open System
open System.Linq
open System.Text
open System.Threading.Tasks
open System.Collections.Generic
open Ractor
open System
open System.Text
open System.Diagnostics
open System.Threading


let redis = Redis("localhost", "test")

let lua = 
            @"  local previousKey = KEYS[1]..':previousKeys'
                local currentKey = KEYS[1]..':currentKeys'
                local currentItems = redis.call('HKEYS', KEYS[1])
                local res = 0
                redis.call('DEL', currentKey)
                if redis.call('HLEN', KEYS[1]) > 0 then
                   redis.call('SADD', currentKey, unpack(currentItems))
                   local intersect
                   if redis.call('SCARD', previousKey) > 0 then
                       intersect = redis.call('SINTER', previousKey, currentKey)
                       if #intersect > 0 then
                            redis.call('HDEL', KEYS[1], unpack(intersect))
                            res = #intersect
                       end
                   end
                end
                redis.call('DEL', previousKey)
                if #currentItems > 0 then
                    redis.call('SADD', previousKey, unpack(currentItems))
                end
                return res
            "
redis.HSet<string>("hkey", "field", "result") |> ignore
redis.HSet<string>("hkey", "field2", "result") |> ignore

let result = deleteRepeatingItemsInHSET(redis, "Ractor:{Greeter}:Mailbox:results")
                                                
result



let nested = (((1,2),3), 4)

let flatTuple4 (tuple : ((('T1 * 'T2) * 'T3) * 'T4)  ) =
    let (((v1, v2), v3), v4) = tuple
    v1, v2, v3, v4

let ft = flatTuple4 nested
    
