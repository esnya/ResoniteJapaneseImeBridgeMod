using FrooxEngine;
using HarmonyLib;
using MozcInput.Composition;

namespace MozcInput.Patches;

[HarmonyPatch(typeof(VirtualMultiKey), nameof(VirtualMultiKey.Pressed))]
internal static class VirtualMultiKeyPatch
{
    [HarmonyPrefix]
    private static bool Prefix(VirtualMultiKey __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);

        return MozcInputController.ProcessVirtualMultiKey(__instance);
    }
}
