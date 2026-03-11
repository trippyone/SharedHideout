using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers.Ws;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace SharedHideoutServer.Websocket;

[Injectable(InjectionType.Singleton)]
public class SharedHideoutServerWebSocket(ISptLogger<SharedHideoutServerWebSocket> logger) : IWebSocketConnectionHandler
{
    private readonly ConcurrentDictionary<string, WebSocket> _sharedhideoutWs = [];

    public string GetHookUrl()
    {
        return "/sharedhideout";
    }

    public string GetSocketId()
    {
        return "Shared Hideout";
    }

    public async Task OnConnection(WebSocket ws, HttpContext context, string sessionIdContext)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(authHeader))
        {
            await ws.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, string.Empty, CancellationToken.None);
            return;
        }

        var base64EncodedString = authHeader.Split(' ')[1];
        var decodedString = Encoding.UTF8.GetString(Convert.FromBase64String(base64EncodedString));
        var authorization = decodedString.Split(':');
        var userSessionID = authorization[0];

        logger.Info($"[{GetSocketId()}] User is {userSessionID}");

        _sharedhideoutWs.TryAdd(userSessionID, ws);
    }

    public Task OnMessage(byte[] rawData, WebSocketMessageType messageType, WebSocket ws, HttpContext context)
    {
        return Task.CompletedTask;
    }

    public Task OnClose(WebSocket ws, HttpContext context, string sessionIdContext)
    {
        var sessionID = _sharedhideoutWs.FirstOrDefault(x => x.Value == ws).Key;

        if (sessionID != null)
        {
            _sharedhideoutWs.TryRemove(sessionID, out _);
        }

        return Task.CompletedTask;
    }

    public async void SendToAllExcept(string excludedSessionID, string message)
    {
        foreach (var (sessionId, ws) in _sharedhideoutWs)
        {
            if (sessionId != excludedSessionID)
            {
                await ws.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}