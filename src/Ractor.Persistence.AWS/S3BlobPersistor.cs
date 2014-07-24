using System;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace Ractor.Persistence.AWS
{
    public class S3BlobPersistor
    {
        public class S3Persistor : IBlobPersistor {

            public ISerializer Serializer { get; set; }

            // TODO constructor param
            private static RegionEndpoint _endpoint = RegionEndpoint.EUWest1;

            private readonly string _bucket;

            public S3Persistor(string bucket) {
                _bucket = bucket;
                Serializer = new JsonSerializer();
            }


            public bool TryPut(Stream stream, out string key) {
                var md5Hash = stream.ComputeSHA256HashString();
                var length = stream.Length;

                key = md5Hash + length;

                return TryPut(_bucket, key, stream);
            }

            public async Task<Tuple<bool, string>> TryPutAsync(Stream stream) {
                var md5Hash = stream.ComputeMD5HashString();
                var length = stream.Length;

                var key = md5Hash + length;

                var res = await TryPutAsync(_bucket, key, stream);
                return Tuple.Create(res, key);
            }


            public bool TryPut(string key, Stream stream) {
                return TryPut(_bucket, key, stream);
            }

            public bool TryPut<T>(string key, T poco) {
                Stream stream = new MemoryStream(Serializer.Serialize(poco).GZip());
                return TryPut(_bucket, key, stream);
            }

            public async Task<bool> TryPutAsync(string key, Stream stream) {
                return await TryPutAsync(_bucket, key, stream);
            }

            public async Task<bool> TryPutAsync<T>(string key, T poco) {
                Stream stream = new MemoryStream(Serializer.Serialize(poco).GZip());
                return await TryPutAsync(_bucket, key, stream);
            }



            public bool TryGet(string key, out Stream stream) {
                return TryGet(_bucket, key, out stream);
            }


            public bool TryGet<T>(string key, out T poco) {
                poco = default(T);
                Stream stream;
                var res = TryGet(_bucket, key, out stream);
                if (!res) return false;
                var ms = new MemoryStream();
                stream.CopyTo(ms);
                poco = Serializer.Deserialize<T>(ms.ToArray().UnGZip());
                return true;
            }


            public async Task<Tuple<bool, Stream>> TryGetAsync(string key) {
                return await TryGetAsync(_bucket, key);
            }

            public async Task<Tuple<bool, T>> TryGetAsync<T>(string key) {
                var poco = default(T);
                var res = await TryGetAsync(_bucket, key);
                if (!res.Item1) return Tuple.Create(false, poco);
                var ms = new MemoryStream();
                res.Item2.CopyTo(ms);
                poco = Serializer.Deserialize<T>(ms.ToArray().UnGZip());
                return Tuple.Create(true, poco);
            }



            public bool Exists(string key) {
                return Exists(_bucket, key);
            }


            public static bool TryPut(string bucket, string key, Stream stream) {
                var length = stream.Length;

                // no need to check for existence because S3 will overwrite
                // files with the same key -> no more space automatically
                // and save one request without cheking for existence
                // could be performance issue for large files

                const int checkExistenceLimit = 5 * 1024 * 1024; // 5 Mb

                if (length > checkExistenceLimit) {
                    if (Exists(bucket, key)) {
                        return true;
                    }
                }

                using (IAmazonS3 client = AWSClientFactory.CreateAmazonS3Client(_endpoint)) {
                    try {
                        var request = new PutObjectRequest {
                            BucketName = bucket,
                            Key = key,
                            InputStream = stream
                        };
                        // ReSharper disable once UnusedVariable
                        var response = client.PutObject(request);
                        return true;
                    } catch {
                        return false;
                    }
                }
            }


            public static async Task<bool> TryPutAsync(string bucket, string key, Stream stream) {
                var length = stream.Length;

                // no need to check for existence because S3 will overwrite
                // files with the same key -> no more space automatically
                // and save one request without cheking for existence
                // could be performance issue for large files

                const int checkExistenceLimit = 1 * 1024 * 1024; // 1 Mb

                if (length > checkExistenceLimit) {
                    if (Exists(bucket, key)) {
                        return true;
                    }
                }

                using (IAmazonS3 client = AWSClientFactory.CreateAmazonS3Client(_endpoint)) {
                    try {
                        var request = new PutObjectRequest {
                            BucketName = bucket,
                            Key = key,
                            InputStream = stream
                        };
                        // ReSharper disable once UnusedVariable
                        var response = await client.PutObjectAsync(request);
                        return true;
                    } catch {
                        return false;
                    }
                }
            }



            public static bool TryGet(string bucket, string key, out Stream stream) {
                stream = null;
                using (IAmazonS3 client = AWSClientFactory.CreateAmazonS3Client(_endpoint)) {
                    try {
                        var request = new GetObjectRequest {
                            BucketName = bucket,
                            Key = key
                        };
                        var response = client.GetObject(request);
                        stream = response.ResponseStream;
                        return true;
                    } catch (AmazonS3Exception ex) {
                        if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                            return false;
                        //status wasn't not found, so throw the exception
                        throw;
                    }
                }
            }


            public static async Task<Tuple<bool, Stream>> TryGetAsync(string bucket, string key) {
                Stream stream = null;
                using (IAmazonS3 client = AWSClientFactory.CreateAmazonS3Client(_endpoint)) {
                    try {
                        var request = new GetObjectRequest {
                            BucketName = bucket,
                            Key = key
                        };
                        var response = await client.GetObjectAsync(request);
                        stream = response.ResponseStream;
                        return Tuple.Create(true, stream);
                    } catch (AmazonS3Exception ex) {
                        if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                            return Tuple.Create(false, stream);
                        //status wasn't not found, so throw the exception
                        throw;
                    }
                }
            }


            public static bool Exists(string bucket, string key) {
                using (var client = AWSClientFactory.CreateAmazonS3Client(_endpoint)) {
                    try {
                        // ReSharper disable once UnusedVariable
                        var response = client.GetObjectMetadata(new GetObjectMetadataRequest {
                            BucketName = bucket,
                            Key = key
                        });

                        return true;
                    } catch (AmazonS3Exception ex) {
                        if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                            return false;
                        //status wasn't not found, so throw the exception
                        throw;
                    }

                }
            }

        }
    }
}
