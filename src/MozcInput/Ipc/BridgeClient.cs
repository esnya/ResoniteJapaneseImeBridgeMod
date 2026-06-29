using System.Diagnostics;
using System.Reflection;
using MozcInput.Protocol;

namespace MozcInput.Ipc;

internal sealed class BridgeClient : IDisposable
{
    private readonly Process process;
    private readonly Lock ioLock = new();
    private long sequence;
    private bool disposed;

    private BridgeClient(Process process)
    {
        this.process = process;
    }

    public static BridgeClient? TryStart(string configuredPath)
    {
        var bridgePath = ResolveBridgePath(configuredPath);
        if (bridgePath is null)
        {
            return null;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = bridgePath,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        try
        {
            var process = Process.Start(startInfo);
            return process is null ? null : new BridgeClient(process);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    public MozcBridgeResponse Send(MozcBridgeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (ioLock)
        {
            ThrowIfDisposed();
            if (process.HasExited)
            {
                throw new InvalidOperationException("Bridge process already exited.");
            }

            var nextSequence = Interlocked.Increment(ref sequence);
            var sequencedRequest = request with { Sequence = nextSequence };
            process.StandardInput.WriteLine(MozcBridgeSerializer.SerializeRequest(sequencedRequest));
            process.StandardInput.Flush();

            var line = process.StandardOutput.ReadLine()
                ?? throw new InvalidOperationException("Bridge process closed stdout.");

            var response = MozcBridgeSerializer.DeserializeResponse(line)
                ?? throw new InvalidOperationException("Bridge returned invalid JSON.");

            if (response.Sequence != nextSequence)
            {
                throw new InvalidOperationException("Bridge returned an unexpected sequence.");
            }

            return response;
        }
    }

    public void Dispose()
    {
        lock (ioLock)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            try
            {
                if (!process.HasExited)
                {
                    process.StandardInput.WriteLine(MozcBridgeSerializer.SerializeRequest(
                        new MozcBridgeRequest(Interlocked.Increment(ref sequence), MozcBridgeCommand.Shutdown)));
                    process.StandardInput.Flush();
                }
            }
            catch (InvalidOperationException)
            {
            }

            process.Dispose();
        }
    }

    private static string? ResolveBridgePath(string configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        return GetBridgePathCandidates()
            .Select(path => Path.Combine(path, "MozcInput.Bridge.exe"))
            .FirstOrDefault(File.Exists);
    }

    private static IEnumerable<string> GetBridgePathCandidates()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            if (!string.IsNullOrEmpty(assemblyDirectory))
            {
                yield return assemblyDirectory;
                yield return Directory.GetParent(assemblyDirectory)?.FullName ?? assemblyDirectory;
            }
        }

        var baseDirectory = AppContext.BaseDirectory;
        if (!string.IsNullOrEmpty(baseDirectory))
        {
            yield return baseDirectory;
            yield return Path.Combine(baseDirectory, "rml_mods");
            yield return Path.Combine(baseDirectory, "rml_mods", "HotReloadMods");
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
