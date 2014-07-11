using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ServiceStack;
using ServiceStack.Text;

namespace Ractor {

    public interface IEncryptedData {

        bool IsEncrypted { get; set; }
        /// <summary>
        /// Initialization vector, salt, etc
        /// </summary>
        byte[] IV { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    interface IEncryptor {
        void Encrypt<T>(ref T item) where T : IEncryptedData;
        void Decrypt<T>(ref T item) where T : IEncryptedData;
    }

    /// <summary>
    /// When applied to a property of types string, byte[] or IEncryptedData of IEncryptedData
    /// objects that properies are encrypted when the oobjects are stored in cache or DB
    /// 
    /// TODO we should encrypt everything that is not an index or FK as default for the interface
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ProtectedAttribute : Attribute {
    }

    /// <summary>
    /// Protects only from fools or accidental glances, use only for tests
    /// </summary>
    public class DummyEncryptor : IEncryptor {
        public virtual void Encrypt<T>(ref T item) where T : IEncryptedData {
            var copy = item; // item.ToJsv().FromJsv<T>(); // TODO if we throw below than it is better to mutate?
            
            if (copy.IsEncrypted) throw new ApplicationException("Already encrypted"); // "return deepClone;" - could hide logic errors, need to throw here
            copy.IsEncrypted = true;

            // TODO in derived 'non-dummy' ones should use static dict to memoize reflection stuff
            var props = copy.GetType().GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(ProtectedAttribute)))
                .ToList();
            if (!props.Any()) return;

            copy.IV = GetKey();
            var key = copy.IV.Reverse().ToArray();

            foreach (var prop in props) {
                var a = prop.GetValue(this, null);

                //encrypt strings, byte[] and IEncryptedData

                if (a == null) {
                    prop.SetValue(copy, null, null);
                } else if (a is string) {
                    prop.SetValue(copy, ((string)a).Encrypt(key, copy.IV), null);
                } else if (a is byte[]) {
                    prop.SetValue(copy, ((byte[])a).Encrypt(key, copy.IV), null);
                } else if (a is IEncryptedData) {
                    var p = (IEncryptedData) a;
                    Encrypt(ref p);
                    prop.SetValue(copy, p, null);
                } else {
                    throw new ApplicationException("Could encrypt only string or byte[] or IEncryptedData properties");
                }
            }         
            
            item = copy;
        }

        public virtual void Decrypt<T>(ref T item) where T : IEncryptedData{
            var copy = item; // item.ToJsv().FromJsv<T>(); // TODO if we throw below than it is better to mutate?

            if (!copy.IsEncrypted) throw new ApplicationException("Already decrypted"); // "return deepClone;" - could hide logic errors, need to throw here
            copy.IsEncrypted = false;

            // TODO in derived 'non-dummy' ones should use static dict to memoize reflection stuff
            var props = copy.GetType().GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(ProtectedAttribute)))
                .ToList();
            if (!props.Any()) return;

            var key = copy.IV.Reverse().ToArray();

            foreach (var prop in props) {
                var a = prop.GetValue(this, null);

                //encrypt strings, byte[] and IEncryptedData

                if (a == null) {
                    prop.SetValue(copy, null, null);
                } else if (a is string) {
                    prop.SetValue(copy, ((string)a).Decrypt(key, copy.IV), null);
                } else if (a is byte[]) {
                    prop.SetValue(copy, ((byte[])a).Decrypt(key, copy.IV), null);
                } else if (a is IEncryptedData) {
                    var p = (IEncryptedData)a;
                    Decrypt(ref p);
                    prop.SetValue(copy, p, null);
                } else {
                    throw new ApplicationException("Could decrypt only string or byte[] or IEncryptedData properties");
                }
            }

            item = copy;
        }

        protected virtual byte[] GetKey(DateTime? moment = null) {
            return CryptoExtentions.GenerateKey();
        }
    }



    public static class CryptoExtentions {

        public static string GetMD5Hash(this Stream input) {
            var position = input.Position;
            var sb = new StringBuilder();
            var hasher = MD5.Create();
            var hashValue = hasher.ComputeHash(input);
            foreach (var b in hashValue)
                sb.Append(b.ToString("x2").ToLower());
            input.Position = position;
            return sb.ToString();
        }

        public static string GetSHA256Hash(this Stream input) {
            var position = input.Position;
            var sb = new StringBuilder();
            var hasher = SHA256.Create();
            var hashValue = hasher.ComputeHash(input);
            foreach (var b in hashValue)
                sb.Append(b.ToString("x2").ToLower());
            input.Position = position;
            return sb.ToString();
        }


        private const int _KEY_LENGTH = 256;

        public static MemoryStream Encrypt(this Stream inputStream, byte[] key, byte[] iv) {
            if (key.Length != _KEY_LENGTH / 8 || iv.Length != _KEY_LENGTH / 8)
                throw new ArgumentException("Wrong length of encryption key or iv", "key");


            using (var algorithm = new AesManaged()) {
                algorithm.KeySize = _KEY_LENGTH;
                algorithm.Mode = CipherMode.CBC;
                algorithm.Padding = PaddingMode.PKCS7;

                using (var inStream = inputStream)
                using (var outStream = new MemoryStream()) {
                    using (var encryptor = algorithm.CreateEncryptor(key, iv))
                    using (var crypt = new CryptoStream(outStream, encryptor, CryptoStreamMode.Write))
                    using (var compress = new GZipStream(crypt, CompressionMode.Compress))
                        inStream.CopyTo(compress);

                    return outStream;

                }
            }
        }

        public static byte[] Encrypt(this byte[] input, byte[] key, byte[] iv) {
            return (new MemoryStream(input)).Encrypt(key, iv).ToArray();
        }

        public static string Encrypt(this string input, byte[] key, byte[] iv) {
            var bytes = input.ToUtf8Bytes();
            bytes = bytes.Encrypt(key, iv);
            return Convert.ToBase64String(bytes);
        }



        public static MemoryStream Decrypt(this Stream input, byte[] key, byte[] iv) {

            if (key.Length == _KEY_LENGTH / 8 && iv.Length == _KEY_LENGTH / 8) {
                using (var algorithm = new AesManaged()) {
                    algorithm.KeySize = _KEY_LENGTH;
                    algorithm.Mode = CipherMode.CBC;
                    algorithm.Padding = PaddingMode.PKCS7;


                    using (var inStream = input)
                    using (var outStream = new MemoryStream()) {
                        using (var decryptor = algorithm.CreateDecryptor(key, iv))
                        using (var crypt = new CryptoStream(inStream, decryptor, CryptoStreamMode.Read))
                        using (var deCompress = new GZipStream(crypt, CompressionMode.Decompress))
                            deCompress.CopyTo(outStream);
                        return outStream;

                    }
                }
            }
            throw new ArgumentException("Wrong length of encryption key or iv", "key");
        }


        public static byte[] Decrypt(this byte[] input, byte[] key, byte[] iv) {
            return (new MemoryStream(input)).Decrypt(key, iv).ToArray();
        }

        public static string Decrypt(this string input, byte[] key, byte[] iv) {
            var bytes = Convert.FromBase64String(input);
            bytes = bytes.Decrypt(key, iv);
            return Encoding.Unicode.GetString(bytes);
        }

        
        public static byte[] GenerateKey() {
            using (var algorithm = new AesManaged()) {
                algorithm.KeySize = _KEY_LENGTH;
                algorithm.Mode = CipherMode.CBC;
                algorithm.Padding = PaddingMode.PKCS7;
                algorithm.GenerateKey();
                return algorithm.Key;
            }
        }


        public static byte[] GenerateIV() {
            using (var algorithm = new AesManaged()) {
                algorithm.KeySize = _KEY_LENGTH;
                algorithm.Mode = CipherMode.CBC;
                algorithm.Padding = PaddingMode.PKCS7;
                algorithm.GenerateIV();
                return algorithm.IV;
            }
        }


    }

}
