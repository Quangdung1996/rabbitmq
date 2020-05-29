using Exchange.Common;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace HeaderExchange.Publish
{
    public static class MessagePublish
    {
        public static void Publish()
        {
            var rabbitMQService = new RabbitMQService();
            using (var connection = rabbitMQService.GetRabbitMQConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(CL_ConstModel.HeaderExchangeName, ExchangeType.Headers, true, false, null);

                    //QUEUE CREATED
                    channel.QueueDeclare(CL_ConstModel.queueName, true, false, false, null);

                    //add header
                    Dictionary<string, object> headerOptionsWithAll = new Dictionary<string, object>();
                    headerOptionsWithAll.Add("x-match", "all");
                    headerOptionsWithAll.Add("category", "animal");
                    headerOptionsWithAll.Add("type", "mammal");
                   

                    channel.QueueBind(CL_ConstModel.queueName, CL_ConstModel.HeaderExchangeName, "", headerOptionsWithAll);


                    Dictionary<string, object> messageHeaders = new Dictionary<string, object>();

                    IBasicProperties properties = channel.CreateBasicProperties();
                    messageHeaders = new Dictionary<string, object>();
                    messageHeaders.Add("category", "animal");
                    messageHeaders.Add("type", "mammal");
                    messageHeaders.Add("x-match", "all");
                    properties.Headers = messageHeaders;
                  

                    //QUEUE MESSAGE SEND
                    var publicationAddress = new PublicationAddress(ExchangeType.Headers, CL_ConstModel.HeaderExchangeName, "");
                    string message = JsonConvert.SerializeObject(AccountService.Load());
                    var body = Encoding.UTF8.GetBytes(message);
                    channel.BasicPublish(publicationAddress, properties, body);

                    Console.WriteLine("Message Sent!");
                }
            }
        }
    }
}
