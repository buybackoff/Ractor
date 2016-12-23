using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ractor {
    public class RedisAsyncDictionary<T> : IAsyncDictionary<T>, IDisposable where T : class
    {
        private readonly CancellationTokenSource _cts;
        private readonly Redis _redis;
        private readonly int _timeout;
        private string _prefix;
        private readonly string _hashSetKey;
        private string _channelKey;

        public RedisAsyncDictionary(Redis redis, string id, int timeout = -1, string group = "")
        {
            _cts = new CancellationTokenSource();
            _redis = redis;
            _timeout = timeout;
            Id = id;
            _prefix = (string.IsNullOrWhiteSpace(group) ? "" : "{" + group + "}:") + id;
            // aware of Redis cluster with {}
            _hashSetKey = _prefix + ":asyncdictionary";
            _channelKey = _prefix + ":asyncdictionary_channel";

            
        }


        public string Id { get; }

        public int Timeout => _timeout / 1000;

        public string Prefix => _prefix;

        public Task<bool> TryFill(string key, T value)
        {
            throw new NotImplementedException();
        }

        public Task<T> TryTake(string key)
        {
            // 
            throw new NotImplementedException();
        }


        /// <summary>
        ///
        /// </summary>
        public void Dispose() {
            _cts.Cancel();
        }
    }
}
