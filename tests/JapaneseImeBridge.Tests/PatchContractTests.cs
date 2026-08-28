using Mono.Cecil;

namespace JapaneseImeBridge.Tests;

public sealed class PatchContractTests
{
    private const string HarmonyPatchAttribute = "HarmonyLib.HarmonyPatch";

    [Fact]
    public void VirtualKeyPatchTargetsPress()
    {
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(GetAssemblyPath());
        TypeDefinition patchType = GetRequiredType(assembly, "JapaneseImeBridge.Patches.VirtualKeyPatch");
        CustomAttribute patch = Assert.Single(
            patchType.CustomAttributes,
            static attribute => attribute.AttributeType.FullName == HarmonyPatchAttribute);

        Assert.Contains(patch.ConstructorArguments, argument => Equals(argument.Value, "Press"));
    }

    [Fact]
    public void VirtualKeyboardPatchTargetsOnCommonUpdate()
    {
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(GetAssemblyPath());
        TypeDefinition patchType = GetRequiredType(assembly, "JapaneseImeBridge.Patches.VirtualKeyboardPatch");

        Assert.Contains(
            patchType.CustomAttributes,
            static attribute => attribute.AttributeType.FullName == HarmonyPatchAttribute);
    }

    [Fact]
    public void VirtualMultiKeyPatchTargetsPressed()
    {
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(GetAssemblyPath());
        TypeDefinition patchType = GetRequiredType(assembly, "JapaneseImeBridge.Patches.VirtualMultiKeyPatch");
        CustomAttribute patch = Assert.Single(
            patchType.CustomAttributes,
            static attribute => attribute.AttributeType.FullName == HarmonyPatchAttribute);

        Assert.Contains(patch.ConstructorArguments, argument => Equals(argument.Value, "Pressed"));
    }

    [Theory]
    [InlineData("Focus")]
    [InlineData("Defocus")]
    public void TextEditorPatchTargetsFocusLifecycle(string methodName)
    {
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(GetAssemblyPath());
        TypeDefinition patchType = GetRequiredType(assembly, "JapaneseImeBridge.Patches.TextEditorPatch");

        Assert.Contains(
            patchType.Methods,
            method => method
                .CustomAttributes
                .Any(attribute =>
                    attribute.AttributeType.FullName == HarmonyPatchAttribute
                    && attribute.ConstructorArguments.Any(argument => Equals(argument.Value, methodName))));
    }

    [Fact]
    public void OldPreeditOverlayManagerIsNotPresent()
    {
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(GetAssemblyPath());

        Assert.Null(assembly.MainModule.GetType("JapaneseImeBridge.Rendering.PreeditOverlayManager"));
    }

    [Fact]
    public void ObsoleteBridgeAndFallbackTypesAreNotPresent()
    {
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(GetAssemblyPath());

        Assert.Null(assembly.MainModule.GetType("JapaneseImeBridge.Ipc.BridgeClient"));
        Assert.Null(assembly.MainModule.GetType("JapaneseImeBridge.Backend.RomanKanaEngine"));
    }

    private static string GetAssemblyPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "JapaneseImeBridge.dll");
    }

    private static TypeDefinition GetRequiredType(AssemblyDefinition assembly, string fullName)
    {
        return assembly.MainModule.GetType(fullName)
            ?? throw new InvalidOperationException($"Required type '{fullName}' was not found in '{assembly.MainModule.FileName}'.");
    }
}
