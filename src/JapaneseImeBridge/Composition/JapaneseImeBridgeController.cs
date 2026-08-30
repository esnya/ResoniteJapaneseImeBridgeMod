using FrooxEngine;
using JapaneseImeBridge.Backend;

namespace JapaneseImeBridge.Composition;

internal static class JapaneseImeBridgeController
{
    private static readonly Lock StateLock = new();
    private static JapaneseImeBridgeSettings settings = new(
        Enabled: true,
        GoogleJapaneseInputDirectory: string.Empty,
        ShowCandidatePanel: true,
        DefaultImeActive: JapaneseImeBridgeMod.DefaultImeActiveByDefault,
        VirtualImeSwitchMatcher.DefaultToggleKeyCombos,
        VirtualImeSwitchMatcher.DefaultOnKeyCombos,
        VirtualImeSwitchMatcher.DefaultOffKeyCombos,
        VirtualImeSwitchMatcher.DefaultToggleTextKeys,
        VirtualImeSwitchMatcher.DefaultOnTextKeys,
        VirtualImeSwitchMatcher.DefaultOffTextKeys);
    private static readonly Dictionary<VirtualKeyboard, CompositionSnapshot> Snapshots = [];
    private static readonly Dictionary<VirtualKeyboard, IText> KeyboardTargets = [];
    private static readonly Dictionary<VirtualKeyboard, bool> ImeStates = [];
    private static JapaneseImeBackendWorker? backendWorker;
    private static bool warningLogged;

    public static void UpdateSettings(JapaneseImeBridgeSettings newSettings)
    {
        ArgumentNullException.ThrowIfNull(newSettings);

        var shouldStart = newSettings.Enabled;
        lock (StateLock)
        {
            if (settings != newSettings)
            {
                settings = newSettings;
                backendWorker?.Dispose();
                backendWorker = null;
                warningLogged = false;
            }
        }

        if (shouldStart)
        {
            _ = GetOrStartWorker(newSettings);
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

        FlushBackendResults();

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

        var worker = GetOrStartWorker(activeSettings);
        if (worker.State != ImeBackendState.Ready)
        {
            LogBackendUnavailableOnce(worker.State);
            return true;
        }

        var replayText = request.Key == ImeBackendKey.None ? request.Text : null;
        if (!worker.TryEnqueue(keyboard, key, request, replayText))
        {
            return true;
        }

        keyboard.KeyPressed(key);
        return false;
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

    public static void FlushBackendResults()
    {
        JapaneseImeBackendWorker? worker;
        lock (StateLock)
        {
            worker = backendWorker;
        }

        if (worker is null)
        {
            return;
        }

        foreach (var result in worker.DrainCompleted())
        {
            ApplyBackendResult(result);
        }
    }

    public static void Reset()
    {
        lock (StateLock)
        {
            backendWorker?.Dispose();
            backendWorker = null;
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

    private static JapaneseImeBackendWorker GetOrStartWorker(JapaneseImeBridgeSettings activeSettings)
    {
        lock (StateLock)
        {
            backendWorker ??= new JapaneseImeBackendWorker(activeSettings);
            return backendWorker;
        }
    }

    private static bool TryHandleImeSwitch(VirtualKeyboard keyboard, VirtualKey? key, VirtualKeyInput input, JapaneseImeBridgeSettings activeSettings)
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
            TryCancelBackendComposition();
        }

        if (key is not null)
        {
            keyboard.KeyPressed(key);
        }

        JapaneseImeBridgeMod.DebugLog(() => $"[Japanese IME Bridge] Virtual IME {(active ? "enabled" : "disabled")}.");
        return true;
    }

    private static bool IsImeActive(VirtualKeyboard keyboard, JapaneseImeBridgeSettings activeSettings)
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

    private static void TryCancelBackendComposition()
    {
        JapaneseImeBackendWorker? worker;
        lock (StateLock)
        {
            worker = backendWorker;
        }

        worker?.TryEnqueueControl(ImeBackendCommand.Cancel);
    }

    private static void TryResetBackendComposition()
    {
        JapaneseImeBackendWorker? worker;
        lock (StateLock)
        {
            worker = backendWorker;
        }

        worker?.TryEnqueueControl(ImeBackendCommand.Reset);
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
        TryResetBackendComposition();
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
        TryCancelBackendComposition();
    }

    public static void HandleKeyboardTargetUnavailable(VirtualKeyboard keyboard)
    {
        ArgumentNullException.ThrowIfNull(keyboard);

        if (!ForgetKeyboardTarget(keyboard))
        {
            return;
        }

        ClearComposition(keyboard);
        TryCancelBackendComposition();
    }

    private static void ApplyBackendResult(ImeBackendResult result)
    {
        if (result.Keyboard is { } keyboard && result.Error is not null)
        {
            ClearComposition(keyboard);
        }

        if (!string.IsNullOrEmpty(result.Error))
        {
            LogBackendUnavailableOnce(ImeBackendState.Faulted, result.Error);
            ReplayTextIfNeeded(result);
            return;
        }

        if (!result.Response.Handled)
        {
            ReplayTextIfNeeded(result);
            return;
        }

        if (result.Keyboard is null || result.Key is null)
        {
            return;
        }

        ApplyResponse(result.Key, result.Keyboard, result.Response);
    }

    private static void ReplayTextIfNeeded(ImeBackendResult result)
    {
        if (string.IsNullOrEmpty(result.ReplayText) || result.Key is null)
        {
            return;
        }

        result.Key.InputInterface.TypeAppend(result.ReplayText, result.Key.World);
    }

    private static void ApplyResponse(VirtualKey key, VirtualKeyboard keyboard, ImeBackendResponse response)
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
        TryResetBackendComposition();
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

    private static void LogBackendUnavailableOnce(ImeBackendState state, string? detail = null)
    {
        if (state is ImeBackendState.Connecting or ImeBackendState.Ready)
        {
            return;
        }

        lock (StateLock)
        {
            if (warningLogged)
            {
                return;
            }

            warningLogged = true;
        }

        JapaneseImeBridgeMod.DebugLog(() =>
            string.IsNullOrEmpty(detail)
                ? "[Japanese IME Bridge] Google Japanese Input backend unavailable; leaving text input in pass-through mode."
                : $"[Japanese IME Bridge] Google Japanese Input backend unavailable; leaving text input in pass-through mode. {detail}");
    }
}
