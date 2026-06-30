using FrooxEngine;
using HarmonyLib;
using JapaneseImeBridge.Rendering;

namespace JapaneseImeBridge.Patches;

[HarmonyPatch(typeof(VirtualKeyboard), "OnCommonUpdate")]
internal static class VirtualKeyboardPatch
{
    [HarmonyPostfix]
    private static void Postfix(VirtualKeyboard __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);

        VirtualKeyboardCompositionDisplay.Update(__instance);
    }
}
