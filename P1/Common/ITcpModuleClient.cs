using System.Threading;
using System.Threading.Tasks;

namespace P1.Common
{
    public interface ITcpModuleClient
    {
        bool IsConnected { get; }
        Task ConnectAsync(string ip, int port, CancellationToken cancellationToken = default);
        Task<byte[]> SendAndReceiveAsync(byte[] request, int responseBufferSize, int timeoutMs, CancellationToken cancellationToken = default);
        Task DisconnectAsync();
    }
}
