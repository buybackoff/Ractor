using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fredis {
    /// <summary>
    /// Generates unque 64 bit integer
    /// </summary>
    public interface IUniqueLongGenerator {
        /// <summary>
        /// Return new unique long Id
        /// </summary>
        long GetNextId();

    }

    /// <summary>
    /// 50% collision probability after 5bn items, use only for test or small sets
    /// </summary>
    public class DummyLongGenerator : IUniqueLongGenerator {
        public long GetNextId() {
            var bytes = GuidGenerator.GuidArray(0);
            return BitConverter.ToInt64(bytes, 8);
        }
    }

    // TODO
    // since we have redis alway, in Fredis implement this Interface
    // using INCRBY 1000, so each client will have a 1000 unique id before requesting new ones
    // but with many clients will have similar problem with non-monotonical ID as with Guids
    // or will have to make many round trips to redis if we reduce 1000 to 100, 10, 1...

}
