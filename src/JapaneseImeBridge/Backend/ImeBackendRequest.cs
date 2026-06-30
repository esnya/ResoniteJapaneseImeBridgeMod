namespace JapaneseImeBridge.Backend;

/// <summary>
/// Request sent from the VirtualKeyboard hook to the local IME backend worker.
/// </summary>
public sealed record ImeBackendRequest(
    long Sequence,
    ImeBackendCommand Command,
    string? Text = null,
    ImeBackendKey Key = ImeBackendKey.None);
