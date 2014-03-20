namespace Fredis

open System
open System.Diagnostics
open System.Threading.Tasks
open System.Runtime.CompilerServices

open Fredis

[<AutoOpenAttribute>]
module Monitor = 
    
    // I still do not understand why many implementations use timeout and in principle
    // allow to enter B if A hasn't finished its job and timed out. It is like preparing 
    // to shoot oneself in the foot instead of protecting from it.

    // System.Threading.Monitor doesn't have timeouts and shouldn't.

    // 
    
    [<AbstractClassAttribute; SealedAttribute>]
    type private RedisMonitor =
        // Key, timeout in milliseconds (should be reliably above )
        static member Enter(conn:Connection,db:int,key:string,?timeout:int) = 
            let rec tryEnter () = async {
                use lockingConn = conn // reuse some connection from the pool
                let lockerKey = key + ":fredis_lock_relay" // weird enough to rule out collisions, but f(key)
                let lockerExpiry = if timeout.IsSome && timeout.Value > 0 then timeout.Value else 5 // TODO set default
                let lua = @"
    local relay = KEYS[1] -- TODO remove comments out of scripts when all tested!
    local started_flag = relay..'_flag'
    local timeout = ARGV[1]
    if (0 == redis.call('exists', started_flag)) and (0 == redis.call('exists', relay)) then
        redis.call('SET', started_flag, '1')
        redis.call('RPUSH', relay, 'baton')
        redis.call('PEXPIRE', started_flag, timeout)
        redis.call('PEXPIRE', relay, timeout+1) -- avoid relay expire before flag due to the 1 ms error when reseting TTL on exit
    end
    return redis.call('PTTL', started_flag)
                    "
                let pttl = (!!!lockingConn.Scripting.Eval(db, lua, [|lockerKey|],[|lockerExpiry * 1000|])) :?> Int64
                // here someone could steal the lock and we could steal from someone as well - it is fair
                let! res = !!lockingConn.Lists.BlockingRemoveFirstString(db,[|lockerKey|], int(pttl/1000L)+1) // +1 to avoid infinite block 
                // max delay 999ms after expiration, but expirations here are not expected at all
                // lock could expire e.g. on power outage on a machine that acquired it
                if box res = null then 
                    //Console.WriteLine("expired - should never happen in production. TODO consider throwing here")
                    return! tryEnter () 
                else
                    //Console.WriteLine("acquired lock") 
                    return ()
            }
            tryEnter ()

        static member Exit(conn:Connection,db:int,key:string,?timeout:int) = 
            let lockerKey = key + ":fredis_lock_relay" 
            let lockerExpiry = if timeout.IsSome && timeout.Value > 0 then timeout.Value else 5 // TODO set default
            let lua = @"
    local relay = KEYS[1] -- TODO remove comments out of scripts when all tested!
    local started_flag = relay..'_flag'
    local timeout = ARGV[1]
    local flag_ok = (1 == redis.call('PEXPIRE', started_flag, timeout))
    -- return flag_ok
    local relay_ok = (1 == redis.call('PEXPIRE', relay, timeout+1))
    if flag_ok and relay_ok then
        -- relay exists, but this should never happen, we are exiting after someone exited and noone entered
        redis.call('LPOP', relay) -- remove baton to close entry, return 0 to take an action
        return -1
    end
    if flag_ok then
        redis.call('INCR', started_flag) -- next round
        redis.call('RPUSH', relay, 'baton') -- baton exchange
        redis.call('PEXPIRE', started_flag, timeout)
        redis.call('PEXPIRE', relay, timeout+1) -- avoid relay expire before flag due to the 1 ms error
        return 1
    else
        return 0 -- should not expire in normal conditions
    end
                    "
            let res = !!!conn.Scripting.Eval(db, lua, [|lockerKey|],[|lockerExpiry * 1000|]) :?> Int64
            if res = 0L then failwith "wrong redis lock exit condition"
            else
                //Console.WriteLine("exited lock OK") 
                ()

    let makeLock (conn:Connection) (db:int) (key:string) (timeout:int) = //: Async<IDisposable> =
        async{
            do! RedisMonitor.Enter(conn, db, key, timeout) 
            return { new System.IDisposable with  
                member x.Dispose() =
                    try
                        RedisMonitor.Exit(conn,db,key,timeout) 
                    with
                    | _ -> ()
            }
        }

    [<Extension>]
    type ConnectionExtension () =
        
        // Task<IDisposable> like other BookSleeve's methods

        [<Extension>]
        static member MakeLock(this:Connection,db:int, key:string, ?timeoutSeconds:int) : Task<IDisposable> =
            let lockerExpiry = if timeoutSeconds.IsSome && timeoutSeconds.Value > 0 then timeoutSeconds.Value else 60 // TODO choose default
            makeLock this db key lockerExpiry 
            |> Async.StartAsTask