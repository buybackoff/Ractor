using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ractor {

    /// <summary>
    /// Persistent MPMC Redis queue
    /// </summary>
    public class RedisQueue<T> : IQueue<T>, IDisposable where T : class {
        private readonly CancellationTokenSource _cts;
        private readonly Redis _redis;
        private readonly int _timeout;
        private string _prefix;
        private readonly string _inboxKey;
        private readonly string _lockKey;
        private readonly string _pipelineKey;
        private string _channelKey;
        private bool _started;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0, 1);

        /// <summary>
        ///
        /// </summary>
        public RedisQueue(Redis redis, string id, int timeout = -1, string group = "") {
            _cts = new CancellationTokenSource();
            _redis = redis;
            _timeout = timeout;
            Id = id;
            Group = group;
            _prefix = (string.IsNullOrWhiteSpace(group) ? "" : "{" + group + "}:") + id + ":"; // aware of Redis cluster with {}
            _inboxKey = _prefix + "inbox";
            _pipelineKey = _prefix + "pipeline";
            _lockKey = _prefix + "lock";
            // NB using keyspace notifications, must be enabled in settings with minimum "Kl"
            // https://redis.io/topics/notifications
            _channelKey = $"__keyspace@{redis.Database}__:" + redis.KeyNameSpace + _inboxKey;

            Task.Run(async () => {
                while (!_cts.Token.IsCancellationRequested) {
                    const string pipelineScript = @"
                        local previousKey = KEYS[1]..':previousKeys'
                        local currentKey = KEYS[1]..':currentKeys'
                        local currentItems = redis.call('HKEYS', KEYS[1])
                        local res = 0
                        redis.call('DEL', currentKey)
                        if redis.call('HLEN', KEYS[1]) > 0 then
                           redis.call('SADD', currentKey, unpack(currentItems))
                           local intersect
                           if redis.call('SCARD', previousKey) > 0 then
                               intersect = redis.call('SINTER', previousKey, currentKey)
                               if #intersect > 0 then
                                    local values = redis.call('HMGET', KEYS[1], unpack(intersect))
                                    redis.call('RPUSH', KEYS[2], unpack(values))
                                    redis.call('HDEL', KEYS[1], unpack(intersect))
                                    res = #intersect
                               end
                           end
                        end
                        redis.call('DEL', previousKey)
                        if #currentItems > 0 then
                            redis.call('SADD', previousKey, unpack(currentItems))
                        end
                        return res
                        ";

                    var expiry = TimeSpan.FromMilliseconds(_timeout);
                    var entered = redis.Set<string>(_lockKey, "collecting garbage",
                        expiry, When.NotExists, false);
                    //Console.WriteLine("checking if entered: " + entered.ToString())
                    if (entered && _started) {
                        // ReSharper disable once UnusedVariable
                        var n = redis.Eval<int>(pipelineScript, new[] { redis.KeyNameSpace + _pipelineKey, _inboxKey });
                        //Console.WriteLine($"Returned from pipeline: {n}");
                    }

                    await Task.Delay(_timeout);
                }
            }, _cts.Token);

            redis.KeyspaceEventSubscribe(_channelKey,
                (channel, messageNotification) => {
                    if ((messageNotification == "lpush" ||
                         messageNotification == "rpush")
                        && _semaphore.CurrentCount == 0) {
                        _semaphore.Release();
                        //Console.WriteLine("Released semaphore with events");
                    }
                });
            _started = true;
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
        public string KeyNameSpace => _redis.KeyNameSpace;

        /// <summary>
        ///
        /// </summary>
        public async Task<QueueSendResult> TrySendMessage(T message) {
            var id = Guid.NewGuid().ToBase64String();
            var messageWithId = new QueueMessageWithId<T> {
                Id = id,
                Payload = message
            };
            // NB with FF, result is alway 0, keep it false, serves as ACK
            var result = await _redis.LPushAsync(_inboxKey, messageWithId, When.Always, false);
            if (result <= 0) return new QueueSendResult {
                Id = null,
                Ok = false
            };
            return new QueueSendResult {
                Id = id,
                Ok = true
            };
        }

        /// <summary>
        ///
        /// </summary>
        public async Task<QueueReceiveResult<T>> TryReceiveMessage() {
            const string lua = @"
                    local result = redis.call('RPOP', KEYS[1])
                    if result ~= nil then
                        redis.call('HSET', KEYS[2], KEYS[3], result)
                    end
                    return result";

            var pipelineId = Guid.NewGuid().ToBase64String();
            var attemts = 0;
            var cumulativeTimeout = 0;
            while (!_cts.IsCancellationRequested) {
                var messageWithId = await _redis.EvalAsync<QueueMessageWithId<T>>
                    (lua, new[]
                    {
                        _redis.KeyNameSpace + _inboxKey,
                        _redis.KeyNameSpace + _pipelineKey,
                        pipelineId
                    });

                if (messageWithId == null || EqualityComparer<T>.Default.Equals(messageWithId.Payload, default(T))) {
                    //timeout, if PubSub dropped notification, recheck the queue, but not very often
                    var timeout = (int)Math.Pow(2, Math.Min(attemts + 3, 13));
                    var signal = await _semaphore.WaitAsync(timeout);
                    if (!signal) {
                        cumulativeTimeout += timeout;
                        if (_timeout > 0 && cumulativeTimeout > _timeout) {
                            throw new TimeoutException();
                        }
                    }
                    attemts++;
                    //Console.WriteLine($"Attempt: {attemts}");
                } else {
                    return new QueueReceiveResult<T> {
                        Ok = true,
                        Id = messageWithId.Id,
                        Value = messageWithId.Payload,
                        DeleteHandle = pipelineId
                    };
                }
            }
            throw new TaskCanceledException();
        }

        /// <summary>
        ///
        /// </summary>
        public async Task<bool> TryDeleteMessage(string deleteHandle) {
            // FF is true, therefore on disconnect we could get a false ACK
            // and then a message will be returned to the inbox.
            // That is, we have implicit "at least once" delivery, not "exactly once"
            // That is simpler, and we actually do not use the result of this call
            // now, just call it to clean up. Without FF, the simple first benchmark is 80% slower
            return await _redis.HDelAsync(_pipelineKey, deleteHandle, true);
        }

        /// <summary>
        ///
        /// </summary>
        public void Dispose() {
            _redis.Unsubscribe(_channelKey);
            _cts.Cancel();
        }
    }

    internal class QueueMessageWithId<T> {

        /// <summary>
        ///
        /// </summary>
        [JsonProperty("i")]
        public string Id { get; set; }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty("p")]
        public T Payload { get; set; }
    }
}
