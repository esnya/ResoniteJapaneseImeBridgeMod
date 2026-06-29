using MozcInput.Protocol;

namespace MozcInput.Tests;

public sealed class ProtocolTests
{
    [Fact]
    public void RequestRoundTripsThroughLineJson()
    {
        var request = new MozcBridgeRequest(42, MozcBridgeCommand.Key, "konnichiha", MozcBridgeKey.None);

        var json = MozcBridgeSerializer.SerializeRequest(request);
        var roundTrip = MozcBridgeSerializer.DeserializeRequest(json);

        Assert.Equal(request, roundTrip);
    }

    [Fact]
    public void ResponseRoundTripsCandidatesAndCommit()
    {
        var response = new MozcBridgeResponse(
            43,
            Handled: true,
            PreeditText: "こんにちは",
            CommitText: "今日は",
            Candidates: ["今日は", "こんにちは"],
            FocusedCandidateIndex: 0);

        var json = MozcBridgeSerializer.SerializeResponse(response);
        var roundTrip = MozcBridgeSerializer.DeserializeResponse(json);

        Assert.Equal(response.Sequence, roundTrip?.Sequence);
        Assert.Equal(response.CommitText, roundTrip?.CommitText);
        Assert.Equal(response.Candidates, roundTrip?.Candidates);
    }
}
