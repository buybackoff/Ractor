using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Ractor
{
    public class PythonActor : Actor<string, string> {
        private readonly string _pythonScriptPath;

        public PythonActor(
            string pythonScriptPath = "",
            string connectionString = "localhost",
            string id = null,
            string group = null,
            int timeout = 60000,
            int priority = 0,
            byte maxConcurrencyPerCpu = 1)
            : base(connectionString, id, "py:" + group, timeout, priority, maxConcurrencyPerCpu)
        {
            _pythonScriptPath = pythonScriptPath;
        }

        public override async Task<string> Computation(string request) {
            var start = new ProcessStartInfo {
                FileName = "python",
                Arguments = _pythonScriptPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            var process = Process.Start(start);

            await process.StandardInput.WriteAsync(request);
            process.StandardInput.Close();
            process.WaitForExit();

            var result = await process.StandardOutput.ReadLineAsync();
            var error = await process.StandardError.ReadToEndAsync();
            var code = process.ExitCode;
            if (code != 0) {
                throw new Exception(error);
            }
            return result;
        }
    }
}