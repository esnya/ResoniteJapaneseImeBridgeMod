using FrooxEngine;
using HarmonyLib;
using MozcInput.Composition;

namespace MozcInput.Patches;

[HarmonyPatch(typeof(VirtualKey), nameof(VirtualKey.Press))]
internal static class VirtualKeyPatch
{
    [HarmonyPrefix]
    private static bool Prefix(VirtualKey __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);

        return MozcInputController.ProcessVirtualKey(__instance);
    }
}
