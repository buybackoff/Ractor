using ServiceStack.Common;
using ServiceStack.Text;
using StackExchange.Redis;

// WIP
//          <T>     <T>Async    key     keyAsycn    Tests
// LIndex


namespace Fredis {

    public partial class Redis {

        /// <summary>
        /// Returns the element at index index in the list stored at key. The index is 
        /// zero-based, so 0 means the first element, 1 the second element and so on. 
        /// Negative indices can be used to designate elements starting at the tail of 
        /// the list. Here, -1 means the last element, -2 means the penultimate and so forth.
        /// 
        /// When the value at key is not a list, an error is returned.
        /// </summary>
        /// <typeparam name="TRoot">Type of root object</typeparam>
        /// <typeparam name="TValue">Type of value stored in the list</typeparam>
        /// <param name="root">Root object (owner) of the list</param>
        /// <param name="index">Index of value</param>
        /// <param name="listName">Optional list name that is appended to root schema instead of 
        /// TValue type name </param>
        /// <returns>The requested element, or default(TValue) when index is out of range.</returns>
        public TValue LIndex<TRoot, TValue>(TRoot root, long index, string listName = null) {
            var key = GetItemFullKey(root) + ":list:" + (listName ?? typeof(TValue).Name);
            var result = GetDb().ListGetByIndex(key, index);
            return UnpackResultNullable<TValue>(result);
        }

    }
}
