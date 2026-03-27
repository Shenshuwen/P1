using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace P1.Common
{
    public class TcpModuleClient : ITcpModuleClient
    {
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;

        public bool IsConnected => _tcpClient?.Connected == true;

        public async Task ConnectAsync(string ip, int port, CancellationToken cancellationToken = default)
        {
            if (IsConnected)
            {
                return;
            }

            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(ip, port, cancellationToken);
            _stream = _tcpClient.GetStream();
        }

        public async Task<byte[]> SendAndReceiveAsync(byte[] request, int responseBufferSize, int timeoutMs, CancellationToken cancellationToken = default)
        {
            if (_stream is null || !IsConnected)
            {
                throw new InvalidOperationException("TCP 连接未建立");
            }

            await _stream.WriteAsync(request, 0, request.Length, cancellationToken);

            var buffer = new byte[responseBufferSize];
            using var timeoutCts = new CancellationTokenSource(timeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            int read = await _stream.ReadAsync(buffer.AsMemory(0, buffer.Length), linkedCts.Token);
            if (read <= 0)
            {
                return Array.Empty<byte>();
            }

            var result = new byte[read];
            Buffer.BlockCopy(buffer, 0, result, 0, read);
            return result;
        }

        public Task DisconnectAsync()
        {
            try
            {
                _stream?.Dispose();
                _tcpClient?.Dispose();
            }
            finally
            {
                _stream = null;
                _tcpClient = null;
            }

            return Task.CompletedTask;
        }
    }
}
