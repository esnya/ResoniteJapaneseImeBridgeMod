using JapaneseImeBridge.Backend.Google;

namespace JapaneseImeBridge.Backend;

internal sealed class GoogleJapaneseInputEngine(GoogleJapaneseInputRuntime runtime)
{
    private readonly GoogleJapaneseInputIpcClient client = new(runtime);
    private ulong sessionId;

    public GoogleJapaneseInputRuntime Runtime { get; } = runtime ?? throw new ArgumentNullException(nameof(runtime));

    public ImeBackendResponse Handle(ImeBackendRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Command switch
        {
            ImeBackendCommand.CreateSession => CreateSession(request.Sequence),
            ImeBackendCommand.Reset => Reset(request.Sequence),
            ImeBackendCommand.Cancel => SendSessionCommand(request.Sequence, MozcIpcCommandCodec.Revert),
            ImeBackendCommand.Commit => SendSessionCommand(request.Sequence, MozcIpcCommandCodec.Submit),
            ImeBackendCommand.Key => SendKey(request),
            _ => ImeBackendResponse.PassThrough(request.Sequence),
        };
    }

    private ImeBackendResponse CreateSession(long sequence)
    {
        var output = Call(MozcIpcCommandCodec.CreateSession());
        sessionId = output.SessionId;
        if (sessionId != 0)
        {
            _ = Call(MozcIpcCommandCodec.TurnOnIme(sessionId));
        }

        return new ImeBackendResponse(sequence, Handled: sessionId != 0);
    }

    private ImeBackendResponse Reset(long sequence)
    {
        if (sessionId != 0)
        {
            _ = SendSessionCommand(sequence, MozcIpcCommandCodec.ResetContext);
        }

        sessionId = 0;
        return CreateSession(sequence);
    }

    private ImeBackendResponse SendKey(ImeBackendRequest request)
    {
        EnsureSession(request.Sequence);
        var output = Call(MozcIpcCommandCodec.SendKey(sessionId, request));
        return ToBridgeResponse(request.Sequence, output);
    }

    private ImeBackendResponse SendSessionCommand(long sequence, Func<ulong, byte[]> writeCommand)
    {
        EnsureSession(sequence);
        var output = Call(writeCommand(sessionId));
        return ToBridgeResponse(sequence, output);
    }

    private void EnsureSession(long sequence)
    {
        if (sessionId == 0)
        {
            _ = CreateSession(sequence);
        }
    }

    private MozcIpcOutput Call(byte[] request) => MozcIpcCommandCodec.ParseOutput(client.Call(request));

    private static ImeBackendResponse ToBridgeResponse(long sequence, MozcIpcOutput output) =>
        new(
            sequence,
            output.Consumed || !string.IsNullOrEmpty(output.Preedit) || !string.IsNullOrEmpty(output.CommitText),
            PreeditText: output.Preedit,
            CommitText: output.CommitText,
            Candidates: output.Candidates,
            FocusedCandidateIndex: output.FocusedCandidateIndex);
}
