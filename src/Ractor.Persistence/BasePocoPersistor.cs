using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace Ractor {
    /// <summary>
    /// Base implementation of IPocoPersistor using Entity Framework 6
    /// </summary>
    public class BasePocoPersistor : IPocoPersistor {
        private readonly string _connectionName;
        private readonly SequentialGuidType _guidType;
        private readonly Dictionary<byte, string> _shards = new Dictionary<byte, string>();
        private readonly HashSet<byte> _readOnlyShards = new HashSet<byte>();
        private List<byte> _writableShards;
        private byte NumberOfShards { get; set; }

        /// <summary>
        /// Base implementation of IPocoPersistor using Entity Framework 6
        /// </summary>
        /// <param name="connectionName">Connection name in .config files (default is 'Ractor', it
        /// is also used as a prefix for zero-based distributed connections, e.g. Ractor.0 )</param>
        /// <param name="readOnlyShards"></param>
        /// <param name="guidType">Use AtEnd only for MS SQL Server, for MySQL and others DBMS without 
        /// native GUID types use binary</param>
        /// <param name="migrationDataLossAllowed"></param>
        public BasePocoPersistor(string connectionName = "Ractor",
            IEnumerable<byte> readOnlyShards = null,
            SequentialGuidType guidType = SequentialGuidType.SequentialAsBinary,
            bool migrationDataLossAllowed = false) {
            // Validate name presence
            _connectionName = Config.DataConnectionName(connectionName);

            DataContext.UpdateAutoMigrations(_connectionName, migrationDataLossAllowed);

            _guidType = guidType;
            if (readOnlyShards != null) {
                foreach (var readOnlyShard in readOnlyShards) { _readOnlyShards.Add(readOnlyShard); }
            }
            _shards = Config.DistibutedDataConnectionNames(_connectionName).ToDictionary(x => x.Key, y => y.Value);
            if (_readOnlyShards.Count >= _shards.Count)
                throw new ArgumentException("Too few writable shards!");
            // check and register shards
            using (var ctx = GetContext()) {
                var two = ctx.Database.SqlQuery<int>("SELECT 1+1").SingleOrDefault(); // check DB engine is working
                if (two != 2) throw new ApplicationException("Connection string is not working: " + connectionName);
            }

            CheckShardsAndSetEpoch(migrationDataLossAllowed);
        }

        /// <summary>
        /// Get new DataContext instance
        /// </summary>
        private DataContext GetContext() {
            var ctx = new DataContext(_connectionName);
            ctx.Configuration.AutoDetectChangesEnabled = false;
            ctx.Configuration.ProxyCreationEnabled = false;
            return ctx;
        }

        /// <summary>
        /// Get new DistributedDataContext instance for specified shard id
        /// </summary>
        /// <param name="bucket"></param>
        /// <returns></returns>
        private DistributedDataContext GetContext(byte bucket) {
            var ctx = new DistributedDataContext(_shards[bucket]);
            ctx.Configuration.AutoDetectChangesEnabled = false;
            return ctx;
        }

        private void CheckShardsAndSetEpoch(bool migrationDataLossAllowed) {
            var sortedShards = _shards.OrderBy(kvp => kvp.Key).ToList();
            var numberOfShards = sortedShards.Count;
            if (numberOfShards > 254) throw new ArgumentException("Too many shards!");
            NumberOfShards = (byte)numberOfShards;
            _writableShards = _shards.Keys.Except(_readOnlyShards).ToList();
            if (_writableShards.Count == 0) throw new ApplicationException("No writable shards");
            // one based
            var i = 0;
            foreach (var keyValuePair in sortedShards) {
                if (i != keyValuePair.Key) {
                    // TODO unit test
                    throw new ApplicationException("Wrong numbering of shards");
                }
                i++;
            }
            foreach (var key in sortedShards.Select(keyValuePair => keyValuePair.Key)) {
                DistributedDataContext.UpdateAutoMigrations(_shards[key], migrationDataLossAllowed);
                using (var ctx = GetContext(key)) {
                    var two = ctx.Database.SqlQuery<int>("SELECT 1+1").SingleOrDefault(); // check DB engine is working
                    if (two != 2) throw new ApplicationException("Shard " + key + " doesn't work");
                }
            }
        }

        /// <summary>
        /// Insert
        /// </summary>
        public void Insert<T>(T item) where T : class, IDataObject, new() {
            var list = item.ItemAsList();
            Insert(list);
        }

        /// <summary>
        /// Insert
        /// </summary>
        public void Insert<T>(List<T> items) where T : class, IDataObject, new() {
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
                        using (var db = GetContext(lu.Key)) {
                            try {
                                db.Set<T>().AddRange(basket);
                                db.SaveChanges();
                            } catch (Exception e) {
                                internalError = e;
                            }
                        }
                    });

                if (internalError != null) throw internalError;
            } else {
                using (var db = GetContext()) {
                    db.Set<T>().AddRange(items);
                    db.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Soft-update
        /// </summary>
        [Obsolete("Try to avoid data mutation")]
        public void Update<T>(T item) where T : class, IDataObject, new() {
            Update(item.ItemAsList());
        }


        /// <summary>
        /// Soft-update
        /// </summary>
        public void Update<T>(List<T> items) where T : class, IDataObject, new() {
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

                baskets
                    .AsParallel()
                    .WithDegreeOfParallelism(baskets.Count())
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .ForAll(lu => {
                        using (var db = GetContext(lu.Key)) {
                            SoftUpdate(lu, db);
                        }
                    });
            } else { using (var db = GetContext()) { SoftUpdate(items, db); } }
        }

        private void SoftUpdate<T>(IEnumerable<T> items, DbContext db) where T : class, IDataObject, new() {
            // TODO which isolation scope?
            using (var dbTransaction = db.Database.BeginTransaction()) {
                try {
                    foreach (var newItem in items) {
                        var id = newItem.Id;
                        // previous state, could be cached
                        var oldPrevious = this.GetById<T>(id); // could be null
                        if (oldPrevious != null) {
                            // object to store as previous in a new record
                            var newPrevious = oldPrevious.DeepClone();
                            CheckOrGenerateGuid(ref newPrevious, true, null, true);
                            newPrevious.IsActive = false;
                            // now new previous has new Id and inactive state, with all other props cloned
                            var newPreviousId = newPrevious.Id;
                            newItem.PreviousId = newPreviousId;

                            // Update oldPrevious with newItem
                            db.Set<T>().Attach(oldPrevious);
                            db.Entry(oldPrevious).CurrentValues.SetValues(newItem);
                            db.Set<T>().Add(newPrevious);
                        } else {
                            // here we could deal with deleted (GetByID = null for deleted)
                            // but addition will throw
                            db.Set<T>().Add(newItem);
                        }
                    }
                    db.SaveChanges();
                    dbTransaction.Commit();
                } catch (Exception) {
                    dbTransaction.Rollback();
                }
            }
        }


        /// <summary>
        /// Soft-delete
        /// </summary>
        public void Delete<T>(T item) where T : class, IDataObject, new() {
            Delete(item.ItemAsList());
        }

        /// <summary>
        /// Soft-delete
        /// </summary>
        public void Delete<T>(List<T> items) where T : class, IDataObject, new() {
            if (items == null) throw new ArgumentNullException("items");
            var length = items.Count;
            if (length == 0) return;
            items.ForEach(x => CheckOrGenerateGuid(ref x, true));

            var isDistributed = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

            if (isDistributed) {
                var baskets = items.ToLookup(i => i.Id.Bucket()).ToList();
                baskets
                    .AsParallel()
                    .WithDegreeOfParallelism(baskets.Count())
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .ForAll(lu => {
                        using (var db = GetContext(lu.Key)) {
                            foreach (var item in lu) {
                                item.IsActive = false;
                                item.PreviousId = default(Guid); // Zero guid
                                db.Set<T>().Attach(item);
                                db.Entry(item).State = EntityState.Modified;
                                db.Entry(item).Property(x => x.IsActive).IsModified = true;
                                db.Entry(item).Property(x => x.PreviousId).IsModified = true;
                            }
                            db.SaveChanges();
                        }
                    });
            } else {
                using (var db = GetContext()) {
                    foreach (var item in items) {
                        item.IsActive = false;
                        item.PreviousId = default(Guid); // Zero guid
                        db.Set<T>().Attach(item);
                        db.Entry(item).State = EntityState.Modified;
                        db.Entry(item).Property(x => x.IsActive).IsModified = true;
                        db.Entry(item).Property(x => x.PreviousId).IsModified = true;
                    }
                    db.SaveChanges();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public List<T> Select<T>(Expression<Func<T, bool>> predicate = null) where T : class, IDataObject, new() {

            return Query<T, List<T>>(db => (predicate == null ? db : db.Where(predicate)).ToList(), result => result.SelectMany(x => x).ToList());
        }

        // TODO simplify for single id and add private method to check for inactive state
        /// <summary>
        /// 
        /// </summary>
        public T GetById<T>(Guid guid) where T : class, IDataObject, new() {
            return GetByIds<T>(guid.ItemAsList()).SingleOrDefault();
        }

        /// <summary>
        /// 
        /// </summary>
        public List<T> GetByIds<T>(List<Guid> guids) where T : class, IDataObject, new() {
            return Query<T, List<T>>(db => db.Where(t => guids.Contains(t.Id)).ToList(), result => result.SelectMany(x => x).ToList());
        }

        /// <summary>
        /// 
        /// </summary>
        public long Count<T>() where T : class, IDataObject, new() {
            return Query<T, long>(db => db.Count(),
                result => result.Sum());
        }

        /// <summary>
        ///  
        /// </summary>
        public TR Query<T, TR>(Func<IQueryable<T>, TR> query,
            Func<List<TR>, TR> aggregation, IEnumerable<byte> shards = null)
            where T : class, IDataObject, new() {

            var isDistributed = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

            if (isDistributed) {
                var luShards = shards == null
                ? _shards
                : shards.ToDictionary(luShard => luShard, luShard => _shards[luShard]);

                var result = luShards
                    .AsParallel()
                    .WithDegreeOfParallelism(_shards.Count)
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(dbName => {
                        using (var db = GetContext(dbName.Key)) {
                            return query(db.Set<T>().Where(x => x.IsActive));
                        }
                    }).ToList();
                return aggregation(result);
            }
            using (var db = GetContext()) {
                return aggregation(query(db.Set<T>().Where(x => x.IsActive)).ItemAsList());
            }
        }


        // Method could be desctructive, cannot reply on DBMS permissions only
        // That is why soft-delete/update and no public destructive methods
        //private void ExecuteSql(string sql, bool distributed = false) {
        //    if (_readOnlyShards.Count > 0) throw new ReadOnlyException("Cannot execute SQL with read-only shards!");

        //    if (distributed) {
        //        _shards
        //            .AsParallel()
        //            .WithDegreeOfParallelism(_shards.Count)
        //            .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
        //            .Each(dbName => {
        //                using (var db = GetContext(dbName.Key)) {
        //                    db.Database.ExecuteSqlCommand(sql);
        //                }
        //            });
        //        return;
        //    }

        //    using (var db = GetContext()) {
        //        db.Database.ExecuteSqlCommand(sql);
        //    }

        //}

        internal void CheckOrGenerateGuid<T>(ref T item, bool onlyWritable, DateTime? utcDateTime = null, bool replace = false) where T : IDataObject {
            if (item.Id != default(Guid) && !replace) {
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
                    var randomWritableBucket =
                        _writableShards[(byte)(new Random()).Next(0, _writableShards.Count)]; // no '-1', maxValue is exlusive
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
        /// Generate new Guid for an item if Guid was not set, or keep existing.
        /// </summary>
        public void GenerateGuid<T>(ref T item, DateTime? utcDateTime = null, bool replace = false) where T : IDataObject {
            CheckOrGenerateGuid(ref item, false, utcDateTime, replace);
        }


    }
}
