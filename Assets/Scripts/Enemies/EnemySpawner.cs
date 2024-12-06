using System;
using System.Collections;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using UnityEngine;
using static ChallengeManager;

[DisallowMultipleComponent]
public class EnemySpawner : SingletonMonobehaviour<EnemySpawner>
{
    private int enemiesToSpawn;
    private int currentEnemyCount;
    private int enemiesSpawnedSoFar;
    private int enemyMaxConcurrentSpawnNumber;
    public Room currentRoom;
    public ChallengeManager challengeManager;
    private RoomEnemySpawnParameters roomEnemySpawnParameters;
    public GameObject challengeUI;

    private void OnEnable()
    {
        // subscribe to room changed event
        StaticEventHandler.OnRoomChanged += StaticEventHandler_OnRoomChanged;
        StaticEventHandler.OnChallengeStart += OnChallengeStart;
        StaticEventHandler.OnChallengeEnd += OnChallengeEnd;
    }

    private void OnDisable()
    {
        // unsubscribe from room changed event
        StaticEventHandler.OnRoomChanged -= StaticEventHandler_OnRoomChanged;
        StaticEventHandler.OnChallengeEnd -= OnChallengeEnd;
        StaticEventHandler.OnChallengeStart -= OnChallengeStart;
    }

    /// <summary>
    /// Process a change in room
    /// </summary>
    /// 

    public Room GetRoom
    {
        get; private set;
    }
    private void StaticEventHandler_OnRoomChanged(RoomChangedEventArgs roomChangedEventArgs)
    {
        enemiesSpawnedSoFar = 0;
        currentEnemyCount = 0;

        currentRoom = roomChangedEventArgs.room;

        // Update music for room
        MusicManager.Instance.PlayMusic(currentRoom.ambientMusic, 0.2f, 2f);

        // if the room is a corridor or the entrance then return
        if (currentRoom.roomNodeType.isCorridorEW || currentRoom.roomNodeType.isCorridorNS || currentRoom.roomNodeType.isEntrance)
            return;

        // if the room has already been defeated then return
        if (currentRoom.isClearedOfEnemies) return;

        // Get random number of enemies to spawn
        enemiesToSpawn = currentRoom.GetNumberOfEnemiesToSpawn(GameManager.Instance.GetCurrentDungeonLevel());

        // Get room enemy spawn parameters
        roomEnemySpawnParameters = currentRoom.GetRoomEnemySpawnParameters(GameManager.Instance.GetCurrentDungeonLevel());

        // If no enemies to spawn return
        if (enemiesToSpawn == 0)
        {
            // Mark the room as cleared
            currentRoom.isClearedOfEnemies = true;

            return;
        }

        // Get concurrent number of enemies to spawn
        enemyMaxConcurrentSpawnNumber = GetConcurrentEnemies();

        // Update music for room
        MusicManager.Instance.PlayMusic(currentRoom.battleMusic, 0.2f, 0.5f);

        // Lock doors
        currentRoom.instantiatedRoom.LockDoors();



        if (UnityEngine.Random.value >= 0f && !currentRoom.roomNodeType.isCorridorEW && !currentRoom.roomNodeType.isCorridorNS && !currentRoom.roomNodeType.isEntrance && !currentRoom.roomNodeType.isBossRoom) // 30% chance for a challenge
        {
            var challengeType = (ChallengeManager.ChallengeType)UnityEngine.Random.Range(0, Enum.GetValues(typeof(ChallengeManager.ChallengeType)).Length);
            Debug.Log(challengeType);
            ChallengeManager.Challenge challenge;
            switch (challengeType)
            {
                case ChallengeManager.ChallengeType.AvoidGettingHit:
                    challenge = new ChallengeManager.Challenge(
                        challengeType,
                        duration: 0f,
                        maxHits: 5, // Adjust as needed for specific challenge types
                        onSuccess: OnChallengeSuccess
                    );
                    challengeManager.StartChallenge(challenge);
                    break;
                case ChallengeManager.ChallengeType.SurviveForDuration:
                    challenge = new ChallengeManager.Challenge(
                        challengeType,
                        duration: 30,
                        maxHits: 0, // Adjust as needed for specific challenge types
                        onSuccess: OnChallengeSuccess
                    );
                    challengeManager.StartChallenge(challenge);
                    break;
                case ChallengeManager.ChallengeType.DefeatEnemiesInTime:
                    challenge = new ChallengeManager.Challenge(
                        challengeType,
                        duration: 60f,
                        maxHits: 1000, // Adjust as needed for specific challenge types
                        onSuccess: OnChallengeSuccess
                    );
                     challengeManager.StartChallenge(challenge);
                    break;
            }
        }
        Debug.Log(currentRoom.roomNodeType.isNone);
        // Spawn enemies
        SpawnEnemies();
    }
    private void OnChallengeStart()
    {
        challengeUI.SetActive(true);
    }

    public void OnChallengeEnd(bool success)
    {
        StartCoroutine(ChallengeUIDisable());
    }
    private IEnumerator ChallengeUIDisable()
    {
        float timer = 2f;
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        challengeUI.SetActive(false);

    }
    private void OnChallengeSuccess()
    {
        StaticEventHandler.CallChallengeEnd(true);
    }
    /// <summary>
    /// Spawn the enemies
    /// </summary>
    private void SpawnEnemies()
    {
        // Set gamestate engaging boss
        if (GameManager.Instance.gameState == GameState.bossStage)
        {
            GameManager.Instance.previousGameState = GameState.bossStage;
            GameManager.Instance.gameState = GameState.engagingBoss;
        }

        // Set gamestate engaging enemies
        else if(GameManager.Instance.gameState == GameState.playingLevel)
        {
            GameManager.Instance.previousGameState = GameState.playingLevel;
            GameManager.Instance.gameState = GameState.engagingEnemies;
        }

        StartCoroutine(SpawnEnemiesRoutine());
    }

    /// <summary>
    /// Spawn the enemies coroutine
    /// </summary>
    private IEnumerator SpawnEnemiesRoutine()
    {
        Grid grid = currentRoom.instantiatedRoom.grid;

        // Create an instance of the helper class used to select a random enemy
        RandomSpawnableObject<EnemyDetailsSO> randomEnemyHelperClass = new RandomSpawnableObject<EnemyDetailsSO>(currentRoom.enemiesByLevelList);

        // Check we have somewhere to spawn the enemies
        if (currentRoom.spawnPositionArray.Length > 0)
        {
            // Loop through to create all the enemeies
            for (int i = 0; i < enemiesToSpawn; i++)
            {
                // wait until current enemy count is less than max concurrent enemies
                while (currentEnemyCount >= enemyMaxConcurrentSpawnNumber)
                {
                    yield return null;
                }

                Vector3Int cellPosition = (Vector3Int)currentRoom.spawnPositionArray[UnityEngine.Random.Range(0, currentRoom.spawnPositionArray.Length)];

                // Create Enemy - Get next enemy type to spawn 
                CreateEnemy(randomEnemyHelperClass.GetItem(), grid.CellToWorld(cellPosition));

                yield return new WaitForSeconds(GetEnemySpawnInterval());
            }
        }
    }

    /// <summary>
    /// Get a random spawn interval between the minimum and maximum values
    /// </summary>
    private float GetEnemySpawnInterval()
    {
        return (UnityEngine.Random.Range(roomEnemySpawnParameters.minSpawnInterval, roomEnemySpawnParameters.maxSpawnInterval));
    }

    /// <summary>
    /// Get a random number of concurrent enemies between the minimum and maximum values
    /// </summary>
    private int GetConcurrentEnemies()
    {
        return (UnityEngine.Random.Range(roomEnemySpawnParameters.minConcurrentEnemies, roomEnemySpawnParameters.maxConcurrentEnemies));
    }

    /// <summary>
    /// Create an enemy in the specified position
    /// </summary>
    private void CreateEnemy(EnemyDetailsSO enemyDetails, Vector3 position)
    {
        // keep track of the number of enemies spawned so far 
        enemiesSpawnedSoFar++;

        // Add one to the current enemy count - this is reduced when an enemy is destroyed
        currentEnemyCount++;

        // Get current dungeon level
        DungeonLevelSO dungeonLevel = GameManager.Instance.GetCurrentDungeonLevel();

        // Instantiate enemy
        GameObject enemy = Instantiate(enemyDetails.enemyPrefab, position, Quaternion.identity, transform);

        // Initialize Enemy
        enemy.GetComponent<Enemy>().EnemyInitialization(enemyDetails, enemiesSpawnedSoFar, dungeonLevel);

        // subscribe to enemy destroyed event
        enemy.GetComponent<DestroyedEvent>().OnDestroyed += Enemy_OnDestroyed;

    }

    /// <summary>
    /// Process enemy destroyed
    /// </summary>
    private void Enemy_OnDestroyed(DestroyedEvent destroyedEvent, DestroyedEventArgs destroyedEventArgs)
    {
        // Unsubscribe from event
        destroyedEvent.OnDestroyed -= Enemy_OnDestroyed;

        // reduce current enemy count
        currentEnemyCount--;

        // Score points - call points scored event
        StaticEventHandler.CallPointsScoredEvent(destroyedEventArgs.points);

        if (currentEnemyCount <= 0 && enemiesSpawnedSoFar == enemiesToSpawn)
        {
            currentRoom.isClearedOfEnemies = true;

            // Set game state
            if (GameManager.Instance.gameState == GameState.engagingEnemies)
            {
                GameManager.Instance.gameState = GameState.playingLevel;
                GameManager.Instance.previousGameState = GameState.engagingEnemies;
            }

            else if (GameManager.Instance.gameState == GameState.engagingBoss)
            {
                GameManager.Instance.gameState = GameState.bossStage;
                GameManager.Instance.previousGameState = GameState.engagingBoss;
            }

            // unlock doors
            currentRoom.instantiatedRoom.UnlockDoors(Settings.doorUnlockDelay);

            // Update music for room
            MusicManager.Instance.PlayMusic(currentRoom.ambientMusic, 0.2f, 2f);

            // Trigger room enemies defeated event
            StaticEventHandler.CallRoomEnemiesDefeatedEvent(currentRoom);
        }
    }

}