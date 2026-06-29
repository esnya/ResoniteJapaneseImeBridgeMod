using FrooxEngine;
using MozcInput.Ipc;
using MozcInput.Protocol;

namespace MozcInput.Composition;

internal static class MozcInputController
{
    private static readonly Lock StateLock = new();
    private static MozcInputSettings settings = new(
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
    private static readonly Dictionary<VirtualKeyboard, CompositionSnapshot> Snapshots = [];
    private static readonly Dictionary<VirtualKeyboard, IText> KeyboardTargets = [];
    private static readonly Dictionary<VirtualKeyboard, bool> ImeStates = [];
    private static BridgeClient? bridgeClient;
    private static bool warningLogged;

    public static void UpdateSettings(MozcInputSettings newSettings)
    {
        ArgumentNullException.ThrowIfNull(newSettings);

        lock (StateLock)
        {
            if (settings == newSettings)
            {
                return;
            }

            settings = newSettings;
            bridgeClient?.Dispose();
            bridgeClient = null;
            warningLogged = false;
        }
    }

    public static bool ProcessVirtualKey(VirtualKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var keyboard = key.Keyboard.Target;
        var targetText = keyboard?.TargetText;
        if (keyboard is null || targetText is null || targetText.IsDestroyed)
        {
            return true;
        }

        var activeSettings = settings;
        if (!activeSettings.Enabled)
        {
            return true;
        }

        var input = VirtualKeyInputResolver.Resolve(key);
        if (TryHandleImeSwitch(keyboard, key, input, activeSettings))
        {
            return false;
        }

        if (!IsImeActive(keyboard, activeSettings))
        {
            return true;
        }

        TrackTargetText(keyboard, targetText);

        if (!input.TryToBridgeRequest(out var request, HasActiveComposition(keyboard)))
        {
            return true;
        }

        var client = GetClient(activeSettings);
        if (client is null)
        {
            LogBridgeWarningOnce();
            return true;
        }

        try
        {
            var response = client.Send(request);
            if (!response.Handled)
            {
                return true;
            }

            ApplyResponse(key, keyboard, response);
            keyboard.KeyPressed(key);
            return false;
        }
        catch (InvalidOperationException ex)
        {
            LogBridgeWarningOnce(ex.Message);
            return true;
        }
    }

    public static bool ProcessVirtualMultiKey(VirtualMultiKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var keyboard = key.Slot.GetComponentInParents<VirtualKeyboard>();
        var targetText = keyboard?.TargetText;
        if (keyboard is null || targetText is null || targetText.IsDestroyed)
        {
            return true;
        }

        var activeSettings = settings;
        if (!activeSettings.Enabled)
        {
            return true;
        }

        var input = new VirtualKeyInput(null, Renderite.Shared.Key.None, [.. key.Keys]);
        return !TryHandleImeSwitch(keyboard, key: null, input, activeSettings);
    }

    public static void Reset()
    {
        lock (StateLock)
        {
            bridgeClient?.Dispose();
            bridgeClient = null;
        }

        lock (Snapshots)
        {
            Snapshots.Clear();
        }

        lock (KeyboardTargets)
        {
            KeyboardTargets.Clear();
        }

        lock (ImeStates)
        {
            ImeStates.Clear();
        }
    }

    public static void Dispose()
    {
        Reset();
    }

    private static BridgeClient? GetClient(MozcInputSettings activeSettings)
    {
        lock (StateLock)
        {
            if (bridgeClient is not null)
            {
                return bridgeClient;
            }

            if (!activeSettings.AutoStartBridge)
            {
                return null;
            }

            bridgeClient = BridgeClient.TryStart(activeSettings.BridgePath);
            bridgeClient?.Send(new MozcBridgeRequest(0, MozcBridgeCommand.CreateSession));
            return bridgeClient;
        }
    }

    private static bool TryHandleImeSwitch(VirtualKeyboard keyboard, VirtualKey? key, VirtualKeyInput input, MozcInputSettings activeSettings)
    {
        var action = VirtualImeSwitchMatcher.Match(input, activeSettings);
        if (action == VirtualImeSwitchAction.None)
        {
            return false;
        }

        var active = action switch
        {
            VirtualImeSwitchAction.Toggle => !IsImeActive(keyboard, activeSettings),
            VirtualImeSwitchAction.Enable => true,
            VirtualImeSwitchAction.Disable => false,
            _ => IsImeActive(keyboard, activeSettings),
        };

        SetImeActive(keyboard, active);
        ClearComposition(keyboard);
        if (!active)
        {
            TryCancelBridgeComposition();
        }

        if (key is not null)
        {
            keyboard.KeyPressed(key);
        }

        MozcInputMod.DebugLog(() => $"[Mozc Input] Virtual IME {(active ? "enabled" : "disabled")}.");
        return true;
    }

    private static bool IsImeActive(VirtualKeyboard keyboard, MozcInputSettings activeSettings)
    {
        lock (ImeStates)
        {
            return ImeStates.TryGetValue(keyboard, out var active) ? active : activeSettings.DefaultImeActive;
        }
    }

    private static void SetImeActive(VirtualKeyboard keyboard, bool active)
    {
        lock (ImeStates)
        {
            ImeStates[keyboard] = active;
        }
    }

    private static void TryCancelBridgeComposition()
    {
        BridgeClient? client;
        lock (StateLock)
        {
            client = bridgeClient;
        }

        try
        {
            client?.Send(new MozcBridgeRequest(0, MozcBridgeCommand.Cancel));
        }
        catch (InvalidOperationException ex)
        {
            LogBridgeWarningOnce(ex.Message);
        }
    }

    private static void TryResetBridgeComposition()
    {
        BridgeClient? client;
        lock (StateLock)
        {
            client = bridgeClient;
        }

        try
        {
            client?.Send(new MozcBridgeRequest(0, MozcBridgeCommand.Reset));
        }
        catch (InvalidOperationException ex)
        {
            LogBridgeWarningOnce(ex.Message);
        }
    }

    public static bool TryGetSnapshot(VirtualKeyboard keyboard, out CompositionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(keyboard);

        lock (Snapshots)
        {
            return Snapshots.TryGetValue(keyboard, out snapshot!);
        }
    }

    public static bool TryGetDisplaySnapshot(VirtualKeyboard keyboard, out CompositionSnapshot snapshot)
    {
        snapshot = CompositionSnapshot.Empty;
        return settings.ShowCandidatePanel && TryGetSnapshot(keyboard, out snapshot);
    }

    public static void HandleTextEditorFocus(TextEditor editor, User user)
    {
        ArgumentNullException.ThrowIfNull(editor);

        if (user is null || !user.IsLocalUser)
        {
            return;
        }

        var targetText = editor.Text.Target;
        if (targetText is null || targetText.IsDestroyed)
        {
            return;
        }

        ClearCompositionForTarget(targetText);
        TryResetBridgeComposition();
    }

    public static void HandleTextEditorDefocus(TextEditor editor, User user)
    {
        ArgumentNullException.ThrowIfNull(editor);

        if (user is null || !user.IsLocalUser)
        {
            return;
        }

        var targetText = editor.Text.Target;
        if (targetText is null || targetText.IsDestroyed)
        {
            return;
        }

        ClearCompositionForTarget(targetText);
        TryCancelBridgeComposition();
    }

    public static void HandleKeyboardTargetUnavailable(VirtualKeyboard keyboard)
    {
        ArgumentNullException.ThrowIfNull(keyboard);

        if (!ForgetKeyboardTarget(keyboard))
        {
            return;
        }

        ClearComposition(keyboard);
        TryCancelBridgeComposition();
    }

    private static void ApplyResponse(VirtualKey key, VirtualKeyboard keyboard, MozcBridgeResponse response)
    {
        if (!string.IsNullOrEmpty(response.CommitText))
        {
            key.InputInterface.TypeAppend(response.CommitText, key.World);
        }

        var snapshot = new CompositionSnapshot(
            response.PreeditText ?? string.Empty,
            response.CommitText,
            response.Candidates ?? [],
            response.FocusedCandidateIndex);
        lock (Snapshots)
        {
            if (snapshot.HasVisibleComposition)
            {
                Snapshots[keyboard] = snapshot;
            }
            else
            {
                Snapshots.Remove(keyboard);
            }
        }
    }

    private static void TrackTargetText(VirtualKeyboard keyboard, IText targetText)
    {
        var targetChanged = false;
        lock (KeyboardTargets)
        {
            if (!KeyboardTargets.TryGetValue(keyboard, out var previousTarget)
                || !ReferenceEquals(previousTarget, targetText))
            {
                KeyboardTargets[keyboard] = targetText;
                targetChanged = previousTarget is not null;
            }
        }

        if (!targetChanged)
        {
            return;
        }

        ClearComposition(keyboard);
        TryResetBridgeComposition();
    }

    private static bool ForgetKeyboardTarget(VirtualKeyboard keyboard)
    {
        lock (KeyboardTargets)
        {
            return KeyboardTargets.Remove(keyboard);
        }
    }

    private static void ClearCompositionForTarget(IText targetText)
    {
        List<VirtualKeyboard> keyboards;
        lock (KeyboardTargets)
        {
            keyboards = [.. KeyboardTargets
                .Where(pair => ReferenceEquals(pair.Value, targetText))
                .Select(pair => pair.Key)];
            foreach (var keyboard in keyboards)
            {
                KeyboardTargets.Remove(keyboard);
            }
        }

        if (keyboards.Count == 0)
        {
            return;
        }

        lock (Snapshots)
        {
            foreach (var keyboard in keyboards)
            {
                Snapshots.Remove(keyboard);
            }
        }
    }

    private static void ClearComposition(VirtualKeyboard keyboard)
    {
        lock (Snapshots)
        {
            Snapshots.Remove(keyboard);
        }
    }

    private static bool HasActiveComposition(VirtualKeyboard keyboard)
    {
        lock (Snapshots)
        {
            return Snapshots.TryGetValue(keyboard, out var snapshot) && snapshot.HasVisibleComposition;
        }
    }

    private static void LogBridgeWarningOnce(string? detail = null)
    {
        lock (StateLock)
        {
            if (warningLogged)
            {
                return;
            }

            warningLogged = true;
        }

        MozcInputMod.DebugLog(() =>
            string.IsNullOrEmpty(detail)
                ? "[Mozc Input] Bridge unavailable; leaving text input in pass-through mode."
                : $"[Mozc Input] Bridge unavailable; leaving text input in pass-through mode. {detail}");
    }
}
