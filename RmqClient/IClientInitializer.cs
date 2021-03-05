using System.Threading.Tasks;

namespace RmqClient
{
    internal interface IClientInitializer
    {
        Task InitiateClientAsync();
    }
}