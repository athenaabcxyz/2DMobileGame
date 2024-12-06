using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class StaticEventHandler
{
    // Room changed event
    public static event Action<RoomChangedEventArgs> OnRoomChanged;

    public static void CallRoomChangedEvent(Room room)
    {
        OnRoomChanged?.Invoke(new RoomChangedEventArgs() { room = room });
    }


    // Room enemies defeated event
    public static event Action<RoomEnemiesDefeatedArgs> OnRoomEnemiesDefeated;

    public static void CallRoomEnemiesDefeatedEvent(Room room)
    {
        OnRoomEnemiesDefeated?.Invoke(new RoomEnemiesDefeatedArgs() { room = room });
    }

    // Points scored event
    public static event Action<PointsScoredArgs> OnPointsScored;

    public static void CallPointsScoredEvent(int points)
    {
        OnPointsScored?.Invoke(new PointsScoredArgs() { points = points });
    }

    // Score changed event
    public static event Action<ScoreChangedArgs> OnScoreChanged;

    public static void CallScoreChangedEvent(long score, int multiplier)
    {
        OnScoreChanged?.Invoke(new ScoreChangedArgs() { score = score, multiplier = multiplier });
    }

    // Multiplier event
    public static event Action<MultiplierArgs> OnMultiplier;

    public static void CallMultiplierEvent(bool multiplier)
    {
        OnMultiplier?.Invoke(new MultiplierArgs() { multiplier = multiplier });
    }

    public static event Action<int> OnDestroyItemForChallenge;

    public static void CallDestroyItemForChallengeEvent()
    {
        OnDestroyItemForChallenge?.Invoke(1);
    }

    public static event Action OnPlayerHit;
    public static void CallPlayerHitEvent()
    {
        OnPlayerHit?.Invoke();
    }

    public static event Action<bool> OnChallengeEnd;

    public static void CallChallengeEnd(bool success)
    {
        OnChallengeEnd?.Invoke(success);
    }

    public static event Action<ChallengeInfoEventArgs> UpdateChallengeInfo;

    public static void CallUpdateChallengeInfo(ChallengeInfoEventArgs args)
    {
        UpdateChallengeInfo?.Invoke(args);
    }

    public static event Action OnChallengeStart;

    public static void CallChallengeStart()
    {
        OnChallengeStart?.Invoke();
    }
}

public class ChallengeInfoEventArgs: EventArgs
{
    public float remainDuration;
    public int hitCount;
}
public class RoomChangedEventArgs : EventArgs
{
    public Room room;
}

public class RoomEnemiesDefeatedArgs : EventArgs
{
    public Room room;
}

public class PointsScoredArgs : EventArgs
{
    public int points;
}

public class ScoreChangedArgs : EventArgs
{
    public long score;
    public int multiplier;
}

public class MultiplierArgs : EventArgs
{
    public bool multiplier;

}