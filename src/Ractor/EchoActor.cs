using System.Threading.Tasks;

namespace Ractor {

    public class EchoActor : Actor<string, string> {

        public override async Task<string> Computation(string request) {
            await Task.Delay(0);
            return request;
        }
    }

    public class PythonEchoActor : PythonActor {

        public PythonEchoActor() : base("PythonWorker.py", id: "PythonEcho", maxConcurrencyPerCpu: 1) {
        }
    }

}