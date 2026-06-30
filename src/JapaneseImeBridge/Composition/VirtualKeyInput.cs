using JapaneseImeBridge.Backend;
using Renderite.Shared;

namespace JapaneseImeBridge.Composition;

internal sealed record VirtualKeyInput(string? Text, Key TargetKey, IReadOnlyList<Key>? ChordKeys = null, bool ShiftActive = false)
{
    public IReadOnlyList<Key> EffectiveKeys =>
        ChordKeys ?? (TargetKey == Key.None ? [] : [TargetKey]);

    public bool TryToBridgeRequest(out ImeBackendRequest request, bool hasComposition = false)
    {
        if (TryMapControlKey(TargetKey, hasComposition, out var bridgeKey))
        {
            request = new ImeBackendRequest(0, ImeBackendCommand.Key, Key: bridgeKey);
            return true;
        }

        if (!string.IsNullOrEmpty(Text))
        {
            request = new ImeBackendRequest(0, ImeBackendCommand.Key, Text);
            return true;
        }

        request = new ImeBackendRequest(0, ImeBackendCommand.Key);
        return false;
    }

    private static bool TryMapControlKey(Key key, bool hasComposition, out ImeBackendKey bridgeKey)
    {
        bridgeKey = key switch
        {
            Key.Space => ImeBackendKey.Space,
            Key.Return or Key.KeypadEnter => ImeBackendKey.Enter,
            Key.Escape => ImeBackendKey.Escape,
            Key.UpArrow => ImeBackendKey.Up,
            Key.DownArrow => ImeBackendKey.Down,
            Key.Backspace => ImeBackendKey.Backspace,
            Key.LeftArrow when hasComposition => ImeBackendKey.Left,
            Key.RightArrow when hasComposition => ImeBackendKey.Right,
            Key.LeftBracket when hasComposition => ImeBackendKey.SegmentWidthShrink,
            Key.RightBracket when hasComposition => ImeBackendKey.SegmentWidthExpand,
            _ => ImeBackendKey.None,
        };
        return bridgeKey != ImeBackendKey.None;
    }
}
