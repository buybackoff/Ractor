using System;

namespace Ractor {
    /// <summary>
    /// Distributed queue (AWS SQS model)
    /// </summary>
    public interface IQueue<T> {
        ISerializer Serializer { get; set; }
        bool TrySendMessage(T message);
        bool TryReceiveMessage(out Tuple<T, string> messageWithDeleteHandle);
        bool TryDeleteMessage(string deleteHandle);
    }
}
