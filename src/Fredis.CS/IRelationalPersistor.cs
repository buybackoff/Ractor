using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Fredis {

    [Obsolete]
    public interface IRelationalPersistor {

        void CreateTable<T>(bool overwrite = false) where T : new();

        void InsertSingle<T>(T item) where T : IDataObject, new();
        void Insert<T>(List<T> items) where T : IDataObject, new();

        void InsertGetIdSingle<T>(ref T item) where T : IDataObject, new();
        void InsertGetIds<T>(ref List<T> items) where T : IDataObject, new();

        void Update<T>(List<T> items) where T : IDataObject, new();

        void Delete<T>(List<T> items) where T : IDataObject, new();

        List<T> Select<T>(Expression<Func<T, bool>> predicate) where T : IDataObject, new();

        List<T> Select<T>(string sqlFilter, params object[] filterParams) where T : new();

        void ExecuteSql(string sql, bool onShards = false);

        T GetById<T>(long id) where T : IDataObject, new();
        List<T> GetByIds<T>(List<long> ids) where T : IDataObject, new();

    }
}