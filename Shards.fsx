(**
Shards are use for assets and for a 'tape' replacement for events (could think of events as 
assets as well and vice versa).

Shards are epochal. Epoch if a function of timestamp. Only objects that naturally have a timestamp
should be stored in shards, e.g.
    - page content is always at some point in time, is part of PageVisitedEvent
    - PageSeen event
    - user profile update

We could store incremental changes or snapshots or mix or both.

In addition, there is always a state that doesn't have timestamp - it is cached current state.

We could recover a state at any point in time by taking all events/snapshots before that time.

Every sharded event/asset belongs to some entity. Virtual shard depends on owner, phisical
shard depends on epoch -> timestamp.

Entities/identities are not sharded. Should use different 'main' DBs instead separated by functionality, e.g.
    - users
    - pages
    - hosts

PageVisitByUser is an event that belongs both to a page and a user. Storing both is possible.
Better decide what we need more - all pages visited by a user or all visitors to a page. With 
proper indexes we could select any of those in comparable time.


Sharding/functional division could only be useful for write scalability. Reading should mostly be
done from cache.


If we use AI int64 ID for identities,



**)