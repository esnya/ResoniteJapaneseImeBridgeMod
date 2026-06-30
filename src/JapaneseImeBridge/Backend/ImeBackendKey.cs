namespace JapaneseImeBridge.Backend;

/// <summary>
/// Control keys understood by the local IME backend.
/// </summary>
public enum ImeBackendKey
{
    /// <summary>No control key.</summary>
    None,

    /// <summary>Space conversion key.</summary>
    Space,

    /// <summary>Enter commit key.</summary>
    Enter,

    /// <summary>Escape cancel key.</summary>
    Escape,

    /// <summary>Move to the previous candidate.</summary>
    Up,

    /// <summary>Move to the next candidate.</summary>
    Down,

    /// <summary>Delete the previous input unit.</summary>
    Backspace,

    /// <summary>Move conversion segment focus left.</summary>
    Left,

    /// <summary>Move conversion segment focus right.</summary>
    Right,

    /// <summary>Shrink the active conversion segment.</summary>
    SegmentWidthShrink,

    /// <summary>Expand the active conversion segment.</summary>
    SegmentWidthExpand,
}
