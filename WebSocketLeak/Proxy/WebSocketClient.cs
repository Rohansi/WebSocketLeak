using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketLeak.Proxy
{
    public class WebSocketClient : IAsyncDisposable
    {
        public ClientWebSocket Socket { get; }

        public WebSocketClient()
        {
            Socket = new ClientWebSocket();
            Socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(120);
            Socket.Options.SetBuffer(1024, 1024);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            catch
            {
                // ignored
            }

            Socket?.Dispose();
        }

        public Task Connect(IPEndPoint endpoint)
        {
#if DEBUG
            var uri = new Uri("ws://echo.websocket.org");
#else
            var uri = new Uri($"ws://{endpoint}");
#endif

            return Socket.ConnectAsync(uri, CancellationToken.None);
        }
    }
}
