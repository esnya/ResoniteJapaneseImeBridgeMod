namespace JapaneseImeBridge.Backend;

internal sealed record GoogleJapaneseInputRuntime(string InstallDirectory, string ConverterPath)
{
    private const string DefaultInstallDirectory = @"C:\Program Files (x86)\Google\Google Japanese Input";
    private const string ConverterFileName = "GoogleIMEJaConverter.exe";

    public static GoogleJapaneseInputRuntime? Detect(string? configuredDirectory = null)
    {
        var installDirectory = string.IsNullOrWhiteSpace(configuredDirectory)
            ? DefaultInstallDirectory
            : configuredDirectory;
        var converterPath = Path.Combine(installDirectory, ConverterFileName);
        return File.Exists(converterPath)
            ? new GoogleJapaneseInputRuntime(installDirectory, converterPath)
            : null;
    }
}
