using System;
using NUnit.Framework;

namespace Ractor.Persistence.Tests {
    [TestFixture]
    public class SerializationTests {
        [Test]
        public void SerializePrimitives() {
            Console.WriteLine("test".ToJson());
            Console.WriteLine(123.ToJson());
            Console.WriteLine(DateTime.Now.ToJson());
            string s = null;
            var ser = s.ToJson();
            //var bytes = Encoding.UTF8.GetBytes(ser);
            Console.WriteLine(ser == null);
        }
    }
}
