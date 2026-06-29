using System.Reflection;
using MozcInput;

namespace MozcInput.Tests;

public sealed class MetadataTests
{
    [Fact]
    public void ModMetadataUsesPublicMozcInputIdentity()
    {
        var assembly = typeof(MozcInputMod).Assembly;

        Assert.Equal("MozcInput", assembly.GetName().Name);
        Assert.Equal("Mozc Input", assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title);
        Assert.Equal("esnya", assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company);
        Assert.Contains(
            assembly.GetCustomAttributes<AssemblyMetadataAttribute>(),
            metadata => metadata is { Key: "RepositoryUrl", Value: "https://github.com/esnya/ResoniteMozcInputMod" });
    }

#if USE_RESONITE_HOT_RELOAD_LIB
    [Theory]
    [InlineData(nameof(MozcInputMod.BeforeHotReload))]
    [InlineData(nameof(MozcInputMod.OnHotReload))]
    public void ModAlwaysExposesHotReloadLifecycleMethods(string methodName)
    {
        var method = typeof(MozcInputMod).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(method);
    }
#endif
}
