namespace MozcInput.Bridge.Engine.Google;

internal sealed record MozcIpcOutput(
    ulong SessionId,
    bool Consumed,
    string Preedit,
    IReadOnlyList<string> Candidates,
    int FocusedCandidateIndex,
    string CommitText);
