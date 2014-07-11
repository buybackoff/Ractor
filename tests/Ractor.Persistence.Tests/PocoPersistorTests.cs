using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.OrmLite;

namespace Ractor.Persistence.Tests {

    public class DataObject : BaseDataObject {
        public string Value { get; set; }
    }

    public class RootAsset : BaseDistributedDataObject {
        public string Value { get; set; }
    }

    public class DependentAsset : BaseDistributedDataObject {
        public string Value { get; set; }
        public Guid RootAssetGuid { get; set; }

        public override Guid GetRootGuid() {
            return RootAssetGuid;
        }
    }

    [TestFixture]
    public class PocoPersistorTests {

        public IPocoPersistor Persistor { get; set; }

        public PocoPersistorTests() {
            //var shards = new Dictionary<ushort, string> {
            //    {0, "App_Data/0.sqlite"}
            //    //,{1, "App_Data/1.sqlite"}
            //};
            //Persistor = new BasePocoPersistor(SqliteDialect.Provider, "App_Data/main.sqlite", shards);

            var shards = new Dictionary<ushort, string> {
                {0, "Server=localhost;Database=fredis.0;Uid=test;Pwd=test;"}
                //,{1, "App_Data/1.sqlite"}
            };
            Persistor = new BasePocoPersistor(MySqlDialect.Provider,
                "Server=localhost;Database=fredis;Uid=test;Pwd=test;", shards);


        }


        [Test]
        public void CouldCreateTableAndCrudDataObject() {
            

            Persistor.CreateTable<DataObject>(true);

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

            Persistor.CreateTable<RootAsset>(true);

            for (int i = 0; i < 1; i++) {


                var dobj = new RootAsset() {
                    Value = "inserted"
                };

                Persistor.Insert(dobj);

                var fromDb = Persistor.GetById<RootAsset>(dobj.Guid);
                Assert.AreEqual("inserted", fromDb.Value);

                Console.WriteLine(dobj.Guid);

                fromDb.Value = "updated";
                Persistor.Update(fromDb);
                fromDb = Persistor.GetById<RootAsset>(dobj.Guid);
                Assert.AreEqual("updated", fromDb.Value);
            }
        }

        [Test]
        public void CouldCreateTableAndInsertManyDataObject() {

            Persistor.CreateTable<DataObject>(true);
            var list = new List<DataObject>();
            for (int i = 0; i < 5000; i++) {

                var dobj = new DataObject() {
                    Value = "inserted"
                };
                list.Add(dobj);
            }
            Persistor.Insert(list);
        }


        [Test]
        public void CouldCreateTableAndInsertManyDistributedDataObject() {

            Persistor.CreateTable<RootAsset>(false);
            var list = new List<RootAsset>();
            for (int i = 0; i < 5000; i++) {

                var dobj = new RootAsset() {
                    Value = "inserted"
                };
                list.Add(dobj);
            }
            Persistor.Insert(list);
        }


        [Test]
        public void CouldSelectManyDistributedDataObject() {

            //Persistor.CreateTable<RootAsset>(true);
            //var list = new List<RootAsset>();
            //for (int i = 0; i < 100000; i++) {

            //    var dobj = new RootAsset() {
            //        Value = "inserted"
            //    };
            //    list.Add(dobj);
            //}
            //Persistor.Insert(list);

            var values = Persistor.Select<RootAsset>(q => q.Id < 201).Select(ra => ra.Guid).ToList();
            RootAsset a;
            foreach (var value in values) {
                a = Persistor.GetById<RootAsset>(value);
            }
            //Persistor.GetByIds<RootAsset>(values);
        }

    }
}
