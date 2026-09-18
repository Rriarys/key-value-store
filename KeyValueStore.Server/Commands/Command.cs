using System;
using System.Collections.Generic;
using System.Text;

namespace KeyValueStore.Server.Commands;

internal sealed class Command
{
    public CommandOperation Operation { get; }
    public string Key { get; }
    public string? Value { get; }
    public int? TtlSeconds { get; }

    public Command(
        CommandOperation operation,
        string key,
        string? value = null,
        int? ttlSeconds = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        }

        if ((operation is CommandOperation.Set or CommandOperation.Update) && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace for Set or Update operations.", nameof(value));
        }

        if (ttlSeconds is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ttlSeconds), "TTL must be positive.");
        }

        Operation = operation;
        Key = key;
        Value = value;
        TtlSeconds = ttlSeconds;
    }
}
