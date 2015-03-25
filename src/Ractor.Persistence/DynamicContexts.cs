using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.Migrations;
using System.Linq;

namespace Ractor {

    /// <summary>
    /// Dynamic context with all IData loaded into current AppDomain
    /// </summary>
    public class DataContext : DbContext { //}, IDbContextFactory<DataContext> {
        public DataContext() : base() { }
        /// <summary>
        /// 
        /// </summary>
        internal DataContext(string name)
            : base(name) {
            // TODO move migrations here?
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            // Use IDataObject.Guid as primary key
            modelBuilder.Types<IDataObject>().Configure(c => {
                c.HasKey(e => e.Id);
                c.Property(p => p.Id)
                    .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity).IsRequired()
                    .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("LogicalId")));
            });


            var entityMethod = typeof(DbModelBuilder).GetMethod("Entity");
            var types = AppDomain.CurrentDomain
                .GetAssemblies()
                .Except(this.GetType().Assembly.ItemAsList())
                .SelectMany(s => s.GetTypes())
                .Where(p =>
                    typeof(IData).IsAssignableFrom(p)
                    && !typeof(IDistributedDataObject).IsAssignableFrom(p)
                    && p.IsClass && !p.IsAbstract);

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
                TargetDatabase = new DbConnectionInfo(name),
            };
            var migrator = new DbMigrator(config);
            migrator.Update();
        }

        public DataContext Create() {
            return new DataContext("Ractor");
        }
    }

    /// <summary>
    /// Dynamic context with all IDistributedDataObjects loaded into current AppDomain
    /// </summary>
    public class DistributedDataContext : DbContext {
        public DistributedDataContext() : base() { }

        /// <summary>
        /// 
        /// </summary>
        internal DistributedDataContext(string name)
            : base(name) {
            // TODO move migrations here?
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            // Use IDistributedDataObject.Guid as primary key
            modelBuilder.Types<IDistributedDataObject>().Configure(c =>
            {
                c.HasKey(e => e.Id);
                c.Property(p => p.Id)
                    .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity).IsRequired()
                    .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("LogicalId")));
            });

            var entityMethod = typeof(DbModelBuilder).GetMethod("Entity");
            var types = AppDomain.CurrentDomain
                .GetAssemblies()
                .Except(this.GetType().Assembly.ItemAsList())
                .SelectMany(s => s.GetTypes())
                .Where(p =>
                    typeof(IDistributedDataObject).IsAssignableFrom(p)
                    && p.IsClass && !p.IsAbstract);

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
                TargetDatabase = new DbConnectionInfo(name),
            };
            var migrator = new DbMigrator(config);
            migrator.Update();
        }
    }


}
