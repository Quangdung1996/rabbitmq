using Exchange.Common;
using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SubscribeRabbitMQ
{
    class Program
    {
        static void Main(string[] args)
        {
            var rabbitMQService = new RabbitMQService();
            using (var connection = rabbitMQService.GetRabbitMQConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    // uint prefetchSize, ushort prefetchCount, bool global

                    //channel.BasicQos(0, 1, false);

                    var consumer = new EventingBasicConsumer(channel);
                    var messageReceiver = new MessageReceiver(channel);

                    channel.BasicConsume(CL_ConstModel.queueName, false, messageReceiver);

                    Console.ReadLine();
                }
            }
        }
    }
}
