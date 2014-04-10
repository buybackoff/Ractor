using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Fredis {

    [Obsolete]
    public interface IDistributedPersistor {

        void CreateTable<T>(bool overwrite = false) where T : IDistributedDataObject, new();
        
        void Insert<T>(List<T> items) where T : IDistributedDataObject, new();
        
        T GetById<T>(long id) where T : IDistributedDataObject, new();
        List<T> GetByIds<T>(List<long> ids) where T : IDistributedDataObject, new();

        void Update<T>(List<T> items) where T : IDistributedDataObject, new();

        void Delete<T>(List<T> items) where T : IDistributedDataObject, new();

        List<T> Select<T>(Expression<Func<T, bool>> predicate) where T : IDistributedDataObject, new();

    }

}
