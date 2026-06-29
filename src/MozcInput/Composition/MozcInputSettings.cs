namespace MozcInput.Composition;

internal sealed record MozcInputSettings(
    bool Enabled,
    string BridgePath,
    bool AutoStartBridge,
    bool ShowCandidatePanel,
    bool DefaultImeActive,
    string ImeToggleKeyCombos,
    string ImeOnKeyCombos,
    string ImeOffKeyCombos,
    string ImeToggleTextKeys,
    string ImeOnTextKeys,
    string ImeOffTextKeys);
