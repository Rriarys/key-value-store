using KeyValueStore.Server.Commands;
using KeyValueStore.Server.Data;
using KeyValueStore.Server.Network;

namespace KeyValueStore.Server.Application;

public sealed class ConsoleServerApplication
{
    private const int DefaultPort = 5000;

    public async Task<int> RunAsync(string[] arguments)
    {
        if (!TryReadPort(arguments, out var port))
        {
            return 1;
        }

        var store = new InMemoryKeyValueStore();
        var commandParser = new CommandParser();
        var commandHandler = new CommandHandler(store);
        var server = new KeyValueTcpServer(commandParser, commandHandler, port);

        using var cancellationSource = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };

        Console.CancelKeyPress += cancelHandler;

        try
        {
            Console.WriteLine($"Key-value server is listening on port {port}");
            await server.RunAsync(cancellationSource.Token);
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }

        return 0;
    }

    private static bool TryReadPort(string[] arguments, out int port)
    {
        port = DefaultPort;

        if (arguments.Length > 0 &&
            (!int.TryParse(arguments[0], out port) || port is < 1 or > 65535))
        {
            Console.Error.WriteLine("Port must be a number between 1 and 65535");
            return false;
        }

        return true;
    }
}
