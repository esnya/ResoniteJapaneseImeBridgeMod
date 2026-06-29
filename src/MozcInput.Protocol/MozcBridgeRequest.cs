namespace MozcInput.Protocol;

/// <summary>
/// One line-delimited JSON request sent to the bridge process.
/// </summary>
public sealed record MozcBridgeRequest(
    long Sequence,
    MozcBridgeCommand Command,
    string? Text = null,
    MozcBridgeKey Key = MozcBridgeKey.None);
