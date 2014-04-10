using System;

namespace Fredis {

    /// <summary>
    /// Use one table to store a hierarchy of POCOs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TablePerHierarchyAttribute : Attribute {
        /// <summary>
        /// A POCO that defines storage table
        /// </summary>
        public Type TableDefinition { get; set; }

        public TablePerHierarchyAttribute(Type tableDefinition) {
            TableDefinition = tableDefinition;
        }

    }
}
