using RabbitMQ.Client;
using System;

namespace Exchange.Common
{
    public class RabbitMQService
    {
        private readonly string _hostName = "localhost";
        private readonly string _name = "guest";
        private readonly string _password = "guest";

        public IConnection GetRabbitMQConnection()
        {
            var connectionFactory = new ConnectionFactory()
            {
                HostName = _hostName,
                UserName = _name,
                Password = _password,
                VirtualHost = "Demo.RabbitMq"
            };

            return connectionFactory.CreateConnection();
        }
    }
}
