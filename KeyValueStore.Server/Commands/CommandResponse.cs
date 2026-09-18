using System;
using System.Collections.Generic;
using System.Text;

namespace KeyValueStore.Server.Commands;

internal sealed class CommandResponse
{
    public bool Success { get; }
    public string Message { get; }
    public string? Value { get; }
    public bool? Exists { get; }

    private CommandResponse(
        bool success,
        string message,
        string? value = null,
        bool? exists = null)
    {
        Success = success;
        Message = message;
        Value = value;
        Exists = exists;
    }

    public static CommandResponse Succeeded(
        string message,
        string? value = null,
        bool? exists = null)
    {
        return new CommandResponse(true, message, value, exists);
    }

    public static CommandResponse Failed(string message)
    {
        return new CommandResponse(false, message);
    }

    public static CommandResponse Existence(bool exists)
    {
        return new CommandResponse(
            success: true,
            message: exists ? "Key exists." : "Key does not exist.",
            exists: exists
            );
    }
}
