using FrooxEngine;
using MozcInput.Composition;

namespace MozcInput.Rendering;

internal static class VirtualKeyboardCompositionDisplay
{
    public static void Update(VirtualKeyboard keyboard)
    {
        ArgumentNullException.ThrowIfNull(keyboard);

        var targetText = keyboard.TargetText;
        if (targetText is null || targetText.IsDestroyed)
        {
            MozcInputController.HandleKeyboardTargetUnavailable(keyboard);
            return;
        }

        if (!MozcInputController.TryGetDisplaySnapshot(keyboard, out var snapshot) || !snapshot.HasVisibleComposition)
        {
            return;
        }

        var preview = keyboard.TextPreview.Target;
        if (preview is null || preview.IsDestroyed)
        {
            return;
        }

        var displayText = CompositionDisplayFormatter.Format(
            targetText.Text ?? string.Empty,
            targetText.CaretPosition,
            targetText.SelectionStart,
            snapshot);
        preview.Text = displayText.Text;
        preview.SelectionStart = displayText.SelectionStart;
        preview.CaretPosition = displayText.CaretPosition;
    }
}
