using FrooxEngine;
using JapaneseImeBridge.Composition;

namespace JapaneseImeBridge.Rendering;

internal static class VirtualKeyboardCompositionDisplay
{
    public static void Update(VirtualKeyboard keyboard)
    {
        ArgumentNullException.ThrowIfNull(keyboard);

        JapaneseImeBridgeController.FlushBackendResults();

        var targetText = keyboard.TargetText;
        if (targetText is null || targetText.IsDestroyed)
        {
            JapaneseImeBridgeController.HandleKeyboardTargetUnavailable(keyboard);
            return;
        }

        if (!JapaneseImeBridgeController.TryGetDisplaySnapshot(keyboard, out var snapshot) || !snapshot.HasVisibleComposition)
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
