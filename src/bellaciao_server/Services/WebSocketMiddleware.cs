using System.Net.WebSockets;
using System.Text;

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
            var socket = await context.WebSockets.AcceptWebSocketAsync();

            // Ожидаем первое сообщение с chatId
            var buffer = new byte[1024 * 4];
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            var chatId = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var id = Guid.NewGuid();
            WebSocketManager.AddSocket(chatId, socket);

            try
            {
                while (!result.CloseStatus.HasValue)
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
            }
            finally
            {
                WebSocketManager.RemoveSocket(chatId, id);
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
            }
        }
        else
        {
            await _next(context);
        }
    }
}