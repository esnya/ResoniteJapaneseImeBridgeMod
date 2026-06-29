using MozcInput.Protocol;
using Renderite.Shared;

namespace MozcInput.Composition;

internal sealed record VirtualKeyInput(string? Text, Key TargetKey, IReadOnlyList<Key>? ChordKeys = null, bool ShiftActive = false)
{
    public IReadOnlyList<Key> EffectiveKeys =>
        ChordKeys ?? (TargetKey == Key.None ? [] : [TargetKey]);

    public bool TryToBridgeRequest(out MozcBridgeRequest request, bool hasComposition = false)
    {
        if (TryMapControlKey(TargetKey, hasComposition, out var bridgeKey))
        {
            request = new MozcBridgeRequest(0, MozcBridgeCommand.Key, Key: bridgeKey);
            return true;
        }

        if (!string.IsNullOrEmpty(Text))
        {
            request = new MozcBridgeRequest(0, MozcBridgeCommand.Key, Text);
            return true;
        }

        request = new MozcBridgeRequest(0, MozcBridgeCommand.Key);
        return false;
    }

    private static bool TryMapControlKey(Key key, bool hasComposition, out MozcBridgeKey bridgeKey)
    {
        bridgeKey = key switch
        {
            Key.Space => MozcBridgeKey.Space,
            Key.Return or Key.KeypadEnter => MozcBridgeKey.Enter,
            Key.Escape => MozcBridgeKey.Escape,
            Key.UpArrow => MozcBridgeKey.Up,
            Key.DownArrow => MozcBridgeKey.Down,
            Key.Backspace => MozcBridgeKey.Backspace,
            Key.LeftArrow when hasComposition => MozcBridgeKey.Left,
            Key.RightArrow when hasComposition => MozcBridgeKey.Right,
            Key.LeftBracket when hasComposition => MozcBridgeKey.SegmentWidthShrink,
            Key.RightBracket when hasComposition => MozcBridgeKey.SegmentWidthExpand,
            _ => MozcBridgeKey.None,
        };
        return bridgeKey != MozcBridgeKey.None;
    }
}
