using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.Topology;
using Infrastructure.Models;
using Microsoft.Extensions.Logging;

namespace RmqServer
{
    public class RmqServer : IRmqServer
    {
        private readonly ILogger _logger;

        public RmqServer(ILogger logger)
        {
            _logger = logger;
        }

        public void RespondRequests(IQueue queue, IAdvancedBus advancedBus, IExchange exchange)
        {
            advancedBus.Consume<Request>(queue, (request, info) =>
            {
                try
                {
                    var message = new Message<Response>(new Response
                        {ResponseId = request.Body.RequestId, ResponseTimeStamp = DateTime.Now});
                    _logger.LogInformation($"Received RequestId={request.Body.RequestId}");

                    advancedBus.PublishAsync(exchange, request.Properties.ReplyTo, false, message);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Failed responding with the following error : {e.Message}");
                    throw;
                }
            });
            Console.ReadLine();
        }

        public async Task RespondRequestsPubSub(IBus bus)
        {
            await bus.PubSub.SubscribeAsync<Request>("Subscribe_Request", async request =>
            {
                try
                {
                    var message = new Message<Response>(new Response
                        {ResponseId = request.RequestId, ResponseTimeStamp = DateTime.Now});
                    _logger.LogInformation($"Received RequestId={request.RequestId}");

                    await bus.PubSub.PublishAsync(message).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Server failed to process the request with the following error message: {e.Message}");
                    throw;
                }
            });
            Console.ReadLine();
        }
    }
}
