using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lyria.Api.Extensions;

internal static class SensitiveDataRedactor
{
    private const string RedactedValue = "***REDACTED***";
    private const string InvalidJsonMarker = "[OMITIDO: contenido JSON no válido]";

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "passwordHash",
        "passwordConfirmation",
        "pass",
        "pwd",
        "token",
        "accessToken",
        "refreshToken",
        "tokenHash",
        "refreshTokenHash",
        "signingKey",
        "authorization",
        "cookie",
        "setCookie",
        "secret",
        "apiKey",
        "clientSecret",
        "connectionString",
        "databasePassword",
        "creditCard",
        "cardNumber",
        "cvv",
        "email",
        "contactEmail",
        "phone",
        "contactPhone",
        "telephone",
        "mobile",
        "whatsApp",
        "street",
        "address",
        "addressComplement",
        "number",
        "neighborhood",
        "postalCode",
        "latitude",
        "longitude"
    };

    public static string? Redact(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return InvalidJsonMarker;
        }

        if (node is null)
        {
            return json;
        }

        RedactNode(node);

        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static void RedactNode(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                RedactObject(obj);
                break;
            case JsonArray array:
                RedactArray(array);
                break;
        }
    }

    private static void RedactObject(JsonObject obj)
    {
        foreach (var property in obj.ToArray())
        {
            if (SensitiveKeys.Contains(property.Key))
            {
                obj[property.Key] = RedactedValue;
            }
            else if (property.Value is not null)
            {
                RedactNode(property.Value);
            }
        }
    }

    private static void RedactArray(JsonArray array)
    {
        foreach (JsonNode? element in array)
        {
            if (element is not null)
            {
                RedactNode(element);
            }
        }
    }
}
