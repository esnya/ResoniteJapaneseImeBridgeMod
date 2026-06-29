using MozcInput.Protocol;
using MozcInput.Bridge.Engine.Google;
using System.Runtime.Versioning;

namespace MozcInput.Bridge.Engine;

[SupportedOSPlatform("windows")]
internal sealed class GoogleJapaneseInputEngine(GoogleJapaneseInputRuntime runtime) : IBridgeEngine
{
    private readonly RomanKanaEngine fallback = new();
    private readonly GoogleJapaneseInputIpcClient client = new(runtime);
    private ulong sessionId;

    public GoogleJapaneseInputRuntime Runtime { get; } = runtime ?? throw new ArgumentNullException(nameof(runtime));

    public MozcBridgeResponse Handle(MozcBridgeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            return request.Command switch
            {
                MozcBridgeCommand.CreateSession => CreateSession(request.Sequence),
                MozcBridgeCommand.Reset => Reset(request.Sequence),
                MozcBridgeCommand.Shutdown => new MozcBridgeResponse(request.Sequence, Handled: true),
                MozcBridgeCommand.Cancel => SendSessionCommand(request.Sequence, MozcIpcCommandCodec.Revert),
                MozcBridgeCommand.Commit => SendSessionCommand(request.Sequence, MozcIpcCommandCodec.Submit),
                MozcBridgeCommand.Key => SendKey(request),
                _ => MozcBridgeResponse.PassThrough(request.Sequence),
            };
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or InvalidDataException or UnauthorizedAccessException)
        {
            sessionId = 0;
            return fallback.Handle(request);
        }
    }

    private MozcBridgeResponse CreateSession(long sequence)
    {
        var output = Call(MozcIpcCommandCodec.CreateSession());
        sessionId = output.SessionId;
        if (sessionId != 0)
        {
            _ = Call(MozcIpcCommandCodec.TurnOnIme(sessionId));
        }

        return new MozcBridgeResponse(sequence, Handled: sessionId != 0);
    }

    private MozcBridgeResponse Reset(long sequence)
    {
        if (sessionId != 0)
        {
            _ = SendSessionCommand(sequence, MozcIpcCommandCodec.ResetContext);
        }

        sessionId = 0;
        return CreateSession(sequence);
    }

    private MozcBridgeResponse SendKey(MozcBridgeRequest request)
    {
        EnsureSession(request.Sequence);
        var output = Call(MozcIpcCommandCodec.SendKey(sessionId, request));
        return ToBridgeResponse(request.Sequence, output);
    }

    private MozcBridgeResponse SendSessionCommand(long sequence, Func<ulong, byte[]> writeCommand)
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

    private static MozcBridgeResponse ToBridgeResponse(long sequence, MozcIpcOutput output) =>
        new(
            sequence,
            output.Consumed || !string.IsNullOrEmpty(output.Preedit) || !string.IsNullOrEmpty(output.CommitText),
            PreeditText: output.Preedit,
            CommitText: output.CommitText,
            Candidates: output.Candidates,
            FocusedCandidateIndex: output.FocusedCandidateIndex);
}
