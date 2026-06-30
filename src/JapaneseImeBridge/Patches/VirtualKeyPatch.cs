using FrooxEngine;
using HarmonyLib;
using JapaneseImeBridge.Composition;

namespace JapaneseImeBridge.Patches;

[HarmonyPatch(typeof(VirtualKey), nameof(VirtualKey.Press))]
internal static class VirtualKeyPatch
{
    [HarmonyPrefix]
    private static bool Prefix(VirtualKey __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);

        return JapaneseImeBridgeController.ProcessVirtualKey(__instance);
    }
}
