using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ractor {
    /// <summary>
    /// 
    /// </summary>
    public static class WaitHandleExtensions {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static Task WaitAsync(this WaitHandle handle) {
            return WaitAsync(handle, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static Task<bool> WaitAsync(this WaitHandle handle, TimeSpan timeout) {
            var tcs = new TaskCompletionSource<bool>();
            var registration = ThreadPool.RegisterWaitForSingleObject(handle, (state, timedOut) => {
                var localTcs = (TaskCompletionSource<bool>)state;
                localTcs.TrySetResult(!timedOut);
            }, tcs, timeout, executeOnlyOnce: true);
            tcs.Task.ContinueWith((_, state) => ((RegisteredWaitHandle)state).Unregister(null), registration, TaskScheduler.Default);
            return tcs.Task;
        }
    }
}
