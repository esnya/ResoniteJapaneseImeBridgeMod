namespace JapaneseImeBridge.Backend;

/// <summary>
/// Commands sent from the Resonite mod to the local IME backend.
/// </summary>
public enum ImeBackendCommand
{
    /// <summary>Create or reset the bridge session.</summary>
    CreateSession,

    /// <summary>Submit text or a control key to the session.</summary>
    Key,

    /// <summary>Commit the current composition.</summary>
    Commit,

    /// <summary>Cancel the current composition.</summary>
    Cancel,

    /// <summary>Reset the bridge session state.</summary>
    Reset,

    /// <summary>No further command.</summary>
    NoOp,
}
