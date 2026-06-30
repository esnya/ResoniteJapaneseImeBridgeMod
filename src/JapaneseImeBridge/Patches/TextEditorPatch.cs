using FrooxEngine;
using HarmonyLib;
using JapaneseImeBridge.Composition;

namespace JapaneseImeBridge.Patches;

[HarmonyPatch(typeof(TextEditor))]
internal static class TextEditorPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(TextEditor.Focus))]
    private static void FocusPostfix(TextEditor __instance, User user)
    {
        ArgumentNullException.ThrowIfNull(__instance);

        JapaneseImeBridgeController.HandleTextEditorFocus(__instance, user);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(TextEditor.Defocus))]
    private static void DefocusPostfix(TextEditor __instance, User user)
    {
        ArgumentNullException.ThrowIfNull(__instance);

        JapaneseImeBridgeController.HandleTextEditorDefocus(__instance, user);
    }
}
