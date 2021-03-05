using System;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.Topology;
using Infrastructure;

namespace RmqClient
{
    internal class ClientInitializer : IClientInitializer
    {
        private readonly IRmqClient _rmqClient;

        public ClientInitializer(IRmqClient rmqClient)
        {
            _rmqClient = rmqClient;
        }

        public async Task InitiateClientAsync()
        {
            try
            {
                var advancedBus = Configuration.Bus.Advanced;
                var exchange =
                    await advancedBus.ExchangeDeclareAsync(Configuration.ExchangeName, ExchangeType.Direct);
                var responseQueue = await advancedBus.QueueDeclareAsync(Configuration.ResponseQueueName, config =>
                {
                    config.AsAutoDelete(false)
                        .AsDurable(true)
                        .AsExclusive(false)
                        .WithArgument("expires", Configuration.ExpiryTime)
                        .WithArgument("perQueueMessageTtl", Configuration.ExpiryTime);
                });
                advancedBus.Bind(exchange, responseQueue, Configuration.ResponseRoutingKey);

                Console.WriteLine("Sending requests using Advanced API");
                await _rmqClient?.SendRequests(advancedBus, responseQueue, exchange);
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}