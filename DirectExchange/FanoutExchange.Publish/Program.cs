using System;

namespace FanoutExchange.Publish
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