using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Common;
using ServiceStack.Text;

namespace Fredis {

    
    /// <summary>
    /// File based persistor on blobs
    /// </summary>
    public class FileBlobPersistor : IBlobPersistor {
        private readonly string _path;

        /// <summary>
        /// File based persistor on blobs
        /// </summary>
        /// <param name="path">Directory to store blobs</param>
        public FileBlobPersistor(string path) {
            _path = path;
        }


        public bool TryPut(Stream stream, out string key) {
            var md5Hash = stream.GetSHA256Hash();
            var length = stream.Length;

            key = md5Hash + length;

            return TryPut(_path, key, stream);
        }

        public async Task<Tuple<bool, string>> TryPutAsync(Stream stream) {
            return await Task.Factory.StartNew(() => {
                var md5Hash = stream.GetSHA256Hash();
                var length = stream.Length;
                var key = md5Hash + length;
                var res = TryPut(_path, key, stream);
                return Tuple.Create(res, key);
            });

        }


        public bool TryPut(string key, Stream stream) {
            return TryPut(_path, key, stream);
        }

        public bool TryPut<T>(string key, T poco) {
            Stream stream = new MemoryStream(poco.ToJsv().GZip());
            return TryPut(_path, key, stream);
        }

        public async Task<bool> TryPutAsync(string key, Stream stream) {
            return await TryPutAsync(_path, key, stream);
        }

        public async Task<bool> TryPutAsync<T>(string key, T poco) {
            return await Task.Factory.StartNew(() => {

                Stream stream = new MemoryStream(poco.ToJsv().GZip());
                var res = TryPut(_path, key, stream);
                return res;
            });
        }



        public bool TryGet(string key, out Stream stream) {
            return TryGet(_path, key, out stream);
        }


        public bool TryGet<T>(string key, out T poco) {
            poco = default(T);
            Stream stream;
            var res = TryGet(_path, key, out stream);
            if (!res) return false;
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            poco = ms.ToArray().GUnzip().FromJsv<T>();
            return true;
        }


        public async Task<Tuple<bool, Stream>> TryGetAsync(string key) {
            return await Task.Factory.StartNew(() => {
                Directory.CreateDirectory(_path);
                var filename = Path.Combine(_path, key);
                if (!File.Exists(filename)) return Tuple.Create<bool, Stream>(false, null);
                Stream stream = File.OpenRead(filename);
                return Tuple.Create(true, stream);
            });
        }

        public async Task<Tuple<bool, T>> TryGetAsync<T>(string key) {
            return await Task.Factory.StartNew(() => {
                var poco = default(T);
                MemoryStream ms;
                var suc = TryGet(key, out ms);
                if (!suc) return Tuple.Create(false, poco);
                poco = ms.ToArray().GUnzip().FromJsv<T>();
                return Tuple.Create(true, poco);
            });
        }



        public bool Exists(string key) {
            return Exists(_path, key);
        }


        public static bool TryPut(string path, string key, Stream stream) {
            Directory.CreateDirectory(path);
            var filename = Path.Combine(path, key);

            if (File.Exists(filename)) File.Delete(filename);

            using (var destinationStream = File.Create(filename)) {
                stream.CopyTo(destinationStream);
            }

            return true;
        }


        public static async Task<bool> TryPutAsync(string path, string key, Stream stream) {
            return await Task.Factory.StartNew(() => {

                Directory.CreateDirectory(path);
                var filename = Path.Combine(path, key);

                if (File.Exists(filename)) File.Delete(filename);

                using (var destinationStream = File.Create(filename)) {
                    stream.CopyTo(destinationStream);
                }

                return true;
            });

        }



        public static bool TryGet(string path, string key, out Stream stream) {
            stream = null;

            Directory.CreateDirectory(path);
            var filename = Path.Combine(path, key);
            if (!File.Exists(filename)) return false;
            stream = File.OpenRead(filename);
            return true;
        }


        public static async Task<Tuple<bool, Stream>> TryGetAsync(string path, string key) {
            return await Task.Factory.StartNew(() => {
                Directory.CreateDirectory(path);
                var filename = Path.Combine(path, key);
                if (!File.Exists(filename)) return Tuple.Create<bool, Stream>(false, null);
                Stream stream = File.OpenRead(filename);
                return Tuple.Create(true, stream);
            });
        }


        public static bool Exists(string path, string key) {
            Directory.CreateDirectory(path);
            var filename = Path.Combine(path, key);
            return File.Exists(filename);
        }
    }


    // Thanks to John Skeet
    // https://msmvps.com/blogs/jon_skeet/archive/2011/05/17/eduasync-part-5-making-task-lt-t-gt-awaitable.aspx

    internal static class TaskExtensions {
        public static TaskAwaiter<T> GetAwaiter<T>(this Task<T> task) {
            return new TaskAwaiter<T>(task);
        }
    }

    internal struct TaskAwaiter<T> {
        private readonly Task<T> _task;

        internal TaskAwaiter(Task<T> task) {
            this._task = task;
        }

        public bool IsCompleted { get { return _task.IsCompleted; } }

        public void OnCompleted(Action action) {
            SynchronizationContext context = SynchronizationContext.Current;
            TaskScheduler scheduler = context == null ? TaskScheduler.Current
                : TaskScheduler.FromCurrentSynchronizationContext();
            _task.ContinueWith(ignored => action(), scheduler);
        }

        public T GetResult() {
            return _task.Result;
        }
    }
}