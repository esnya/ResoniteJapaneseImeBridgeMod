using MozcInput.Composition;

namespace MozcInput.Tests;

public sealed class VirtualKeyboardCompositionTests
{
    [Fact]
    public void TextMutationInsertsAtCaret()
    {
        var mutation = TextMutationEngine.Insert("ab", 1, -1, "X");

        Assert.Equal("aXb", mutation.Text);
        Assert.Equal(2, mutation.CaretPosition);
        Assert.Equal(-1, mutation.SelectionStart);
    }

    [Fact]
    public void TextMutationReplacesSelection()
    {
        var mutation = TextMutationEngine.Insert("abcd", 3, 1, "X");

        Assert.Equal("aXd", mutation.Text);
        Assert.Equal(2, mutation.CaretPosition);
        Assert.Equal(-1, mutation.SelectionStart);
    }

    [Fact]
    public void DirectITextCommitterIsNotPresent()
    {
        var assembly = typeof(MozcInputController).Assembly;

        Assert.Null(assembly.GetType("MozcInput.Composition.ITextCommitter", throwOnError: false));
        Assert.Null(assembly.GetType("MozcInput.Composition.TextEditorFocusTracker", throwOnError: false));
    }

    [Fact]
    public void DisplayFormatterShowsPreeditInsideExistingTextAndLimitsCandidates()
    {
        var snapshot = new CompositionSnapshot(
            "にほんご",
            null,
            [.. Enumerable.Range(0, 12).Select(index => $"候補{index}")],
            2);

        var formatted = CompositionDisplayFormatter.Format("ab", 1, -1, snapshot);

        Assert.StartsWith("aにほんごb", formatted.Text, StringComparison.Ordinal);
        Assert.Contains("[候補2]", formatted.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("候補9", formatted.Text, StringComparison.Ordinal);
        Assert.Equal(5, formatted.CaretPosition);
        Assert.Equal(-1, formatted.SelectionStart);
    }

    [Fact]
    public void DisplayFormatterReplacesSelectionForPreviewOnly()
    {
        var snapshot = new CompositionSnapshot("X", null, [], -1);

        var formatted = CompositionDisplayFormatter.Format("abcd", 3, 1, snapshot);

        Assert.Equal("aXd", formatted.Text);
        Assert.Equal(2, formatted.CaretPosition);
        Assert.Equal(-1, formatted.SelectionStart);
    }

    [Fact]
    public void VirtualKeyInputMapsControlKeysBeforeText()
    {
        var input = new VirtualKeyInput(" ", Renderite.Shared.Key.Space);

        var mapped = input.TryToBridgeRequest(out var request);

        Assert.True(mapped);
        Assert.Equal(Protocol.MozcBridgeKey.Space, request.Key);
        Assert.Null(request.Text);
    }

    [Fact]
    public void VirtualKeyInputMapsSegmentKeysOnlyDuringComposition()
    {
        var input = new VirtualKeyInput(null, Renderite.Shared.Key.LeftArrow);

        Assert.False(input.TryToBridgeRequest(out _, hasComposition: false));

        var mapped = input.TryToBridgeRequest(out var request, hasComposition: true);

        Assert.True(mapped);
        Assert.Equal(Protocol.MozcBridgeKey.Left, request.Key);
    }

    [Fact]
    public void VirtualKeyInputMapsSegmentWidthKeysOnlyDuringComposition()
    {
        var shrink = new VirtualKeyInput(null, Renderite.Shared.Key.LeftBracket);
        var expand = new VirtualKeyInput(null, Renderite.Shared.Key.RightBracket);

        Assert.False(shrink.TryToBridgeRequest(out _, hasComposition: false));
        Assert.False(expand.TryToBridgeRequest(out _, hasComposition: false));

        Assert.True(shrink.TryToBridgeRequest(out var shrinkRequest, hasComposition: true));
        Assert.True(expand.TryToBridgeRequest(out var expandRequest, hasComposition: true));
        Assert.Equal(Protocol.MozcBridgeKey.SegmentWidthShrink, shrinkRequest.Key);
        Assert.Equal(Protocol.MozcBridgeKey.SegmentWidthExpand, expandRequest.Key);
    }

    [Theory]
    [InlineData("半角/全角", 1)]
    [InlineData("Kana", 2)]
    [InlineData("Eisu", 3)]
    [InlineData("a", 0)]
    public void ImeSwitchMatcherUsesConfiguredVirtualKeyText(string text, int expected)
    {
        var settings = new MozcInputSettings(
            Enabled: true,
            string.Empty,
            AutoStartBridge: true,
            ShowCandidatePanel: true,
            DefaultImeActive: true,
            VirtualImeSwitchMatcher.DefaultToggleKeyCombos,
            VirtualImeSwitchMatcher.DefaultOnKeyCombos,
            VirtualImeSwitchMatcher.DefaultOffKeyCombos,
            VirtualImeSwitchMatcher.DefaultToggleTextKeys,
            VirtualImeSwitchMatcher.DefaultOnTextKeys,
            VirtualImeSwitchMatcher.DefaultOffTextKeys);
        var input = new VirtualKeyInput(text, Renderite.Shared.Key.None);

        var action = VirtualImeSwitchMatcher.Match(input, settings);

        Assert.Equal(expected, (int)action);
    }

    [Theory]
    [InlineData(new[] { Renderite.Shared.Key.LeftWindows }, 1)]
    [InlineData(new[] { Renderite.Shared.Key.RightCommand }, 0)]
    [InlineData(new[] { Renderite.Shared.Key.LeftAlt, Renderite.Shared.Key.BackQuote }, 1)]
    [InlineData(new[] { Renderite.Shared.Key.Control, Renderite.Shared.Key.CapsLock }, 2)]
    [InlineData(new[] { Renderite.Shared.Key.RightShift, Renderite.Shared.Key.CapsLock }, 3)]
    [InlineData(new[] { Renderite.Shared.Key.A, Renderite.Shared.Key.B }, 0)]
    public void ImeSwitchMatcherUsesKeyEnumCombos(Renderite.Shared.Key[] keys, int expected)
    {
        var settings = new MozcInputSettings(
            Enabled: true,
            string.Empty,
            AutoStartBridge: true,
            ShowCandidatePanel: true,
            DefaultImeActive: true,
            VirtualImeSwitchMatcher.DefaultToggleKeyCombos,
            VirtualImeSwitchMatcher.DefaultOnKeyCombos,
            VirtualImeSwitchMatcher.DefaultOffKeyCombos,
            VirtualImeSwitchMatcher.DefaultToggleTextKeys,
            VirtualImeSwitchMatcher.DefaultOnTextKeys,
            VirtualImeSwitchMatcher.DefaultOffTextKeys);
        var input = new VirtualKeyInput(null, Renderite.Shared.Key.None, keys);

        var action = VirtualImeSwitchMatcher.Match(input, settings);

        Assert.Equal(expected, (int)action);
    }

}
