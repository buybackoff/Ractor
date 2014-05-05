using System;
using System.IO;
using System.Threading.Tasks;

namespace Fredis {
    public interface IBlobPersistor {
        bool TryPut(Stream stream, out string key);
        Task<Tuple<bool, string>> TryPutAsync(Stream stream);
        bool TryPut(string key, Stream stream);
        bool TryPut<T>(string key, T poco);
        Task<bool> TryPutAsync(string key, Stream stream);
        Task<bool> TryPutAsync<T>(string key, T poco);
        bool TryGet(string key, out Stream stream);
        bool TryGet<T>(string key, out T poco);
        Task<Tuple<bool, Stream>> TryGetAsync(string key);
        Task<Tuple<bool, T>> TryGetAsync<T>(string key);
        bool Exists(string key);
    }
}