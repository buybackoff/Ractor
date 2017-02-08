using System;
using MySql.Data.Entity;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.History;
using System.Linq;
using Dapper;
using Dapper.Contrib.Extensions;

namespace Ractor.Persistence.CoreTest {

    public class MySqlMigrationsConfiguration : DbMigrationsConfiguration //<DataContext>
    {
        //<Ractor.DataContext>
        public MySqlMigrationsConfiguration() {
            this.AutomaticMigrationsEnabled = true;
            this.AutomaticMigrationDataLossAllowed = true; // NB!!! set to false on live data
            SetSqlGenerator("MySql.Data.MySqlClient", new MySqlMigrationSqlGenerator());
            // This will add our MySQLClient as SQL Generator
            CodeGenerator = new MySqlMigrationCodeGenerator();
        }
    }

    public class MySqlConfiguration : DbConfiguration {

        public MySqlConfiguration() {
            //Database.SetInitializer(new MigrateDatabaseToLatestVersion<DataContext, MySqlMigrationsConfiguration>());
            SetHistoryContext("MySql.Data.MySqlClient", (conn, schema) => new MySqlHistoryContext(conn, schema));
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

    [Table("TestDataClass")]
    public class TestDataClass : IData {
        [System.ComponentModel.DataAnnotations.Key]
        public int Id { get; set; }

        public string Value { get; set; }
    }

    public class Program {

        public static void Main(string[] args) {
            var persitor = new DatabasePersistor("DataContext", migrationConfig: new MySqlMigrationsConfiguration());


            persitor.Insert(new TestDataClass { Value = ".NET Core" });



            using (var db = persitor.GetConnection()) {

                var cnt = db.Insert(new TestDataClass() { Value = "From Dapper" });
                Console.WriteLine($"Inserted {cnt}");

                var result = db.Query<TestDataClass>("select * from TestDataClass where Id = 1");
                if (result.Single().Value == ".NET Core") {
                    Console.WriteLine("Dapper works");
                }
            }

            Console.WriteLine("Done...");
            Console.ReadLine();
        }
    }
}