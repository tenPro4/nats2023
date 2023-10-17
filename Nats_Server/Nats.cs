using NATS.Client.JetStream;
using NATS.Client;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Net.Mime.MediaTypeNames;

namespace Nats_Server
{
    public class Nats
    {
        static IConnection c;
        static IJetStream js;
        public static ILogger? Logger;
        const string STREAM = "NERVE";
        const string DURABLE_NAME = STREAM + "_NODE_SERVICE";
        static bool NatsConnected = false;
        public static bool DiconnectNats = false;

        public static void Connect()
        {
            string[] servers = new string[] { "nats://localhost:4871" };
            Options opts = ConnectionFactory.GetDefaultOptions();
            opts.MaxReconnect = 2;
            opts.ReconnectWait = 1000;
            opts.Servers = servers;
            opts.Name = "Client1";

            var cf = new ConnectionFactory();
            c = cf.CreateConnection(opts);
            js = c.CreateJetStreamContext();
            var jsmgt = c.CreateJetStreamManagementContext();

            try
            {
                jsmgt.GetStreamInfo(STREAM);
            }
            catch (NATSJetStreamException)
            {
                try
                {
                    jsmgt.AddStream(StreamConfiguration.Builder()
                           .WithName(STREAM)
                           .WithMaxAge(3 * 24 * 60 * 60 * 1000) // 3 days
                           .WithSubjects(STREAM + ".>")
                           .WithStorageType(StorageType.File)
                           .Build());
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to create stream");
                    throw;
                }
            }
            NatsConnected = true;
        }

        public static void Disconnect()
        {
            //Draining and closing a connection
            c.Drain();

            //Closing a connection
            c.Close();
        }

        public static void CloseNats()
        {
            DiconnectNats = true;
        }

        #region pub/sub
        public static void PubSub(string message)
        {
            Console.Clear();
            Console.WriteLine("Pub/Sub demo");
            Console.WriteLine("============");

            byte[] data = Encoding.UTF8.GetBytes(message);
            c.Publish("nats.demo.pubsub", data);
        }

        public static void SubscribePubSub()
        {
            Task.Run(() =>
            {
                ISyncSubscription sub = c.SubscribeSync("nats.demo.pubsub");
                while (!DiconnectNats)
                {
                    var message = sub.NextMessage();
                    if (message != null)
                    {
                        string data = Encoding.UTF8.GetString(message.Data);
                        LogMessage(data);
                    }
                }
            });
        }
        #endregion

        #region load balance
        public static void QueueGroups()
        {
            Console.Clear();
            Console.WriteLine("Load-balancing demo");
            Console.WriteLine("===================");

            for (int i = 1; i <= 10; i++)
            {
                string message = $"Message {i}";

                Console.WriteLine($"Sending: {message}");

                byte[] data = Encoding.UTF8.GetBytes(message);

                c.Publish("nats.demo.queuegroups", data);

                Thread.Sleep(100);
            }
        }

        public static void SubscribeQueueGroups()
        {
            EventHandler<MsgHandlerEventArgs> handler = (sender, args) =>
            {
                string data = Encoding.UTF8.GetString(args.Message.Data);
                LogMessage(data);
            };

            IAsyncSubscription s = c.SubscribeAsync(
                "nats.demo.queuegroups", "load-balancing-queue", handler);
        }
        #endregion

        #region wildcard
        public static void Wildcards()
        {
            Console.Clear();
            Console.WriteLine("Wildcards demo");
            Console.WriteLine("==============");

            Console.WriteLine("Available subjects:");
            Console.WriteLine("- nats.*.wildcards");
            Console.WriteLine("- nats.demo.wildcards.*");
            Console.WriteLine("- nats.demo.wildcards.>");

            int messageCounter = 1;
            while (true)
            {
                Console.Write("\nSubject: ");
                string subject = Console.ReadLine();
                if (string.IsNullOrEmpty(subject))
                {
                    return;
                }

                string message = $"Message {messageCounter++}";

                Console.WriteLine($"Sending: {message} to {subject}");

                byte[] data = Encoding.UTF8.GetBytes(message);

                c.Publish(subject, data);
            }
        }

        public static void SubscribeWildcards(string subject)
        {
            EventHandler<MsgHandlerEventArgs> handler = (sender, args) =>
            {
                string data = Encoding.UTF8.GetString(args.Message.Data);
                LogMessage($"{data} (subject {subject})");
            };

            IAsyncSubscription s = c.SubscribeAsync(
                subject, handler);
        }
        #endregion

        #region response
        public static void RequestResponseExplicit()
        {
            Console.Clear();
            Console.WriteLine("Request/Response (explicit) demo");
            Console.WriteLine("================================");

            for (int i = 1; i <= 10; i++)
            {
                string replySubject = $"_INBOX.{Guid.NewGuid().ToString("N")}";
                ISyncSubscription subscription = c.SubscribeSync(replySubject);
                subscription.AutoUnsubscribe(1);

                // client also has a convenience-method to do this in line:
                //string replySubject = conn.NewInbox();

                string message = $"Message {i}";

                Console.WriteLine($"Sending: {message}");

                // send with reply subject
                byte[] data = Encoding.UTF8.GetBytes(message);

                c.Publish("nats.demo.requestresponse", replySubject, data);

                // wait for response in reply subject
                var response = subscription.NextMessage(5000);

                string responseMsg = Encoding.UTF8.GetString(response.Data);
                Console.WriteLine($"Response: {responseMsg}");

                Thread.Sleep(100);
            }
        }

        public static void RequestResponseImplicit()
        {
            Console.Clear();
            Console.WriteLine("Request/Response (implicit) demo");
            Console.WriteLine("================================");

            for (int i = 1; i <= 10; i++)
            {
                string message = $"Message {i}";

                Console.WriteLine($"Sending: {message}");

                byte[] data = Encoding.UTF8.GetBytes(message);

                var response = c.Request("nats.demo.requestresponse", data, 5000);

                var responseMsg = Encoding.UTF8.GetString(response.Data);

                Console.WriteLine($"Response: {responseMsg}");

                Thread.Sleep(100);
            }
        }

        public static void SubscribeRequestResponse()
        {
            EventHandler<MsgHandlerEventArgs> handler = (sender, args) =>
            {
                string data = Encoding.UTF8.GetString(args.Message.Data);
                LogMessage(data);

                string replySubject = args.Message.Reply;
                if (replySubject != null)
                {
                    byte[] responseData = Encoding.UTF8.GetBytes($"ACK for {data}");
                    c.Publish(replySubject, responseData);
                }
            };

            IAsyncSubscription s = c.SubscribeAsync(
                "nats.demo.requestresponse", "request-response-queue", handler);
        }
        #endregion

        #region streaming
        public static void PublishStreaming()
        {
            for (int i = 1; i <= 25; i++)
            {
                string message = $"[{DateTime.Now.ToString("hh:mm:ss:fffffff")}] Message {i}";
                Console.WriteLine($"Sending {message}");
                string subject = STREAM + $".DATA.{Guid.NewGuid().ToString()}";
                js.Publish(subject, Encoding.UTF8.GetBytes(message));
            }
        }
        public static void SubscribeStreaming(string durableName)
        {
            if (!NatsConnected) Connect();

            string SUBJECT = STREAM + $".DATA.>";
            //subscribe to nats jetstream

            PullSubscribeOptions pso = PullSubscribeOptions.Builder()
                    .WithDurable(durableName)
                   .Build();

            using (IJetStreamPullSubscription sub = js.PullSubscribe(SUBJECT, pso))
            {
                c.Flush(1000);
                while (!DiconnectNats)
                {
                    var messages = sub.Fetch(10, 1000);
                    foreach (var message in messages)
                    {
                        LogMessage(Encoding.UTF8.GetString(message.Data));
                        message.Ack();
                    }
                }
            }
        }
        #endregion
        private static void LogMessage(string message)
        {
            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fffffff")} - {message}");
        }
    }
}