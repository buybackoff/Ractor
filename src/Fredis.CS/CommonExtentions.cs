using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using ServiceStack.Text;

namespace Fredis {
    public static class CommonExtentions {

        public static List<T> ItemAsList<T>(this T o) {
            return new List<T>
            {
                           o
                       };
        }

        public static T[] ItemAsArray<T>(this T o) {
            return new[] {
                           o
                       };
        }

        
    }

}
