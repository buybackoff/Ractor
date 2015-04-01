using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.History;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Npgsql;
using NUnit.Framework;

namespace Ractor.Persistence.Tests {
    public class PostgresMigrationsConfiguration : DbMigrationsConfiguration<DataContext>
    {
        public PostgresMigrationsConfiguration()
        {
            this.AutomaticMigrationsEnabled = true;
            this.AutomaticMigrationDataLossAllowed = true; // NB!!! set to false on live data
            SetSqlGenerator("Npgsql", new NpgsqlMigrationSqlGenerator());
        }
    }

	public class DistributedPostgresMigrationsConfiguration : DbMigrationsConfiguration<DistributedDataContext>
	{
		public DistributedPostgresMigrationsConfiguration()
		{
			this.AutomaticMigrationsEnabled = true;
			this.AutomaticMigrationDataLossAllowed = true; // NB!!! set to false on live data
			SetSqlGenerator("Npgsql", new NpgsqlMigrationSqlGenerator());
		}
	}

	
    [StructLayout(LayoutKind.Sequential)]
    public class DataRecord : IData
    {
        [Key, Column(Order = 0)]
        public Int16 Source { get; set; }
        [Key, Column(Order = 1)] 
        public Int32 Entity { get; set; }
        [Key, Column(Order = 2)]
        public Int64 Relationship { get; set; }
        [Key, Column(Order = 3)]
        public Int32 Metric { get; set; }
        [Key, Column(Order = 4)]
        public DateTime Period { get; set; }
        [Key, Column(Order = 5)]
        public DateTime ObservationTime { get; set; }

        public double Value { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class TextRecord : IData {
        [Key, Column(Order = 0)]
        public Int16 Source { get; set; }
        [Key, Column(Order = 1)]
        public Int32 Entity { get; set; }
        [Key, Column(Order = 2)]
        public Int64 Relationship { get; set; }
        [Key, Column(Order = 3)]
        public Int32 Metric { get; set; }
        [Key, Column(Order = 4)]
        public DateTime Period { get; set; }
        [Key, Column(Order = 5)]
        public DateTime ObservationTime { get; set; }

        public string Value { get; set; }
    }

    public class TestDataObject : BaseDataObject {
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
        public void CouldInsertDataRecords(){
            var Persistor = new DatabasePersistor(migrationConfig: new PostgresMigrationsConfiguration(), distributedMigrationConfig: new DistributedPostgresMigrationsConfiguration(), guidType: SequentialGuidType.SequentialAsBinary);

            var list = new List<DataRecord>();


            for (int i = 0; i < 100000; i++) {
                var dobj = new DataRecord() {
                    Source = 0,
                    Entity = i*10,
                    Relationship = i*10,
                    Metric = i/100,
                    Period = DateTime.Today,
                    ObservationTime = DateTime.Now,
                    Value = 123 + ((double)i)/100.0
                };
                list.Add(dobj);
                
            }

            Persistor.Insert(list);
        }

        
        [Test]
        public void CouldCreateTableAndCrudDataObject() {
			var Persistor = new DatabasePersistor(migrationConfig: new PostgresMigrationsConfiguration(), distributedMigrationConfig: new DistributedPostgresMigrationsConfiguration(), guidType: SequentialGuidType.SequentialAsBinary);

			for (int i = 0; i < 10; i++) {
                var dobj = new TestDataObject() {
                    Value = "inserted"
                };
                Persistor.Insert(dobj);

                var fromDb = Persistor.GetById<TestDataObject>(dobj.Id);
                Assert.AreEqual("inserted", fromDb.Value);

                fromDb.Value = "updated";
                Persistor.Update(fromDb);
                fromDb = Persistor.GetById<TestDataObject>(dobj.Id);
                Assert.AreEqual("updated", fromDb.Value);
            }
        }

        [Test]
        public void CouldCreateTableAndCrudDistributedDataObject() {
			var Persistor = new DatabasePersistor(migrationConfig: new PostgresMigrationsConfiguration(), distributedMigrationConfig: new DistributedPostgresMigrationsConfiguration(), guidType: SequentialGuidType.SequentialAsBinary);
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
			var Persistor = new DatabasePersistor(migrationConfig: new PostgresMigrationsConfiguration(), distributedMigrationConfig: new DistributedPostgresMigrationsConfiguration(), guidType: SequentialGuidType.SequentialAsBinary);
			var sw = new Stopwatch();
            sw.Start();
            var list = new List<TestDataObject>();
            for (int i = 0; i < 1000; i++) {

                var dobj = new TestDataObject() {
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
        public void CouldCreateTableAndInsertManyDistributedDataObject() {
			var Persistor = new DatabasePersistor(migrationConfig: new PostgresMigrationsConfiguration(), distributedMigrationConfig: new DistributedPostgresMigrationsConfiguration(), guidType: SequentialGuidType.SequentialAsBinary);
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
			var Persistor = new DatabasePersistor(migrationConfig: new PostgresMigrationsConfiguration(), distributedMigrationConfig: new DistributedPostgresMigrationsConfiguration(), guidType: SequentialGuidType.SequentialAsBinary);
			var values = Persistor.Select<RootAsset>().Select(ra => ra.Id).ToList();
            RootAsset a;
            foreach (var value in values) {
                a = Persistor.GetById<RootAsset>(value);
            }
        }

    }
}
