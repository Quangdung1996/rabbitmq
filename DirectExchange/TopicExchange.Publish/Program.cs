using System;

namespace TopicExchange.Publish
{
    class Program
    {
        static void Main(string[] args)
        {
            new MessagePublish().Publish();
            Console.ReadKey();
        }
    }
}
