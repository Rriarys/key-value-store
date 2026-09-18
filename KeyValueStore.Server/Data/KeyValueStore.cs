namespace KeyValueStore.Server.Data;

// Storage for KeyValueEntry objects
internal class InMemoryKeyValueStore
{
    private readonly Dictionary<string, KeyValueEntry> _store = new();
    public void Set(KeyValueEntry entry)
    {
        _store[entry.Key] = entry;
    }
    public bool Exists(string key)
    {
        return TryGetLiveEntry(key) is not null;
    }

    public KeyValueEntry? TryGet(string key)
    {
        return TryGetLiveEntry(key);
    }

    public bool Update(string key, string value, int? ttlSeconds)
    {
        var entry = TryGetLiveEntry(key);
        if (entry is null)
        {
            return false;
        }

        entry.Value = value;
        entry.ExpiresAt = ttlSeconds.HasValue
            ? DateTimeOffset.UtcNow.AddSeconds(ttlSeconds.Value)
            : null;
        return true;
    }

    public bool Delete(string key)
    {
        if (TryGetLiveEntry(key) is null)
        {
            return false;
        }

        return _store.Remove(key);
    }

    public bool Expire(string key, int ttlSeconds)
    {
        var entry = TryGetLiveEntry(key);
        if (entry is null)
        {
            return false;
        }

        entry.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(ttlSeconds);
        return true;
    }

    public int GetTtl(string key)
    {
        var entry = TryGetLiveEntry(key);
        if (entry is null)
        {
            return -2;
        }

        if (entry.ExpiresAt is null)
        {
            return -1;
        }

        var remainingTime = entry.ExpiresAt.Value - DateTimeOffset.UtcNow;
        if (remainingTime <= TimeSpan.Zero)
        {
            _store.Remove(key);
            return -2;
        }

        var remainingSeconds = (int)remainingTime.TotalSeconds;
        return Math.Max(1, remainingSeconds);
    }

    public bool Persist(string key)
    {
        var entry = TryGetLiveEntry(key);
        if (entry is null)
        {
            return false;
        }

        entry.ExpiresAt = null;
        return true;
    }

    private KeyValueEntry? TryGetLiveEntry(string key)
    {
        if (!_store.TryGetValue(key, out var entry))
        {
            return null;
        }

        if (entry.ExpiresAt is not { } expiresAt || expiresAt > DateTimeOffset.UtcNow)
        {
            return entry;
        }

        _store.Remove(key);
        return null;
    }
}
