using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Ractor {

    /// <summary>
    /// Redis-based dictionary to store and retrieve values asynchronously by a key
    /// </summary>
    public class RedisAsyncDictionary<T> : IAsyncDictionary<T>, IDisposable where T : class {
        private readonly CancellationTokenSource _cts;
        private readonly Redis _redis;
        private readonly int _timeout;
        private readonly string _prefix;
        private readonly RedisChannel _redisChannel;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _listeners =
            new ConcurrentDictionary<string, TaskCompletionSource<bool>>();

        /// <summary>
        ///
        /// </summary>
        public RedisAsyncDictionary(Redis redis, string id, int timeout = -1, string group = "") {
            _cts = new CancellationTokenSource();
            _redis = redis;
            _timeout = timeout;
            Id = id;
            Group = group;
            _prefix = (string.IsNullOrWhiteSpace(group) ? "" : "{" + group + "}:") + id + ":asyncdictionary:";

            // [_prefix][id]
            // NB using keyspace notifications, must be enabled in settings with minimum "K$"
            // https://redis.io/topics/notifications
            var channelKey = $"__keyspace@{redis.Database}__:" + _prefix + "*";

            _redisChannel = new RedisChannel(channelKey, RedisChannel.PatternMode.Pattern);
            redis.KeyspaceEventSubscribe(_redisChannel,
                (channel, key) => {
                    var resultId = key.Substring(key.LastIndexOf(':') + 1);
                    TaskCompletionSource<bool> tcs;
                    if (_listeners.TryGetValue(resultId, out tcs)) {
                        // notify awaiter that its result is ready
                        tcs.TrySetResult(true);
                    }
                });
        }

        /// <summary>
        ///
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///
        /// </summary>
        public string Group { get; }

        /// <summary>
        ///
        /// </summary>
        public int Timeout => _timeout / 1000;

        /// <summary>
        ///
        /// </summary>
        public string Prefix => _prefix;

        /// <summary>
        ///
        /// </summary>
        public async Task<bool> TryFill(string key, T value) {
            var fullKey = _prefix + key;
            return await _redis.SetAsync<T>(fullKey, value, (_timeout > 0 ? TimeSpan.FromMilliseconds(_timeout) : (TimeSpan?)null), When.Always, false);
        }

        /// <summary>
        ///
        /// </summary>
        public async Task<T> TryTake(string key) {
            const string lua = @"
                    local result = redis.call('GET', KEYS[1])
                    if result ~= nil then
                        redis.call('DEL', KEYS[1])
                    end
                    return result";

            var fullKey = _prefix + key;
            var attemts = 0;
            var cumulativeTimeout = 0;
            while (!_cts.IsCancellationRequested) {
                var result = await _redis.EvalAsync<T>
                    (lua, new[]
                    {
                        _redis.KeyNameSpace + fullKey
                    }); //_redis.GetAsync<T>(fullKey);
                if (result == null) {
                    var timeout = (int)Math.Pow(2, Math.Min(attemts + 7, 13));
                    var tcs = _listeners.GetOrAdd(key, k => new TaskCompletionSource<bool>());
                    Trace.Assert(tcs.Task.Status != TaskStatus.RanToCompletion, "TCS is completed only by a key event which means the result should not be null");
                    var delay = Task.Delay(timeout);
                    // we dont'care who was the first, we recheck the result either on a signal or on retry timeout, but need to check for timeout
                    var t = await Task.WhenAny(tcs.Task, delay);
                    if (t == delay) {
                        cumulativeTimeout += timeout;
                        if (_timeout > 0 && cumulativeTimeout > _timeout) {
                            // NB if timeout is set, we will be there right after Redis evicts a value
                            // because we touch the value with the GET call
                            throw new TimeoutException();
                        }
                    }
                    attemts++;
                } else {
                    return result;
                }
            }
            throw new TaskCanceledException();
        }

        /// <summary>
        ///
        /// </summary>
        public void Dispose() {
            _redis.Unsubscribe(_redisChannel);
            _cts.Cancel();
        }
    }
}
