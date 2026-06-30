using System.Reflection;
using JapaneseImeBridge;

namespace JapaneseImeBridge.Tests;

public sealed class MetadataTests
{
    [Fact]
    public void ModMetadataUsesPublicJapaneseImeBridgeIdentity()
    {
        var assembly = typeof(JapaneseImeBridgeMod).Assembly;

        Assert.Equal("JapaneseImeBridge", assembly.GetName().Name);
        Assert.Equal("Japanese IME Bridge", assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title);
        Assert.Equal("esnya", assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company);
        Assert.Contains(
            assembly.GetCustomAttributes<AssemblyMetadataAttribute>(),
            metadata => metadata is { Key: "RepositoryUrl", Value: "https://github.com/esnya/ResoniteJapaneseImeBridgeMod" });
    }

#if USE_RESONITE_HOT_RELOAD_LIB
    [Theory]
    [InlineData(nameof(JapaneseImeBridgeMod.BeforeHotReload))]
    [InlineData(nameof(JapaneseImeBridgeMod.OnHotReload))]
    public void ModAlwaysExposesHotReloadLifecycleMethods(string methodName)
    {
        var method = typeof(JapaneseImeBridgeMod).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(method);
    }
#endif
}
