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
            CommandOperation.Expire => Expire(command),
            CommandOperation.Ttl => Ttl(command),
            CommandOperation.Persist => Persist(command),
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
            Value = value,
            ExpiresAt = command.TtlSeconds.HasValue
                ? DateTimeOffset.UtcNow.AddSeconds(command.TtlSeconds.Value)
                : null
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

        return _store.Update(command.Key, value, command.TtlSeconds)
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

    private CommandResponse Expire(Command command)
    {
        var ttlSeconds = command.TtlSeconds
            ?? throw new InvalidOperationException("A TTL is required for EXPIRE");

        return _store.Expire(command.Key, ttlSeconds)
            ? CommandResponse.Succeeded("Expiration set")
            : CommandResponse.Failed("Key not found");
    }

    private CommandResponse Ttl(Command command)
    {
        var ttl = _store.GetTtl(command.Key);
        return CommandResponse.Succeeded("TTL returned", ttl.ToString());
    }

    private CommandResponse Persist(Command command)
    {
        var entry = _store.TryGet(command.Key);

        if (entry is null)
        {
            return CommandResponse.Failed("Key not found");
        }

        if (entry.ExpiresAt is null)
        {
            return CommandResponse.Succeeded("Key is already persistent");
        }

        _store.Persist(command.Key);
        return CommandResponse.Succeeded("Expiration removed");
    }
}