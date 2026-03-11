using BepInEx.Logging;
using SPT.Common.Http;
using System;
using WebSocketSharp;
using Logger = BepInEx.Logging.Logger;
using WebSocket = WebSocketSharp.WebSocket;
using WebSocketState = WebSocketSharp.WebSocketState;

namespace SharedHideoutClient.Websocket
{
    public class SharedHideoutClientWebsocket
    {
        private static readonly ManualLogSource _logger = Logger.CreateLogSource("SharedHideout Websocket");

        public event Action<string> OnMessageReceived;
        public string Host { get; set; }
        public string Url { get; set; }
        public string SessionId { get; set; }
        public bool Connected
        {
            get
            {
                return _webSocket.ReadyState == WebSocketState.Open;
            }
        }

        private readonly WebSocket _webSocket;

        public SharedHideoutClientWebsocket()
        {
            Host = RequestHandler.Host.Replace("http", "ws");
            SessionId = RequestHandler.SessionId;
            Url = $"{Host}/sharedhideout";

            _webSocket = new WebSocket(Url)
            {
                WaitTime = TimeSpan.FromMinutes(1),
                EmitOnPing = true
            };

            _webSocket.SetCredentials(SessionId, "", true);

            _webSocket.OnOpen += WebSocket_OnOpen;
            _webSocket.OnMessage += WebSocket_OnMessage;
            _webSocket.OnError += WebSocket_OnError;
            //_webSocket.OnClose += WebSocket_OnClose;
        }

        public void Connect()
        {
            _logger.LogInfo($"Attempting to connect to {Url}...");
            _webSocket.Connect();
        }

        public void Close()
        {
            _webSocket.Close();
        }

        private void WebSocket_OnOpen(object sender, EventArgs e)
        {
            _logger.LogMessage("Connected to SharedHostServer Websocket");
        }

        private void WebSocket_OnMessage(object sender, MessageEventArgs e)
        {
            OnMessageReceived?.Invoke(e.Data.ToString());
        }

        private void WebSocket_OnError(object sender, ErrorEventArgs e)
        {
            _logger.LogError($"Web socket error: {e.Message}");
        }
    }
}
