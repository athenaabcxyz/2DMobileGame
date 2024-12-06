using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using static ChallengeManager;

public class ChallengeUI : MonoBehaviour
{

    [SerializeField] TextMeshProUGUI ChallengeName;
    [SerializeField] TextMeshProUGUI ChallengeInfo;
    private int challengeCounter;

    private void OnEnable()
    {
        StaticEventHandler.OnChallengeEnd += OnChallengeEnd;

    }


    private void OnDisable()
    {
        StaticEventHandler.OnChallengeEnd -= OnChallengeEnd;
 
    }

    //private void Start()
    //{
    //    challengeCounter = 0;
    //    currentChallenge = EnemySpawner.Instance.GetRoom.roomChallenge;
    //    challengeManager = EnemySpawner.Instance.GetRoom.challengeManager;
    //    switch (currentChallenge.Type)
    //    {
    //        case ChallengeManager.ChallengeType.AvoidGettingHit:
    //            ChallengeName.text = "Defeat All Enemies";
    //            ChallengeInfo.text = "Remaining Duration: ";
    //            challengeCounter = (int)challengeManager.getDuration();
    //            break;
    //        case ChallengeManager.ChallengeType.SurviveForDuration:
    //            ChallengeName.text = "Don't Get Hit";
    //            ChallengeInfo.text = "Remaining Duration: ";
    //            challengeCounter = (int)challengeManager.getDuration();
    //            break;
    //        case ChallengeManager.ChallengeType.DefeatEnemiesInTime:
    //            ChallengeName.text = "Avoid Getting Hit";
    //            ChallengeInfo.text = "Hit count: ";
    //            challengeCounter = (int)challengeManager.getHitCount();
    //            break;
    //    }
    //}
    // Update is called once per frame
    private void Update()
    {
        if (EnemySpawner.Instance.currentRoom != null)
        {
            if (EnemySpawner.Instance.challengeManager != null)
            {
                if (EnemySpawner.Instance.challengeManager.isChallengeActive)
                {
                    switch (EnemySpawner.Instance.challengeManager.currentChallenge.Type)
                    {
                        case ChallengeManager.ChallengeType.DefeatEnemiesInTime:
                            ChallengeName.text = "Defeat All Enemies";
                            ChallengeInfo.text = "Remaining Duration: " + Mathf.FloorToInt(EnemySpawner.Instance.challengeManager.getDuration());
                            break;
                        case ChallengeManager.ChallengeType.SurviveForDuration:
                            ChallengeName.text = "Don't Get Hit";
                            ChallengeInfo.text = "Remaining Duration: " +Mathf.FloorToInt(EnemySpawner.Instance.challengeManager.getDuration());
                            break;
                        case ChallengeManager.ChallengeType.AvoidGettingHit:
                            ChallengeName.text = "Avoid Getting Hit";
                            ChallengeInfo.text = "Hit count: " + EnemySpawner.Instance.challengeManager.getHitCount() + "/" + EnemySpawner.Instance.challengeManager.currentChallenge.MaxHits;
                            break;
                    }
                }
            }
        }
        
    }

    public void OnChallengeEnd(bool success)
    {
        if(success)
        {
            ChallengeInfo.text = "Completed. Reward ahead.";
        }
        else
        {
            ChallengeInfo.text = "Failed.";
        }
    }


}
