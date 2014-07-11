using System;

namespace Ractor {

    // TODO remove most of the attributes

    // SS attributes to hide RDBMS's ORM implementation
    // We will always depend on SS but must be able to use other IDBPersistor implementation


    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
    public class AliasAttribute : ServiceStack.DataAnnotations.AliasAttribute {
        public AliasAttribute(string name) : base(name) { }
    }


    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class AutoIncrementAttribute : ServiceStack.DataAnnotations.AutoIncrementAttribute {
    }


    [AttributeUsage(AttributeTargets.Property)]
    public class BelongToAttribute : ServiceStack.DataAnnotations.BelongToAttribute {
        public BelongToAttribute(Type belongToTableType) : base(belongToTableType) { }
    }


    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class CompositeIndexAttribute : ServiceStack.DataAnnotations.CompositeIndexAttribute {
        public CompositeIndexAttribute(params string[] fieldNames) : base(fieldNames) { }
        public CompositeIndexAttribute(bool unique, params string[] fieldNames) : base(unique, fieldNames) { }
    }


    [AttributeUsage(AttributeTargets.Property)]
    public class ComputeAttribute : ServiceStack.DataAnnotations.ComputeAttribute {
        public ComputeAttribute(string expression) : base(expression) { }
    }


    [AttributeUsage(AttributeTargets.Property)]
    public class DecimalLengthAttribute : ServiceStack.DataAnnotations.DecimalLengthAttribute {
        public DecimalLengthAttribute(int precision, int scale) : base(precision, scale) { }
        public DecimalLengthAttribute(int precision) : base(precision) { }
        public DecimalLengthAttribute() { }
    }


    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class DefaultAttribute : ServiceStack.DataAnnotations.DefaultAttribute {
        public DefaultAttribute(int intValue) : base(intValue) { }
        public DefaultAttribute(double doubleValue) : base(doubleValue) { }
        public DefaultAttribute(Type defaultType, string defaultValue) : base(defaultType, defaultValue) { }
    }



    [AttributeUsage(AttributeTargets.Property)]
    public class ForeignKeyAttribute : ServiceStack.DataAnnotations.ForeignKeyAttribute {
        public ForeignKeyAttribute(Type type) : base(type) { }
    }


    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreAttribute : ServiceStack.DataAnnotations.IgnoreAttribute {

    }


    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
    public class IndexAttribute : ServiceStack.DataAnnotations.IndexAttribute {
        public IndexAttribute(bool unique) : base(unique) { }
    }


    public class MetaAttribute : ServiceStack.DataAnnotations.MetaAttribute {
        public MetaAttribute(string name, string value) : base(name, value) { }
    }

    /// <summary>
    /// Execute the provided SQL instead of default table generations. All other attributes 
    /// that affect schema generation are ignored
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class CreateTableAttribute : Attribute {
        public string Sql { get; set; }

        public CreateTableAttribute(string sql) { Sql = sql; }
    }

    /// <summary>
    /// Execute custom SQL code after a table is created
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AfterCreateTableAttribute : Attribute {
        public string Sql { get; set; }
        public AfterCreateTableAttribute(string sql) { Sql = sql; }
    }

    /// <summary>
    /// Execute custom SQL code after a table is dropped
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AfterDropTableAttribute : Attribute {
        public string Sql { get; set; }
        public AfterDropTableAttribute(string sql) { Sql = sql; }
    }

    /// <summary>
    /// Execute custom SQL code before a table is created
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class BeforeCreateTableAttribute : Attribute {
        public string Sql { get; set; }
        public BeforeCreateTableAttribute(string sql) { Sql = sql; }
    }

    /// <summary>
    /// Execute custom SQL code before a table is dropped
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class BeforeDropTableAttribute : Attribute {
        public string Sql { get; set; }
        public BeforeDropTableAttribute(string sql) { Sql = sql; }
    }


    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryKeyAttribute : ServiceStack.DataAnnotations.PrimaryKeyAttribute {

    }


    public class RangeAttribute : System.ComponentModel.DataAnnotations.RangeAttribute {
        public RangeAttribute(int min, int max) : base(min, max) { }
        public RangeAttribute(double min, double max) : base(min, max) { }
        public RangeAttribute(Type type, string min, string max) : base(type, min, max) { }
    }





    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
    public class ReferencesAttribute : ServiceStack.DataAnnotations.ReferencesAttribute {
        public ReferencesAttribute(Type type) : base(type) { }
    }


    public class RequiredAttribute : System.ComponentModel.DataAnnotations.RequiredAttribute {

    }


    [AttributeUsage(AttributeTargets.Class)]
    public class SchemaAttribute : ServiceStack.DataAnnotations.SchemaAttribute {
        public SchemaAttribute(string name) : base(name) { }
    }


    [AttributeUsage(AttributeTargets.Property)]
    public class SequenceAttribute : ServiceStack.DataAnnotations.SequenceAttribute {
        public SequenceAttribute(string name) : base(name) { }
    }


    public class StringLengthAttribute : System.ComponentModel.DataAnnotations.StringLengthAttribute {
        public StringLengthAttribute(int maximumLength) : base(maximumLength) { }
    }

}
