using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Migrations.History;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace Ractor.Persistence.Tests {
    public class MySqlConfiguration : DbConfiguration {
        public MySqlConfiguration() {
            SetHistoryContext(
                "MySql.Data.MySqlClient", (conn, schema) => new MySqlHistoryContext(conn, schema));
        }
    }

    public class MySqlHistoryContext : HistoryContext {
        public MySqlHistoryContext(
            DbConnection existingConnection,
            string defaultSchema)
            : base(existingConnection, defaultSchema) {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<HistoryRow>().Property(h => h.MigrationId).HasMaxLength(100).IsRequired();
            modelBuilder.Entity<HistoryRow>().Property(h => h.ContextKey).HasMaxLength(200).IsRequired();
        }
    }

    public class DataObject : BaseDataObject {
        public string Value { get; set; }
        public string NewValue { get; set; }
    }

    public class RootAsset : BaseDistributedDataObject {
        public string Value { get; set; }
        public string NewValue { get; set; }
    }

    public class DependentAsset : BaseDistributedDataObject {
        public string Value { get; set; }
        public string NewValue { get; set; }
        public Guid RootAssetId { get; set; }

        public override Guid GetRootGuid() {
            return RootAssetId;
        }
    }

    [TestFixture]
    public class PocoPersistorTests {

        
        [Test]
        public void CouldCreateTableAndCrudDataObject() {
            var Persistor = new DatabasePersistor(guidType: SequentialGuidType.SequentialAsBinary);

            for (int i = 0; i < 10; i++) {
                var dobj = new DataObject() {
                    Value = "inserted"
                };
                Persistor.Insert(dobj);

                var fromDb = Persistor.GetById<DataObject>(dobj.Id);
                Assert.AreEqual("inserted", fromDb.Value);

                fromDb.Value = "updated";
                Persistor.Update(fromDb);
                fromDb = Persistor.GetById<DataObject>(dobj.Id);
                Assert.AreEqual("updated", fromDb.Value);
            }
        }

        [Test]
        public void CouldCreateTableAndCrudDistributedDataObject() {
            var Persistor = new DatabasePersistor(guidType: SequentialGuidType.SequentialAsBinary);
            for (int i = 0; i < 1; i++) {

                var dobj = new RootAsset() {
                    Value = "inserted"
                };

                Persistor.Insert(dobj);

                var fromDb = Persistor.GetById<RootAsset>(dobj.Id);
                Assert.AreEqual("inserted", fromDb.Value);

                Console.WriteLine(dobj.Id);

                fromDb.Value = "updated";
                Persistor.Update(fromDb);
                fromDb = Persistor.GetById<RootAsset>(dobj.Id);
                Assert.AreEqual("updated", fromDb.Value);
            }
        }

        [Test]
        public void CouldCreateTableAndInsertManyDataObject() {
            var Persistor = new DatabasePersistor(guidType: SequentialGuidType.SequentialAsBinary);
            var sw = new Stopwatch();
            sw.Start();
            var list = new List<DataObject>();
            for (int i = 0; i < 100000; i++) {

                var dobj = new DataObject() {
                    Value = "inserted"
                };
                //Persistor.Insert(dobj);
                list.Add(dobj);
            }
            Persistor.Insert(list);
            sw.Stop();
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds);
        }

         [Test]
        public void RandomTest() {
            for (int i = 0; i < 100; i++) { Console.WriteLine((new Random()).Next(0, 2)); }
        }

        [Test]
        public void CouldCreateTableAndInsertManyDistributedDataObject() {
            var Persistor = new DatabasePersistor(guidType: SequentialGuidType.SequentialAsBinary);
            var sw = new Stopwatch();
            sw.Start();
            var list = new List<RootAsset>();
            for (int i = 0; i < 100000; i++) {
                var dobj = new RootAsset() {
                    Value = "inserted"
                };
                list.Add(dobj);
            }
            Persistor.Insert(list);
            sw.Stop();
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds);
        }


        [Test]
        public void CouldSelectManyDistributedDataObject() {
            var Persistor = new DatabasePersistor(guidType: SequentialGuidType.SequentialAsBinary);
            //Persistor.CreateTable<RootAsset>(true);
            //var list = new List<RootAsset>();
            //for (int i = 0; i < 100000; i++) {

            //    var dobj = new RootAsset() {
            //        Value = "inserted"
            //    };
            //    list.Add(dobj);
            //}
            //Persistor.Insert(list);

            var values = Persistor.Select<RootAsset>().Select(ra => ra.Id).ToList();
            RootAsset a;
            foreach (var value in values) {
                a = Persistor.GetById<RootAsset>(value);
            }
            //Persistor.GetByIds<RootAsset>(values);
        }

    }
}
