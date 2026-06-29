using HarmonyLib;
using MozcInput.Patches;

namespace MozcInput.Tests;

public sealed class PatchContractTests
{
    [Fact]
    public void VirtualKeyPatchTargetsPress()
    {
        var patch = typeof(VirtualKeyPatch)
            .GetCustomAttributesData()
            .Single(attribute => attribute.AttributeType == typeof(HarmonyPatch));

        Assert.Contains(patch.ConstructorArguments, argument => Equals(argument.Value, "Press"));
    }

    [Fact]
    public void VirtualKeyboardPatchTargetsOnCommonUpdate()
    {
        Assert.Contains(
            typeof(VirtualKeyboardPatch).GetCustomAttributesData(),
            attribute => attribute.AttributeType == typeof(HarmonyPatch));
    }

    [Fact]
    public void VirtualMultiKeyPatchTargetsPressed()
    {
        var patch = typeof(VirtualMultiKeyPatch)
            .GetCustomAttributesData()
            .Single(attribute => attribute.AttributeType == typeof(HarmonyPatch));

        Assert.Contains(patch.ConstructorArguments, argument => Equals(argument.Value, "Pressed"));
    }

    [Theory]
    [InlineData("Focus")]
    [InlineData("Defocus")]
    public void TextEditorPatchTargetsFocusLifecycle(string methodName)
    {
        Assert.Contains(
            typeof(TextEditorPatch).GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static),
            method => method
                .GetCustomAttributesData()
                .Any(attribute =>
                    attribute.AttributeType == typeof(HarmonyPatch)
                    && attribute.ConstructorArguments.Any(argument => Equals(argument.Value, methodName))));
    }

    [Fact]
    public void OldPreeditOverlayManagerIsNotPresent()
    {
        var assembly = typeof(VirtualKeyPatch).Assembly;

        Assert.Null(assembly.GetType("MozcInput.Rendering.PreeditOverlayManager", throwOnError: false));
    }
}
