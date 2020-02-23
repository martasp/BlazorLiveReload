using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace RazorComponentsPreview
{
    public static class RazorRuntimeCompilationExtensions
    {
        private static IServiceCollection _serviceCollection;

        public static void AddRazorComponentsRuntimeCompilation(this IServiceCollection services) //Todo need serviceCollection, how to make without this function??
        {
            _serviceCollection = services;
        }

        public static void UseRazorComponentsRuntimeCompilation(this IApplicationBuilder app)
        {
            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 4 * 1024
            };
            webSocketOptions.AllowedOrigins.Add("https://localhost:5001");
            webSocketOptions.AllowedOrigins.Add("https://localhost:5000");
            webSocketOptions.AllowedOrigins.Add("http://localhost:5000");
            app.UseWebSockets(webSocketOptions);


            var runtimeComponentsGenerator = new RuntimeComponentsGenerator(_serviceCollection);
            var firstTimeRender = runtimeComponentsGenerator.FirstTimeRender();
            runtimeComponentsGenerator.AddRazorStaticRuntimeGeneration();
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/preview")
                {
                    await context.Response.WriteAsync(firstTimeRender);
                }
                else if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        runtimeComponentsGenerator.AttachWebsocket(webSocket);
                        await KeepAlive(context, webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }
            });
        }
        private static async Task KeepAlive(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
    }
}
