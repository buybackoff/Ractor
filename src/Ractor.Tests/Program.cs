using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Ractor.Tests {
    public class Program {
        public static void Main(string[] args) {
            Run().Wait();
            
        }

        public static async Task Run() {
            using (var actor = new PythonActor("PythonWorker.py", id: "PythonEcho", connectionString: "127.0.0.1")) {
                actor.Start();
                var sw = new Stopwatch();
                var message = "Hello, Python"; //new String('x', 10000); // "Hello, Python";
                sw.Start();
                List<Task<string>> tasks = new List<Task<string>>();
                for (int i = 0; i < 20000; i++) {
                    tasks.Add(actor.PostAndGetResult(i.ToString()));
                    //await Task.Delay(10);
                }
                await Task.WhenAll(tasks);
                sw.Stop();
                for (int i = 0; i < 20000; i++) {
                    if ((i * 2).ToString() != tasks[i].Result) throw new Exception();
                }
                Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds}");
                Console.ReadLine();
            }
            Console.ReadLine();
        }
    }
}
