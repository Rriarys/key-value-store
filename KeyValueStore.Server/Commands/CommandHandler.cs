using KeyValueStore.Server.Data;

namespace KeyValueStore.Server.Commands;

internal sealed class CommandHandler
{
    private readonly InMemoryKeyValueStore _store;

    public CommandHandler(InMemoryKeyValueStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public CommandResponse Handle(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command.Operation switch
        {
            CommandOperation.Set => Set(command),
            CommandOperation.Get => Get(command),
            CommandOperation.Update => Update(command),
            CommandOperation.Delete => Delete(command),
            CommandOperation.Exists => Exists(command),
            _ => CommandResponse.Failed("Unsupported operation")
        };
    }

    private CommandResponse Set(Command command)
    {
        var value = command.Value
            ?? throw new InvalidOperationException("A value is required for SET");

        _store.Set(new KeyValueEntry
        {
            Key = command.Key,
            Value = value
        });

        return CommandResponse.Succeeded("Key set");
    }

    private CommandResponse Get(Command command)
    {
        var entry = _store.TryGet(command.Key);

        return entry is null
            ? CommandResponse.Failed("Key not found")
            : CommandResponse.Succeeded("Key found", entry.Value);
    }

    private CommandResponse Update(Command command)
    {
        var value = command.Value
            ?? throw new InvalidOperationException("A value is required for UPDATE");

        return _store.Update(command.Key, value)
            ? CommandResponse.Succeeded("Key updated")
            : CommandResponse.Failed("Key not found");
    }

    private CommandResponse Delete(Command command)
    {
        return _store.Delete(command.Key)
            ? CommandResponse.Succeeded("Key deleted")
            : CommandResponse.Failed("Key not found");
    }

    private CommandResponse Exists(Command command)
    {
        return CommandResponse.Existence(_store.Exists(command.Key));
    }
}