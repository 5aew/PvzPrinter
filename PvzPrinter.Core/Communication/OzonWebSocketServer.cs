using System.Text.Json;
using Fleck;
using PvzPrinter.Core.Logging;
using PvzPrinter.Core.Printing;

namespace PvzPrinter.Core.Communication;

public class OzonWebSocketServer : IWebSocketServer
{
    private readonly ILogger _logger;
    private WebSocketServer? _server;
    private const int Port = 8765;

    public bool IsRunning { get; private set; }
    public event Action<PrintCommand>? PrintRequested;

    public OzonWebSocketServer(ILogger logger)
    {
        _logger = logger;
    }

    public void Start()
    {
        try
        {
            _server = new WebSocketServer($"ws://127.0.0.1:{Port}");

            _server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    _logger.Info($"WebSocket client connected: {socket.ConnectionInfo.ClientIpAddress}");
                };

                socket.OnClose = () =>
                {
                    _logger.Info("WebSocket client disconnected");
                };

                socket.OnMessage = message => HandleMessage(message);

                socket.OnError = ex =>
                {
                    _logger.Error("WebSocket error", ex);
                };
            });

            IsRunning = true;
            _logger.Info($"WebSocket server started on port {Port}");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to start WebSocket server", ex);
            IsRunning = false;
        }
    }

    public void Stop()
    {
        _server?.Dispose();
        IsRunning = false;
        _logger.Info("WebSocket server stopped");
    }

    private void HandleMessage(string message)
    {
        try
        {
            var command = JsonSerializer.Deserialize<PrintCommand>(message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (command != null && !string.IsNullOrWhiteSpace(command.CellCode))
            {
                _logger.Info($"Received print command: {command.CellCode} x{command.Copies}");

                // Отправляем событие подписчикам (LabelPrinter)
                for (int i = 0; i < Math.Max(1, command.Copies); i++)
                {
                    PrintRequested?.Invoke(command);
                }
            }
            else
            {
                _logger.Error($"Invalid print command received: {message}");
            }
        }
        catch (JsonException ex)
        {
            _logger.Error($"Failed to parse WebSocket message: {message}", ex);
        }
    }

    public void Dispose() => Stop();
}