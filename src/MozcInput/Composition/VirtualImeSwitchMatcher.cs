using Renderite.Shared;

namespace MozcInput.Composition;

internal static class VirtualImeSwitchMatcher
{
    public const string DefaultToggleKeyCombos = "LeftWindows;Alt+BackQuote";
    public const string DefaultOnKeyCombos = "Control+CapsLock;Alt+CapsLock";
    public const string DefaultOffKeyCombos = "Shift+CapsLock";
    public const string DefaultToggleTextKeys = "半角/全角;Hankaku/Zenkaku;Kanji";
    public const string DefaultOnTextKeys = "Kana";
    public const string DefaultOffTextKeys = "Eisu";

    public static VirtualImeSwitchAction Match(VirtualKeyInput input, MozcInputSettings settings)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(settings);

        var keys = Normalize(input.EffectiveKeys);
        if (keys.Count > 0)
        {
            if (ContainsCombo(settings.ImeToggleKeyCombos, keys))
            {
                return VirtualImeSwitchAction.Toggle;
            }

            if (ContainsCombo(settings.ImeOnKeyCombos, keys))
            {
                return VirtualImeSwitchAction.Enable;
            }

            if (ContainsCombo(settings.ImeOffKeyCombos, keys))
            {
                return VirtualImeSwitchAction.Disable;
            }
        }

        var text = NormalizeText(input.Text);
        if (string.IsNullOrEmpty(text))
        {
            return VirtualImeSwitchAction.None;
        }

        if (ContainsText(settings.ImeToggleTextKeys, text))
        {
            return VirtualImeSwitchAction.Toggle;
        }

        if (ContainsText(settings.ImeOnTextKeys, text))
        {
            return VirtualImeSwitchAction.Enable;
        }

        return ContainsText(settings.ImeOffTextKeys, text)
            ? VirtualImeSwitchAction.Disable
            : VirtualImeSwitchAction.None;
    }

    private static bool ContainsCombo(string combos, IReadOnlySet<Key> keys) =>
        SplitCombos(combos).Any(combo => combo.SetEquals(keys));

    private static IEnumerable<HashSet<Key>> SplitCombos(string combos)
    {
        foreach (var combo in combos.Split([';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var keys = combo
                .Split(['+'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(ParseKey)
                .Where(static key => key != Key.None)
                .Select(Normalize)
                .ToHashSet();
            if (keys.Count > 0)
            {
                yield return keys;
            }
        }
    }

    private static Key ParseKey(string keyName) =>
        Enum.TryParse<Key>(keyName, ignoreCase: true, out var key) ? key : Key.None;

    private static HashSet<Key> Normalize(IEnumerable<Key> keys) =>
        [.. keys
            .Where(static key => key != Key.None)
            .Select(Normalize)];

    private static Key Normalize(Key key) =>
        key switch
        {
            Key.LeftAlt or Key.RightAlt or Key.AltGr => Key.Alt,
            Key.LeftControl or Key.RightControl => Key.Control,
            Key.LeftShift or Key.RightShift => Key.Shift,
            _ => key,
        };

    private static bool ContainsText(string keys, string text) =>
        SplitTextKeys(keys).Any(key => string.Equals(key, text, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<string> SplitTextKeys(string keys) =>
        keys
            .Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeText)
            .Where(static key => key.Length > 0);

    private static string NormalizeText(string? text) => (text ?? string.Empty).Trim();
}
