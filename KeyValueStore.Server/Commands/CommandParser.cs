using System.Text.Json;

namespace KeyValueStore.Server.Commands;

internal sealed class CommandParser
{
    public Command Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Command JSON cannot be empty", nameof(json));
        }

        try
        {
            using var document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("Command must be a JSON object");
            }

            var root = document.RootElement;

            var operationText = ReadRequiredString(root, "operation");
            var key = ReadRequiredString(root, "key");
            var value = ReadOptionalString(root, "value");
            var ttlSeconds = ReadOptionalInteger(root, "seconds");

            if (!Enum.TryParse<CommandOperation>(
                    operationText,
                    ignoreCase: true,
                    out var operation))
            {
                throw new JsonException($"Unsupported operation: {operationText}");
            }

            ValidateArguments(root, operation, value, ttlSeconds);
            return new Command(operation, key, value, ttlSeconds);
        }
        catch (JsonException exception)
        {
            throw new JsonException("Invalid command JSON", exception);
        }
        catch (ArgumentException exception)
        {
            throw new JsonException("Invalid command JSON", exception);
        }
        catch (Exception exception)
        {
            throw new JsonException("Invalid command JSON", exception);
        }
    }

    private static string ReadRequiredString(JsonElement root, string propertyName)
    {
        if (!TryGetProperty(root, propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            throw new JsonException($"Property '{propertyName}' is required");
        }

        var value = property.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException($"Property '{propertyName}' cannot be empty");
        }

        return value;
    }

    private static string? ReadOptionalString(JsonElement root, string propertyName)
    {
        if (!TryGetProperty(root, propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            throw new JsonException($"Property '{propertyName}' must be a string");
        }

        return property.GetString();
    }

    private static int? ReadOptionalInteger(JsonElement root, string propertyName)
    {
        if (!TryGetProperty(root, propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out var value))
        {
            throw new JsonException($"Property '{propertyName}' must be an integer");
        }

        if (value <= 0)
        {
            throw new JsonException($"Property '{propertyName}' must be positive");
        }

        return value;
    }

    private static void ValidateArguments(
        JsonElement root,
        CommandOperation operation,
        string? value,
        int? ttlSeconds)
    {
        var hasValue = HasProperty(root, "value");
        var hasTtl = HasProperty(root, "seconds");

        switch (operation)
        {
            case CommandOperation.Set or CommandOperation.Update:
                if (value is null || !hasValue)
                {
                    throw new JsonException($"Property 'value' is required for {operation}");
                }

                break;
            case CommandOperation.Expire:
                if (!hasTtl || ttlSeconds is null)
                {
                    throw new JsonException("Property 'seconds' is required for Expire");
                }

                if (hasValue)
                {
                    throw new JsonException("Operation Expire does not accept value");
                }

                break;
            case CommandOperation.Get or CommandOperation.Delete or CommandOperation.Exists
                or CommandOperation.Ttl or CommandOperation.Persist:
                if (hasValue || hasTtl)
                {
                    throw new JsonException($"Operation {operation} does not accept value or seconds");
                }

                break;
        }

        foreach (var property in root.EnumerateObject())
        {
            if (property.Name is not ("operation" or "key" or "value" or "seconds"))
            {
                throw new JsonException($"Unsupported property '{property.Name}'");
            }
        }

        if (operation is not (CommandOperation.Set or CommandOperation.Update) && hasTtl)
        {
            if (operation != CommandOperation.Expire)
            {
                throw new JsonException($"Operation {operation} does not accept seconds");
            }
        }
    }

    private static bool TryGetProperty(
        JsonElement root,
        string propertyName,
        out JsonElement property)
    {
        foreach (var item in root.EnumerateObject())
        {
            if (string.Equals(
                    item.Name,
                    propertyName,
                    StringComparison.OrdinalIgnoreCase))
            {
                property = item.Value;
                return true;
            }
        }

        property = default;
        return false;
    }

    private static bool HasProperty(JsonElement root, string propertyName)
    {
        return TryGetProperty(root, propertyName, out _);
    }
}