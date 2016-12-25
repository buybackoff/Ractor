using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ractor {
    public class Message<T> {
        public T Value { get; set; }
        public bool HasError { get; set; }
        public Exception Error { get; set; }
    }
}
