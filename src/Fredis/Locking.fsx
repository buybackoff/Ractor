#I "../../bin"
#r "BookSleeve.dll"
#r "Fredis.dll"
#load "Connection.fs"
#load "Monitor.fs"

open System
open System.Text
open System.Diagnostics
open Fredis

// ported from F# rewrite of original BookSleeve tests here http://caxelrud.blogspot.ru/2013/03/redis-tests-with-f-using-booksleeve.html
// Here all test are rewritten with Fredis

let conn = (new Connection("127.0.0.1"))
//let db = 0
//let lockingConn = +conn
//let lockerKey = "l" + ":fredis_lock"
//let lockerExpiry = 10
//
//let rec tryEnter () =
//    async {
//        let lua = @"
//        local relay = KEYS[1] -- TODO remove comments out of scripts when all tested!
//        local started_flag = relay..'_flag'
//        local timeout = ARGV[1]
//        if (0 == redis.call('exists', started_flag)) and (0 == redis.call('exists', relay)) then
//            redis.call('SET', started_flag, '1')
//            redis.call('RPUSH', relay, 'baton')
//            redis.call('PEXPIRE', started_flag, timeout)
//            redis.call('PEXPIRE', relay, timeout)
//        end
//        return redis.call('PTTL', started_flag)
//                        "
//
//        let pttl = (!!!lockingConn.Scripting.Eval(db, lua, [|lockerKey|],[|lockerExpiry * 1000|])) :?> Int64
//        // here someone could steal the lock and we could steal from someone as well - it is fair
//        let! res = !!lockingConn.Lists.BlockingRemoveFirstString(db,[|lockerKey|], int(pttl/1000L)+1) // +1 to avoid infinite block 
//        // max delay 999ms after expiration, but expirations here are not expected at all
//        // lock could expire e.g. on power outage on a machine that acquired it
//        if box res = null then 
//            Console.WriteLine("expired - should never happen in production. TODO consider throwing here")
//            return! tryEnter () 
//        else
//            Console.WriteLine("acquired lock") 
//            ()
//    }
//
//let exit () =
//    let lua = @"
//        local relay = KEYS[1] -- TODO remove comments out of scripts when all tested!
//        local started_flag = relay..'_flag'
//        local timeout = ARGV[1]
//        local flag_ok = (1 == redis.call('PEXPIRE', started_flag, timeout))
//        -- return flag_ok
//        local relay_ok = (1 == redis.call('PEXPIRE', relay, timeout+1))
//        if flag_ok and relay_ok then
//            -- relay exists, but this should never happen, we are exiting after someone exited and noone entered
//            redis.call('LPOP', relay) -- remove baton to close entry, return 0 to take an action
//            return -1
//        end
//        if flag_ok then
//            redis.call('INCR', started_flag) -- next round
//            redis.call('RPUSH', relay, 'baton') -- baton exchange
//            redis.call('PEXPIRE', started_flag, timeout)
//            redis.call('PEXPIRE', relay, timeout+1) -- avoid relay expire before flag due to the 1 ms error
//            return 1
//        else
//            return 0 -- should not expire in normal conditions
//        end
//                    "
//    let res = !!!conn.Scripting.Eval(db, lua, [|lockerKey|],[|lockerExpiry * 1000|]) :?> Int64
//    res 
//
////tryEnter () |> Async.RunSynchronously
////exit()
////1
//
//

//
//let t1 = async {
//        use! lock = makeLock conn 0 "l" 10
//        Console.Write(" 1.")
//        //do! Async.Sleep(1000) // long task
//        Console.Write(".1 ")
//    }
//
//let t2 = async {
//        use! lock = makeLock conn 0 "l" 10
//        Console.Write(" 2.")
//        //do! Async.Sleep(1000) // long task
//        Console.Write(".2 ")
//    }
//
//let agent1 = MailboxProcessor<unit>.Start(fun inbox ->
//        async {
//            for i in 0..10 do
//                do! t1
//            ()
//        }
//        )
//let agent2 = MailboxProcessor<unit>.Start(fun inbox ->
//        async {
//            for i in 0..10 do
//                do! t2
//            ()
//        }
//        )
1

let incTest (c:Connection)  =
        use lock = !!!c.MakeLock(0, "l", 10)
        let v1 = !!!c.Strings.GetInt64(0, "testinc")
        let v2 = (if v1.HasValue then v1.Value else 0L) + 1L
        !~!c.Strings.Set(0,"testinc",v2)
    
incTest(+conn)

!~!conn.Strings.Set(0,"testinc",0L)
let countPerThread = 10
let threadCount = 1
!~!conn.Strings.Set(0,"testinc",0L)

let incrementers () =
    Array.Parallel.init threadCount (fun _ ->
    let c = +conn
    for i = 1 to countPerThread do incTest(c))

//let res = !!!conn.Strings.GetInt64(0, "testinc")

incrementers ()

//res.Value
