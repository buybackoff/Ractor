using System;

namespace Fredis {

    // Instead use schemaless type extensions, e.g. User and User.UserInfo
    // and instead of hierarchy just add properties - will be stored in serialized format
    // Other usage of TPH are unclear

    /// <summary>
    /// Use one table to store a hierarchy of POCOs.
    /// </summary>
    [Obsolete] // TODO delete or implement? with automaping should not be that complex...
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
