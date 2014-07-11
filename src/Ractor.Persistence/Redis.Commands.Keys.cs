using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// WIP
//          <T>     <T>Async    key     keyAsycn    Tests
// 
using System.Threading.Tasks;


namespace Ractor {
    public partial class Redis {


        #region Exists

        public bool Exists(string fullKey) {
            var k = _nameSpace + fullKey;
            var result = GetDb().KeyExists(k);
            return result;
        }

        public async Task<bool> ExistsAsync(string fullKey) {
            var k = _nameSpace + fullKey;
            var result = await GetDb().KeyExistsAsync(k);
            return result;
        }

        #endregion
        

    }
}
