using System.Text.Json;
using System.Text.Json.Serialization;

namespace MozcInput.Protocol;

/// <summary>
/// Stable JSON serializer for the local bridge protocol.
/// </summary>
public static class MozcBridgeSerializer
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    /// <summary>Serializes a bridge request.</summary>
    public static string SerializeRequest(MozcBridgeRequest request) =>
        JsonSerializer.Serialize(request, Options);

    /// <summary>Serializes a bridge response.</summary>
    public static string SerializeResponse(MozcBridgeResponse response) =>
        JsonSerializer.Serialize(response, Options);

    /// <summary>Deserializes a bridge request.</summary>
    public static MozcBridgeRequest? DeserializeRequest(string json) =>
        JsonSerializer.Deserialize<MozcBridgeRequest>(json, Options);

    /// <summary>Deserializes a bridge response.</summary>
    public static MozcBridgeResponse? DeserializeResponse(string json) =>
        JsonSerializer.Deserialize<MozcBridgeResponse>(json, Options);

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
