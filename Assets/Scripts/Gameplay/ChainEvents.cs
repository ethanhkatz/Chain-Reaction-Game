using System;
using UnityEngine;

// Mechanics call Report() whenever something reacts (rock cracks, domino tips, gate opens...).
// The combo meter listens and turns quick successions into chains. Safe to call with nobody listening.
public static class ChainEvents
{
    public static event Action<Vector3, string> Reported;

    public static void Report(Vector3 position, string kind)
    {
        try { Reported?.Invoke(position, kind); }
        catch (Exception e) { Debug.LogException(e); }
    }
}
