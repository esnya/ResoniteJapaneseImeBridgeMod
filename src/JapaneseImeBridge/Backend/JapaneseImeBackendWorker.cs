using System.Collections.Concurrent;
using FrooxEngine;
using JapaneseImeBridge.Composition;

namespace JapaneseImeBridge.Backend;

internal sealed class JapaneseImeBackendWorker : IDisposable
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(3);
    private readonly ConcurrentQueue<ImeBackendWorkItem> pending = new();
    private readonly ConcurrentQueue<ImeBackendResult> completed = new();
    private readonly SemaphoreSlim signal = new(0);
    private readonly CancellationTokenSource cancellation = new();
    private readonly Task workerTask;
    private long sequence;
    private volatile bool disposed;
    private volatile ImeBackendState state = ImeBackendState.Connecting;

    public JapaneseImeBackendWorker(JapaneseImeBridgeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        workerTask = Task.Run(() => RunAsync(settings, cancellation.Token));
    }

    public ImeBackendState State => state;

    public bool TryEnqueue(VirtualKeyboard keyboard, VirtualKey key, ImeBackendRequest request, string? replayText)
    {
        ArgumentNullException.ThrowIfNull(keyboard);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(request);

        if (state != ImeBackendState.Ready || disposed)
        {
            return false;
        }

        pending.Enqueue(new ImeBackendWorkItem(
            request with { Sequence = Interlocked.Increment(ref sequence) },
            keyboard,
            key,
            replayText));
        signal.Release();
        return true;
    }

    public bool TryEnqueueControl(ImeBackendCommand command)
    {
        if (state != ImeBackendState.Ready || disposed)
        {
            return false;
        }

        pending.Enqueue(new ImeBackendWorkItem(new ImeBackendRequest(
            Interlocked.Increment(ref sequence),
            command)));
        signal.Release();
        return true;
    }

    public IReadOnlyList<ImeBackendResult> DrainCompleted()
    {
        var results = new List<ImeBackendResult>();
        while (completed.TryDequeue(out var result))
        {
            results.Add(result);
        }

        return results;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        cancellation.Cancel();
        signal.Release();
        signal.Dispose();
        cancellation.Dispose();
    }

    private async Task RunAsync(JapaneseImeBridgeSettings settings, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            state = ImeBackendState.Unavailable;
            return;
        }

        var runtime = GoogleJapaneseInputRuntime.Detect(settings.GoogleJapaneseInputDirectory);
        if (runtime is null)
        {
            state = ImeBackendState.Unavailable;
            return;
        }

        GoogleJapaneseInputEngine engine;
        try
        {
            engine = new GoogleJapaneseInputEngine(runtime);
            var createSession = await CallWithTimeoutAsync(
                engine,
                new ImeBackendRequest(Interlocked.Increment(ref sequence), ImeBackendCommand.CreateSession),
                cancellationToken).ConfigureAwait(false);
            if (!createSession.Handled)
            {
                state = ImeBackendState.Unavailable;
                return;
            }

            state = ImeBackendState.Ready;
        }
        catch (Exception ex) when (IsBackendException(ex))
        {
            state = ImeBackendState.Faulted;
            completed.Enqueue(new ImeBackendResult(null, null, ImeBackendResponse.Failure(0, ex.Message), Error: ex.Message));
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await signal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (pending.TryDequeue(out var work))
            {
                try
                {
                    var response = await CallWithTimeoutAsync(engine, work.Request, cancellationToken).ConfigureAwait(false);
                    completed.Enqueue(new ImeBackendResult(work.Keyboard, work.Key, response, work.ReplayText));
                }
                catch (Exception ex) when (IsBackendException(ex))
                {
                    state = ImeBackendState.Faulted;
                    completed.Enqueue(new ImeBackendResult(
                        work.Keyboard,
                        work.Key,
                        ImeBackendResponse.Failure(work.Request.Sequence, ex.Message),
                        work.ReplayText,
                        ex.Message));
                    return;
                }
            }
        }
    }

    private static async Task<ImeBackendResponse> CallWithTimeoutAsync(
        GoogleJapaneseInputEngine engine,
        ImeBackendRequest request,
        CancellationToken cancellationToken)
    {
        var callTask = Task.Run(() => engine.Handle(request), cancellationToken);
        return await callTask.WaitAsync(RequestTimeout, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsBackendException(Exception ex) =>
        ex is IOException
            or TimeoutException
            or InvalidDataException
            or UnauthorizedAccessException
            or OperationCanceledException;
}
