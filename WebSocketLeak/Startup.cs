using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebSocketLeak.Proxy;

namespace WebSocketLeak
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            var webSocketOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 1 * 1024,
            };

            app.UseWebSockets(webSocketOptions);

            app.UseEndpoints(endpoints =>
            {
                endpoints.Map("/game/{ip}/{port}", async context =>
                {
                    var ip = (string)context.GetRouteValue("ip");
                    var port = (string)context.GetRouteValue("port");

                    if (!IPAddress.TryParse(ip, out var ipAddress) ||
                        ipAddress.AddressFamily != AddressFamily.InterNetwork ||
                        ip.Count(c => c == '.') != 3)
                    {
                        context.Response.StatusCode = 400;
                        return;
                    }

                    if (!ushort.TryParse(port, out var parsedPort) || parsedPort < 10000)
                    {
                        context.Response.StatusCode = 400;
                        return;
                    }

                    if (!context.WebSockets.IsWebSocketRequest)
                    {
                        context.Response.StatusCode = 400;
                        return;
                    }

                    var socket = await context.WebSockets.AcceptWebSocketAsync();
                    await WebSocketProxy.Run(context, socket, new IPEndPoint(ipAddress, parsedPort));
                });

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
            });
        }
    }
}
