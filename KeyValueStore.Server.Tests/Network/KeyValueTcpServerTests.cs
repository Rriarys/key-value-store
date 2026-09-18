using Xunit;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using KeyValueStore.Server.Commands;
using KeyValueStore.Server.Data;
using KeyValueStore.Server.Network;

namespace KeyValueStore.Server.Tests.Network;

public sealed class KeyValueTcpServerTests : IAsyncLifetime
{
    private static readonly Encoding Utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private readonly CancellationTokenSource _cancellationSource = new();
    private readonly int _port;
    private Task? _serverTask;

    public KeyValueTcpServerTests()
    {
        using var portListener = new TcpListener(IPAddress.Loopback, 0);
        portListener.Start();
        _port = ((IPEndPoint)portListener.LocalEndpoint).Port;
    }

    public async Task InitializeAsync()
    {
        var store = new InMemoryKeyValueStore();
        var server = new KeyValueTcpServer(
            new CommandParser(),
            new CommandHandler(store),
            _port);

        _serverTask = server.RunAsync(_cancellationSource.Token);

        using var readinessCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (true)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, _port, readinessCancellation.Token);
                return;
            }
            catch (SocketException) when (!readinessCancellation.IsCancellationRequested)
            {
                await Task.Delay(20, readinessCancellation.Token);
            }
        }
    }

    public async Task DisposeAsync()
    {
        _cancellationSource.Cancel();

        if (_serverTask is not null)
        {
            try
            {
                await _serverTask;
            }
            catch (OperationCanceledException) when (_cancellationSource.IsCancellationRequested)
            {
            }
        }

        _cancellationSource.Dispose();
    }

    [Fact]
    public async Task SetFollowedByGet_ReturnsStoredValue()
    {
        using var client = await ConnectClientAsync();

        var setResponse = await SendAsync(client, "{\"operation\":\"SET\",\"key\":\"key\",\"value\":\"value\"}");
        var getResponse = await SendAsync(client, "{\"operation\":\"GET\",\"key\":\"key\"}");

        AssertSuccess(setResponse, "Key set");
        AssertSuccess(getResponse, "Key found", "value");
    }

    [Fact]
    public async Task SetFollowedByExists_ReturnsTrue()
    {
        using var client = await ConnectClientAsync();

        await SendAsync(client, "{\"operation\":\"SET\",\"key\":\"key\",\"value\":\"value\"}");
        var response = await SendAsync(client, "{\"operation\":\"EXISTS\",\"key\":\"key\"}");

        AssertSuccess(response, "Key exists");
        Assert.True(response.RootElement.GetProperty("exists").GetBoolean());
    }

    [Fact]
    public async Task UpdateFollowedByGet_ReturnsUpdatedValue()
    {
        using var client = await ConnectClientAsync();

        await SendAsync(client, "{\"operation\":\"SET\",\"key\":\"key\",\"value\":\"before\"}");
        var updateResponse = await SendAsync(client, "{\"operation\":\"UPDATE\",\"key\":\"key\",\"value\":\"after\"}");
        var getResponse = await SendAsync(client, "{\"operation\":\"GET\",\"key\":\"key\"}");

        AssertSuccess(updateResponse, "Key updated");
        AssertSuccess(getResponse, "Key found", "after");
    }

    [Fact]
    public async Task DeleteFollowedByGet_ReturnsNotFound()
    {
        using var client = await ConnectClientAsync();

        await SendAsync(client, "{\"operation\":\"SET\",\"key\":\"key\",\"value\":\"value\"}");
        var deleteResponse = await SendAsync(client, "{\"operation\":\"DELETE\",\"key\":\"key\"}");
        var getResponse = await SendAsync(client, "{\"operation\":\"GET\",\"key\":\"key\"}");

        AssertSuccess(deleteResponse, "Key deleted");
        Assert.False(getResponse.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Key not found", getResponse.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task InvalidCommand_ReturnsErrorResponse()
    {
        using var client = await ConnectClientAsync();

        var response = await SendAsync(client, "{\"operation\":\"UNKNOWN\",\"key\":\"key\"}");

        Assert.False(response.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Invalid command JSON", response.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task InvalidJson_ReturnsErrorResponse()
    {
        using var client = await ConnectClientAsync();

        var response = await SendAsync(client, "not-json");

        Assert.False(response.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Invalid command JSON", response.RootElement.GetProperty("message").GetString());
    }

    private async Task<TcpClient> ConnectClientAsync()
    {
        var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, _port);
        return client;
    }

    private static async Task<JsonDocument> SendAsync(TcpClient client, string request)
    {
        var stream = client.GetStream();
        using var writer = new StreamWriter(stream, Utf8Encoding, leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\n"
        };
        using var reader = new StreamReader(stream, Utf8Encoding, detectEncodingFromByteOrderMarks: false, leaveOpen: true);

        await writer.WriteLineAsync(request);
        var response = await reader.ReadLineAsync();
        Assert.NotNull(response);
        return JsonDocument.Parse(response);
    }

    private static void AssertSuccess(JsonDocument response, string message, string? value = null)
    {
        Assert.True(response.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(message, response.RootElement.GetProperty("message").GetString());

        if (value is not null)
        {
            Assert.Equal(value, response.RootElement.GetProperty("value").GetString());
        }
    }
}
