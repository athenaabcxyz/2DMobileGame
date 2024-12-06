using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ChallengeManager;

public class ChallengeManager : MonoBehaviour
{
    public enum ChallengeType
    {
        DefeatEnemiesInTime,
        AvoidGettingHit,
        SurviveForDuration,
    }

    public class Challenge
    {
        public ChallengeType Type;
        public float Duration; // Time limit for the challenge
        public int MaxHits;   // For AvoidGettingHit
        public Action OnSuccess; // Callback for success

        // Constructor
        public Challenge(ChallengeType type, float duration, int maxHits = 0, Action onSuccess = null)
        {
            Type = type;
            Duration = duration;
            MaxHits = maxHits;
            OnSuccess = onSuccess;
        }

        public Challenge() { }
    }

    public Challenge currentChallenge;
    public bool isChallengeActive;
    private float challengeTimer;
    private int playerHits;

    private void OnEnable()
    {
        StaticEventHandler.OnPlayerHit += RegisterPlayerHit;
        StaticEventHandler.OnRoomEnemiesDefeated += EndChallengeSuccessEvent;
    }


    private void OnDisable()
    {
        StaticEventHandler.OnPlayerHit -= RegisterPlayerHit;
        StaticEventHandler.OnRoomEnemiesDefeated -= EndChallengeSuccessEvent;
    }
    public void StartChallenge(Challenge challenge)
    {
        if (isChallengeActive)
        {
            Debug.LogWarning("A challenge is already active!");
            return;
        }

        currentChallenge = challenge;
        challengeTimer = challenge.Duration;
        playerHits = 0;
        isChallengeActive = true;

        Debug.Log($"Challenge started: {challenge.Type}, Duration: {challenge.Duration}s");

        // Trigger challenge-specific logic
        switch (challenge.Type)
        {
            case ChallengeType.DefeatEnemiesInTime:
                SpawnEnemies();
                break;
            case ChallengeType.AvoidGettingHit:
                EnablePlayerHitTracking();
                break;
            case ChallengeType.SurviveForDuration:
                SpawnContinuousEnemies();
                break;
            //case ChallengeType.DestroyAllObjects:
            //    SpawnDestroyableObjects();
            //    break;
        }
        if(challengeTimer>0)
        {
            StartCoroutine(ChallengeTimerCoroutine());
        }

        StaticEventHandler.CallChallengeStart();
      
    }

    private IEnumerator ChallengeTimerCoroutine()
    {
        while (challengeTimer > 0f)
        {
            challengeTimer -= Time.deltaTime;
            yield return null;
        }

        if (currentChallenge.Type == ChallengeType.SurviveForDuration)
        {
            EndChallenge(true);
        }
        else
        {
            EndChallenge(false); // Challenge failed due to timeout
        }
        
    }

    public void RegisterPlayerHit()
    {
        if (!isChallengeActive || currentChallenge.Type != ChallengeType.AvoidGettingHit||currentChallenge.Type!=ChallengeType.SurviveForDuration)
            return;

        playerHits++;
        if (playerHits > currentChallenge.MaxHits)
        {
            EndChallenge(false); // Challenge failed due to excessive hits
        }
    }

    //public void RegisterObjectDestroyed(int i)
    //{
    //    if (!isChallengeActive || currentChallenge.Type != ChallengeType.DestroyAllObjects)
    //        return;

    //    objectsDestroyed++;
    //    if (objectsDestroyed >= CountTotalObjects())
    //    {
    //        EndChallenge(true); // Challenge succeeded
    //    }
    //}

    public void EndChallengeSuccessEvent(RoomEnemiesDefeatedArgs roomEnemiesDefeatedArgs)
    {
        if(isChallengeActive)
        {
            EndChallenge(true);
        }
    }
    private void EndChallenge(bool success)
    {
        isChallengeActive = false;

        if (success)
        {
           currentChallenge.OnSuccess?.Invoke();
        }
        else
        {
            StaticEventHandler.CallChallengeEnd(false);
            Debug.Log("Challenge Failed");
        }

        // Reset challenge state
        currentChallenge = null;
        StopAllCoroutines();
    }
    public float getDuration()
    {
        return challengeTimer;
    }
    public float getHitCount()
    {
        return playerHits;
    }

    private void SpawnEnemies()
    {
        // Implement enemy spawning logic for DefeatEnemiesInTime
    }

    private void EnablePlayerHitTracking()
    {
        // Enable player hit tracking logic for AvoidGettingHit
    }

    private void SpawnContinuousEnemies()
    {
        // Implement continuous enemy spawning logic for SurviveForDuration
    }

    private void SpawnDestroyableObjects()
    {
        // Implement spawn logic for destroyable objects
    }

    private int CountTotalObjects()
    {
        // Implement logic to count total destroyable objects in the scene
        return 0;
    }
}
