Ractor
=======================

There are several really useful things here:
* DynamicContext for EF6 that starts per-table automatic migrations, which is very convenient while
prototyping RDMS structure and adding/changing schema. By default, only non-destructive updates are 
allowed, but this is a config setting.
* SE.Redis wrapper with automatic serialization of generic values, with JSON.NET by default.
* Redis-based distributed MPMC RedisQueue and RedisAsyncDictionary, which together allow to build any 
complex Actor topology manually without a separate Actor abstraction.
* Distributed actors with reliability guarantees, concurrency limits and priority scheduling.


Install & Usage
----------------------

	PM> Install-Package Ractor
	PM> Install-Package Ractor.Persistence
	PM> Install-Package Ractor.Persistence.AWS


Docs & test are work in progress...


License
----------------------

(c) Victor Baybekov 2016

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

This software is distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.