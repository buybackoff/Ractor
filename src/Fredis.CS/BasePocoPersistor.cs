//using System;
//using System.Collections.Generic;
//using System.Configuration;
//using System.Data;
//using System.Diagnostics;
//using System.Globalization;
//using System.Linq;
//using System.Linq.Expressions;
//using ServiceStack.Common;
//using ServiceStack.OrmLite;
//using ServiceStack.Text;

//namespace Fredis {

//    public class Shard : BaseDataObject {
//        //public new ushort Id { get; set; }

//        //public string DbName { get; set; }
//        public string ConnectionString { get; set; }

//        #region Implementation of IDataObject

//        //[Ignore]
//        //long IDataObject.Id
//        //{
//        //    get { return Id; }
//        //    set { Id = (ushort)value; }
//        //}

//        #endregion
//    }


//    public abstract class BasePocoPersistor : IPocoPersistor {

//        public BasePocoPersistor(OrmLiteConnectionFactory factory) {
//            DbFactory = factory;
//        }

//        protected OrmLiteConnectionFactory DbFactory { get; set; }

//        protected static string GetCreateScriptForType<T>()
//            where T : IDataObject, new() {
//            var createTableAttribute = typeof (T).FirstAttribute<CreateTableAttribute>();

//            return createTableAttribute != null ? createTableAttribute.Sql : null;
//        }



//        protected ushort NumberOfShards { get; set; }

//        protected Dictionary<ushort, Shard> StoredShards = new Dictionary<ushort, Shard>();

//        protected abstract void CreateTableInShard<T>(IDbConnection db, bool overwrite = false, ushort shardId = 0)
//            where T : IDataObject, new();

//        protected abstract bool RegisterShards();

//        public void CreateTable<T>(bool overwrite = false) where T : IDataObject, new() {
//            bool isSharded = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

//            if (isSharded) {
//                foreach (var storedShard in StoredShards) {
//                    using (var db = DbFactory.OpenDbConnection(storedShard.Value.Id.ToString(CultureInfo.InvariantCulture))) {
//                        CreateTableInShard<T>(db, overwrite, (ushort)storedShard.Value.Id);
//                    }
//                }

//            } else {

//                using (var db = DbFactory.OpenDbConnection()) {
//                    CreateTableInShard<T>(db, overwrite);
//                }
//            }

//        }


//        public void InsertSetId<T>(ref T item) where T : IDataObject, new() {
//            var list = item.ItemAsList();
//            InsertSetId(ref list);
//            item = list.Single();
//        }


//        public void InsertSetId<T>(ref List<T> items) where T : IDataObject, new() {
//            if (items == null) throw new ArgumentNullException("items");
//            var length = items.Count;
//            if (length == 0) return;

//            items.ForEach(x => {
//                x.UpdatedAt = DateTime.UtcNow;
//            });

//            var isDistributed = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

//            if (isDistributed) {
//                var shardedItems = items.Cast<IDistributedDataObject>();
//                var baskets = shardedItems.ToLookup(i => (ushort)(1 + (i.GetShardId() % NumberOfShards))).ToList();
//                var hasErrors = false;

//                var result = baskets
//                    .AsParallel()
//                    .WithDegreeOfParallelism(baskets.Count())
//                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
//                    .Select(lu => {
//                        var basket = lu.Cast<T>().ToList();
//                        using (var db = DbFactory.OpenDbConnection(StoredShards[lu.Key].Id.ToString(CultureInfo.InvariantCulture)))
//                        using (var trans = db.OpenTransaction()) {
//                            try {
//                                for (var i = 0; i < basket.Count; i++) {
//                                    db.Insert(basket[i]);
//                                    var id = db.GetLastInsertId();
//                                    basket[i].Id = id;
//                                }
//                                //db.InsertAll(basket);
//                                trans.Commit();
//                            } catch {
//                                Trace.TraceError("Insert error on shard " + lu.Key);
//                                trans.Rollback();
//                                hasErrors = true;
//                            }
//                        }
//                        return basket;
//                    }).SelectMany(x => x).ToList(); // ToArray to get the actual result

//                if (hasErrors) throw new DataException("Could not insert data");

//                items = result;
//            } else {
//                using (var db = DbFactory.OpenDbConnection())
//                using (var trans = db.OpenTransaction()) {
//                    try {
//                        for (var i = 0; i < length; i++) {
//                            db.Insert(items[i]);
//                            var id = db.GetLastInsertId();
//                            items[i].Id = id;
//                        }
//                        trans.Commit();
//                    } catch {
//                        trans.Rollback();
//                        throw;
//                    }
//                }
//            }
//        }


//        public void Insert<T>(T item) where T : IDataObject, new() {
//            Insert(item.ItemAsList());
//        }



//        public void Insert<T>(List<T> items) where T : IDataObject, new() {
//            if (items == null) throw new ArgumentNullException("items");
//            var length = items.Count;
//            if (length == 0) return;

//            items.ForEach(x => {
//                x.UpdatedAt = DateTime.UtcNow;
//            });

//            bool isSharded = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

//            if (isSharded) {
//                var shardedItems = items.Cast<IDistributedDataObject>();
//                var baskets = shardedItems.ToLookup(i => (ushort)(1 + (i.GetShardId() % NumberOfShards)));
//                var hasErrors = false;

//                // ReSharper disable once UnusedVariable
//                var result = baskets
//                    .AsParallel()
//                    .WithDegreeOfParallelism(baskets.Count)
//                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
//                    .Select(lu => {
//                        var basket = lu.Cast<T>().ToList();
//                        using (var db = DbFactory.OpenDbConnection(StoredShards[lu.Key].Id.ToString(CultureInfo.InvariantCulture)))
//                        using (var trans = db.OpenTransaction()) {
//                            try {
//                                db.InsertAll(basket);
//                                trans.Commit();
//                            } catch {
//                                Trace.TraceError("Insert error on shard " + lu.Key);
//                                trans.Rollback();
//                                hasErrors = true;
//                            }
//                        }
//                        return basket;
//                    }).SelectMany(x => x).ToList(); // ToArray to get the actual result

//                if (hasErrors) throw new DataException("Could not insert data");

//            } else {
//                using (IDbConnection db = DbFactory.OpenDbConnection())
//                using (var trans = db.OpenTransaction()) {
//                    try {
//                        db.InsertAll(items);
//                        trans.Commit();
//                    } catch {
//                        trans.Rollback();
//                        throw;
//                    }
//                }
//            }
//        }



//        public void UpdateSingle<T>(T item) where T : IDataObject, new() {
//            Update(item.ItemAsList());
//        }

//        public override void Update<T>(List<T> items) {
//            if (items == null) throw new ArgumentNullException("items");
//            var length = items.Count;
//            if (length == 0) return;

//            items.ForEach(x => {
//                x.UpdatedAt = DateTime.UtcNow;
//            });

//            bool isSharded = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

//            if (isSharded) {
//                var shardedItems = items.Cast<IDistributedDataObject>();
//                var baskets = shardedItems.ToLookup(i => (ushort)(1 + (i.GetShardId() % NumberOfShards)));

//                // ReSharper disable once NotAccessedVariable
//                var result = baskets
//                    .AsParallel()
//                    .WithDegreeOfParallelism(baskets.Count)
//                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
//                    .Select(lu => {
//                        var basket = lu.Cast<T>().ToList();
//                        using (var db = DbFactory.OpenDbConnection(StoredShards[lu.Key].Id.ToString(CultureInfo.InvariantCulture)))
//                        using (var trans = db.OpenTransaction()) {
//                            try {
//                                db.UpdateAll(basket);
//                                trans.Commit();
//                            } catch {
//                                Trace.TraceError("Update error on shard " + lu.Key);
//                                trans.Rollback();
//                                throw;
//                            }
//                        }
//                        return basket;
//                    }).ToArray(); // ToArray to get the actual result
//                // ReSharper disable once RedundantAssignment
//                result = null;
//            } else {
//                base.Update(items);
//            }
//        }



//        public void DeleteSingle<T>(T item) where T : IDataObject, new() {
//            Delete(item.ItemAsList());
//        }
//        public override void Delete<T>(List<T> items) {
//            if (items == null) throw new ArgumentNullException("items");
//            var length = items.Count;
//            if (length == 0) return;

//            bool isSharded = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

//            if (isSharded) {
//                var shardedItems = items.Cast<IDistributedDataObject>();
//                var baskets = shardedItems.ToLookup(i => (ushort)(1 + (i.GetShardId() % NumberOfShards)));

//                // ReSharper disable once NotAccessedVariable
//                var result = baskets
//                    .AsParallel()
//                    .WithDegreeOfParallelism(baskets.Count)
//                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
//                    .Select(lu => {
//                        var basket = lu.Cast<T>().ToList();
//                        using (var db = DbFactory.OpenDbConnection(StoredShards[lu.Key].Id.ToString(CultureInfo.InvariantCulture)))
//                        using (var trans = db.OpenTransaction()) {
//                            try {
//                                db.DeleteAll(basket);
//                                trans.Commit();
//                            } catch {
//                                Trace.TraceError("Delete error on shard " + lu.Key);
//                                trans.Rollback();
//                                throw;
//                            }
//                        }
//                        return basket;
//                    }).ToArray(); // ToArray to get the actual result
//                // ReSharper disable once RedundantAssignment
//                result = null;
//            } else {
//                base.Delete(items);
//            }
//        }



//        public override List<T> Select<T>(Expression<Func<T, bool>> predicate = null) {
//            bool isSharded = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

//            if (isSharded) {
//                var dbNames =
//                    StoredShards.GroupBy(kvp => kvp.Value.Id.ToString(CultureInfo.InvariantCulture)).Select(g => g.Key)
//                        .ToList();
//                var result = dbNames
//                    .AsParallel()
//                    .WithDegreeOfParallelism(dbNames.Count)
//                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
//                    .Select(dbName => {
//                        using (var db = DbFactory.OpenDbConnection(dbName)) {
//                            return predicate == null ? db.Select<T>() : db.Select(predicate);
//                        }
//                    }).ToList();

//                var flatResult = result.SelectMany(x => x).ToList();
//                return flatResult;
//            }

//            return base.Select(predicate);
//        }


//        public override List<T> Select<T>(string sqlFilter, params object[] filterParams) {
//            bool isSharded = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

//            if (isSharded) {
//                var dbNames =
//                    StoredShards.GroupBy(kvp => kvp.Value.Id.ToString(CultureInfo.InvariantCulture)).Select(g => g.Key)
//                        .ToList();
//                var result = dbNames
//                    .AsParallel()
//                    .WithDegreeOfParallelism(dbNames.Count)
//                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
//                    .Select(dbName => {
//                        using (var db = DbFactory.OpenDbConnection(dbName)) {
//                            return db.Select<T>(sqlFilter, filterParams);
//                        }
//                    }).ToList();

//                var flatResult = result.SelectMany(x => x).ToList();
//                return flatResult;
//            }

//            return base.Select<T>(sqlFilter, filterParams);

//        }


//        public override void ExecuteSql(string sql, bool onShards = false) {
//            if (onShards) {
//                var dbNames =
//                    StoredShards.GroupBy(kvp => kvp.Value.Id.ToString(CultureInfo.InvariantCulture)).Select(g => g.Key)
//                        .ToList();
//                dbNames
//                    .AsParallel()
//                    .WithDegreeOfParallelism(dbNames.Count)
//                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
//                    .ForEach(dbName => {
//                        using (var db = DbFactory.OpenDbConnection(dbName)) {
//                            db.ExecuteSql(sql);
//                        }
//                    });
//            }

//            // ReSharper disable once BaseMethodCallWithDefaultParameter
//            base.ExecuteSql(sql);

//        }


//        public override List<T> GetByIds<T>(List<long> ids) {
//            bool isSharded = typeof(IDistributedDataObject).IsAssignableFrom(typeof(T));

//            if (isSharded) {
//                var shardLookup = ids.ToLookup(i => (ushort)(i >> 48));

//                var result = shardLookup
//                    .AsParallel()
//                    .WithDegreeOfParallelism(shardLookup.Count)
//                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
//                    .Select(idValues => {
//                        using (
//                            var db =
//                                DbFactory.OpenDbConnection(
//                                    idValues.Key.ToString(CultureInfo.InvariantCulture))) {
//                            return db.GetByIds<T>(idValues);
//                        }
//                    }).ToList();

//                var flatResult = result.SelectMany(x => x).ToList();
//                return flatResult;
//            }

//            return base.GetByIds<T>(ids);
//        }


//        public override T GetById<T>(long id) {
//            return GetByIds<T>(id.ItemAsList()).Single();
//        }




//    }
//}
