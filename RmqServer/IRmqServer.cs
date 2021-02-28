using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.Topology;

namespace RmqServer
{
    public interface IRmqServer
    {
        void RespondRequests(IQueue queue, IAdvancedBus advancedBus, IExchange exchange);

        Task RespondRequestsPubSub(IBus bus);
    }
}
