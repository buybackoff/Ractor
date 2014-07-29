using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using ServiceStack;
using ServiceStack.OrmLite;

namespace Ractor {
    /// <summary>
    /// Base implementation of IPocoPersistor using ServiceStack.ORMLite v4
    /// </summary>
    public class BasePocoPersistor : IPocoPersistor {
        private readonly SequentialGuidType _guidType = SequentialGuidType.SequentialAsString;
        private readonly IOrmLiteDialectProvider _provider;

        /// <summary>
        /// 
        /// </summary>
        internal OrmLiteConnectionFactory DbFactory { get; set; }

        private readonly Dictionary<ushort, string> _shards = new Dictionary<ushort, string>();
        private readonly HashSet<byte> _readOnlyShards = new HashSet<byte>();
        private List<byte> _writableShards;

        private byte NumberOfShards { get; set; }

        /// <summary>
        /// Base implementation of IPocoPersistor using ServiceStack.ORMLite v4
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="mainConnectionString">Connection String for central DB (relational data with complex joins, limited growth, stable usage, settings, etc)</param>
        /// <param name="shardConnectionStringsStartFrom1">Connection string for shards, keys MUST START FROM ONE and be consecutive</param>
        /// <param name="readOnlyShards">No new Guids will be generated for these shards. Updates and rooted inserts will throw.</param>
        /// <param name="guidType"></param>
        public BasePocoPersistor(IOrmLiteDialectProvider provider,
            string mainConnectionString,
            Dictionary<byte, string> shardConnectionStringsStartFrom1,
            byte[] readOnlyShards = null,
            SequentialGuidType guidType = SequentialGuidType.SequentialAsString) {
            _provider = provider;
            _guidType = guidType;
            if (readOnlyShards != null) {
                foreach (var readOnlyShard in readOnlyShards) { _readOnlyShards.Add(readOnlyShard); }
            }
            if (_readOnlyShards.Count >= shardConnectionStringsStartFrom1.Count)
                throw new ArgumentException("Too few writable shards!");


            DbFactory = CreateDbFactory(mainConnectionString);
            // check and register shards
            using (var db = DbFactory.OpenDbConnection()) {
                db.SqlScalar<int>("SELECT 1+1"); // check DB engine is working
            }
            CheckShardsAndSetEpoch(shardConnectionStringsStartFrom1);
        }

        private OrmLiteConnectionFactory CreateDbFactory(string connectionString) {
            // TODO unit test
            return new OrmLiteConnectionFactory(connectionString, _provider);
        }

        private void CheckShardsAndSetEpoch(Dictionary<byte, string> shardConnectionStrings) {
            var sortedShards = shardConnectionStrings.OrderBy(kvp => kvp.Key).ToList();
            var numberOfShards = sortedShards.Count;
            if (numberOfShards > 254) throw new ArgumentException("Too many shards!");
            NumberOfShards = (byte)numberOfShards;
            _writableShards = shardConnectionStrings.Keys.Except(_readOnlyShards).ToList();
            if (_writableShards.Count == 0) throw new ApplicationException("No writable shards");
            var i = 1;
            foreach (var keyValuePair in sortedShards) {
                if (i != keyValuePair.Key) {
                    // TODO unit test
                    throw new ApplicationException("Wrong numbering of shards");
                }
                i++;
            }

            foreach (var keyValuePair in sortedShards) {
                var key = keyValuePair.Key.ToString(CultureInfo.InvariantCulture);
                var factory = CreateDbFactory(keyValuePair.Value);
                // test factory
                using (var sdb = factory.OpenDbConnection()) {
                    var two = sdb.SqlScalar<int>("SELECT 1+1");
                    // TODO unit test
                    if (two != 2) throw new ApplicationException("Shard " + key + " doesn't work");
                }
                _shards.Add(keyValuePair.Key, keyValuePair.Value);
                DbFactory.RegisterConnection(key, factory);
            }
        }

        private static void CreateTableOnConnection<T>(bool overwrite, IDbConnection db) where T : IDataObject, new() {
            var createTableAttribute = typeof(T).FirstAttribute<CreateTableAttribute>();
            var createScript = createTableAttribute != null ? createTableAttribute.Sql : null;
            if (!string.IsNullOrWhiteSpace(createScript)) {
                if (overwrite) db.DropTable<T>();
                db.ExecuteSql(createScript);
            } else { db.CreateTable<T>(overwrite); }
        }


        /// <summary>
        /// 
        /// </summary>
        public void CreateTable<T>(bool overwrite = false) where T : IDataObject, new() {
            bool isDistributed = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

            if(_readOnlyShards.Count > 0) throw new ReadOnlyException("Cannot create table with read-only shards!");

            if (isDistributed) {
                foreach (var storedShard in _shards) {
                    using (var db = DbFactory.OpenDbConnection(storedShard.Key.ToString(CultureInfo.InvariantCulture))) {
                        CreateTableOnConnection<T>(overwrite, db);
                    }
                }
            } else {
                using (var db = DbFactory.OpenDbConnection()) {
                    CreateTableOnConnection<T>(overwrite, db);
                }
            }

        }


        public void Insert<T>(T item) where T : IDataObject, new() {
            var list = item.ItemAsList();
            Insert(list);
        }


        public void Insert<T>(List<T> items) where T : IDataObject, new() {
            if (items == null) throw new ArgumentNullException("items");
            var length = items.Count;
            if (length == 0) return;

            items.ForEach(x => CheckOrGenerateGuid(ref x, true));


            var isDistributed = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

            if (isDistributed) {

                var baskets = items.ToLookup(i => i.Id.Bucket()).ToList();
                Exception internalError = null;

                baskets
                    .AsParallel()
                    .WithDegreeOfParallelism(baskets.Count())
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .ForAll(lu => {
                        var basket = lu.ToList();
                        using (var db = DbFactory.OpenDbConnection(lu.Key.ToString(CultureInfo.InvariantCulture))) {
                            try {
                                db.InsertAll(basket);
                            } catch (Exception e) {
                                internalError = e;
                            }
                        }
                    });

                if (internalError != null) throw internalError;

            } else {
                using (var db = DbFactory.OpenDbConnection()) {
                    db.InsertAll(items);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Obsolete("Try to avoid data mutation")]
        public void Update<T>(T item) where T : IDataObject, new() {
            Update(item.ItemAsList());
        }


        /// <summary>
        /// 
        /// </summary>
        [Obsolete("Try to avoid data mutation")]
        public void Update<T>(List<T> items) where T : IDataObject, new() {
            if (items == null) throw new ArgumentNullException("items");
            var length = items.Count;
            if (length == 0) return;
            items.ForEach(x => CheckOrGenerateGuid(ref x, true));

            var isDistributed = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

            if (isDistributed) {
                // check that Guids are here, otherwise cannot update
                if (items
                    .Any(distributedItem => distributedItem.Id == default(Guid))) {
                    throw new ApplicationException("Cannot update an object without Guid");
                }

                var baskets = items.ToLookup(i => i.Id.Bucket()).ToList();
                var hasErrors = false;

                var result = baskets
                    .AsParallel()
                    .WithDegreeOfParallelism(baskets.Count())
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(lu => {
                        using (var db = DbFactory.OpenDbConnection(lu.Key.ToString(CultureInfo.InvariantCulture)))
                        using (var trans = db.OpenTransaction()) {
                            try {
                                foreach (var ddo in lu) {
                                    var guid = ddo.Id;
                                    db.Update(ddo, x => ((IDistributedDataObject)x).Id == guid); // TODO unit test that casting works here
                                }
                                trans.Commit();
                            } catch {
                                Trace.TraceError("Update error on shard " + lu.Key);
                                trans.Rollback();
                                hasErrors = true;
                            }
                        }
                        return lu.ToList();
                    }).SelectMany(x => x).ToList();

                if (hasErrors) throw new DataException("Could not update data");

                // ReSharper disable once RedundantAssignment
                items = result;
            } else {
                using (var db = DbFactory.OpenDbConnection())
                using (var trans = db.OpenTransaction()) {
                    try {
                        db.UpdateAll(items);
                        trans.Commit();
                    } catch {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        [Obsolete("Try to avoid data mutation")]
        public void Delete<T>(T item) where T : IDataObject, new() {
            Delete(item.ItemAsList());
        }

        /// <summary>
        /// 
        /// </summary>
        [Obsolete("Try to avoid data mutation")]
        public void Delete<T>(List<T> items) where T : IDataObject, new() {
            if (items == null) throw new ArgumentNullException("items");
            var length = items.Count;
            if (length == 0) return;
            items.ForEach(x => CheckOrGenerateGuid(ref x, true));

            var isDistributed = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

            if (isDistributed) {
                var baskets = items.ToLookup(i => i.Id.Bucket()).ToList();
                var hasErrors = false;

                var result = baskets
                    .AsParallel()
                    .WithDegreeOfParallelism(baskets.Count())
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(lu => {
                        using (var db = DbFactory.OpenDbConnection(lu.Key.ToString(CultureInfo.InvariantCulture)))
                        using (var trans = db.OpenTransaction()) {
                            try {
                                foreach (var ddo in lu) {
                                    var guid = ddo.Id;
                                    db.Delete<T>(x => ((IDistributedDataObject)x).Id == guid); // TODO unit test that casting works here
                                }
                                //db.DeleteAll(basket);
                                trans.Commit(); // 
                            } catch {
                                Trace.TraceError("Delete error on shard " + lu.Key);
                                trans.Rollback();
                                hasErrors = true;
                            }
                        }
                        return lu.ToList();
                    }).SelectMany(x => x).ToList();

                if (hasErrors) throw new DataException("Could not delete data");
                // ReSharper disable once RedundantAssignment
                items = result;
            } else {
                using (var db = DbFactory.OpenDbConnection())
                using (var trans = db.OpenTransaction()) {
                    try {
                        db.Delete(items);
                        trans.Commit();
                    } catch {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<T> Select<T>(Expression<Func<T, bool>> predicate = null) where T : IDataObject, new() {

            return QueryOperation<T, List<T>>(db => predicate == null ? db.Select<T>() : db.Select(predicate), result => result.SelectMany(x => x).ToList());
        }


        public long Count<T>() where T : IDataObject, new() {
            return QueryOperation<T, long>(db => db.Count<T>(),
                result => result.Aggregate(0L, (acc, i) => acc + i));
        }

        // TODO add aggregation Func
        // TODO this is SS specific, should be private but convenient to have it for now
        private TR QueryOperation<T, TR>(Func<IDbConnection, TR> operation, Func<List<TR>, TR> aggregation = null, List<ushort> shards = null) where T : IDataObject, new() {

            var isDistributed = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

            if (isDistributed) {
                var luShards = shards == null
                ? _shards
                : shards.ToDictionary(luShard => luShard, luShard => _shards[luShard]);

                var result = luShards
                    .AsParallel()
                    .WithDegreeOfParallelism(Math.Min(_shards.Count, 64))
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(dbName => {
                        using (var db = DbFactory.OpenDbConnection(dbName.Key.ToString(CultureInfo.InvariantCulture))) {
                            return operation(db);
                        }
                    }).ToList();
                if (aggregation == null) throw new ApplicationException("No aggregation function for distributed data objects");
                var flatResult = aggregation(result);
                return flatResult;
            }

            // TODO this doesn't feel right
            if (aggregation != null) throw new ApplicationException("Aggregation function is not expected");
            if (shards != null) throw new ApplicationException("Shards parameter is not expected");


            using (var db = DbFactory.OpenDbConnection()) {
                return operation(db);
            }
        }


        public List<T> Select<T>(string sqlFilter, params object[] filterParams) where T : IDataObject, new() {
            var isDistributed = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

            if (isDistributed) {
                var result = _shards
                    .AsParallel()
                    .WithDegreeOfParallelism(_shards.Count)
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(dbName => {
                        using (var db = DbFactory.OpenDbConnection(dbName.Key.ToString(CultureInfo.InvariantCulture))) {
                            return db.Select<T>(sqlFilter, filterParams);
                        }
                    }).ToList();

                var flatResult = result.SelectMany(x => x).ToList();
                return flatResult;
            }

            using (var db = DbFactory.OpenDbConnection()) {
                return db.Select<T>(sqlFilter, filterParams);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public void ExecuteSql(string sql, bool distributed = false) {
            if (_readOnlyShards.Count > 0) throw new ReadOnlyException("Cannot execute SQL with read-only shards!");

            if (distributed) {
                _shards
                    .AsParallel()
                    .WithDegreeOfParallelism(_shards.Count)
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Each(dbName => {
                        using (var db = DbFactory.OpenDbConnection(dbName.Key.ToString(CultureInfo.InvariantCulture))) {
                            db.ExecuteSql(sql);
                        }
                    });
                return;
            }

            using (var db = DbFactory.OpenDbConnection()) {
                db.ExecuteSql(sql);
            }

        }

        internal void CheckOrGenerateGuid<T>(ref T item, bool onlyWritable, DateTime? utcDateTime = null) where T : IDataObject {
            if (item.Id != default(Guid)) {
                var bucket = item.Id.Bucket();
                if (bucket > NumberOfShards) throw new ApplicationException("Wrong Guid bucket");
                if (onlyWritable && _readOnlyShards.Contains(bucket)) throw new ReadOnlyException("Could not write to shard: " + bucket);
                return;
            }
            var distributed = item as IDistributedDataObject;
            if (distributed != null) {
                if (distributed.GetRootGuid() == default(Guid)
                    || distributed.GetRootGuid().Bucket() == 0) {
                    // this will generate guids only for writable buckets
                    var randomWritableBucket = _writableShards[(byte)(new Random()).Next(0, _writableShards.Count)]; // no '-1', maxValue is exlusive
                    item.Id = GuidGenerator.NewBucketGuid(randomWritableBucket, _guidType, utcDateTime);
                } else {
                    var bucket = distributed.GetRootGuid().Bucket();
                    if (onlyWritable && _readOnlyShards.Contains(bucket)) throw new ReadOnlyException("Could not write to shard: " + bucket);
                    item.Id = GuidGenerator.NewBucketGuid(bucket,
                        _guidType, utcDateTime);
                }
            } else {
                if (onlyWritable && _readOnlyShards.Contains(0)) throw new ReadOnlyException("Could not write to shard: " + 0);
                item.Id = GuidGenerator.NewBucketGuid(0, _guidType, utcDateTime);
            }
        }

        /// <summary>
        /// Generate new Guid for an item if Guid was not set, or return existing.
        /// </summary>
        public void GenerateGuid<T>(ref T item, DateTime? utcDateTime = null) where T : IDataObject {
            CheckOrGenerateGuid(ref item, false, utcDateTime);
        }



        public List<T> GetByIds<T>(List<Guid> guids) where T : IDataObject, new() {
            var isDistributed = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

            if (isDistributed) {
                var baskets = guids.ToLookup(i => i.Bucket()).ToList();
                var result = baskets
                    .AsParallel()
                    .WithDegreeOfParallelism(baskets.Count())
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(lu => {
                        var shardedGuids = lu.ToList();
                        using (var db = DbFactory.OpenDbConnection(lu.Key.ToString(CultureInfo.InvariantCulture))) {
                            return db.SelectByIds<T>(shardedGuids);
                        }
                    }).SelectMany(x => x).ToList();

                return result;
            }
            using (var db = DbFactory.OpenDbConnection()) {
                return db.SelectByIds<T>(guids);
            }
        }

        public T GetById<T>(Guid guid) where T : IDataObject, new() {
            return GetByIds<T>(guid.ItemAsList()).Single();
        }



    }
}
