using EasyNetQ;
using EasyNetQ.Topology;
using System.Threading.Tasks;
using Infrastructure;
using Infrastructure.Models;
using System;
using Serilog;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace RmqClient
{
    public class RmqClient : IRmqClient
    {
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch;
        private readonly IList<Request> _requestList;

        public RmqClient(ILogger logger)
        {
            _logger = logger;
            _stopwatch = Stopwatch.StartNew();
            _requestList = new List<Request>();
        }

        public async Task SendRequests(IAdvancedBus advancedBus, IQueue responseQueue, IExchange exchange)
        {
            try
            {
                _stopwatch.Reset();
                _stopwatch.Start();

                var requestsPerSec = Configuration.RequestsPerSec;
                var totalRequests = Configuration.TotalRequests;
                var delay = 1000 / requestsPerSec;

                advancedBus.Consume<Response>(responseQueue, (receivedMessage, info) => ProcessRoundTripTime(receivedMessage.Body));

                var count = 0;
                while (count < totalRequests)
                {
                    var request = await PublishRequests(advancedBus, exchange);
                    _requestList.Add(request);
                    await Task.Delay(delay);
                    count++;
                }
                _stopwatch.Stop();
                _logger.Information($"TotalRequests={totalRequests}, TotalRoundTripTime={_stopwatch.ElapsedMilliseconds} Milliseconds");
            }
            catch(Exception ex)
            {
                _logger.Error($"Server failed to process the request with following error : {ex.Message}");
            }            
        }

        public async Task SendRequestsPubSub(IBus bus)
        {
            try
            {
                _stopwatch.Reset();
                _stopwatch.Start();

                var requestsPerSec = Configuration.RequestsPerSec;
                var totalRequests = Configuration.TotalRequests;
                var delay = 1000 / requestsPerSec;

                await bus.PubSub.SubscribeAsync<Response>("Subscribe_Reponse", ProcessRoundTripTime);

                var count = 0;
                while (count < totalRequests)
                {
                    var request = new Request();
                    await bus.PubSub.PublishAsync(request).ConfigureAwait(false);
                    _requestList.Add(request);
                    await Task.Delay(delay);
                    count++;
                }
                _stopwatch.Stop();
                _logger.Information($"TotalRequests={totalRequests}, TotalRoundTripTime={_stopwatch.ElapsedMilliseconds} Milliseconds");
            }
            catch (Exception ex)
            {
                _logger.Error($"Server failed to process the request with following error : {ex.Message}");
            }
        }

        private async Task<Request> PublishRequests(IAdvancedBus advancedBus, IExchange exchange)
        {
            var message = new Message<Request>(new Request { RequestId = Guid.NewGuid().ToString(), RequestTimeStamp = DateTime.Now });
            message.Properties.ReplyTo = Configuration.ResponseRoutingKey;

            await advancedBus.PublishAsync(exchange, Configuration.RequestRoutingKey, false, message);

            return message.Body;
        }

        private void ProcessRoundTripTime(Response response)
        {
            var receivedTime = DateTime.Now;

            var request = _requestList.First(_ => _.RequestId == response.ResponseId);
            _requestList.Remove(request);
            var roundTripTime = (receivedTime - request.RequestTimeStamp).Milliseconds;

            _logger.Information($"RequestId={request.RequestId}, ResponseId={response.ResponseId}, RoundTripTime={roundTripTime}");
        }
    } 
}
