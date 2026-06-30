using FrooxEngine;
using HarmonyLib;
using JapaneseImeBridge.Composition;

namespace JapaneseImeBridge.Patches;

[HarmonyPatch(typeof(VirtualMultiKey), nameof(VirtualMultiKey.Pressed))]
internal static class VirtualMultiKeyPatch
{
    [HarmonyPrefix]
    private static bool Prefix(VirtualMultiKey __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);

        return JapaneseImeBridgeController.ProcessVirtualMultiKey(__instance);
    }
}
