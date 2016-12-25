using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace Ractor.Persistence.AWS {
    public class AWSQueue<T> : IQueue<T> where T : class {
        public ISerializer Serializer { get; set; }
        readonly IAmazonSQS _sqs = new AmazonSQSClient(RegionEndpoint.EUWest1);
        private readonly string _queueUrl;

        public AWSQueue(string queueName = null) {
            _queueUrl = GetOrCreateQueue(queueName);
            Serializer = new JsonSerializer();
        }

        private string GetOrCreateQueue(string name) {
            string queueName;
            if (string.IsNullOrWhiteSpace(name)) {
                queueName = "_" + typeof(T).Name + "_";
            } else {
                queueName = name;
            }

            var listQueuesRequest = new ListQueuesRequest {
                QueueNamePrefix = queueName
            };
            var listQueuesResponse = _sqs.ListQueuesAsync(listQueuesRequest).Result;
            if (listQueuesResponse.QueueUrls.Count == 1) {
                return listQueuesResponse.QueueUrls[0];
            }
            if (listQueuesResponse.QueueUrls.Count > 1) {
                throw new Exception();
            }

            var attributes = new Dictionary<string, string>();
            attributes["DelaySeconds"] = "0";
            attributes["MaximumMessageSize"] = "262144";
            attributes["MessageRetentionPeriod"] = "1209600";
            attributes["ReceiveMessageWaitTimeSeconds"] = "20";
            attributes["VisibilityTimeout"] = $"3600"; // an hour
            var sqsRequest = new CreateQueueRequest {
                QueueName = queueName,
                Attributes = attributes
            };
            var createQueueResponse = _sqs.CreateQueueAsync(sqsRequest).Result;
            return createQueueResponse.QueueUrl;

        }

        public void DeleteQueue() {
            var sqsDeleRequest = new DeleteQueueRequest { QueueUrl = _queueUrl };
            _sqs.DeleteQueueAsync(sqsDeleRequest).Wait();
        }

        public int Timeout => 3600;

        public async Task<QueueSendResult> TrySendMessage(T message) {
            var id = Guid.NewGuid().ToBase64String();
            var messageWithId = new QueueMessageWithId<T> {
                Id = id,
                Payload = message
            };
            try {
                var txt = Encoding.UTF8.GetString(Serializer.Serialize(messageWithId));
                var sendMessageRequest = new SendMessageRequest {
                    QueueUrl = _queueUrl,
                    MessageBody = txt
                };
                //Console.WriteLine("Send");
                var response = await _sqs.SendMessageAsync(sendMessageRequest);
                return new QueueSendResult {
                    Id = id,
                    Ok = true
                };
            } catch {
                return new QueueSendResult {
                    Id = null,
                    Ok = false
                };
            }
        }



        public async Task<QueueReceiveResult<T>> TryReceiveMessage() {

            var qrr = new QueueReceiveResult<T>();
            try {
                var receiveMessageRequest = new ReceiveMessageRequest {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 1
                };
                var receiveMessageResponse = await _sqs.ReceiveMessageAsync(receiveMessageRequest);
                //Console.WriteLine("Receive");
                if (receiveMessageResponse.Messages.Count < 1)
                    return new QueueReceiveResult<T> { Ok = false };

                var message = receiveMessageResponse.Messages[0];
                var res = Serializer.Deserialize<QueueMessageWithId<T>>(Encoding.UTF8.GetBytes(message.Body));
                var handle = message.ReceiptHandle;
                return new QueueReceiveResult<T> {
                    Ok = true,
                    Id = res.Id,
                    Value = res.Payload,
                    DeleteHandle = handle
                };
            } catch {
                return new QueueReceiveResult<T> {
                    Ok = false
                };
            }
        }


        public async Task<bool> TryDeleteMessage(string deleteHandle) {
            try {
                var deleteRequest = new DeleteMessageRequest {
                    QueueUrl = _queueUrl,
                    ReceiptHandle = deleteHandle
                };
                var response = await _sqs.DeleteMessageAsync(deleteRequest);
                //Console.WriteLine("Delete");
                return true;
            } catch {
                return false;
            }
        }


    }
}
