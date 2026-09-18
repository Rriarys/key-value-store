using KeyValueStore.Client.Commands;
using KeyValueStore.Client.Network;

namespace KeyValueStore.Client.Application;

public sealed class ConsoleClientApplication
{
    private readonly ClientCommandParser _commandParser;

    public ConsoleClientApplication(ClientCommandParser commandParser)
    {
        _commandParser = commandParser ?? throw new ArgumentNullException(nameof(commandParser));
    }

    public async Task<int> RunAsync(string[] arguments)
    {
        if (!TryReadConnectionArguments(arguments, out var host, out var port))
        {
            return 1;
        }

        using var cancellationSource = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };

        Console.CancelKeyPress += cancelHandler;

        try
        {
            using var client = new KeyValueTcpClient(host, port);

            try
            {
                await client.ConnectAsync(cancellationSource.Token);
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("Connection was cancelled");
                return 1;
            }
            catch (InvalidOperationException exception)
            {
                Console.Error.WriteLine(exception.Message);
                return 1;
            }

            Console.WriteLine($"Connected to {host}:{port}");
            await RunCommandLoopAsync(client, cancellationSource.Token);
            return 0;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    private async Task RunCommandLoopAsync(
        KeyValueTcpClient client,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write("> ");

            var input = Console.ReadLine();

            if (input is null)
            {
                return;
            }

            if (!_commandParser.TryParse(
                    input,
                    out var requestJson,
                    out var errorMessage,
                    out var exitRequested))
            {
                Console.Error.WriteLine($"Invalid command: {errorMessage}");
                continue;
            }

            if (exitRequested)
            {
                return;
            }

            if (requestJson is null)
            {
                Console.Error.WriteLine("Could not create a request");
                continue;
            }

            try
            {
                var response = await client.SendAsync(
                    requestJson,
                    cancellationToken);

                Console.WriteLine(response);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (InvalidOperationException exception)
            {
                Console.Error.WriteLine(exception.Message);
                return;
            }
        }
    }

    private static bool TryReadConnectionArguments(
        string[] arguments,
        out string host,
        out int port)
    {
        host = string.Empty;
        port = 0;

        if (arguments.Length != 2)
        {
            Console.Error.WriteLine("Usage: dotnet run -- <host> <port>");
            return false;
        }

        if (!int.TryParse(arguments[1], out port) || port is < 1 or > 65535)
        {
            Console.Error.WriteLine("Port must be a number between 1 and 65535");
            return false;
        }

        host = arguments[0];
        return !string.IsNullOrWhiteSpace(host);
    }
}
