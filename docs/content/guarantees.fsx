(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"


(**
Reliability guarantees
===================
An actor computation:

Post(message) : unit - the computation will run eventually when there is at least on started actor,
result will be discarded

Post(message, resultId) - same as Post(message) but the result will could be claimed once by resultId
via GetResult(resultId) method

PostAndReply(message) - caller will get results if it is alive

A continuation is an actor itself, so guarantees above apply to it as well


Latency guarantees
===================

TODO calculate methods latencies in term of big R (redis call with payload), 
small R (redis call with only keys) and computation time

When actor is started, it starts processing locally without sending a message to inbox,
but we must be sure that a message is not lost during processing due to some error
during computation or power outage. So we must save message to a processing pipeline
in Redis with ACK to meet reliability guarantees.

Post(message) will return only after a message is saved to pipiline: time = R when 



Throughput guarantees
===================

Redis is the limit!

Each actor could live on its own Redis server via different instances of Fredis object.

TODO passing data or references and side effects are different concers. Bad description below, rework!

Thoughts dump:

* Actors with side effects - should not pass data objects as Fredis messages but pass references
to data objects and then manipulate data from within actors. This will offload work from
Fredis instance to other cache/persistence servers accessed from actor computations.

If actor dies the same computation with the same data pointer will restart and that could corrupt
data or cause errors. (e.g. increase counter twice or seond insert into a database table with existing
primary key). 

Some side effects are unavoidable, e.g. persistence to DB, therefore such operations must
be designed with an assumption that they could be run twice if computation fails.

Each side effect should be the only operation in a computation of a special actor. Then the actor
could fail only when the operation fails, which means there were no side effect.


* Actors without side effects - should pass data as messages for reliability



Preferred usage is to use actors without side effects for processing, to pass data pointers 
rather than data if the data size is "large" and to put operations with side effects as a single 
operation in a separate actor.



*)




(**
 

*)
