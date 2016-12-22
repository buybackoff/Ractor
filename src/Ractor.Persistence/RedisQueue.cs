using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ractor {

    /// <summary>
    ///
    /// </summary>
    public class RedisQueue<T> : IQueue<T>, IDisposable {
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

        public RedisQueue(Redis redis, string id, int timeout = -1, string group = "") {
            _cts = new CancellationTokenSource();
            _redis = redis;
            _timeout = timeout;
            Id = id;
            _prefix = (group == null ? "" : "{" + group + "}") + id; // aware of Redis cluster with {}
            _inboxKey = _prefix + ":inbox";
            _pipelineKey = _prefix + ":pipeline";
            _lockKey = _prefix + ":lock";
            _channelKey = _prefix + ":channel";

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
                        var n = redis.Eval<int>(pipelineScript, new[] { redis.KeyNameSpace + ":" + _pipelineKey, _inboxKey });
                        Console.WriteLine($"Collected pipelines: {n}");
                    }

                    await Task.Delay(_timeout);
                }
            }, _cts.Token);

            redis.Subscribe(_channelKey,
                new Action<string, string>((channel, messageNotification) => {
                    if (messageNotification == "" && _semaphore.CurrentCount == 0) _semaphore.Release();
                }));
            _started = true;
        }

        public string Id { get; }

        public int Timeout => _timeout / 1000;

        /// <summary>
        ///
        /// </summary>
        public async Task<bool> TrySendMessage(T message) {
            // TODO combine push and publish inside a lua script
            var result = await _redis.LPushAsync<T>(_inboxKey, message, When.Always, false);
            if (result <= 0) return false;
            await _redis.PublishAsync(_channelKey, "", true);
            return true;
        }

        /// <summary>
        ///
        /// </summary>
        public async Task<QueueReceiveResult<T>> TryReceiveMessage() {
            // TODO timeout
            const string lua = @"
                    local result = redis.call('RPOP', KEYS[1])
                    if result ~= nil then
                        redis.call('HSET', KEYS[2], KEYS[3], result)
                    end
                    return result";

            var pipelineId = Guid.NewGuid().ToBase64String();

            while (!_cts.IsCancellationRequested) {
                var message = await _redis.EvalAsync<T>
                    (lua, new[]
                    {
                        _redis.KeyNameSpace + ":" + _inboxKey,
                        _redis.KeyNameSpace + ":" + _pipelineKey,
                        pipelineId
                    });

                if (EqualityComparer<T>.Default.Equals(message, default(T))) {
                    //timeout, if PubSub dropped notification, recheck the queue, but not very often
                    await _semaphore.WaitAsync(10000);
                } else {
                    return new QueueReceiveResult<T> {
                        OK = true,
                        Value = message,
                        DeleteHandle = pipelineId
                    };
                }
            }
            throw new Exception("Should not erach this line");
        }

        /// <summary>
        ///
        /// </summary>
        public async Task<bool> TryDeleteMessage(string deleteHandle) {
            return await _redis.HDelAsync(_pipelineKey, deleteHandle, false);
        }

        /// <summary>
        ///
        /// </summary>
        public void Dispose() {
            _cts.Cancel();
        }
    }
}
