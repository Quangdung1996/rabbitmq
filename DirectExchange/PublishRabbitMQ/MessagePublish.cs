using Exchange.Common;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Text;

namespace PublishRabbitMQ
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
                    channel.ExchangeDeclare(CL_ConstModel.ExchangeName, ExchangeType.Direct, true, false, null);

                    //QUEUE CREATED
                    channel.QueueDeclare(CL_ConstModel.queueName, true, false, false, null);
                    channel.QueueBind(CL_ConstModel.queueName, CL_ConstModel.ExchangeName, CL_ConstModel.queueRouteKey);

                    //QUEUE MESSAGE SEND
                    var publicationAddress = new PublicationAddress(ExchangeType.Direct, CL_ConstModel.ExchangeName, CL_ConstModel.queueRouteKey);

                    string message = JsonConvert.SerializeObject(AccountService.Load());

                    var body = Encoding.UTF8.GetBytes(message);

                    channel.BasicPublish(publicationAddress, null, body);

                    Console.WriteLine("Message Sent!");
                }
            }
        }
    }
}