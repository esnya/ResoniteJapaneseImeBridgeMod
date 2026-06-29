namespace MozcInput.Composition;

internal sealed record TextMutation(string Text, int CaretPosition, int SelectionStart);

internal static class TextMutationEngine
{
    public static TextMutation Insert(string currentText, int caretPosition, int selectionStart, string insertedText)
    {
        ArgumentNullException.ThrowIfNull(currentText);
        ArgumentNullException.ThrowIfNull(insertedText);

        var caret = ClampCaret(caretPosition, currentText.Length);
        if (selectionStart >= 0 && selectionStart != caret)
        {
            var start = Math.Min(ClampCaret(selectionStart, currentText.Length), caret);
            var end = Math.Max(ClampCaret(selectionStart, currentText.Length), caret);
            var replaced = currentText.Remove(start, end - start).Insert(start, insertedText);
            return new TextMutation(replaced, start + insertedText.Length, -1);
        }

        var text = currentText.Insert(caret, insertedText);
        return new TextMutation(text, caret + insertedText.Length, -1);
    }

    private static int ClampCaret(int caretPosition, int textLength)
    {
        if (caretPosition < 0)
        {
            return textLength;
        }

        return Math.Clamp(caretPosition, 0, textLength);
    }
}
