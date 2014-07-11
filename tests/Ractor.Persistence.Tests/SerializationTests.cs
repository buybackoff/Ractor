using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Text;

namespace Ractor.Persistence.Tests {
    [TestFixture]
    public class SerializationTests {
        [Test]
        public void SerializePrimitives() {
            Console.WriteLine("test".ToJsv());
            Console.WriteLine(123.ToJsv());
            Console.WriteLine(DateTime.Now.ToJsv());
            string s = null;
            var ser = s.ToJsv();
            //var bytes = Encoding.UTF8.GetBytes(ser);
            Console.WriteLine(ser == null);
        }
    }
}
