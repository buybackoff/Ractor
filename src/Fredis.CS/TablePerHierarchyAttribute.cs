using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fredis {

    /// <summary>
    /// Use one table to store a hierarchy of POCOs. Google "Table Per Hierarchy" for details
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TablePerHierarchyAttribute {
        /// <summary>
        /// A POCO that defines storage table
        /// </summary>
        public Type TableDefinition { get; set; }

        public TablePerHierarchyAttribute(Type tableDefinition) {
            TableDefinition = tableDefinition;
        }

    }
}
