using System;

namespace PublishRabbitMQ
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            new MessagePublish().Publish();
            Console.ReadLine();
        }
    }
}