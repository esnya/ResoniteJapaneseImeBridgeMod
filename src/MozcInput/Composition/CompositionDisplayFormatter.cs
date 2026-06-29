namespace MozcInput.Composition;

internal sealed record CompositionDisplayText(string Text, int CaretPosition, int SelectionStart);

internal static class CompositionDisplayFormatter
{
    private const int MaxCandidates = 9;

    public static CompositionDisplayText Format(
        string currentText,
        int caretPosition,
        int selectionStart,
        CompositionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(currentText);
        ArgumentNullException.ThrowIfNull(snapshot);

        if (!snapshot.HasVisibleComposition)
        {
            return new CompositionDisplayText(currentText, caretPosition, selectionStart);
        }

        var body = TextMutationEngine.Insert(
            currentText,
            caretPosition,
            selectionStart,
            snapshot.PreeditText);
        if (snapshot.Candidates.Count == 0)
        {
            return new CompositionDisplayText(body.Text, body.CaretPosition, body.SelectionStart);
        }

        var candidates = snapshot.Candidates
            .Take(MaxCandidates)
            .Select((candidate, index) => index == snapshot.FocusedCandidateIndex ? $"[{candidate}]" : candidate);
        var candidateLine = string.Join(" ", candidates);
        return new CompositionDisplayText(
            string.IsNullOrEmpty(body.Text) ? candidateLine : $"{body.Text}\n{candidateLine}",
            body.CaretPosition,
            body.SelectionStart);
    }
}
