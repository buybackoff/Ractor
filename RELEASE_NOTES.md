#### 0.2.1 - August 5 2014
* Remove record types and FsPickler dependency


#### 0.2.0 - July 30 2014
* Concrete shards instead of virtual shards.
* Sequential GUID generator with timestamp.
* Use sequential GUIDs for primary everywhere. With timestamp stored inside GUID
there is effectively zero overhead. We always need 8 bytes timestamp and an incrementing ID
for clustered index, with sequential GUIDs we get both + distributed generation. (use NTP
to sync time on machines)
* ServiceStack.ORMLite.MySQL version with GUID stored as binary(16) 
(/lib with 4.0.23 version and [source here](https://github.com/buybackoff/ServiceStack.OrmLite)).


#### 0.1.1 - July 11 2014
* Rename to Ractor and change direction: the goal is to make CLR/JVM interoperable actors. An 
actor could be defined in any language on any platform and called from another platform. The only
requirement is that message types are defined on both platforms and are serializable to the same 
Json representation.


#### 0.1.0 - June 24 2014
* Stabilized public API - no more methods expected unless I cannot live without them
* Added Ractor.Persistence.AWS project with S3 IBlobPersistor and SQS IQueue implementations
* NOT TESTED, MANY REDIS METHODS ARE MISSING

#### 0.0.12 - June 19 2014
* ServiceStack.ORMLite v4 for POCOs

#### 0.0.10 - June 19 2014
* June 19 2014 update

#### 0.0.9 - May 5 2014
* C# methods and added FSharp.Core.dll to NuGet package

#### 0.0.8 - May 5 2014
* Main methods Post and PostAndReply tested for basic corectness (still could be unobvious issues)

#### 0.0.7 - May 4 2014
* Ractor first draft with actros

#### 0.0.4 - April 28 2014
* Add perstistence stuff (DB, files, queues) for which redis is cache

#### 0.0.3 - February 1 2014
* Keep things very simple and remove unneeded pool
