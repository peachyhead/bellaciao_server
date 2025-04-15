using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using Context;
using Microsoft.EntityFrameworkCore;

public static class WebSocketManager
{
    // Активные подключения: ChatID -> список сокетов
    private static readonly ConcurrentDictionary<string, List<WebSocket>> _chatSockets = new();
    
    public static void AddSocket(string chatId, WebSocket socket)
    {
        var sockets = _chatSockets.GetOrAdd(chatId, _ => new List<WebSocket>());
        lock (sockets)
        {
            sockets.Add(socket);
        }
    }

    public static void RemoveSocket(string chatId, WebSocket socket)
    {
        if (_chatSockets.TryGetValue(chatId, out var sockets))
        {
            lock (sockets)
            {
                sockets.Remove(socket);
                if (sockets.Count == 0)
                {
                    _chatSockets.TryRemove(chatId, out _);
                }
            }
        }
    }

    public static async Task BroadcastToChatAsync(string chatId, string message)
    {
        if (!_chatSockets.TryGetValue(chatId, out var sockets)) return;

        var buffer = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(buffer);

        List<WebSocket> disconnected = new();

        foreach (var socket in sockets)
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else
            {
                disconnected.Add(socket);
            }
        }

        foreach (var socket in disconnected)
        {
            RemoveSocket(chatId, socket);
        }
    }

    public static async Task HandleMessageAsync(string messageJson, string chatId, MyClassroomContext dbContext)
    {
        var doc = JsonDocument.Parse(messageJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeElement) || !root.TryGetProperty("data", out var data))
            return;

        string type = typeElement.GetString();

        switch (type)
        {
            case "send_message":
                await HandleSendMessage(data, chatId, dbContext);
                break;

            case "edit_message":
                await HandleEditMessage(data, chatId, dbContext);
                break;

            case "delete_message":
                await HandleDeleteMessage(data, chatId, dbContext);
                break;

            case "view_message":
                await HandleViewMessage(data, chatId, dbContext);
                break;
        }
    }

    private static async Task HandleSendMessage(JsonElement data, string chatId, MyClassroomContext dbContext)
    {
        var message = new Message
        {
            ID = Guid.NewGuid().ToString(),
            ChatID = chatId,
            AuthorID = data.GetProperty("author_id").GetString(),
            Text = data.GetProperty("text").GetString(),
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        dbContext.Messages.Add(message);

        var chat = await dbContext.Chats.FindAsync(chatId);
        if (chat != null)
        {
            chat.LastMessage = message.Text;
            chat.LastMessageTime = message.CreatedAt;
        }

        await dbContext.SaveChangesAsync();

        var response = JsonSerializer.Serialize(new
        {
            type = "new_message",
            data = new
            {
                message.ID,
                message.ChatID,
                message.AuthorID,
                message.Text,
                message.CreatedAt
            }
        });

        await BroadcastToChatAsync(chatId, response);
    }

    private static async Task HandleEditMessage(JsonElement data, string chatId, MyClassroomContext dbContext)
    {
        var messageId = data.GetProperty("message_id").GetString();
        var newText = data.GetProperty("text").GetString();

        var message = await dbContext.Messages
            .Where(m => m.ID == messageId && m.ChatID == chatId)
            .FirstOrDefaultAsync();

        if (message == null) return;

        message.Text = newText;
        await dbContext.SaveChangesAsync();

        var response = JsonSerializer.Serialize(new
        {
            type = "edit_message",
            data = new
            {
                message.ID,
                message.ChatID,
                message.Text
            }
        });

        await BroadcastToChatAsync(chatId, response);
    }

    private static async Task HandleDeleteMessage(JsonElement data, string chatId, MyClassroomContext dbContext)
    {
        var messageId = data.GetProperty("message_id").GetString();

        var message = await dbContext.Messages
            .Where(m => m.ID == messageId && m.ChatID == chatId)
            .FirstOrDefaultAsync();

        if (message == null) return;

        dbContext.Messages.Remove(message);
        await dbContext.SaveChangesAsync();

        var response = JsonSerializer.Serialize(new
        {
            type = "delete_message",
            data = new
            {
                message_id = messageId,
                chat_id = chatId
            }
        });

        await BroadcastToChatAsync(chatId, response);
    }

    private static async Task HandleViewMessage(JsonElement data, string chatId, MyClassroomContext dbContext)
    {
        var userId = data.GetProperty("user_id").GetString();

        var lastMessage = await dbContext.Messages
            .Where(m => m.ChatID == chatId)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        if (lastMessage == null) return;

        var participant = await dbContext.ChatParticipants
            .Where(cp => cp.ChatID == chatId && cp.UserID == userId)
            .FirstOrDefaultAsync();

        if (participant == null) return;

        participant.LastMessageViewed = lastMessage.ID;
        await dbContext.SaveChangesAsync();

        var response = JsonSerializer.Serialize(new
        {
            type = "view_message",
            data = new
            {
                user_id = userId,
                chat_id = chatId,
                last_viewed = lastMessage.ID
            }
        });

        await BroadcastToChatAsync(chatId, response);
    }
}