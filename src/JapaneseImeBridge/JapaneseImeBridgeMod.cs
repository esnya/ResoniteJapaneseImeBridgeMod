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
    internal const bool DefaultImeActiveByDefault = false;
    private static readonly Assembly Assembly = typeof(JapaneseImeBridgeMod).Assembly;
    private static readonly string HarmonyId = $"{ModNamespace}.{Assembly.GetName().Name}";
    private static readonly Harmony Harmony = new(HarmonyId);
    private static readonly Lock PatchStateLock = new();

    private static ModConfiguration? config;
    private static bool patchesApplied;

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<bool> EnabledKey = new(
        "Enabled",
        "IMEブリッジを有効にする",
        computeDefault: () => true);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<string> GoogleJapaneseInputDirectoryKey = new(
        "GoogleJapaneseInputDirectory",
        "Google 日本語入力のインストール先",
        computeDefault: static () => string.Empty);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<bool> ShowCandidatePanelKey = new(
        "ShowCandidatePanel",
        "入力中の文字と変換候補を表示",
        computeDefault: () => true);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<bool> DefaultImeActiveKey = new(
        "DefaultImeActive",
        "IMEをオンで開始",
        computeDefault: () => DefaultImeActiveByDefault);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<string> ImeToggleKeyCombosKey = new(
        "ImeToggleKeyCombos",
        "IME切替キー",
        computeDefault: () => VirtualImeSwitchMatcher.DefaultToggleKeyCombos);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<string> ImeOnKeyCombosKey = new(
        "ImeOnKeyCombos",
        "IMEオンキー",
        computeDefault: () => VirtualImeSwitchMatcher.DefaultOnKeyCombos);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<string> ImeOffKeyCombosKey = new(
        "ImeOffKeyCombos",
        "IMEオフキー",
        computeDefault: () => VirtualImeSwitchMatcher.DefaultOffKeyCombos);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<string> ImeToggleTextKeysKey = new(
        "ImeToggleTextKeys",
        "IME切替用の仮想キー名",
        computeDefault: () => VirtualImeSwitchMatcher.DefaultToggleTextKeys);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<string> ImeOnTextKeysKey = new(
        "ImeOnTextKeys",
        "IMEオン用の仮想キー名",
        computeDefault: () => VirtualImeSwitchMatcher.DefaultOnTextKeys);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<string> ImeOffTextKeysKey = new(
        "ImeOffTextKeys",
        "IMEオフ用の仮想キー名",
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
            GetConfigValue(DefaultImeActiveKey, fallback: DefaultImeActiveByDefault),
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
