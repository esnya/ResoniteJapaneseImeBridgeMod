using FrooxEngine;

namespace JapaneseImeBridge.Backend;

internal sealed record ImeBackendResult(
    VirtualKeyboard? Keyboard,
    VirtualKey? Key,
    ImeBackendResponse Response,
    string? ReplayText = null,
    string? Error = null);
