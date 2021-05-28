using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dezbateri
{
    public class DateUtilizator
    {
        public string Name { get; set; }
        public int TotalTokenDuration { get; set; }
        public int LeftTokenDuration { get; set; }
        public int EntryPort { get; set; }
        public int ExitPort { get; set; }
        public List<string> PublishTopics { get; set; } = new List<string>();
        public List<string> SubscribeTopics { get; set; } = new List<string>();

        public RabbitMQHandler RabbitMQHandler { get; private set; } = new RabbitMQHandler();

        private List<(IConnection conn, IModel channel)> subscriptionConnectionsAndChannels = new List<(IConnection, IModel)>();

        public void CloseSubscriptions()
        {
            foreach (var sub in subscriptionConnectionsAndChannels)
            {
                sub.channel.Close();
                sub.conn.Close();
            }
            subscriptionConnectionsAndChannels.Clear();
        }

        public void SubscribeToSubscriptionTopics()
        {
            foreach (string topic in SubscribeTopics)
            {
                var connAndChannel = RabbitMQHandler.SubscribeToTopic($"exchg-{topic}", Name, topic);
                subscriptionConnectionsAndChannels.Add(connAndChannel);
            }
        }

        public void SendMessage(string topic, string msg)
        {
            RabbitMQHandler.PublishMessage($"exchg-{topic}", $"{Name}@{topic}@{msg}");
        }
    }
}
