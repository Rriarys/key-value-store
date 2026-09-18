using Xunit;
using KeyValueStore.Server.Data;

namespace KeyValueStore.Server.Tests.Storage;

public sealed class KeyValueStoreTests
{
    [Fact]
    public void Set_CreatesKey()
    {
        var store = new InMemoryKeyValueStore();

        store.Set(new KeyValueEntry { Key = "key", Value = "value" });

        Assert.True(store.Exists("key"));
    }

    [Fact]
    public void Set_ReplacesExistingValue()
    {
        var store = new InMemoryKeyValueStore();
        store.Set(new KeyValueEntry { Key = "key", Value = "first" });

        store.Set(new KeyValueEntry { Key = "key", Value = "second" });

        Assert.Equal("second", store.TryGet("key")?.Value);
    }

    [Fact]
    public void TryGet_ReturnsExistingValue()
    {
        var store = new InMemoryKeyValueStore();
        store.Set(new KeyValueEntry { Key = "key", Value = "value" });

        Assert.Equal("value", store.TryGet("key")?.Value);
    }

    [Fact]
    public void TryGet_ReturnsNullForUnknownKey()
    {
        var store = new InMemoryKeyValueStore();

        Assert.Null(store.TryGet("missing"));
    }

    [Fact]
    public void Update_ChangesExistingValue()
    {
        var store = new InMemoryKeyValueStore();
        store.Set(new KeyValueEntry { Key = "key", Value = "before" });

        var updated = store.Update("key", "after", null);

        Assert.True(updated);
        Assert.Equal("after", store.TryGet("key")?.Value);
    }

    [Fact]
    public void Update_ReturnsFalseForMissingKey()
    {
        var store = new InMemoryKeyValueStore();

        Assert.False(store.Update("missing", "value", null));
    }

    [Fact]
    public void Delete_RemovesExistingKey()
    {
        var store = new InMemoryKeyValueStore();
        store.Set(new KeyValueEntry { Key = "key", Value = "value" });

        var deleted = store.Delete("key");

        Assert.True(deleted);
        Assert.False(store.Exists("key"));
    }

    [Fact]
    public void Delete_ReturnsFalseForMissingKey()
    {
        var store = new InMemoryKeyValueStore();

        Assert.False(store.Delete("missing"));
    }

    [Fact]
    public void Exists_ReturnsTrueForExistingKey()
    {
        var store = new InMemoryKeyValueStore();
        store.Set(new KeyValueEntry { Key = "key", Value = "value" });

        Assert.True(store.Exists("key"));
    }

    [Fact]
    public void Exists_ReturnsFalseForMissingKey()
    {
        var store = new InMemoryKeyValueStore();

        Assert.False(store.Exists("missing"));
    }

    [Fact]
    public async Task ExpiredKey_IsTreatedAsMissing()
    {
        var store = new InMemoryKeyValueStore();
        store.Set(new KeyValueEntry
        {
            Key = "key",
            Value = "value",
            ExpiresAt = DateTimeOffset.UtcNow.AddMilliseconds(100)
        });

        await Task.Delay(150);

        Assert.False(store.Exists("key"));
        Assert.Null(store.TryGet("key"));
    }

    [Fact]
    public void GetTtl_ReturnsMinusOneForPersistentKey()
    {
        var store = new InMemoryKeyValueStore();
        store.Set(new KeyValueEntry { Key = "key", Value = "value" });

        Assert.Equal(-1, store.GetTtl("key"));
    }

    [Fact]
    public void GetTtl_ReturnsMinusTwoForMissingKey()
    {
        var store = new InMemoryKeyValueStore();

        Assert.Equal(-2, store.GetTtl("missing"));
    }

    [Fact]
    public async Task GetTtl_ReturnsMinusTwoForExpiredKey()
    {
        var store = new InMemoryKeyValueStore();
        store.Set(new KeyValueEntry
        {
            Key = "key",
            Value = "value",
            ExpiresAt = DateTimeOffset.UtcNow.AddMilliseconds(100)
        });

        await Task.Delay(150);

        Assert.Equal(-2, store.GetTtl("key"));
    }

    [Fact]
    public void Expire_AssignsTtlToExistingKey()
    {
        var store = new InMemoryKeyValueStore();
        store.Set(new KeyValueEntry { Key = "key", Value = "value" });

        var expired = store.Expire("key", 10);

        Assert.True(expired);
        Assert.InRange(store.GetTtl("key"), 1, 10);
    }

    [Fact]
    public void Persist_RemovesExistingTtl()
    {
        var store = new InMemoryKeyValueStore();
        store.Set(new KeyValueEntry { Key = "key", Value = "value" });
        store.Expire("key", 10);

        var persisted = store.Persist("key");

        Assert.True(persisted);
        Assert.Equal(-1, store.GetTtl("key"));
    }

    [Fact]
    public void SetWithoutTtl_RemovesPreviousTtl()
    {
        var store = new InMemoryKeyValueStore();
        store.Set(new KeyValueEntry { Key = "key", Value = "old", ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(10) });

        store.Set(new KeyValueEntry { Key = "key", Value = "new" });

        Assert.Equal(-1, store.GetTtl("key"));
    }

    [Fact]
    public void UpdateWithoutTtl_RemovesPreviousTtl()
    {
        var store = new InMemoryKeyValueStore();
        store.Set(new KeyValueEntry { Key = "key", Value = "old", ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(10) });

        Assert.True(store.Update("key", "new", null));
        Assert.Equal(-1, store.GetTtl("key"));
    }
}
