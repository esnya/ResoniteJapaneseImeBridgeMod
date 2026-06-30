using FrooxEngine;

namespace JapaneseImeBridge.Backend;

internal sealed record ImeBackendWorkItem(
    ImeBackendRequest Request,
    VirtualKeyboard? Keyboard = null,
    VirtualKey? Key = null,
    string? ReplayText = null);
