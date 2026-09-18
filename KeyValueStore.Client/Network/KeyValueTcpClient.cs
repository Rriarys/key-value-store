using System.Net.Sockets;
using System.Text;

namespace KeyValueStore.Client.Network;

public sealed class KeyValueTcpClient : IDisposable
{
    private static readonly Encoding Utf8Encoding = new UTF8Encoding(
        encoderShouldEmitUTF8Identifier: false);

    private readonly string _host;
    private readonly int _port;

    private TcpClient? _tcpClient;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    public KeyValueTcpClient(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("Host cannot be empty", nameof(host));
        }

        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(
                nameof(port),
                "Port must be between 1 and 65535");
        }

        _host = host;
        _port = port;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(_host, _port, cancellationToken);

            var networkStream = _tcpClient.GetStream();

            _reader = new StreamReader(
                networkStream,
                Utf8Encoding,
                detectEncodingFromByteOrderMarks: false);

            _writer = new StreamWriter(
                networkStream,
                Utf8Encoding,
                leaveOpen: false)
            {
                AutoFlush = true,
                NewLine = "\n"
            };
        }
        catch (SocketException exception)
        {
            throw new InvalidOperationException(
                $"Could not connect to {_host}:{_port}: {exception.Message}",
                exception);
        }
        catch (IOException exception)
        {
            throw new InvalidOperationException(
                $"Could not connect to {_host}:{_port}: {exception.Message}",
                exception);
        }
    }

    public async Task<string> SendAsync(
        string requestJson,
        CancellationToken cancellationToken)
    {
        if (_reader is null || _writer is null)
        {
            throw new InvalidOperationException("The client is not connected");
        }

        try
        {
            await _writer.WriteLineAsync(
                requestJson.AsMemory(),
                cancellationToken);

            var response = await _reader.ReadLineAsync(cancellationToken);

            if (response is null)
            {
                throw new IOException("The server closed the connection");
            }

            return response;
        }
        catch (IOException exception)
        {
            throw new InvalidOperationException(
                $"Communication with the server failed: {exception.Message}",
                exception);
        }
        catch (SocketException exception)
        {
            throw new InvalidOperationException(
                $"Communication with the server failed: {exception.Message}",
                exception);
        }
    }

    public void Dispose()
    {
        _writer?.Dispose();
        _reader?.Dispose();
        _tcpClient?.Dispose();
    }
}
