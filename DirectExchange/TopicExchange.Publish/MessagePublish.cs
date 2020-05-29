using Exchange.Common;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace TopicExchange.Publish
{
    public class MessagePublish
    {
        public void Publish()
        {
            var rabbitMQService = new RabbitMQService();
            using (var connection = rabbitMQService.GetRabbitMQConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(CL_ConstModel.TopicExchangeName, ExchangeType.Topic, true, false, null);

                    //QUEUE CREATED
                    channel.QueueDeclare(CL_ConstModel.queueName, true, false, false, null);
                    channel.QueueDeclare(CL_ConstModel.queue2Name, true, false, false, null);

                    channel.QueueBind(CL_ConstModel.queueName, CL_ConstModel.TopicExchangeName, CL_ConstModel.queueRouteKey);
                    channel.QueueBind(CL_ConstModel.queue2Name, CL_ConstModel.TopicExchangeName, CL_ConstModel.queue2RouteKey);

                    //QUEUE MESSAGE SEND 
                    var publicationAddress = new PublicationAddress(ExchangeType.Topic, CL_ConstModel.TopicExchangeName, CL_ConstModel.queueRouteKey);
                    var publicationAddress2 = new PublicationAddress(ExchangeType.Topic, CL_ConstModel.TopicExchangeName, CL_ConstModel.queue2RouteKey);
                    string message = JsonConvert.SerializeObject(AccountService.Load());

                    var body = Encoding.UTF8.GetBytes(message);

                    channel.BasicPublish(publicationAddress, null, body);
                    channel.BasicPublish(publicationAddress2, null, body);
                    Console.WriteLine("Message Sent!");
                }
            }
        }
    }
}
