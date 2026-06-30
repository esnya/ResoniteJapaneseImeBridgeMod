using System.Reflection;
using HarmonyLib;
using JapaneseImeBridge.Composition;
using ResoniteModLoader;
#if USE_RESONITE_HOT_RELOAD_LIB
using ResoniteHotReloadLib;
#endif

namespace JapaneseImeBridge;

/// <summary>
/// ResoniteModLoader entry point for Japanese IME Bridge.
/// </summary>
public sealed class JapaneseImeBridgeMod : ResoniteMod
{
    private const string ModNamespace = "com.nekometer.esnya";
    private static readonly Assembly Assembly = typeof(JapaneseImeBridgeMod).Assembly;
    private static readonly string HarmonyId = $"{ModNamespace}.{Assembly.GetName().Name}";
    private static readonly Harmony Harmony = new(HarmonyId);
    private static readonly Lock PatchStateLock = new();

    private static ModConfiguration? config;
    private static bool patchesApplied;

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<bool> EnabledKey = new(
        "Enabled",
        "Enable Japanese IME Bridge for focused text editors.",
        computeDefault: () => true);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<string> GoogleJapaneseInputDirectoryKey = new(
        "GoogleJapaneseInputDirectory",
        "Path to the Google Japanese Input install directory containing GoogleIMEJaConverter.exe.",
        computeDefault: static () => string.Empty);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<bool> ShowCandidatePanelKey = new(
        "ShowCandidatePanel",
        "Show composition and candidate text in the virtual keyboard text preview.",
        computeDefault: () => true);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<bool> DefaultImeActiveKey = new(
        "DefaultImeActive",
        "Start each virtual keyboard target with Japanese IME mode active.",
        computeDefault: () => true);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<string> ImeToggleKeyCombosKey = new(
        "ImeToggleKeyCombos",
        "Semicolon-separated Renderite.Shared.Key combos that toggle Japanese IME mode.",
        computeDefault: () => VirtualImeSwitchMatcher.DefaultToggleKeyCombos);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<string> ImeOnKeyCombosKey = new(
        "ImeOnKeyCombos",
        "Semicolon-separated Renderite.Shared.Key combos that enable Japanese IME mode.",
        computeDefault: () => VirtualImeSwitchMatcher.DefaultOnKeyCombos);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<string> ImeOffKeyCombosKey = new(
        "ImeOffKeyCombos",
        "Semicolon-separated Renderite.Shared.Key combos that disable Japanese IME mode.",
        computeDefault: () => VirtualImeSwitchMatcher.DefaultOffKeyCombos);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<string> ImeToggleTextKeysKey = new(
        "ImeToggleTextKeys",
        "Fallback semicolon-separated virtual key text values that toggle Japanese IME mode.",
        computeDefault: () => VirtualImeSwitchMatcher.DefaultToggleTextKeys);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<string> ImeOnTextKeysKey = new(
        "ImeOnTextKeys",
        "Fallback semicolon-separated virtual key text values that enable Japanese IME mode.",
        computeDefault: () => VirtualImeSwitchMatcher.DefaultOnTextKeys);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<string> ImeOffTextKeysKey = new(
        "ImeOffTextKeys",
        "Fallback semicolon-separated virtual key text values that disable Japanese IME mode.",
        computeDefault: () => VirtualImeSwitchMatcher.DefaultOffTextKeys);

    /// <inheritdoc />
    public override string Name =>
        Assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? Assembly.GetName().Name ?? string.Empty;

    /// <inheritdoc />
    public override string Author =>
        Assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? string.Empty;

    /// <inheritdoc />
    public override string Version =>
        Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(static metadata => metadata.Key == "ModVersion")
            ?.Value
        ?? (Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty)
            .Split('+')[0];

    /// <inheritdoc />
    public override string Link =>
        Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(static metadata => metadata.Key == "RepositoryUrl")
            ?.Value ?? string.Empty;

    /// <inheritdoc />
    public override void OnEngineInit()
    {
        Initialize(this);
    }

    internal static JapaneseImeBridgeSettings CurrentSettings =>
        new(
            GetConfigValue(EnabledKey, fallback: true),
            GetConfigValue(GoogleJapaneseInputDirectoryKey, fallback: string.Empty),
            GetConfigValue(ShowCandidatePanelKey, fallback: true),
            GetConfigValue(DefaultImeActiveKey, fallback: true),
            GetConfigValue(ImeToggleKeyCombosKey, fallback: VirtualImeSwitchMatcher.DefaultToggleKeyCombos),
            GetConfigValue(ImeOnKeyCombosKey, fallback: VirtualImeSwitchMatcher.DefaultOnKeyCombos),
            GetConfigValue(ImeOffKeyCombosKey, fallback: VirtualImeSwitchMatcher.DefaultOffKeyCombos),
            GetConfigValue(ImeToggleTextKeysKey, fallback: VirtualImeSwitchMatcher.DefaultToggleTextKeys),
            GetConfigValue(ImeOnTextKeysKey, fallback: VirtualImeSwitchMatcher.DefaultOnTextKeys),
            GetConfigValue(ImeOffTextKeysKey, fallback: VirtualImeSwitchMatcher.DefaultOffTextKeys));

    internal static void DebugLog(Func<string> messageFactory)
    {
        DebugFunc(messageFactory);
    }

#if USE_RESONITE_HOT_RELOAD_LIB
    /// <summary>
    /// Removes Harmony patches before a hot reload cycle.
    /// </summary>
    public static void BeforeHotReload()
    {
        config?.OnThisConfigurationChanged -= HandleConfigurationChanged;
        config = null;

        SetPatchesApplied(shouldPatch: false);
        JapaneseImeBridgeController.Dispose();
    }

    /// <summary>
    /// Reinitializes the mod after a hot reload cycle.
    /// </summary>
    /// <param name="mod">The reloaded mod instance.</param>
    public static void OnHotReload(ResoniteMod mod)
    {
        Initialize(mod);
    }
#endif

    private static void Initialize(ResoniteMod mod)
    {
        ArgumentNullException.ThrowIfNull(mod);

        config?.OnThisConfigurationChanged -= HandleConfigurationChanged;

        config = mod.GetConfiguration();
        config?.OnThisConfigurationChanged += HandleConfigurationChanged;

        RefreshPatchState();
#if USE_RESONITE_HOT_RELOAD_LIB
        HotReloader.RegisterForHotReload(mod);
#endif
    }

    private static void HandleConfigurationChanged(ConfigurationChangedEvent _)
    {
        RefreshPatchState();
    }

    private static void RefreshPatchState()
    {
        var settings = CurrentSettings;
        SetPatchesApplied(settings.Enabled);
        JapaneseImeBridgeController.UpdateSettings(settings);
    }

    private static void SetPatchesApplied(bool shouldPatch)
    {
        lock (PatchStateLock)
        {
            if (patchesApplied == shouldPatch)
            {
                return;
            }

            if (shouldPatch)
            {
                Harmony.PatchAll(Assembly);
            }
            else
            {
                Harmony.UnpatchAll(HarmonyId);
                JapaneseImeBridgeController.Reset();
            }

            patchesApplied = shouldPatch;
        }

        DebugFunc(() => $"[Japanese IME Bridge] Harmony patches {(shouldPatch ? "applied" : "removed")}.");
    }

    private static T GetConfigValue<T>(ModConfigurationKey<T> key, T fallback)
    {
        ArgumentNullException.ThrowIfNull(key);

        return config is null ? fallback : config.TryGetValue(key, out T? value) ? value! : fallback;
    }
}
