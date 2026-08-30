using Mono.Cecil;

namespace JapaneseImeBridge.Tests;

public sealed class MetadataTests
{
    [Fact]
    public void ModMetadataUsesPublicJapaneseImeBridgeIdentity()
    {
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(GetAssemblyPath());

        Assert.Equal("JapaneseImeBridge", assembly.Name.Name);
        Assert.Contains(
            assembly.CustomAttributes,
            static attribute => HasSingleValue(attribute, "System.Reflection.AssemblyTitleAttribute", "Japanese IME Bridge"));
        Assert.Contains(
            assembly.CustomAttributes,
            static attribute => HasSingleValue(attribute, "System.Reflection.AssemblyCompanyAttribute", "esnya"));
        Assert.Contains(
            assembly.CustomAttributes,
            static attribute =>
                attribute.AttributeType.FullName == "System.Reflection.AssemblyMetadataAttribute"
                && attribute.ConstructorArguments.Count == 2
                && Equals(attribute.ConstructorArguments[0].Value, "RepositoryUrl")
                && Equals(attribute.ConstructorArguments[1].Value, "https://github.com/esnya/ResoniteJapaneseImeBridgeMod"));
    }

    [Fact]
    public void DefaultConfigurationStartsImeInactive()
    {
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(GetAssemblyPath());
        TypeDefinition modType = assembly.MainModule.GetType("JapaneseImeBridge.JapaneseImeBridgeMod")
            ?? throw new InvalidOperationException("JapaneseImeBridgeMod was not found.");
        FieldDefinition field = Assert.Single(modType.Fields, field => field.Name == "DefaultImeActiveByDefault");

        Assert.True(field.IsLiteral);
        Assert.False(Assert.IsType<bool>(field.Constant));
    }

#if USE_RESONITE_HOT_RELOAD_LIB
    [Theory]
    [InlineData(nameof(JapaneseImeBridgeMod.BeforeHotReload))]
    [InlineData(nameof(JapaneseImeBridgeMod.OnHotReload))]
    public void ModAlwaysExposesHotReloadLifecycleMethods(string methodName)
    {
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(GetAssemblyPath());
        TypeDefinition modType = assembly.MainModule.GetType("JapaneseImeBridge.JapaneseImeBridgeMod")
            ?? throw new InvalidOperationException("JapaneseImeBridgeMod was not found.");

        Assert.Contains(modType.Methods, method => method.IsPublic && method.IsStatic && method.Name == methodName);
    }
#endif

    private static bool HasSingleValue(CustomAttribute attribute, string attributeType, string value)
    {
        return attribute.AttributeType.FullName == attributeType
            && attribute.ConstructorArguments.Count == 1
            && Equals(attribute.ConstructorArguments[0].Value, value);
    }

    private static string GetAssemblyPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "JapaneseImeBridge.dll");
    }
}
