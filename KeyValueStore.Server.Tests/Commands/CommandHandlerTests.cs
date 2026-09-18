using Xunit;
using KeyValueStore.Server.Data;
using KeyValueStore.Server.Commands;

namespace KeyValueStore.Server.Tests.Commands;

public sealed class CommandHandlerTests
{
    [Fact]
    public void Set_ReturnsSuccess()
    {
        var handler = CreateHandler();

        var response = handler.Handle(new Command(CommandOperation.Set, "key", "value"));

        AssertResponse(response, true, "Key set");
    }

    [Fact]
    public void Get_ReturnsExistingValue()
    {
        var handler = CreateHandler();
        handler.Handle(new Command(CommandOperation.Set, "key", "value"));

        var response = handler.Handle(new Command(CommandOperation.Get, "key"));

        AssertResponse(response, true, "Key found", "value");
    }

    [Fact]
    public void Get_ReturnsFailureForMissingKey()
    {
        var response = CreateHandler().Handle(new Command(CommandOperation.Get, "missing"));

        AssertResponse(response, false, "Key not found");
    }

    [Fact]
    public void Update_ReturnsSuccess()
    {
        var handler = CreateHandler();
        handler.Handle(new Command(CommandOperation.Set, "key", "before"));

        var response = handler.Handle(new Command(CommandOperation.Update, "key", "after"));

        AssertResponse(response, true, "Key updated");
        Assert.Equal("after", handler.Handle(new Command(CommandOperation.Get, "key")).Value);
    }

    [Fact]
    public void Update_ReturnsFailureForMissingKey()
    {
        var response = CreateHandler().Handle(new Command(CommandOperation.Update, "missing", "value"));

        AssertResponse(response, false, "Key not found");
    }

    [Fact]
    public void Delete_ReturnsSuccess()
    {
        var handler = CreateHandler();
        handler.Handle(new Command(CommandOperation.Set, "key", "value"));

        var response = handler.Handle(new Command(CommandOperation.Delete, "key"));

        AssertResponse(response, true, "Key deleted");
    }

    [Fact]
    public void Delete_ReturnsFailureForMissingKey()
    {
        var response = CreateHandler().Handle(new Command(CommandOperation.Delete, "missing"));

        AssertResponse(response, false, "Key not found");
    }

    [Fact]
    public void Exists_ReturnsTrueForExistingKey()
    {
        var handler = CreateHandler();
        handler.Handle(new Command(CommandOperation.Set, "key", "value"));

        var response = handler.Handle(new Command(CommandOperation.Exists, "key"));

        AssertResponse(response, true, "Key exists", exists: true);
    }

    [Fact]
    public void Exists_ReturnsFalseForMissingKey()
    {
        var response = CreateHandler().Handle(new Command(CommandOperation.Exists, "missing"));

        AssertResponse(response, true, "Key does not exist", exists: false);
    }

    [Fact]
    public void Expire_ReturnsSuccessForExistingKey()
    {
        var handler = CreateHandler();
        handler.Handle(new Command(CommandOperation.Set, "key", "value"));

        var response = handler.Handle(new Command(CommandOperation.Expire, "key", ttlSeconds: 10));

        AssertResponse(response, true, "Expiration set");
    }

    [Fact]
    public void Expire_ReturnsFailureForMissingKey()
    {
        var response = CreateHandler().Handle(new Command(CommandOperation.Expire, "missing", ttlSeconds: 10));

        AssertResponse(response, false, "Key not found");
    }

    [Fact]
    public void Ttl_ReturnsMinusOneForPersistentKey()
    {
        var handler = CreateHandler();
        handler.Handle(new Command(CommandOperation.Set, "key", "value"));

        var response = handler.Handle(new Command(CommandOperation.Ttl, "key"));

        AssertResponse(response, true, "TTL returned", "-1");
    }

    [Fact]
    public void Ttl_ReturnsPositiveValueForExpiringKey()
    {
        var handler = CreateHandler();
        handler.Handle(new Command(CommandOperation.Set, "key", "value"));

        handler.Handle(new Command(CommandOperation.Expire, "key", ttlSeconds: 10));
        var response = handler.Handle(new Command(CommandOperation.Ttl, "key"));

        AssertResponse(response, true, "TTL returned");
        Assert.True(int.TryParse(response.Value, out var ttl));
        Assert.InRange(ttl, 1, 10);
    }

    [Fact]
    public void Ttl_ReturnsMinusTwoForMissingKey()
    {
        var response = CreateHandler().Handle(new Command(CommandOperation.Ttl, "missing"));

        AssertResponse(response, true, "TTL returned", "-2");
    }

    [Fact]
    public void Persist_ReturnsSuccessForExpiringKey()
    {
        var handler = CreateHandler();
        handler.Handle(new Command(CommandOperation.Set, "key", "value", 10));

        var response = handler.Handle(new Command(CommandOperation.Persist, "key"));

        AssertResponse(response, true, "Expiration removed");
        Assert.Equal("-1", handler.Handle(new Command(CommandOperation.Ttl, "key")).Value);
    }

    [Fact]
    public void Persist_ReturnsFailureForMissingKey()
    {
        var response = CreateHandler().Handle(new Command(CommandOperation.Persist, "missing"));

        AssertResponse(response, false, "Key not found");
    }

    private static CommandHandler CreateHandler()
    {
        return new CommandHandler(new InMemoryKeyValueStore());
    }

    private static void AssertResponse(
        CommandResponse response,
        bool success,
        string message,
        string? value = null,
        bool? exists = null)
    {
        Assert.Equal(success, response.Success);
        Assert.Equal(message, response.Message);

        if (value is not null)
        {
            Assert.Equal(value, response.Value);
        }

        if (exists is not null)
        {
            Assert.Equal(exists, response.Exists);
        }
    }
}
