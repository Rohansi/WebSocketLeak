using System;
using System.Buffers;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebSocketLeak.Proxy
{
    public static class WebSocketProxy
    {
        public static async Task Run(HttpContext context, WebSocket socket, IPEndPoint endpoint)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

            try
            {
                await using var gameSocket = new WebSocketClient();
                await gameSocket.Connect(endpoint);

                await Task.WhenAny(
                    Forward(socket, gameSocket.Socket, cts.Token, cts), // only messages from the client reset timeout!
                    Forward(gameSocket.Socket, socket, cts.Token));
            }
            finally
            {
                cts.Cancel();

                try
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                catch
                {
                    // ignored
                }
            }
        }

        private static async Task Forward(WebSocket from, WebSocket to, CancellationToken ct, CancellationTokenSource cts = null)
        {
            using var buffer = MemoryPool<byte>.Shared.Rent(1024);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var result = await from.ReceiveAsync(buffer.Memory, ct);
                    var message = buffer.Memory.Slice(0, result.Count);
                    await to.SendAsync(message, result.MessageType, result.EndOfMessage, ct);

                    cts?.CancelAfter(TimeSpan.FromSeconds(60)); // reset the timeout
                }
            }
            catch
            {
                // connection is dead
            }
        }
    }
}
