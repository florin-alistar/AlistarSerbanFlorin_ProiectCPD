using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Dezbateri
{
    public class RabbitMQHandler
    {
        public event Action<string> MessageReceivedHandler;

        // Fiecare proces isi contruieste o coada cu un nume unic pe care o leaga de exchange-ul dorit (de tip FANOUT)
        //   --> cand se face publish la un mesaj in exchange, atunci acesta va pune in fiecare coada legata de el mesajul
        public (IConnection connection, IModel channel) SubscribeToTopic(string exchangeName, string username, string topic)
        {
            ConnectionFactory factory = GetNewConnectionFactory();
            IConnection connection = factory.CreateConnection();
            IModel channel = connection.CreateModel();
            channel.ExchangeDeclare(exchangeName, "fanout");
            string queueName = $"queue-{topic}-{username}";
            channel.QueueDeclare(queueName);
            channel.QueueBind(queueName, exchangeName, "");
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (ch, ea) =>
            {
                byte[] body = ea.Body.ToArray();
                string message = Encoding.UTF8.GetString(body);
                channel.BasicAck(ea.DeliveryTag, false);
                MessageReceivedHandler?.Invoke(message);
            };
            string consumerTag = channel.BasicConsume(queueName, false, consumer);
            return (connection, channel);
        }

        // Publish se face direct in exchange, urmand ca acesta sa redirecteze mesajul in cozile legate de el
        public void PublishMessage(string exchangeName, string message)
        {
            ConnectionFactory factory = GetNewConnectionFactory();
            using (IConnection connection = factory.CreateConnection())
            {
                using (IModel channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(exchangeName, "fanout");
                    channel.BasicPublish(exchangeName, "", null, Encoding.UTF8.GetBytes(message));
                }
            }
        }

        private static ConnectionFactory GetNewConnectionFactory()
        {
            return new ConnectionFactory
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };
        }
    }
}
