using FrooxEngine;
using HarmonyLib;
using MozcInput.Composition;

namespace MozcInput.Patches;

[HarmonyPatch(typeof(TextEditor))]
internal static class TextEditorPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(TextEditor.Focus))]
    private static void FocusPostfix(TextEditor __instance, User user)
    {
        ArgumentNullException.ThrowIfNull(__instance);

        MozcInputController.HandleTextEditorFocus(__instance, user);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(TextEditor.Defocus))]
    private static void DefocusPostfix(TextEditor __instance, User user)
    {
        ArgumentNullException.ThrowIfNull(__instance);

        MozcInputController.HandleTextEditorDefocus(__instance, user);
    }
}
