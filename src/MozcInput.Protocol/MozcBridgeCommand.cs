namespace MozcInput.Protocol;

/// <summary>
/// Commands sent from the Resonite mod to the local input bridge.
/// </summary>
public enum MozcBridgeCommand
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

    /// <summary>Ask the bridge process to exit.</summary>
    Shutdown,
}
