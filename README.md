Fredis
=======================
**Fredis** (F# + Redis) is a light distributed actors framework built on top of Redis. Its API is similar to 
F#'s [MailboxProcessor](http://msdn.microsoft.com/en-us/library/ee370357.aspx) and [Fsharp.Actor](https://github.com/colinbull/Fsharp.Actor) library. The main difference is that in Fredis actors exist 
is Redis per se as lists of messages, while a number of ephemeral workers (actors' "incarnations") take messages
from Redis, process them and post results back.

Benchmarks (e.g. [1](http://blog.jupo.org/2013/02/23/a-tale-of-two-queues/)) show that Redis is as performant 
as old popular message queues and even newer ones, like ZMQ. 
Existing distributed actor systems use many to many connections, a design that at the first glance 
removes a single point of failure. But a closer look reveals that such design introduces multiple points
of failure because data is stored in some random nodes and at each point in time some node acts as a central
one. If that node fails the system will have to elect another lead node, but the messages will be lost.

Fredis was build with AWS infrastructure in mind. In Amazon cloud, it is easy to create one central
Redis cluster in multiple availability zones with multiple replicas. This is the most reliable 
setup one could get, and it is available in minutes for cheap price. With this reliable central node
one could then use autoscale group and even add spot instances to the system. Random shutdowns of any 
worker nodes will not affect the system in any way. This setup gives an elastic, easy to maintain and 
(automatically) scalable to a large size system of distributed actors.

>Also Urban Dictionary [defines](http://www.urbandictionary.com/define.php?term=fredis) Fredis as: 
"A Fredis is a man/woman who likes to party hard. He/she is the life of the party even though they 
would not remember it." The goal of Fredis library is to become a life of a moderately-sized server
application where a developer knows in advance that a single box will choke but there is not enough
 resources to invest into "proper" infrastructure. Fredis will buy time and limit (for good!) design of the
 application to very simple data objects and persistence patterns. And while the application grows,
Fredis will grow as well and will become mature and robust for its stated tasks at a larger scale.

**Fredis.Persistence** is a collection of APIs for POCOs and blobs persistence and a strongly typed Redis
client based on excellent [Stackexchange.Redis](https://github.com/StackExchange/StackExchange.Redis) 
library. The typed Redis client has strong opinion about keys schema inside Redis and uses a concept of
root/owner objects to store dependent objects/collections. POCO/database persistor base implementation
wraps around ServiceStack.ORMLite.v3, however there is no binary dependency and any ORM could be plugged 
in. Blob persistor saves large data objects to files or S3 (TODO).



Install & Usage
----------------------

	PM> Install-Package Fredis
	PM> Install-Package Fredis.Persistence


Docs & test are work in progress...


License
----------------------

(c) Victor Baybekov 2014

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

This software is distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

StackExchange.Redis is licensed separately; see https://github.com/StackExchange/StackExchange.Redis/blob/master/LICENSE.
ServiceStackV3 is licensed separately; see https://github.com/ServiceStack/ServiceStack/blob/v3/LICENSE.
Redis is licensed separately; see http://redis.io/topics/license. 
