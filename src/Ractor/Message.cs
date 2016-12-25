using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ractor {
    public class Message<T> {
        [JsonProperty("v")]
        public T Value { get; set; }
        [JsonProperty("h")]
        public bool HasError { get; set; }
        [JsonProperty("e")]
        public Exception Error { get; set; }
    }
}
