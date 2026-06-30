using System.Diagnostics;
using System.IO.Pipes;

namespace JapaneseImeBridge.Backend.Google;

#pragma warning disable CA1416
internal sealed class GoogleJapaneseInputIpcClient(GoogleJapaneseInputRuntime runtime)
{
    private const string PipePrefix = "googlejapaneseinput.";
    private const string PipeSuffix = ".session";
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromMilliseconds(1500);
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(3);

    public byte[] Call(byte[] request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pipeName = FindSessionPipeName() ?? StartConverterAndWaitForPipe();
        using var pipe = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.None);
        pipe.Connect(checked((int)ConnectTimeout.TotalMilliseconds));
        pipe.ReadMode = PipeTransmissionMode.Message;
        pipe.Write(request);
        pipe.Flush();

        using var response = new MemoryStream();
        var buffer = new byte[16 * 16384];
        do
        {
            var read = pipe.Read(buffer);
            if (read == 0)
            {
                break;
            }

            response.Write(buffer, 0, read);
        }
        while (!pipe.IsMessageComplete);

        return response.ToArray();
    }

    private string StartConverterAndWaitForPipe()
    {
        using var _ = Process.Start(new ProcessStartInfo(runtime.ConverterPath)
        {
            WorkingDirectory = runtime.InstallDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
        });

        var deadline = DateTimeOffset.UtcNow + StartupTimeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var pipeName = FindSessionPipeName();
            if (pipeName is not null)
            {
                return pipeName;
            }

            Thread.Sleep(100);
        }

        throw new IOException("Google Japanese Input session pipe was not found.");
    }

    private static string? FindSessionPipeName()
    {
        foreach (var path in Directory.EnumerateFiles(@"\\.\pipe\"))
        {
            var name = Path.GetFileName(path);
            if (name.StartsWith(PipePrefix, StringComparison.OrdinalIgnoreCase)
                && name.EndsWith(PipeSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }
        }

        return null;
    }
}
#pragma warning restore CA1416
