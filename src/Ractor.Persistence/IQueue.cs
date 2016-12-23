using System.Threading.Tasks;

namespace Ractor {

    public struct QueueSendResult {

        /// <summary>
        /// Send result status
        /// </summary>
        public bool Ok { get; internal set; }

        /// <summary>
        /// Unique ID for a message inside a particular queue
        /// </summary>
        public string Id { get; internal set; }
    }

    public struct QueueReceiveResult<T> where T : class {

        /// <summary>
        ///
        /// </summary>
        public bool Ok { get; internal set; }

        public T Value { get; internal set; }

        public string Id { get; internal set; }

        public string DeleteHandle { get; internal set; }
    }

    /// <summary>
    /// Distributed queue (AWS SQS model)
    /// </summary>
    public interface IQueue<T> where T : class {
        int Timeout { get; }

        Task<QueueSendResult> TrySendMessage(T message);

        Task<QueueReceiveResult<T>> TryReceiveMessage();

        Task<bool> TryDeleteMessage(string deleteHandle);
    }


    public interface IAsyncDictionary<T> where T : class {
        int Timeout { get; }

        Task<bool> TryFill(string key, T value);

        Task<T> TryTake(string key);
    }
}
