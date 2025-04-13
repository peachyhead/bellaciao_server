using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

public static class WebSocketManager
{
    private static readonly ConcurrentDictionary<string, List<(Guid Id, WebSocket Socket)>> _chatSockets = new();

    public static void AddSocket(string chatId, WebSocket socket)
    {
        var id = Guid.NewGuid();
        var entry = (id, socket);

        _chatSockets.AddOrUpdate(
            chatId,
            _ => new List<(Guid, WebSocket)> { entry },
            (_, list) =>
            {
                list.Add(entry);
                return list;
            });
    }

    public static void RemoveSocket(string chatId, Guid id)
    {
        if (_chatSockets.TryGetValue(chatId, out var list))
        {
            list.RemoveAll(x => x.Id == id);
            if (list.Count == 0)
            {
                _chatSockets.TryRemove(chatId, out _);
            }
        }
    }

    public static async Task BroadcastToChatAsync(string chatId, string message)
    {
        if (_chatSockets.TryGetValue(chatId, out var list))
        {
            var bytes = Encoding.UTF8.GetBytes(message);

            foreach (var (id, socket) in list.ToList()) // копия на случай модификации
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
}