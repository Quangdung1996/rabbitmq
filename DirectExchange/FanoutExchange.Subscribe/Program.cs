﻿using Exchange.Common;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;

namespace FanoutExchange.Subscribe
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
                    channel.BasicConsume(CL_ConstModel.queue2Name, false, messageReceiver);
                    Console.ReadLine();
                }
            }
        }
    }
}
