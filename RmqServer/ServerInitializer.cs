using System;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.Topology;
using Infrastructure;

namespace RmqServer
{
    class ServerInitializer : IServerInitializer
    {
        private readonly IRmqServer _rmqServer;

        public ServerInitializer(IRmqServer rmqServer)
        {
            _rmqServer = rmqServer;
        }

        public async Task InitializeServerAsync()
        {
            try
            {
                var advancedBus = Configuration.Bus.Advanced;
                var exchange = await advancedBus.ExchangeDeclareAsync(Configuration.ExchangeName, ExchangeType.Direct);
                var responseQueue = await advancedBus.QueueDeclareAsync(Configuration.RequestQueueName, declareConfig =>
                {
                    declareConfig.AsAutoDelete(false)
                        .AsDurable(true)
                        .AsExclusive(false)
                        .WithArgument("expires", Configuration.ExpiryTime)
                        .WithArgument("perQueueMessageTtl", Configuration.ExpiryTime);
                });
                advancedBus.Bind(exchange, responseQueue, Configuration.RequestRoutingKey);

                Console.WriteLine("Responding requests using Advanced API");
                _rmqServer?.RespondRequests(responseQueue, advancedBus, exchange);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}