Fredis
=======================
Fredis is a BookSleeve wrapper for convenient usage from F# (plus planned some extentions, 
additional usefull commands, patterns and a DSL implemented as custom query expression commands).

>Urban Dictionary defines Fredis as:

>A Fredis is a man/woman who likes to party hard. He/she is the life of the party even though they would not remember it.


Minimum Value Proposition
----------------------
In the current pre-alpha state the library could be already used as a slim wrapper over RedisConnection 
object from BookSleeve. The type Connection inherits from RedisConnection and adds a connection pool
accessible via Connection.Use() instance method or a prefix operator `+`.

BookSleeve recommends using the same connection accross an application because the connection is thread safe,
however in some cases you may need to create a new connection, e.g. for using blocking BLPOP/BRPOP
commands (for subscriptions BookSleeve has a connection instance method `GetOpenSubscriberChannel` that could
be accesses via `%` prefix operator).

Operators:

	/// Get an existing connection from a pool or a new connection to the same server with same parameters
	/// Use this method when a call to Redis could be blocking, e.g. when using distributed locks
	let (~+) (conn:Connection) = conn.Use
	/// GetOpenSubscriberChannel on connection
	let (~%) (conn:Connection) = conn.GetOpenSubscriberChannel()
	/// Async await plain Task and return Async<unit>, to be used with do! inside Async
	let (!~)  (t: IAsyncResult) = t |> (Async.AwaitIAsyncResult >> Async.Ignore)
	/// Async await typed Task<'T> and return Async<'T>, to be used with let! inside Async
	let inline (!!)  (t: Task<'T>) = t |> Async.AwaitTask
	/// Run plain Task/IAsyncResult on current thread
	let (!~!)  (t: IAsyncResult) = t |> (Async.AwaitIAsyncResult >> Async.Ignore >> Async.StartImmediate)
	/// Run task Task<'T> on current thread and return results
	let inline (!!!)  (t: Task<'T>) = t.Result // |> (Async.AwaitTask >> Async.RunSynchronously)
    

Install & Usage
----------------------

	PM> Install-Package Fredis

See BookSleeve docs for API. Some examples how to use Fredis operators from Tests.fsx

	let connection = new Connection("localhost")
	let anotherReusedConnection = +connection
	let subscriberChannel = %connection
	let r1 = !!!conn.Server.Ping()
	!~!conn.Strings.Set(2,"k1","1")
	let a' = 
    async{
        // Async.AwaitIAsyncResult >> Async.Ignore
        return! !~conn.Strings.Set(1,"k1","abc")
    } |> Async.StartImmediate
	// !~! shortcut for (Async.AwaitIAsyncResult >> Async.Ignore >> Async.StartImmediate)
	!~!conn.Strings.Set(1,"k1","abc") 
	let r2 = async { return! !!conn.Strings.Append(1,"k1","def") } |> Async.RunSynchronously
	let r3 = !!!conn.Strings.GetString(1,"k1")


Up for grabs!
----------------------
I started this library to add "correct distributed blocking lock" (not yet implemented) 
and to add convenience shortcuts for working with Task<'T> from F# Async. Additional useful 
features could be automatic serialization of generic types using protobuf-net and
a DSL that will allow to use native Redis commands with .NET types. 


I won't be able to spend all my time on this and am not sure that I could complete the features alone.
This project is an ideal candidate to be a shared community project. Please fork, contribute your
 edits and new ideas, add your name to the license and let's party hard on this project!

I hope to move this to github.com/fsprojects but cannot find the detailed instructons I believe I've seen some time ago.

**Extensions:**
- correct distributed blocking lock (honors the order of consumers trying to access a locked resource). ETA next release if the idea is feasible 
TODO anything else

**Build-in serialization:** 
Typed expressions with serialization hidden behind the scenes (some conventions with overridable params or IoC, by default protobuf-net?)

**DSL:**

expressions implemented as custom query expression commands, with native Redis commands on any serializable types

	let conn = .. // new connection
	let version = // should equal pong
		fredis conn {
			PING
		}
	
	let value:'T = new T()
	let key = "mykey"
		fredis conn {
			SET key value // automatic serialization
		}	

	let value2:'T = // atomatic deserialization to 'T
		fredis +conn {
			GET key
		}

	let channel = "mychannel"
	let value = 
		fredis %conn {
			SUBSCRIBE channel
		}

**Distributed transport and persistence for Actors:**

Redis is the ideal transport for distributed actors. E.g. in a MailBoxProcessor a Redis list + distributed blocking lock (replicating BlockingCollection behavior)
could be used for a distributed concurrent queue implementation. Atomic `(B)RPOPLPUSH` commands could be 
use for continuations.


License
----------------------

(c) Victor Baybekov 2014

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

This software is distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

BookSleeve is licensed separately; see https://code.google.com/p/booksleeve/.
Redis is licensed separately; see http://redis.io/.
