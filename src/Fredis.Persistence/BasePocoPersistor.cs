using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using ServiceStack;
using ServiceStack.OrmLite;

namespace Fredis {
    /// <summary>
    /// Base implementation of IPocoPersistor using ServiceStack.ORMLite v3
    /// </summary>
    public class BasePocoPersistor : IPocoPersistor {
        private readonly IOrmLiteDialectProvider _provider;

        // public to reuse
        public OrmLiteConnectionFactory DbFactory { get; set; } // TODO? make public??

        private readonly Dictionary<ushort, string> _shards = new Dictionary<ushort, string>();

        /// <summary>
        /// A number from 0 to 15 (HEX digit) showing number of shards
        /// {0,1},
        /// {1,2},
        /// {2,3},
        /// {3,5},
        /// {4,8},
        /// {5,13},
        /// {6,21},
        /// {7,34},
        /// {8,55},
        /// {9,89},
        /// {10,144},
        /// {11,233},
        /// {12,377},
        /// {13,610},
        /// {14,987},
        /// {15,1597}
        /// </summary>
        public ushort Epoch { get; private set; }

        /// <summary>
        /// Base implementation of IPocoPersistor using ServiceStack.ORMLite v3
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="connectionString">Connection String for central DB (relational data with complex joins, limited growth, stable usage, settings, etc)</param>
        /// <param name="shardConnectionStrings">Connection string for shards, keys must start from zero and be consecutive, total number of shards must be a Fibbonacci number</param>
        public BasePocoPersistor(IOrmLiteDialectProvider provider, string connectionString, Dictionary<ushort, string> shardConnectionStrings) {
            _provider = provider;
            DbFactory = CreateDbFactory(connectionString);
            // check and register shards
            using (var db = DbFactory.OpenDbConnection()) {
                db.SqlScalar<int>("SELECT 1+1");
            }
            CheckShardsAndSetEpoch(shardConnectionStrings);
        }

        public BasePocoPersistor(IOrmLiteDialectProvider provider, string connectionString, string shardsConnectionStrings) 
        {
            // check and register shards
            var shards = shardsConnectionStrings
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .ToDictionary(line => {
                        var pair = line.Split(new[] { "=>" }, StringSplitOptions.RemoveEmptyEntries);
                        return new KeyValuePair<ushort, string>(ushort.Parse(pair[0].Trim()), pair[1].Trim());
                    });
            _provider = provider;
            DbFactory = CreateDbFactory(connectionString);
            // check and register shards
            using (var db = DbFactory.OpenDbConnection()) {
                db.SqlScalar<int>("SELECT 1+1");
            }
            CheckShardsAndSetEpoch(shards);
        }

        private OrmLiteConnectionFactory CreateDbFactory(string connectionString) {
            // TODO unit test
            return new OrmLiteConnectionFactory(connectionString, _provider);
        }

        private void CheckShardsAndSetEpoch(Dictionary<ushort, string> shardConnectionStrings) {
            var sortedShards = shardConnectionStrings.OrderBy(kvp => kvp.Key).ToList();
            var numberOfShards = sortedShards.Count;
            // power of two
            // TODO unit test
            if ((numberOfShards & (numberOfShards - 1)) != 0) throw new ApplicationException("Number of shards must be a power of 2");

            var i = 0;
            foreach (var keyValuePair in sortedShards) {
                if (i != keyValuePair.Key) {
                    // TODO unit test
                    throw new ApplicationException("Wrong numbering of shards");
                }
                i++;
            }

            // epoch is determined from number of connection strings for shards
            // TODO unit test
            var epochKvp = GuidGenerator.EpochToShards.FirstOrDefault(x => x.Value == numberOfShards);

            if (epochKvp.Equals(default(KeyValuePair<ushort, ushort>))) {
                throw new ApplicationException("Wrong number of shards");
            }

            Epoch = epochKvp.Key;
            // 

            foreach (var keyValuePair in sortedShards) {
                var key = keyValuePair.Key.ToString(CultureInfo.InvariantCulture);
                var factory = CreateDbFactory(keyValuePair.Value);
                // test factory
                using (var sdb = factory.OpenDbConnection()) {
                    var two = sdb.SqlScalar<int>("SELECT 1+1");
                    Trace.Assert(two == 2, "Shard " + key + " fails!");
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

        public void CreateTable<T>(bool overwrite = false) where T : IDataObject, new() {
            bool isDistributed = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

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

            items.ForEach(x => {
                // do not check existing value, always overwrite
                x.UpdatedAt = DateTime.UtcNow;
            });

            var isDistributed = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

            if (isDistributed) {
                var distributedItems = items.Cast<IDistributedDataObject>().ToList();

                // generate guids for items

                foreach (var distributedItem in distributedItems
                    .Where(distributedItem => distributedItem.Guid == default(Guid))) {
                    distributedItem.Guid = distributedItem.GetRootGuid() == default(Guid)
                        ? GuidGenerator.NewGuid(Epoch)
                        : GuidGenerator.NewGuid(distributedItem.GetRootGuid());
                }

                var baskets = distributedItems.ToLookup(i => i.Guid.Shard()).ToList();
                var hasErrors = false;

                var result = baskets
                    .AsParallel()
                    .WithDegreeOfParallelism(baskets.Count())
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(lu => {
                        var basket = lu.Cast<T>().ToList();
                        using (var db = DbFactory.OpenDbConnection(lu.Key.ToString(CultureInfo.InvariantCulture)))
                        using (var trans = db.OpenTransaction()) {
                            try {
                                db.InsertAll(basket);
                                trans.Commit(); // 
                            } catch {
                                Trace.TraceError("Insert error on shard " + lu.Key);
                                trans.Rollback();
                                hasErrors = true;
                            }
                        }
                        return basket;
                    }).SelectMany(x => x).ToList(); // ToArray to get the actual result

                if (hasErrors) throw new DataException("Could not insert data"); // TODO show error message

                // ReSharper disable once RedundantAssignment
                items = result;

            } else {
                using (var db = DbFactory.OpenDbConnection())
                using (var trans = db.OpenTransaction()) {
                    try {
                        for (var i = 0; i < length; i++) {
                            db.Insert(items[i]);
                            var id = db.LastInsertId();
                            items[i].Id = id;
                        }
                        trans.Commit();
                    } catch {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }




        public void Update<T>(T item) where T : IDataObject, new() {
            Update(item.ItemAsList());
        }

        public void Update<T>(List<T> items) where T : IDataObject, new() {
            if (items == null) throw new ArgumentNullException("items");
            var length = items.Count;
            if (length == 0) return;

            items.ForEach(x => {
                // do not check existing value, always overwrite
                x.UpdatedAt = DateTime.UtcNow;
            });

            var isDistributed = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

            if (isDistributed) {
                var distributedItems = items.Cast<IDistributedDataObject>().ToList();
                // check that Guids are here, otherwise cannot update
                if (distributedItems
                    .Any(distributedItem => distributedItem.Guid == default(Guid))) {
                    throw new ApplicationException("Cannot update an object without Guid");
                }

                var baskets = distributedItems.ToLookup(i => i.Guid.Shard()).ToList();
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
                                    var guid = ddo.Guid;
                                    db.Update((T)ddo, x => ((IDistributedDataObject)x).Guid == guid); // TODO unit test that casting works here
                                }
                                trans.Commit();
                            } catch {
                                Trace.TraceError("Update error on shard " + lu.Key);
                                trans.Rollback();
                                hasErrors = true;
                            }
                        }
                        return lu.Cast<T>().ToList();
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



        public void Delete<T>(T item) where T : IDataObject, new() {
            Delete(item.ItemAsList());
        }
        public void Delete<T>(List<T> items) where T : IDataObject, new() {
            if (items == null) throw new ArgumentNullException("items");
            var length = items.Count;
            if (length == 0) return;

            items.ForEach(x => {
                // do not check existing value, always overwrite
                x.UpdatedAt = DateTime.UtcNow;
            });

            var isDistributed = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

            if (isDistributed) {
                var distributedItems = items.Cast<IDistributedDataObject>().ToList();

                var baskets = distributedItems.ToLookup(i => i.Guid.Shard()).ToList();
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
                                    var guid = ddo.Guid;
                                    db.Delete<T>(x => ((IDistributedDataObject)x).Guid == guid); // TODO unit test that casting works here
                                }
                                //db.DeleteAll(basket);
                                trans.Commit(); // 
                            } catch {
                                Trace.TraceError("Delete error on shard " + lu.Key);
                                trans.Rollback();
                                hasErrors = true;
                            }
                        }
                        return lu.Cast<T>().ToList();
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



        private List<T> SelectOld<T>(Expression<Func<T, bool>> predicate = null) where T : IDataObject, new() {

            var isDistributed = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

            if (isDistributed) {
                var result = _shards
                    .AsParallel()
                    .WithDegreeOfParallelism(_shards.Count)
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(dbName => {
                        using (var db = DbFactory.OpenDbConnection(dbName.Key.ToString(CultureInfo.InvariantCulture))) {
                            return predicate == null ? db.Select<T>() : db.Select(predicate);
                        }
                    }).ToList();

                var flatResult = result.SelectMany(x => x).ToList();
                return flatResult;
            }

            using (var db = DbFactory.OpenDbConnection()) {
                return predicate == null ? db.Select<T>() : db.Select(predicate);
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
        public TR QueryOperation<T, TR>(Func<IDbConnection, TR> operation, Func<List<TR>, TR> aggregation = null, List<ushort> shards = null ) where T : IDataObject, new() {

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


        public void ExecuteSql(string sql, bool onShards = false) {
            if (onShards) {
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

        public T GetById<T>(Guid guid) where T : IDistributedDataObject, new() {
            return GetByIds<T>(guid.ItemAsList()).Single();
        }


        public List<T> GetByIds<T>(List<Guid> guids) where T : IDistributedDataObject, new() {
            var baskets = guids.ToLookup(i => i.Shard()).ToList();
            var result = baskets
                .AsParallel()
                .WithDegreeOfParallelism(baskets.Count())
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .Select(lu => {
                    var shardedGuids = lu.ToList();
                    using (var db = DbFactory.OpenDbConnection(lu.Key.ToString(CultureInfo.InvariantCulture))) {
                        return shardedGuids.Count > 1
                            ? db.Select<T>(q => Sql.In(q.Guid, shardedGuids))
                            : db.SingleWhere<T>("Guid", shardedGuids.Single()).ItemAsList();
                        // "LIMIT 1" increases performance 15x in case when Guid index is not unique - same optimisation "stop when found first" as with uniue index

                    }
                }).SelectMany(x => x).ToList();

            return result;
        }


        public List<T> GetByIds<T>(List<long> ids) where T : IDataObject, new() {
            var isDistributed = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));
            if (isDistributed) throw new ApplicationException("Distributed objects should use Guid");
            using (var db = DbFactory.OpenDbConnection()) {
                return db.SelectByIds<T>(ids);
            }
        }


        public T GetById<T>(long id) where T : IDataObject, new() {
            // TODO use single
            return GetByIds<T>(id.ItemAsList()).SingleOrDefault(); //
        }




    }
}
