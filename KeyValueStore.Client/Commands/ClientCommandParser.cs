using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeyValueStore.Client.Commands;

public sealed class ClientCommandParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public bool TryParse(
        string input,
        out string? requestJson,
        out string? errorMessage,
        out bool exitRequested)
    {
        requestJson = null;
        errorMessage = null;
        exitRequested = false;

        if (string.IsNullOrWhiteSpace(input))
        {
            errorMessage = "Command cannot be empty";
            return false;
        }

        var arguments = input
            .Trim()
            .Split(' ', '\t', StringSplitOptions.RemoveEmptyEntries);

        var operation = arguments[0].ToUpperInvariant();

        if (operation == "EXIT")
        {
            if (arguments.Length != 1)
            {
                errorMessage = "EXIT does not accept arguments";
                return false;
            }

            exitRequested = true;
            return true;
        }

        if (operation is not ("SET" or "GET" or "UPDATE" or "DELETE" or "EXISTS" or "EXPIRE" or "TTL" or "PERSIST"))
        {
            errorMessage = $"Unknown command: {arguments[0]}";
            return false;
        }

        var requiresValue = operation is "SET" or "UPDATE";
        var requiresSeconds = operation == "EXPIRE";
        var requiredArguments = requiresValue ? 3 : requiresSeconds ? 3 : 2;

        if (arguments.Length < requiredArguments || (requiresValue && arguments.Length is not (3 or 5)))
        {
            errorMessage = $"{operation} requires a key{(requiredArguments == 3 ? " and a value" : "")}";
            return false;
        }

        if ((!requiresValue && arguments.Length != requiredArguments) ||
            (requiresValue && arguments.Length == 5 && !string.Equals(arguments[3], "EX", StringComparison.OrdinalIgnoreCase)))
        {
            errorMessage = $"{operation} received too many arguments";
            return false;
        }

        var key = arguments[1];

        if (string.IsNullOrWhiteSpace(key))
        {
            errorMessage = "Key cannot be empty";
            return false;
        }

        var value = requiresValue ? arguments[2] : null;
        int? ttlSeconds = null;

        if (requiresSeconds || (requiresValue && arguments.Length == 5))
        {
            var secondsArgument = requiresSeconds ? arguments[2] : arguments[4];

            if (!int.TryParse(secondsArgument, out var parsedSeconds) || parsedSeconds <= 0)
            {
                errorMessage = "TTL seconds must be a positive integer";
                return false;
            }

            ttlSeconds = parsedSeconds;
            if (requiresSeconds)
            {
                value = null;
            }
        }

        requestJson = JsonSerializer.Serialize(
            new
            {
                operation,
                key,
                value,
                seconds = ttlSeconds
            },
            JsonOptions);

        return true;
    }
}
