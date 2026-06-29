using FrooxEngine;

namespace MozcInput.Composition;

internal static class VirtualKeyInputResolver
{
    public static VirtualKeyInput Resolve(VirtualKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var keyboard = key.Keyboard.Target;
        if (key.UseModifier)
        {
            return new VirtualKeyInput(key.ModifiedAppendString.Value, key.ModifiedTargetKey.Value);
        }

        if (key.IgnoreShift.Value || keyboard is null || !keyboard.ShiftActive.Value)
        {
            return new VirtualKeyInput(key.AppendString.Value, key.TargetKey.Value);
        }

        return new VirtualKeyInput(key.ShiftAppendString.Value, key.ShiftTargetKey.Value, ShiftActive: true);
    }
}
