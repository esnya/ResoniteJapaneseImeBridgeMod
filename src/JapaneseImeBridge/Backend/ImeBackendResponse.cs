namespace JapaneseImeBridge.Backend;

/// <summary>
/// Response returned from the local IME backend worker.
/// </summary>
public sealed record ImeBackendResponse(
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
    public static ImeBackendResponse PassThrough(long sequence) =>
        new(sequence, Handled: false);

    /// <summary>
    /// Creates an error response without throwing across the backend boundary.
    /// </summary>
    public static ImeBackendResponse Failure(long sequence, string error) =>
        new(sequence, Handled: false, Error: error);
}
