using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;

namespace Ractor {

    public class BaseActor {
        protected static readonly Dictionary<string, Redis> SharedConnections = new Dictionary<string, Redis>();
        protected static readonly QueuedTaskScheduler TopScheduler = new QueuedTaskScheduler();
    }

    public abstract class Actor<TReq, TResp> : BaseActor {
        private CancellationTokenSource _cts;
        private SemaphoreSlim _semaphore;
        private readonly RedisQueue<Message<TReq>> _queue;
        private readonly RedisAsyncDictionary<Message<TResp>> _results;
        private readonly TaskScheduler _scheduler;

        /// <summary>
        /// 
        /// </summary>
        protected Actor(string connectionString = "localhost",
                        string id = null,
                        string group = null,
                        int timeout = 60000,
                        int priority = 0,
                        byte maxConcurrencyPerCpu = 1) {
            Redis redis;
            if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
            if (!SharedConnections.TryGetValue(connectionString, out redis)) {
                redis = new Redis(connectionString, "R");
                SharedConnections[connectionString] = redis;
            }

            Id = string.IsNullOrWhiteSpace(id) ? this.GetType().Name : id;
            Group = @group ?? "";
            Timeout = timeout;
            Priority = priority;
            MaxConcurrencyPerCpu = maxConcurrencyPerCpu;

            _scheduler = TopScheduler.ActivateNewQueue(Priority);

            _queue = new RedisQueue<Message<TReq>>(redis, Id, Timeout, Group);
            _results = new RedisAsyncDictionary<Message<TResp>>(redis, Id, Timeout, Group);
        }

        public string Id { get; }

        public string Group { get; }

        public int Timeout { get; }

        public int Priority { get; }

        public byte MaxConcurrencyPerCpu { get; }

        public abstract Task<TResp> Computation(TReq request);

        public void Start() {
            _cts = new CancellationTokenSource();
            var maximumConcurrency = Math.Max(1, Environment.ProcessorCount * MaxConcurrencyPerCpu);
            _semaphore = new SemaphoreSlim(maximumConcurrency, maximumConcurrency);

            Task.Run(async () => {
                while (!_cts.IsCancellationRequested) {
                    await _semaphore.WaitAsync(_cts.Token);
                    var message = await _queue.TryReceiveMessage();
                    // ReSharper disable once UnusedVariable
                    var task = Task.Factory.StartNew(async state => {
                        var queueReceiveResult = (QueueReceiveResult<Message<TReq>>)state;
                        var m = queueReceiveResult.Value;
                        Message<TResp> response;
                        try {
                            if (m.HasError) {
                                response = new Message<TResp> {
                                    HasError = true,
                                    Error = m.Error,
                                    Value = default(TResp)
                                };
                            } else {
                                response = new Message<TResp> {
                                    HasError = false,
                                    Error = null,
                                    Value = await Computation(m.Value)
                                };
                            }
                        } catch (Exception e) {
                            response = new Message<TResp> {
                                HasError = true,
                                Error = e,
                                Value = default(TResp)
                            };
                        }
                        await _results.TryFill(queueReceiveResult.Id, response);
                        _semaphore.Release();
                    }, message, _cts.Token, TaskCreationOptions.None, _scheduler);
                }
            }, _cts.Token);

        }

        public void Stop() {
            _cts.Cancel();
        }


        public async Task<string> Post(TReq request) {
            var result = await _queue.TrySendMessage(new Message<TReq> { Value = request });
            return result.Id;
        }

        public async Task<TResp> GetResult(string resultId) {
            var result = await _results.TryTake(resultId);
            if (result.HasError) {
                throw result.Error;
            }
            return result.Value;
        }

        public async Task<TResp> PostAndGetResult(TReq request) {
            var resultId = await Post(request);
            return await GetResult(resultId);
        }

    }
}
