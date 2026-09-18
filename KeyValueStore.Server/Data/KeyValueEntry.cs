namespace KeyValueStore.Server.Data;

internal class KeyValueEntry
{
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public DateTimeOffset? ExpiresAt { get; set; }
}
