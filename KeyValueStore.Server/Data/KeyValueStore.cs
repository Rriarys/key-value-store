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
        return _store.ContainsKey(key); // just if exists, not returning the value
    }
    public KeyValueEntry? TryGet(string key)
    {
        _store.TryGetValue(key, out var entry);
        return entry; // returns the value if exists
    }
    public bool Update(string key, string value)
    {
        if (_store.ContainsKey(key))
        {
            _store[key].Value = value;
            return true;
        }
        return false;
    }
    public bool Delete(string key)
    {
        return _store.Remove(key);
    }
}
