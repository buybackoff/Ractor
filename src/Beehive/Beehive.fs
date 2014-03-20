namespace Beehive

[<AutoOpenAttribute>]
module Beehive =
    let beehive = "beehive"

// This project is a placeholder to collect ideas on Redis-based distributed actors. If it will 
// go any further from this collection of thoughts, it will depend on Fredis, so keep 
// them in one solution until anything meaninful could be added here.

// Redis is a hive, actors are bees flying around and doing useful and coordinated work
// Data (honey) is stored safely in the hive and once it is there it cannot be lost

// Signle point of failure is bad!? But multiple points of failure are worse when losing honey
// (dropping queued messages) is not an option.

// For cordination using Redis each node has to subscribe to system message channel,
// for ZMQ and similar we have to elect a dealer and subscribe to it - same thing as Redis. 
// However IRL worker nodes are usually ephemeral and the assumption is that they could go offline
// quite badly as if power is going off and no finally block could help. It is much easier to
// build and maintain one reliable central node. We could invest in Redis, make several slaves, 
// never store new data there before it is persisted, or queue a copy of each new data message to 
// SQS or Kinesis and then persist it asyncronously.

// With Redis the whole design is so simple and the data structures functionality is so powerefull,
// that there must be a reason why to use anything else other than 'single point of failure is bad'.

// Redis cluster is going mainstream in 2H2014. AWS will eventually support the cluster 
// as well probably sometime in 2015. With their multi A-Z deployment I could hardly imagine what
//  is more reliable and at the same time practical.

// With AWS S3 for large blobs persistence, RDS/Dynamo for fast structural data persistence 
// and Redis for low latency advanced data structures storage of working set data this could scale 
// high in the sky linearly without any additional complexity. 
// Hadoop and similar won't be needed ever! M-brace being non-FOSS not needed as well! 
// (a kind of anti-community old-school-corporatish sentiment here http://www.slideshare.net/petrogiannakispantelis/mbrace-the-cloud-pitch-deck)


// The project should extend either FSharp.Actor or Pigeon. Pigeon already works with protobuf-net
// and targets Akka's API. Should make messages compatible with Pigeon + use its interfaces?


// Should name it Bee Yard, Beehive is a node

// Analogy with modern CPUs and NUMA

// Bee Yard (cluster of nodes) = CPU

// Node is a Beehive = CPU core with its local cache L1/L2 cache

// Actor = thread

// Redis is a shared L3 or RAM

// Dtatabase/S3 - hard drive 
