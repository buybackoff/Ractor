using System;

namespace Fredis {
    /// <summary>
    /// Data objects that could be stored as key-value where value is a serizlized object 
    /// </summary>
    public interface IDistributedDataObject : IDataObject {
        [Index(true), Required]
        Guid Guid { get; set; }
    }
}