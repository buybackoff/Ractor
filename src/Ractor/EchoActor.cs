using System.Threading.Tasks;

namespace Ractor
{
    public class EchoActor : Actor<string, string> {
        public EchoActor(string connectionString) : base(connectionString) {
        }

        public override async Task<string> Computation(string request) {
            return request;
        }
    }
}