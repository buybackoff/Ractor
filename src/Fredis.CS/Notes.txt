Implementation agostic layer for DB/Caching/Redis/Blob/Files persistance

Only minimum functionality at start

Think how to store a FK to other storage types, e.g. S3 file as a lazy property
 - or fuck this auto-magic?


 All objects has long Ids as primary keys
 Sharded 


 POCOs
 - Persistence
	* IPocoPersistor persists IDataObject POCOs somewhere depending on atrributes (RDBMS is default, but could 
		also extend to DynamoDB or anything else)
	* IDistributedDataObject are persisted in DB shards (virtual or real) or in any other key-value
		storage. 
 - Caching
	* By default we cache all IDataObjects and hot IDistributedDataObjects (they will expire)
	* Permanent cache could only be stored in Redis (and we always have redis, it is in the name)
	* Ephemeral cache could be stored in Memcached as well, to save Redis for more important types
	* But keep in mind that Redis cluster is what will be the best tool already in 2014!
	* As of now just do with Redis the same trick as with DBs - virtual or real shards

	RedShift for Dbs = Redis Cluster for cluster of Redis 2.8 nodes ... 



Insert, Update, Delete, Single, Select - all afect cache always or if attribute is set (default??)
SetCache and ClearCache methods do not touch database

It should be impossible to store/change anything in DB that will differ from cache, while cache could 
have some ephemeral values



Epoch is a log2 number of shards:
	- on epoch 0 there is only one shard - the main database
	- on epoch 1 we add 1 shard
	- on epoch 2 add 2 more shards

The idea is that we could scale up and scale out. Using Amazon RDS, we could scale up by c. 2^8
	0 Micro instances	db.t1.micro	1	Variable	0.615
	1 Standard - First Generation	db.m1.small	1	1	1.7
	2 Standard - Second Generation	db.m3.medium	1	3	3.75
	3 Standard - Second Generation	db.m3.large	2	6.5	7.5
	4 Memory optimized	db.m2.xlarge	2	6.5	17.1 (or Standard - Second Generation	db.m3.xlarge	4	13	15)
	5 Memory optimized	db.m2.2xlarge	4	13	34.2
	6 Memory optimized	db.m2.4xlarge	8	26	68.4
	7 Memory optimized	db.cr1.8xlarge*	32	88	244

Scaling up is simpler than scaling out. Given that a lot of data is cached we could expect that
inserts will take the major part of load into DB (but reads of cold data is there still). The decision to scale up or out will be based
on costs vs required IOPS performance. Also scaling out usually requires half the power of existing
nodes since they will take only half of the load. It should be irrelevant to scale up or out
but we should keep load in the range of 40-80% so it is better to scale out first with less powerfull 
nodes then scale up. TODO - need some basic model with read/writes, costs, etc.



// Thoughts: caching should be done inside Fredis(F#), Fredis object must implement (have properties) IPocoPersistor + ICacheClient
// Cache must be aware of IDataObjects, CacheIndex should be independent from DB indexes
// Think how to leverage Redis lists/hashes/zlists for foreign keys relationships? E.g. FB's friends 
// CacheIndex: 1x1, like a DB index
// CacheSet<T,U>(column) - Id of a column to list of row Ids; typed - returns actual objects
// FriendShip {Id, First, Second, etc} - CacheSet on First and Second with column vice versa
// CacheHash - Id of a column to hash, need expression to select keys and values
// CacheZHash - same as above
// understand deeply the SE.Redis API before doing anything other than CacheIndex

// Better to forget about fancy cache/DB integration stuff
// add ElasticSearch into the equation


// Async: 
// * SS is not going to add async methods
// * MySQL connector doesn't have proper implementation
// * Redis client is done very well. Better to design to big system so that Redis hit ratio is high than to fight DB micro optimizations


// Cache: http://meta.stackexchange.com/questions/69164/does-stack-overflow-use-caching-and-if-so-how
// http://meta.stackexchange.com/questions/110320/stack-overflow-db-performance-and-redis-cache
// http://stackoverflow.com/questions/9596877/stackoverflow-redis-and-cache-invalidation





