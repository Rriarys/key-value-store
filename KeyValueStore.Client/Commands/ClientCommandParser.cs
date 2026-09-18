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

        if (operation is not ("SET" or "GET" or "UPDATE" or "DELETE" or "EXISTS"))
        {
            errorMessage = $"Unknown command: {arguments[0]}";
            return false;
        }

        var requiredArguments = operation is "SET" or "UPDATE" ? 3 : 2;

        if (arguments.Length < requiredArguments)
        {
            errorMessage = $"{operation} requires a key{(requiredArguments == 3 ? " and a value" : "")}";
            return false;
        }

        if (arguments.Length > requiredArguments)
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

        var value = requiredArguments == 3 ? arguments[2] : null;

        requestJson = JsonSerializer.Serialize(
            new
            {
                operation,
                key,
                value
            },
            JsonOptions);

        return true;
    }
}
