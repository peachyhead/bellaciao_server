using System.Net.WebSockets;
using System.Text;
using Context;

public class WebSocketMiddleware
{
    private readonly RequestDelegate _next;

    public WebSocketMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == "/ws" && context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            // Получаем первое сообщение — chatId
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var chatId = Encoding.UTF8.GetString(buffer, 0, result.Count);

            // Добавляем сокет
            WebSocketManager.AddSocket(chatId, webSocket);

            try
            {
                while (!result.CloseStatus.HasValue)
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);

                        // Получаем scoped dbContext
                        using var scope = context.RequestServices.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<MyClassroomContext>();

                        await WebSocketManager.HandleMessageAsync(messageJson, chatId, dbContext);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
            }
            finally
            {
                WebSocketManager.RemoveSocket(chatId, webSocket);
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
            }
        }
        else
        {
            await _next(context);
        }
    }
}