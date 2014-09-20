Entity Framework POCO persistor
================================

* EF6 supports multiple contexts per DB, so we could use context per POCO and 
have the same behavior as with SS.ORMLite - this should work only for distributed objects

* Relational data objects should be in a single context??? But how to create a context dinamically


* The sweetest part of EF6 is migrations. We could (? - this is the question) 
use migrations to update individual tables.

* Models change outside Ractor. Ractor should work with DBContexts -> then IDataObject is not 
directly used

* Could use Dapper for CRUD


* Distributed context - for sharded objects



This is what i need!
http://romiller.com/2012/03/26/dynamically-building-a-model-with-code-first/
http://romiller.com/2012/02/09/running-scripting-migrations-from-code/
http://romiller.com/tag/code-first/