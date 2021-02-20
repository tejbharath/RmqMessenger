using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.Topology;

namespace RmqClient
{
    public interface IRmqClient
    {
        Task SendRequests(IAdvancedBus advancedBus, IQueue responseQueue, IExchange exchange);
    }
}
