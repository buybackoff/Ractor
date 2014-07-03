using System;
using System.Collections.Generic;
using System.Text;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace Fredis.Persistence.AWS {
    public class AWSQueue<T> : IQueue<T> {
        public ISerializer Serializer { get; set; }
        readonly IAmazonSQS _sqs = AWSClientFactory.CreateAmazonSQSClient(RegionEndpoint.EUWest1);
        private readonly string _queueUrl;

        public AWSQueue(string queueName = null) {
            _queueUrl = GetOrCreateQueue(queueName);
            Serializer = new PicklerBinarySerializer();
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
            var listQueuesResponse = _sqs.ListQueues(listQueuesRequest);
            if (listQueuesResponse.QueueUrls.Count == 1) {
                return listQueuesResponse.QueueUrls[0];
            }
            if (listQueuesResponse.QueueUrls.Count > 1) {
                throw new ApplicationException();
            }

            var attributes = new Dictionary<string, string>();
            attributes["DelaySeconds"] = "0";
            attributes["MaximumMessageSize"] = "262144";
            attributes["MessageRetentionPeriod"] = "1209600";
            attributes["ReceiveMessageWaitTimeSeconds"] = "20";
            attributes["VisibilityTimeout"] = "0";
            var sqsRequest = new CreateQueueRequest {
                QueueName = queueName,
                Attributes = attributes
            };
            var createQueueResponse = _sqs.CreateQueue(sqsRequest);
            return createQueueResponse.QueueUrl;

        }

        public void DeleteQueue() {
            var sqsDeleRequest = new DeleteQueueRequest { QueueUrl = _queueUrl };
            _sqs.DeleteQueue(sqsDeleRequest);
        }

        public bool TrySendMessage(T message) {
            try {
                var txt = Encoding.UTF8.GetString(Serializer.Serialize(message));
                var sendMessageRequest = new SendMessageRequest {
                    QueueUrl = _queueUrl,
                    MessageBody = txt
                };
                //Console.WriteLine("Send");
                _sqs.SendMessage(sendMessageRequest);
                return true;
            } catch {
                return false;
            }
        }



        public bool TryReceiveMessage(out Tuple<T, string> messageWithDeleteHandle) {
            var res = default(T);
            var handle = "";
            messageWithDeleteHandle = new Tuple<T, string>(res, handle);
            try {
                var receiveMessageRequest = new ReceiveMessageRequest {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 1
                };
                var receiveMessageResponse = _sqs.ReceiveMessage(receiveMessageRequest);
                //Console.WriteLine("Receive");
                if (receiveMessageResponse.Messages.Count < 1)
                    return false;

                var message = receiveMessageResponse.Messages[0];
                res = Serializer.Deserialize<T>(Encoding.UTF8.GetBytes(message.Body));

                handle = message.ReceiptHandle;
                messageWithDeleteHandle = new Tuple<T, string>(res, handle);
                return true;
            } catch {
                return false;
            }
        }


        public bool TryDeleteMessage(string deleteHandle) {
            try {
                var deleteRequest = new DeleteMessageRequest {
                    QueueUrl = _queueUrl,
                    ReceiptHandle = deleteHandle
                };
                _sqs.DeleteMessage(deleteRequest);
                //Console.WriteLine("Delete");
                return true;
            } catch {
                return false;
            }
        }


    }
}
