using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Ractor {

    public class PythonActor : Actor<string, string> {
        private readonly string _pythonScriptPath;
        private ProcessStartInfo _start;
        private byte _maxConcurrencyPerCpu;

        public PythonActor(
            string pythonScriptPath = "",
            string connectionString = "localhost",
            string id = null,
            string group = null,
            int timeout = 60000,
            int priority = 0,
            byte maxConcurrencyPerCpu = 1)
            : base(connectionString, id, group, timeout, priority, maxConcurrencyPerCpu) {
            _maxConcurrencyPerCpu = maxConcurrencyPerCpu;
            _pythonScriptPath = pythonScriptPath;
            _start = new ProcessStartInfo {
                FileName = "python",
                Arguments = _pythonScriptPath,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardInput = false,
                RedirectStandardError = false,
                CreateNoWindow = false
            };
        }

        public override Task<string> Computation(string request) {
            // Loop
            //var process = Process.Start(_start);

            //await process.StandardInput.WriteAsync(request);
            //process.StandardInput.Close();
            //process.WaitForExit();

            //var result = await process.StandardOutput.ReadLineAsync();
            //var error = await process.StandardError.ReadToEndAsync();
            //var code = process.ExitCode;
            //if (code != 0) {
            //    throw new Exception(error);
            //}
            //return result;
            throw new NotSupportedException("Computations are processed by Python");
        }

        internal override void Loop(CancellationTokenSource cts) {
            var procs = new List<Process>();
            for (var i = 0; i < Math.Max(1, Environment.ProcessorCount * MaxConcurrencyPerCpu); i++) {
                procs.Add(Process.Start(_start));
            }
            cts.Token.Register(() => {
                foreach (var process in procs) {
                    process.Kill();
                }
            });
        }
    }
}