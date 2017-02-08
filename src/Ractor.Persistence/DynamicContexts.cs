#if NET451
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.Migrations;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Data.Entity.Validation;

namespace Ractor {

    /// <summary>
    /// Dynamic context with all IData loaded into current AppDomain
    /// </summary>
    public class DataContext : DbContext { //}, IDbContextFactory<DataContext> {

        private string _name;

        //public static Action<DbModelBuilder> OnModelCreatingAction { get; set; }

        public DataContext() : base() { }

        /// <summary>
        /// 
        /// </summary>
        public DataContext(string name)
            : base(name) {
            _name = name;
        }

        public DataContext(string name, bool updateMigrations, DbMigrationsConfiguration migrationConfig = null)
                    : base(name) {
            _name = name;
            if (updateMigrations) {
                DataContext.UpdateAutoMigrations(name, migrationConfig);
            }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {

            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            // Use IDataObject.Guid as primary key
            modelBuilder.Types<IDataObject>().Configure(c => {
                c.HasKey(e => e.Id);
                c.Property(p => p.Id)
                    .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity).IsRequired()
                    .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("LogicalId")));
            });

            var types = GetDataTypes();

            foreach (var t in types) {
                modelBuilder.RegisterEntityType(t);
            }

            //if (OnModelCreatingAction != null)
            //{
            //          OnModelCreatingAction(modelBuilder);
            //}
        }
        private static bool _typesLoaded = false;
        private static Dictionary<string, Type> _types = new Dictionary<string, Type>();
        public static List<Type> GetDataTypes() {
            try {
                var types = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Except(typeof(DataContext).Assembly.ItemAsList())
                    //.Where(a => !a.CodeBase.Contains("mscorlib.dll"))
                    .SelectMany(s => {
                        try {
                            return s.GetTypes();
                        } catch {
                            return new Type[] { };
                        }
                    })
                    .Where(p => {
                        try {
                            return typeof(IData).IsAssignableFrom(p)
                                   && !typeof(IDistributedDataObject).IsAssignableFrom(p)
                                   && p.IsClass && !p.IsAbstract;
                        } catch {
                            return false;
                        }
                    }).ToList();
                foreach (var t in types) {
                    if (!_types.ContainsKey(t.Name)) {
                        _types.Add(t.Name, t);
                    }
                }
                _typesLoaded = true;

                return _types.Values.ToList();
            } catch {
                return new List<Type>();
            }
        }

        /// <summary>
        /// Run Automatic migrations
        /// </summary>
        internal static void UpdateAutoMigrations(string name, DbMigrationsConfiguration migrationConfig) //<DataContext>
        {
            var types = GetDataTypes();
            foreach (var t in types) {
                var d1 = typeof(SingleTableContext<>);
                Type[] typeArgs = { t };
                var makeme = d1.MakeGenericType(typeArgs);
                object[] args = { name };
                object o = Activator.CreateInstance(makeme, args);

                var migrationMethod = makeme.GetMethod("UpdateAutoMigrations");
                var methods = makeme.GetMethods(); //System.Reflection.BindingFlags.NonPublic);
                migrationMethod.Invoke(o, new object[] { name, migrationConfig });
            }
        }

        public DataContext Create() {
            return new DataContext("Ractor");
        }

        //http://stackoverflow.com/questions/15820505/dbentityvalidationexception-how-can-i-easily-tell-what-caused-the-error

        public override int SaveChanges() {
            try {
                return base.SaveChanges();
            } catch (DbEntityValidationException ex) {
                // Retrieve the error messages as a list of strings.
                var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => String.Format("Property: {0} Error: {1}", x.PropertyName, x.ErrorMessage));

                // Join the list to a single string.
                var fullErrorMessage = string.Join("; ", errorMessages);

                // Combine the original exception message with the new one.
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                // Throw a new DbEntityValidationException with the improved exception message.
                throw new DbEntityValidationException(exceptionMessage, ex.EntityValidationErrors);
            }
        }
    }



    /// <summary>
    /// Dynamic context with all IDistributedDataObjects loaded into current AppDomain
    /// </summary>
    public class DistributedDataContext : DbContext {
        //public static Action<DbModelBuilder> OnModelCreatingAction { get; set; }

        public DistributedDataContext() : base() { }

        /// <summary>
        /// 
        /// </summary>
        internal DistributedDataContext(string name)
            : base(name) {
        }

        private static Dictionary<string, Type> _types = new Dictionary<string, Type>();

        public static List<Type> GetDataTypes() {
            try {
                var types = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Except(typeof(DistributedDataContext).Assembly.ItemAsList())
                    //.Where(a => !a.CodeBase.Contains("mscorlib.dll"))
                    .SelectMany(s => {
                        try {
                            return s.GetTypes();
                        } catch {
                            return new Type[] { };
                        }
                    })
                    .Where(p => {
                        try {
                            return typeof(IDistributedDataObject).IsAssignableFrom(p)
                                   && p.IsClass && !p.IsAbstract;
                        } catch {
                            return false;
                        }
                    }).ToList();
                foreach (var t in types) {
                    if (!_types.ContainsKey(t.Name)) {
                        _types.Add(t.Name, t);
                    }
                }

                return _types.Values.ToList();
            } catch {
                return new List<Type>();
            }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            // Use IDistributedDataObject.Guid as primary key
            modelBuilder.Types<IDistributedDataObject>().Configure(c => {
                c.HasKey(e => e.Id);
                c.Property(p => p.Id)
                    .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity).IsRequired()
                    .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("LogicalId")));
            });

            var types = GetDataTypes();
            var entityMethod = typeof(DbModelBuilder).GetMethod("Entity");
            foreach (var t in types) {
                entityMethod.MakeGenericMethod(t)
                  .Invoke(modelBuilder, new object[] { });
            }

            //if (OnModelCreatingAction != null) {
            //    OnModelCreatingAction(modelBuilder);
            //}
        }

        /// <summary>
        /// Run Automatic migrations
        /// </summary>
        internal static void UpdateAutoMigrations(string name, DbMigrationsConfiguration migrationConfig) {
            var types = GetDataTypes();
            foreach (var t in types) {
                var d1 = typeof(SingleTableContext<>);
                Type[] typeArgs = { t };
                var makeme = d1.MakeGenericType(typeArgs);
                object[] args = { name };
                object o = Activator.CreateInstance(makeme, args);

                var migrationMethod = makeme.GetMethod("UpdateAutoMigrations");
                var methods = makeme.GetMethods(); //System.Reflection.BindingFlags.NonPublic);
                migrationMethod.Invoke(o, new object[] { name, migrationConfig });
            }
        }


        //http://stackoverflow.com/questions/15820505/dbentityvalidationexception-how-can-i-easily-tell-what-caused-the-error

        public override int SaveChanges() {
            try {
                return base.SaveChanges();
            } catch (DbEntityValidationException ex) {
                // Retrieve the error messages as a list of strings.
                var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => String.Format("Property: {0} Error: {1}", x.PropertyName, x.ErrorMessage));

                // Join the list to a single string.
                var fullErrorMessage = string.Join("; ", errorMessages);

                // Combine the original exception message with the new one.
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                // Throw a new DbEntityValidationException with the improved exception message.
                throw new DbEntityValidationException(exceptionMessage, ex.EntityValidationErrors);
            }
        }
    }



    /// <summary>
    /// Dynamic context with all IData loaded into current AppDomain
    /// </summary>
    public class SingleTableContext<T> : DbContext, IDbContextFactory<SingleTableContext<T>> where T : class {

        private string _name;
        private Type _ty;

        public static Action<EntityTypeConfiguration<T>> OnModelCreatingAction { get; set; }

        public SingleTableContext() : base() {
            _ty = typeof(T);
        }

        /// <summary>
        /// 
        /// </summary>
        public SingleTableContext(string name) //, Type ty)
            : base(name) {
            _name = name;
            _ty = typeof(T);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            // Use IDataObject.Guid as primary key
            modelBuilder.Types<IDataObject>().Configure(c => {
                c.HasKey(e => e.Id);
                c.Property(p => p.Id)
                    .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity).IsRequired()
                    .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("LogicalId")));
            });
            // Use IDistributedDataObject.Guid as primary key
            modelBuilder.Types<IDistributedDataObject>().Configure(c => {
                c.HasKey(e => e.Id);
                c.Property(p => p.Id)
                    .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity).IsRequired()
                    .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("LogicalId")));
            });

            //modelBuilder.RegisterEntityType(_ty);
            //var entityMethod = typeof(DbModelBuilder).GetMethod("Entity");

            var econfig = modelBuilder.Entity<T>();
            OnModelCreatingAction?.Invoke(econfig);
        }

        /// <summary>
        /// Run Automatic migrations
        /// </summary>
        public void UpdateAutoMigrations(string connectionName, DbMigrationsConfiguration migrationConfig) { // < DataContext >
            // 
            if (_ty.TypeInitializer != null) {
                var _tempType = Activator.CreateInstance<T>();
                //_ty.TypeInitializer.Invoke(null, null);
            }
            DbMigrationsConfiguration config = migrationConfig ?? new DbMigrationsConfiguration //<DataContext>
            {
                AutomaticMigrationsEnabled = true,
                AutomaticMigrationDataLossAllowed = false,
            };
            Trace.WriteLine("Migrating " + _ty.Name + " with migrator config: " + config.GetType().ToString());
            config.MigrationsAssembly = _ty.Assembly;
            config.MigrationsNamespace = _ty.Namespace;
            config.ContextKey = _ty.Name;
            config.ContextType = this.GetType();
            config.TargetDatabase = new DbConnectionInfo(connectionName);
            var migrator = new DbMigrator(config);
            migrator.Update();
        }

        public SingleTableContext<T> Create() {
            return new SingleTableContext<T>();
        }
    }


}
#endif