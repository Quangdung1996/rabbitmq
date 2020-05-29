using Exchange.Common;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace FanoutExchange.Publish
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
                    channel.ExchangeDeclare(CL_ConstModel.ExchangeName, ExchangeType.Fanout, true, false, null);

                    //QUEUE CREATED
                    channel.QueueDeclare(CL_ConstModel.queueName, true, false, false, null);
                    channel.QueueDeclare(CL_ConstModel.queue2Name, true, false, false, null);

                    channel.QueueBind(CL_ConstModel.queueName, CL_ConstModel.ExchangeName, "");
                    channel.QueueBind(CL_ConstModel.queue2Name, CL_ConstModel.ExchangeName, "");

                    //QUEUE MESSAGE SEND
                    var publicationAddress = new PublicationAddress(ExchangeType.Direct, CL_ConstModel.ExchangeName, "");

                    string message = JsonConvert.SerializeObject(AccountService.Load());

                    var body = Encoding.UTF8.GetBytes(message);

                    channel.BasicPublish(publicationAddress, null, body);

                    Console.WriteLine("Message Sent!");
                }
            }
        }
    }
}
