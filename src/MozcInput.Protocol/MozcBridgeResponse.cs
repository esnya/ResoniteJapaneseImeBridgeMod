namespace MozcInput.Protocol;

/// <summary>
/// One line-delimited JSON response returned from the bridge process.
/// </summary>
public sealed record MozcBridgeResponse(
    long Sequence,
    bool Handled,
    string? PreeditText = null,
    string? CommitText = null,
    IReadOnlyList<string>? Candidates = null,
    int FocusedCandidateIndex = -1,
    string? Error = null)
{
    /// <summary>
    /// Creates a pass-through response for input the bridge did not handle.
    /// </summary>
    public static MozcBridgeResponse PassThrough(long sequence) =>
        new(sequence, Handled: false);

    /// <summary>
    /// Creates an error response without throwing across the process boundary.
    /// </summary>
    public static MozcBridgeResponse Failure(long sequence, string error) =>
        new(sequence, Handled: false, Error: error);
}
