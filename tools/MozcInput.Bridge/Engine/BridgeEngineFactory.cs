namespace MozcInput.Bridge.Engine;

internal static class BridgeEngineFactory
{
    public static IBridgeEngine Create()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new RomanKanaEngine();
        }

        var runtime = GoogleJapaneseInputRuntime.Detect();
        return runtime is null ? new RomanKanaEngine() : new GoogleJapaneseInputEngine(runtime);
    }
}
