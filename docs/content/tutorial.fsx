(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"

(**
Redis commands
------------------
*)

#r "BookSleeve.dll"
#r "Fredis.dll"

open System
open System.Text

open Fredis

// ported from F# rewrite of original BookSleeve tests here http://caxelrud.blogspot.ru/2013/03/redis-tests-with-f-using-booksleeve.html
// Here all test are rewritten with Fredis

let conn = new Connection("127.0.0.1", maxPoolSize = 2)

// evaluates Task<T> on current thread, shortcut for (Async.AwaitTask >> Async.RunSynchronously)
let res = !!!conn.Lists.RemoveFirstString(1, "nonex")


// This will block until in another redis client you do "SELECT 1" then repeat "LPUSH nonex abc" 5 times, see fsi output during this ops
//for i in 0..4 do
//    use c = +conn // this usage should be alway
//    let res = 
//        async {
//            // !! shortcut for Async.AwaitTask
//            let! t = !!c.Lists.BlockingRemoveFirstString(1, [|"nonex"|], 0)
//            Console.WriteLine("done")
//            return t
//        } |> Async.StartAsTask
//    ()

//Connections

let r1= !!!conn.Server.Ping()
conn.Features.Version

//Tests Strings--------------------------------------------

let Encode (value:string)=Encoding.UTF8.GetBytes(value)

let Decode(value:byte[])= Encoding.UTF8.GetString(value)


// APPEND

let a' = 
    async{
        // Async.AwaitIAsyncResult >> Async.Ignore
        return! !~conn.Strings.Set(1,"k1","abc")
    } |> Async.StartImmediate

// !~! shortcut for (Async.AwaitIAsyncResult >> Async.Ignore >> Async.StartImmediate)
let a = !~!conn.Strings.Set(1,"k1","abc") 

let r2 = async { return! !!conn.Strings.Append(1,"k1","def") } |> Async.RunSynchronously

(*> val it : int64 = 6L *)

let r3= !!!conn.Strings.GetString(1,"k1")

(*>val r3 : string = "abcdef" *)

//SET

!~!conn.Strings.Set(2,"k1","1")

conn.Strings.Set(2,"k2","2")

conn.Strings.Set(2,"k3","3")

conn.Strings.Set(2,"k4","4")

conn.Strings.Set(2,"k5","5")

let r4 = !!!conn.Strings.GetString(2,"k2")

(*> val it : string = "2" *)

let r4a= !!!conn.Strings.GetString(2,"k7")

(*> val it : string = null *)

//SETNX - SetNotExist

let r10 = !!!conn.Keys.Remove(1, "k1")

(*> val it : bool = true *)

let r11 = conn.Strings.SetIfNotExists(1,"k1","abc")

(*> val it : bool = true *)

let r12= !!!conn.Strings.SetIfNotExists(1,"k1","abc")

(*> val it : bool = false *)

//SETRANGE - Set

//zero based index

let r13= !!!conn.Strings.Set(1,"k1",1L,"x")

(*> val it : int64 = 3L *)

let r14= !!!conn.Strings.GetString(1,"k1")

(*> val it : string = "axc" *)

//INCR

conn.Strings.Set(1,"k2","1")

let r15= !!!conn.Strings.Increment(1,"k2")



(*> val it : int64 = 2L *)

//DECR

let r16= !!!conn.Strings.Decrement(1,"k2")



(*> val it : int64 = 1L *)

//INCRBY

conn.Strings.Set(1,"k2","1")

let r17= !!!conn.Strings.Increment(1,"k2",1L)



(*> val it : int64 = 2L *)

//DECRBY

let r18= !!!conn.Strings.Decrement(1,"k2",1L)



(*> val it : int64 = 1L *)

//INCRBYFLOAT (not implemented)

conn.Features.IncrementFloat;;

(*> val it : bool = false *)

//GETRANGE

//using GetString

conn.Strings.Set(1,"k3","abcdefghi")

let r19= !!!conn.Strings.GetString(1,"k3",2,4)



(*> val it : string = "cde" *)

//using Get

let r20= !!!conn.Strings.Get(1,"k3",2,4)


(*> val r21 : byte [] = [|99uy; 100uy; 101uy|] *)

Decode(r20);;

(*> val it : string = "cde" *)

//BITCOUNT,BITOP,GETBIT,SETBIT (not implemented in version 2.4)

conn.Features.BitwiseOperations;;

(*> val it : bool = false *)

//STRLEN

let r22= !!!conn.Strings.GetLength(1,"k3")



(*> val it : int64 = 9L *)

//GETSET

let r23= !!!conn.Strings.GetSet(1,"k2","4")



(*> val it : string = "3" *)

//KEYS-------------------------------------------

//DEL

let r30= !!!conn.Keys.Remove(1,"k1")



(*> val it : bool = true *)

let r31= !!!conn.Keys.Remove(1,[|"k2";"k3"|])



(*> val it : int64 = 2L *)

//EXITS

let r32= !!!conn.Keys.Exists(1,"k4")



(*> val it : bool = true *)

//TTL

let r33= !!!conn.Keys.TimeToLive(1,"k4")



(*> val it : int64 = -1L *)

//EXPIRE

let r34= !!!conn.Keys.Expire(1,"k4",1)



(*> val it : bool = true *)

//MOVE

let r35= !!!conn.Keys.Move(1,"k5",2)



(*> val it : bool = true *)

//PERSIST

let r36= !!!conn.Keys.Expire(2,"k5",1000)



let r37= !!!conn.Keys.Persist(2,"k5")



(*> val it : bool = true *)

//RANDOMKEY

let r38= !!!conn.Keys.Random(2)



(*> val it : string = "k4" *)

//SORT

//TYPE

let r39= !!!conn.Keys.Type(2,"k1")



(*> val it : string = "string" *)

//RENAME

conn.Keys.Rename(2,"k1","k1a")

let r40= !!!conn.Strings.GetString(2,"k1a");;



(*> val it : string = "1" *)

//RENAMENX

conn.Keys.RenameIfNotExists(2,"k2","k1a") //it will not rename

let r41= !!!conn.Strings.GetString(2,"k2")



(*> val it : string = "2" *)

//FIND

let r42= !!!conn.Keys.Find(2,"k*")



(*> val it : string [] = [|"k1a"; "k2"; "k3"; "k4"; "k5"|] *)

//HASHES----------------------------------------------------

//HSET

let r43= !!!conn.Hashes.Set(3,"h1","f1","value1")



(*> val it : bool = true *)

let r44= !!!conn.Hashes.Set(3,"h1","f2","value2")



(*> val it : bool = true *)

//HGET

let r45= !!!conn.Hashes.GetString(3,"h1","f2")



(*> val it : string = "value2" *)

let r46= !!!conn.Hashes.Get(3,"h1","f2")

(*> val it : byte [] = [|118uy; 97uy; 108uy; 117uy; 101uy; 50uy|] *)

Decode(r46);;

(*> val it : string = "value2" *)

//HSETNX

let r47= !!!conn.Hashes.SetIfNotExists(3,"h1","f3","value3")



(*> val it : bool = true *)

//HDEL

let r48= !!!conn.Hashes.Remove(3,"h1","f3")



(*> val it : bool = true *)

//HEXISTS

let r49= !!!conn.Hashes.Exists(3,"h1","f2")



(*> val it : bool = true *)

//HGETALL

let r50= !!!conn.Hashes.GetAll(3,"h1")



(*> val r50 : Task<Dictionary<string,byte []>>

val it : Dictionary<string,byte []> = !!!

dict

[("f1", [|118uy; 97uy; 108uy; 117uy; 101uy; 49uy|]);

("f2", [|118uy; 97uy; 108uy; 117uy; 101uy; 50uy|])]

*)

//HKEYS

let r51= !!!conn.Hashes.GetKeys(3,"h1")



(*> val it : string [] = [|"f1"; "f2"|] *)

//HVALUES

let r52= !!!conn.Hashes.GetValues(3,"h1")



(*> val it : byte [] [] = !!!

[|[|118uy; 97uy; 108uy; 117uy; 101uy; 49uy|];

[|118uy; 97uy; 108uy; 117uy; 101uy; 50uy|]|] *)

//HLEN

let r53= !!!conn.Hashes.GetLength(3,"h1")



(*> val it : int64 = 2L *)

//HINCRBY

let r54= !!!conn.Hashes.Set(3,"h1","f4","1")



let r55= !!!conn.Hashes.Increment(3,"h1","f4",1)



(*> val it : int64 = 2L *)

//List-------------------------------------------------------

//RPUSH (just one value)

let r56= !!!conn.Lists.AddLast(4,"l1","L11")



(*> *)

//LLEN

let r57= !!!conn.Lists.GetLength(4,"l1")



(*> val it : int64 = 1L *)

//RPUSHX

//conn.Features.PushIfNotExists;;

let r58= !!!conn.Lists.AddLast(4,"l1","L12",createIfMissing=false)



(*> val it : int64 = 2L *)

conn.Lists.AddLast(4,"l1","L13")

conn.Lists.AddLast(4,"l1","L14")

conn.Lists.AddLast(4,"l1","L15")

//LRANGE

let r59= !!!conn.Lists.RangeString(4,"l1",0,2) //0,-1 gets all



(*> val it : string [] = [|"L11"; "L12"; "L13"|] *)

//LPUSH

let r60= !!!conn.Lists.AddFirst(4,"l1","L10")



(*> val it : int64 = 6L *)

//LPUSHX

//conn.Features.PushIfNotExists

let r61= !!!conn.Lists.AddFirst(4,"l1","L1(-1)",createIfMissing=false)



(*> val it : int64 = 7L *)

//LPOP

let r62= !!!conn.Lists.RemoveFirstString(4,"l1")



(*> val it : string = "L1(-1)" *)

//RPOP

let r63= !!!conn.Lists.RemoveLastString(4,"l1")



(*> val it : string = "L15" *)

//RPOPLPUSH

let r64= !!!conn.Lists.RemoveLastAndAddFirstString(4,"l1","l2")



(*> val it : string = "L14" *)

//LINDEX

let r65= !!!conn.Lists.GetString(4,"l1",0)



(*> val it : string = "L10" *)

//LINSERT

//conn.Features.ListInsert

let r66= !!!conn.Lists.InsertBefore(4,"l1","L13","L12a")



(*> val it : int64 = 5L *)

let r67= !!!conn.Lists.InsertAfter(4,"l1","L13","L14")



(*> val it : int64 = 6L *)

//LREM

let r68= !!!conn.Lists.Remove(4,"l1","L14")



(*> val it : int64 = 1L *)

let r68a= !!!conn.Lists.Remove(4,"l1","L12",count=2)



(*> val it : int64 = 1L *)

//LTRIM

!~!conn.Lists.Trim(4,"l1",4)

//r69.Wait();;

(*> val it : unit = () *)

//LSET

!~!conn.Lists.Set(4,"l1",1,"L1a")

(*> val it : unit = () *)

//Sets----------------------------------------------

//SADD

let r71= !!!conn.Sets.Add(5,"s1","S1")



(*> val it : bool = true *)

let r72= !!!conn.Sets.Add(5,"s1",[|"S2";"S3";"S4";"S5";"S6";"S7"|])



(*> val it : int64 = 6L *)

//SREM

let r73= !!!conn.Sets.Remove(5,"s1","S4")



(*> val it : bool = true *)

let r74= !!!conn.Sets.Remove(5,"s1",[|"S2";"S3"|])



(*> val it : int64 = 2L *)

//SISMEMBER

let r75= !!!conn.Sets.Contains(5,"s1","S1")



(*> val it : bool = true *)

//SRANDMEMBER

let r76= !!!conn.Sets.GetRandomString(5,"s1")



(*> val it : string = "S1" *)

let r77= !!!conn.Sets.RemoveRandomString(5,"s1")



(*> val it : string = "S5" *)

//SMEMBERS

let r78= !!!conn.Sets.GetAllString(5,"s1")



(*> val it : string [] = [|"S1"; "S2"; "S3"; "S4"; "S6"; "S7"|] *)

//SMOVE

let r79= !!!conn.Sets.Move(5,"s1","s2","S7")



(*> val it : bool = true *)

//SDIFF

let r80= !!!conn.Sets.Add(5,"s1","S7")



(*> val it : bool = true *)

let r81= !!!conn.Sets.DifferenceString(5,[|"s1";"s2"|])



(*> val it : string [] = [|"S5"; "S1"|] *)

//SDIFFSTORE

let r82= !!!conn.Sets.DifferenceAndStore(5,"s3",[|"s1";"s2"|])



(*> val it : int64 = 2L *)

let r83= !!!conn.Sets.GetAllString(5,"s3")



(*> val it : string [] = [|"S5"; "S1"|] *)

//SINTER

let r84= !!!conn.Sets.IntersectString(5,[|"s1";"s2"|])



(*> val it : string [] = [|"S7"|] *)

//SINTERSTORE

let r85= !!!conn.Sets.IntersectAndStore(5,"s4",[|"s1";"s2"|])



(*> val it : int64 = 1L *)

let r86= !!!conn.Sets.GetAllString(5,"s4")



(*> val it : string [] = [|"S7"|] *)

//SUNION

let r87= !!!conn.Sets.UnionString(5,[|"s1";"s2"|])



(*> val it : string [] = [|"S5"; "S1"; "S7"|] *)

//SUNIONSTORE

let r88= !!!conn.Sets.UnionAndStore(5,"s5",[|"s1";"s2"|])



(*> val it : int64 = 3L *)

let r89= !!!conn.Sets.GetAllString(5,"s5")



(*> val it : string [] = [|"S5"; "S1"; "S7"|] *)

//SortedSets-----------------------------------------------------

//ZADD

let r90= !!!conn.SortedSets.Add(6,"z1","Z11",10000.0)



(*> val it : bool = true *)

let r91= !!!conn.SortedSets.Add(6,"z1","Z12",10001.0)



(*> val it : bool = true *)

//ZRANGE

let r92= !!!conn.SortedSets.RangeString(6,"z1",0L,-1L)



(*> val it : KeyValuePair<string,float> [] = [|[Z11, 10000]; [Z12, 10001]|] *)

//ZSCORE

let r93= !!!conn.SortedSets.Score(6,"z1","Z11")



(*> val it : Nullable<float> = 10000.0 *)

//ZRANGE

let r94= !!!conn.SortedSets.Rank(6,"z1","Z12")



(*> val it : Nullable<int64> = 1L *)

//ZREM

let r95= !!!conn.SortedSets.Remove(6,"z1","Z12")



(*> val it : bool = true *)

let r96= !!!conn.SortedSets.Add(6,"z1","Z12",10002.0)



let r97= !!!conn.SortedSets.Add(6,"z1","Z13",10003.0)



let r98= !!!conn.SortedSets.Add(6,"z1","Z14",10004.0)



//ZREMRANGEBYSCORE

let r99= !!!conn.SortedSets.RemoveRange(6,"z1",10003.0,10004.0)



(*> val it : int64 = 2L *)

let r100= !!!conn.SortedSets.Add(6,"z1","Z13",10003.0)



let r101= !!!conn.SortedSets.Add(6,"z1","Z14",10004.0)



//ZREMRANGEBYRANGE

let r102= !!!conn.SortedSets.RemoveRange(6,"z1",0L,1L)



(*> val it : int64 = 2L *)

(**
TBD
*)
