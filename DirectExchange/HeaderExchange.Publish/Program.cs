using System;

namespace HeaderExchange.Publish
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            MessagePublish.Publish();
            Console.ReadKey();
        }
    }
}