using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Linq;

namespace Ractor {

    /// <summary>
    /// Dynamic context with all IDataObjects loaded into current AppDomain
    /// </summary>
    internal class DataContext : DbContext {

        /// <summary>
        /// 
        /// </summary>
        internal DataContext(string name) : base(name) {
            // TODO move migrations here?
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            // Use IDataObject.Id as primary key
            modelBuilder.Types<IDataObject>().Configure(c => {
                c.HasKey(e => e.Id);
                //c.Property(p => p.PreviousId).IsRequired();
            });
            
            var entityMethod = typeof(DbModelBuilder).GetMethod("Entity");
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p =>
                    typeof(IDataObject).IsAssignableFrom(p)
                    && !typeof(IDistributedDataObject).IsAssignableFrom(p)
                    && p.IsClass);

            foreach (var t in types) {
                entityMethod.MakeGenericMethod(t)
                  .Invoke(modelBuilder, new object[] { });
            }
        }

        /// <summary>
        /// Run Automatic migrations
        /// </summary>
        internal static void UpdateAutoMigrations(string name, bool migrationDataLossAllowed = false) {
            var config = new DbMigrationsConfiguration<DataContext> {
                AutomaticMigrationsEnabled = true,
                AutomaticMigrationDataLossAllowed = migrationDataLossAllowed,
                TargetDatabase = new DbConnectionInfo(name)
            };
            var migrator = new DbMigrator(config);
            migrator.Update();
        }
    }

    /// <summary>
    /// Dynamic context with all IDistributedDataObjects loaded into current AppDomain
    /// </summary>
    internal class DistributedDataContext : DbContext {
        /// <summary>
        /// 
        /// </summary>
        internal DistributedDataContext(string name) : base(name) {
            // TODO move migrations here?
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            // Use IDistributedDataObject.Id as primary key
            modelBuilder.Types<IDistributedDataObject>().Configure(c => c.HasKey(e => e.Id));

            var entityMethod = typeof(DbModelBuilder).GetMethod("Entity");
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p =>
                    typeof(IDistributedDataObject).IsAssignableFrom(p)
                    && p.IsClass);

            foreach (var t in types) {
                entityMethod.MakeGenericMethod(t)
                  .Invoke(modelBuilder, new object[] { });
            }
        }


        /// <summary>
        /// Run Automatic migrations
        /// </summary>
        internal static void UpdateAutoMigrations(string name, bool migrationDataLossAllowed = false) {
            var config = new DbMigrationsConfiguration<DistributedDataContext> {
                AutomaticMigrationsEnabled = true,
                AutomaticMigrationDataLossAllowed = migrationDataLossAllowed,
                TargetDatabase = new DbConnectionInfo(name)
            };
            var migrator = new DbMigrator(config);
            migrator.Update();
        }
    }

    
}
