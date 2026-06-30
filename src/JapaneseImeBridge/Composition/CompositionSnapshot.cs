namespace JapaneseImeBridge.Composition;

internal sealed record CompositionSnapshot(
    string PreeditText,
    string? CommitText,
    IReadOnlyList<string> Candidates,
    int FocusedCandidateIndex)
{
    public static CompositionSnapshot Empty { get; } = new(string.Empty, null, [], -1);

    public bool HasVisibleComposition =>
        !string.IsNullOrEmpty(PreeditText) || Candidates.Count > 0;
}
