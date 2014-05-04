using System;
using System.Threading.Tasks;

namespace Fredis.CS.Tests {
    class Program {
        static void Main(string[] args) {

            var f = new Fredis("localhost");

            var greeter = f.CreateActor<string, bool>("greeter", (input) => Task.Run(() => {
                Console.WriteLine("Hello, " + input);
                return true;
            }));

            greeter.Post<string>("C#");

        }
    }
}
