namespace JapaneseImeBridge.Composition;

internal sealed record JapaneseImeBridgeSettings(
    bool Enabled,
    string GoogleJapaneseInputDirectory,
    bool ShowCandidatePanel,
    bool DefaultImeActive,
    string ImeToggleKeyCombos,
    string ImeOnKeyCombos,
    string ImeOffKeyCombos,
    string ImeToggleTextKeys,
    string ImeOnTextKeys,
    string ImeOffTextKeys);
