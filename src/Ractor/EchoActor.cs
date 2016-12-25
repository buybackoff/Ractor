using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Ractor {
    public class EchoActor : Actor<string, string> {

        public override async Task<string> Computation(string request) {
            return request;
        }
    }

    public class PythonEchoActor : PythonActor
    {
        public PythonEchoActor() : base("PythonEcho.py") {}
    }
    
}