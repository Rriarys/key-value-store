using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KeyValueStore.Server.Commands;

namespace KeyValueStore.Server.Network;

internal sealed class KeyValueTcpServer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly Encoding Utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly CommandParser _commandParser;
    private readonly CommandHandler _commandHandler;
    private readonly TcpListener _listener;
    private readonly ConcurrentBag<Task> _connectionTasks = [];

    public KeyValueTcpServer(
        CommandParser commandParser,
        CommandHandler commandHandler,
        int port)
    {
        _commandParser = commandParser ?? throw new ArgumentNullException(nameof(commandParser));
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));

        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535");
        }

        _listener = new TcpListener(IPAddress.Any, port);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _listener.Start();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client;

                try
                {
                    client = await _listener.AcceptTcpClientAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var connectionTask = HandleClientAsync(client, cancellationToken);
                _connectionTasks.Add(connectionTask);
            }
        }
        finally
        {
            _listener.Stop();

            try
            {
                await Task.WhenAll(_connectionTasks);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }
    }

    private async Task HandleClientAsync(
        TcpClient client,
        CancellationToken cancellationToken)
    {
        await using var networkStream = client.GetStream();
        using var reader = new StreamReader(
            networkStream,
            Utf8Encoding,
            detectEncodingFromByteOrderMarks: false);

        await using var writer = new StreamWriter(
            networkStream,
            Utf8Encoding,
            leaveOpen: false)
        {
            AutoFlush = true,
            NewLine = "\n"
        };

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var requestJson = await reader.ReadLineAsync(cancellationToken);

                if (requestJson is null)
                {
                    return;
                }

                var response = ProcessRequest(requestJson);
                var responseJson = JsonSerializer.Serialize(response, JsonOptions);

                await writer.WriteLineAsync(responseJson.AsMemory(), cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (IOException exception)
        {
            Console.Error.WriteLine($"Client connection closed: {exception.Message}");
        }
        catch (SocketException exception)
        {
            Console.Error.WriteLine($"Client connection failed: {exception.Message}");
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            client.Dispose();
        }
    }

    private CommandResponse ProcessRequest(string requestJson)
    {
        try
        {
            var command = _commandParser.Parse(requestJson);
            return _commandHandler.Handle(command);
        }
        catch (Exception exception)
        {
            return CommandResponse.Failed(exception.Message);
        }
    }
}