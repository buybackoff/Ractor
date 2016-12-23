using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StackExchange.Redis;


// WIP
//          <T>     <T>Async    key     keyAsycn    Tests
// PUB      x                  x


namespace Ractor {

    public partial class Redis {
        public long Publish<TMessage>(string channel, TMessage message, bool fireAndForget = false) {
            var sub = ConnectionMultiplexer.GetSubscriber();
            var m = PackValueNullable(message);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return sub.Publish(channel, m, ff);
        }

        public async Task<long> PublishAsync<TMessage>(string channel, TMessage message, bool fireAndForget = false) {
            var sub = ConnectionMultiplexer.GetSubscriber();
            var m = PackValueNullable(message);
            var ff = fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None;
            return await sub.PublishAsync(channel, m, ff);
        }

        public void Subscribe<TMessage>(string channel, Action<string, TMessage> handler) {
            var sub = ConnectionMultiplexer.GetSubscriber();
            sub.Subscribe(channel, (ch, v) => {
                var message = UnpackResultNullable<TMessage>(v);
                handler(channel, message);
            });
        }

        // NB Keyspace events are not JSON and do not need deserialization, which fails on them
        public void KeyspaceEventSubscribe(string channel, Action<string, string> handler) {
            var sub = ConnectionMultiplexer.GetSubscriber();
            sub.Subscribe(channel, (ch, v) => {
                handler(channel, v);
            });
        }

        public void KeyspaceEventSubscribe(RedisChannel channel, Action<string, string> handler) {
            var sub = ConnectionMultiplexer.GetSubscriber();
            sub.Subscribe(channel, (ch, v) => {
                handler(channel, v);
            });
        }

        public async Task SubscribeAsync<TMessage>(string channel, Action<string, TMessage> handler) {
            var sub = ConnectionMultiplexer.GetSubscriber();
            await sub.SubscribeAsync(channel, (ch, v) => {
                var message = UnpackResultNullable<TMessage>(v);
                handler(channel, message);
            });
        }

        public void Unsubscribe(RedisChannel channel) {
            var sub = ConnectionMultiplexer.GetSubscriber();
            sub.Unsubscribe(channel);
        }

        public void Unsubscribe(string channel) {
            var sub = ConnectionMultiplexer.GetSubscriber();
            if (channel == null) {
                sub.UnsubscribeAll();
            } else {
                sub.Unsubscribe(channel);
            }
        }

        public async Task UnsubscribeAsync(string channel) {
            var sub = ConnectionMultiplexer.GetSubscriber();
            if (channel == null) {
                await sub.UnsubscribeAllAsync();
            } else {
                await sub.UnsubscribeAsync(channel);
            }
        }


    }
}
