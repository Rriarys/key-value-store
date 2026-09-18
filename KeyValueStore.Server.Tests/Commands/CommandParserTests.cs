using Xunit;
using System.Text.Json;
using KeyValueStore.Server.Commands;

namespace KeyValueStore.Server.Tests.Commands;

public sealed class CommandParserTests
{
    private readonly CommandParser _parser = new();

    [Theory]
    [InlineData("SET", "key", "value", null)]
    [InlineData("GET", "key", null, null)]
    [InlineData("UPDATE", "key", "value", null)]
    [InlineData("DELETE", "key", null, null)]
    [InlineData("EXISTS", "key", null, null)]
    [InlineData("EXPIRE", "key", null, 10)]
    [InlineData("TTL", "key", null, null)]
    [InlineData("PERSIST", "key", null, null)]
    public void Parse_AcceptsValidCommand(
        string operation,
        string key,
        string? value,
        int? seconds)
    {
        var valueJson = value is null ? "" : $",\"value\":\"{value}\"";
        var secondsJson = seconds is null ? "" : $",\"seconds\":{seconds}";

        var command = _parser.Parse($"{{\"operation\":\"{operation}\",\"key\":\"{key}\"{valueJson}{secondsJson}}}");

        Assert.Equal(Enum.Parse<CommandOperation>(operation, ignoreCase: true), command.Operation);
        Assert.Equal(key, command.Key);
        Assert.Equal(value, command.Value);
        Assert.Equal(seconds, command.TtlSeconds);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{}")] // Missing operation and key
    [InlineData("{\"operation\":\"UNKNOWN\",\"key\":\"key\"}")]
    [InlineData("{\"operation\":\"GET\"}")]
    [InlineData("{\"operation\":\"SET\",\"key\":\"key\"}")]
    [InlineData("{\"operation\":\"SET\",\"key\":\"key\",\"value\":\"value\",\"EX\":10}")]
    [InlineData("{\"operation\":\"EXPIRE\",\"key\":\"key\"}")]
    [InlineData("{\"operation\":\"EXPIRE\",\"key\":\"key\",\"seconds\":\"ten\"}")]
    [InlineData("{\"operation\":\"EXPIRE\",\"key\":\"key\",\"seconds\":0}")]
    [InlineData("{\"operation\":\"EXPIRE\",\"key\":\"key\",\"seconds\":-1}")]
    public void Parse_RejectsInvalidCommand(string json)
    {
        var exception = Assert.Throws<JsonException>(() => _parser.Parse(json));

        Assert.Equal("Invalid command JSON", exception.Message);
    }
}
