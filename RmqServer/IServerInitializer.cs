using System.Threading.Tasks;

namespace RmqServer
{
    internal interface IServerInitializer
    {
        Task InitializeServerAsync();
    }
}