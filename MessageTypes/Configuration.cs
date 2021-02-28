using EasyNetQ;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Infrastructure
{
    public static class Configuration
    {
        public static string ResponseQueueName;

        public static string ResponseRoutingKey;

        public static string RequestQueueName;

        public static string RequestRoutingKey;

        public static string ExchangeName;

        public static int RequestsPerSec;

        public static int TotalRequests;

        public static IBus Bus => CreateBus();

        public static string Host;

        public static string Vhost;

        public static string UserName;

        public static string Password;

        public static int ExpiryTime { get; set; }

        static Configuration()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true).Build();
            Host = configuration["Rmq:host"];
            Vhost = configuration["Rmq:vHost"];
            UserName = configuration["Rmq:username"];
            Password = configuration["Rmq:password"];
            ResponseQueueName = configuration["Rmq:responseQueueName"];
            ResponseRoutingKey = configuration["Rmq:responseRoutingKey"];
            RequestQueueName = configuration["Rmq:requestQueueName"];
            RequestRoutingKey = configuration["Rmq:requestRoutingKey"];
            RequestsPerSec = int.Parse(configuration["Rmq:requestsPerSec"]);
            TotalRequests = int.Parse(configuration["Rmq:totalRequests"]);
            ExpiryTime = int.Parse(configuration["Rmq:ttl"]);
            ExchangeName = configuration["Rmq:exchangeName"];
        }

        private static IBus CreateBus()
        {
            return RabbitHutch.CreateBus($"host={Host};vhost={Vhost};username={UserName};password={Password}");
        }
    }
}
