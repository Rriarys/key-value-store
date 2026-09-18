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

            if (!Enum.TryParse<CommandOperation>(
                    operationText,
                    ignoreCase: true,
                    out var operation))
            {
                throw new JsonException($"Unsupported operation: {operationText}");
            }

            return new Command(operation, key, value);
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
}