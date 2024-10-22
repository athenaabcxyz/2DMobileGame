using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameManager : SingletonMonobehaviour<GameManager>
{
    #region Header GAMEOBJECT REFERENCES
    [Space(10)]
    [Header("GAMEOBJECT REFERENCES")]
    #endregion Header GAMEOBJECT REFERENCES

    #region Tooltip

    [Tooltip("Populate with pause menu gameobject in hierarchy")]

    #endregion Tooltip

    [SerializeField] private GameObject pauseMenu;

    #region Tooltip
    [Tooltip("Populate with the MessageText textmeshpro component in the FadeScreenUI")]
    #endregion Tooltip
    [SerializeField] private TextMeshProUGUI messageTextTMP;

    #region Tooltip
    [Tooltip("Populate with the FadeImage canvasgroup component in the FadeScreenUI")]
    #endregion Tooltip
    [SerializeField] private CanvasGroup canvasGroup;

    #region Header DUNGEON LEVELS

    [Space(10)]
    [Header("DUNGEON LEVELS")]

    #endregion Header DUNGEON LEVELS

    #region Tooltip

    [Tooltip("Populate with the dungeon level scriptable objects")]

    #endregion Tooltip

    [SerializeField] private List<DungeonLevelSO> dungeonLevelList;

    #region Tooltip

    [Tooltip("Populate with the starting dungeon level for testing , first level = 0")]

    #endregion Tooltip

    [SerializeField] private int currentDungeonLevelListIndex = 0;
    private Room currentRoom;
    private Room previousRoom;
    private PlayerDetailsSO playerDetails;
    private Player player;

    [HideInInspector] public GameState gameState;
    [HideInInspector] public GameState previousGameState;
    private long gameScore;
    private int scoreMultiplier;
    private InstantiatedRoom bossRoom;
    private bool isFading = false;

    protected override void Awake()
    {
        // Call base class
        base.Awake();

        // Set player details - saved in current player scriptable object from the main menu
        playerDetails = GameResources.Instance.currentPlayer.playerDetails;

        // Instantiate player
        InstantiatePlayer();

    }

    /// <summary>
    /// Create player in scene at position
    /// </summary>
    private void InstantiatePlayer()
    {
        // Instantiate player
        GameObject playerGameObject = Instantiate(playerDetails.playerPrefab);

        // Initialize Player
        player = playerGameObject.GetComponent<Player>();

        player.Initialize(playerDetails);

    }

    private void OnEnable()
    {
        // Subscribe to room changed event.
        StaticEventHandler.OnRoomChanged += StaticEventHandler_OnRoomChanged;

        // Subscribe to room enemies defeated event
        StaticEventHandler.OnRoomEnemiesDefeated += StaticEventHandler_OnRoomEnemiesDefeated;

        // Subscribe to the points scored event
        StaticEventHandler.OnPointsScored += StaticEventHandler_OnPointsScored;

        // Subscribe to score multiplier event
        StaticEventHandler.OnMultiplier += StaticEventHandler_OnMultiplier;

        // Subscribe to player destroyed event
        player.destroyedEvent.OnDestroyed += Player_OnDestroyed;
    }

    private void OnDisable()
    {
        // Unsubscribe from room changed event
        StaticEventHandler.OnRoomChanged -= StaticEventHandler_OnRoomChanged;

        // Unsubscribe from room enemies defeated event
        StaticEventHandler.OnRoomEnemiesDefeated -= StaticEventHandler_OnRoomEnemiesDefeated;

        // Unsubscribe from the points scored event
        StaticEventHandler.OnPointsScored -= StaticEventHandler_OnPointsScored;

        // Unsubscribe from score multiplier event
        StaticEventHandler.OnMultiplier -= StaticEventHandler_OnMultiplier;

        // Unubscribe from player destroyed event
        player.destroyedEvent.OnDestroyed -= Player_OnDestroyed;

    }

    /// <summary>
    /// Handle room changed event
    /// </summary>
    private void StaticEventHandler_OnRoomChanged(RoomChangedEventArgs roomChangedEventArgs)
    {
        SetCurrentRoom(roomChangedEventArgs.room);
    }

    /// <summary>
    /// Handle room enemies defeated event
    /// </summary>
    private void StaticEventHandler_OnRoomEnemiesDefeated(RoomEnemiesDefeatedArgs roomEnemiesDefeatedArgs)
    {
        RoomEnemiesDefeated();
    }

    /// <summary>
    /// Handle points scored event
    /// </summary>
    private void StaticEventHandler_OnPointsScored(PointsScoredArgs pointsScoredArgs)
    {
        // Increase score
        gameScore += pointsScoredArgs.points * scoreMultiplier;

        // Call score changed event
        StaticEventHandler.CallScoreChangedEvent(gameScore, scoreMultiplier);
    }

    /// <summary>
    /// Handle score multiplier event
    /// </summary>
    private void StaticEventHandler_OnMultiplier(MultiplierArgs multiplierArgs)
    {
        if (multiplierArgs.multiplier)
        {
            scoreMultiplier++;
        }
        else
        {
            scoreMultiplier--;
        }

        // clamp between 1 and 30
        scoreMultiplier = Mathf.Clamp(scoreMultiplier, 1, 30);

        // Call score changed event
        StaticEventHandler.CallScoreChangedEvent(gameScore, scoreMultiplier);
    }

    /// <summary>
    /// Handle player destroyed event
    /// </summary>
    private void Player_OnDestroyed(DestroyedEvent destroyedEvent, DestroyedEventArgs destroyedEventArgs)
    {
        previousGameState = gameState;
        gameState = GameState.gameLost;
    }

    // Start is called before the first frame update
    private void Start()
    {
        previousGameState = GameState.gameStarted;
        gameState = GameState.gameStarted;

        // Set score to zero
        gameScore = 0;

        // Set multiplier to 1;
        scoreMultiplier = 1;

        // Set screen to black
        StartCoroutine(Fade(0f, 1f, 0f, Color.black));
    }

    // Update is called once per frame
    private void Update()
    {
        HandleGameState();

        //// For testing
        //if (Input.GetKeyDown(KeyCode.P))
        //{
        //    gameState = GameState.gameStarted;
        //}

    }

    /// <summary>
    /// Handle game state
    /// </summary>
    private void HandleGameState()
    {
        // Handle game state
        switch (gameState)
        {
            case GameState.gameStarted:

                // Play first level
                PlayDungeonLevel(currentDungeonLevelListIndex);

                gameState = GameState.playingLevel;

                // Trigger room enemies defeated since we start in the entrance where there are no enemies (just in case you have a level with just a boss room!)
                RoomEnemiesDefeated();

                break;

            // While playing the level handle the tab key for the dungeon overview map.
            case GameState.playingLevel:

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    PauseGameMenu();
                }

                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    DisplayDungeonOverviewMap();
                }

                break;

            // While engaging enemies handle the escape key for the pause menu
            case GameState.engagingEnemies:

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    PauseGameMenu();
                }

                break;


            // if in the dungeon overview map handle the release of the tab key to clear the map
            case GameState.dungeonOverviewMap:

                // Key released
                if (Input.GetKeyUp(KeyCode.Tab))
                {
                    // Clear dungeonOverviewMap
                    DungeonMap.Instance.ClearDungeonOverViewMap();
                }

                break;

            // While playing the level and before the boss is engaged, handle the tab key for the dungeon overview map.
            case GameState.bossStage:

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    PauseGameMenu();
                }

                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    DisplayDungeonOverviewMap();
                }

                break;

            // While engaging the boss handle the escape key for the pause menu
            case GameState.engagingBoss:

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    PauseGameMenu();
                }

                break;

            // handle the level being completed
            case GameState.levelCompleted:

                // Display level completed text
                StartCoroutine(LevelCompleted());

                break;

            // handle the game being won (only trigger this once - test the previous game state to do this)
            case GameState.gameWon:

                if (previousGameState != GameState.gameWon)
                    StartCoroutine(GameWon());

                break;

            // handle the game being lost (only trigger this once - test the previous game state to do this)
            case GameState.gameLost:

                if (previousGameState != GameState.gameLost)
                {
                    StopAllCoroutines(); // Prevent messages if you clear the level just as you get killed
                    StartCoroutine(GameLost());
                }

                break;

            // restart the game
            case GameState.restartGame:

                RestartGame();

                break;

            // if the game is paused and the pause menu showing, then pressing escape again will clear the pause menu
            case GameState.gamePaused:
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    PauseGameMenu();
                }
                break;
        }

    }

    /// <summary>
    /// Set the current room the player in in
    /// </summary>
    public void SetCurrentRoom(Room room)
    {
        previousRoom = currentRoom;
        currentRoom = room;

        //// Debug
        //Debug.Log(room.prefab.name.ToString());
    }

    /// <summary>
    /// Room enemies defated - test if all dungeon rooms have been cleared of enemies - if so load
    /// next dungeon game level
    /// </summary>
    private void RoomEnemiesDefeated()
    {
        // Initialise dungeon as being cleared - but then test each room
        bool isDungeonClearOfRegularEnemies = true;
        bossRoom = null;

        // Loop through all dungeon rooms to see if cleared of enemies
        foreach (KeyValuePair<string, Room> keyValuePair in DungeonBuilder.Instance.dungeonBuilderRoomDictionary)
        {
            // skip boss room for time being
            if (keyValuePair.Value.roomNodeType.isBossRoom)
            {
                bossRoom = keyValuePair.Value.instantiatedRoom;
                continue;
            }

            // check if other rooms have been cleared of enemies
            if (!keyValuePair.Value.isClearedOfEnemies)
            {
                isDungeonClearOfRegularEnemies = false;
                break;
            }
        }

        // Set game state
        // If dungeon level completly cleared (i.e. dungeon cleared apart from boss and there is no boss room OR dungeon cleared apart from boss and boss room is also cleared)
        if ((isDungeonClearOfRegularEnemies && bossRoom == null) || (isDungeonClearOfRegularEnemies && bossRoom.room.isClearedOfEnemies))
        {
            // Are there more dungeon levels then
            if (currentDungeonLevelListIndex < dungeonLevelList.Count - 1)
            {
                gameState = GameState.levelCompleted;
            }
            else
            {
                gameState = GameState.gameWon;
            }
        }
        // Else if dungeon level cleared apart from boss room
        else if (isDungeonClearOfRegularEnemies)
        {
            gameState = GameState.bossStage;

            StartCoroutine(BossStage());
        }

    }

    /// <summary>
    /// Pause game menu - also called from resume game button on pause menu
    /// </summary>
    public void PauseGameMenu()
    {
        if (gameState != GameState.gamePaused)
        {
            pauseMenu.SetActive(true);
            GetPlayer().playerControl.DisablePlayer();

            // Set game state
            previousGameState = gameState;
            gameState = GameState.gamePaused;
        }
        else if (gameState == GameState.gamePaused)
        {
            pauseMenu.SetActive(false);
            GetPlayer().playerControl.EnablePlayer();

            // Set game state
            gameState = previousGameState;
            previousGameState = GameState.gamePaused;

        }
    }

    /// <summary>
    /// Dungeon Map Screen Display
    /// </summary>
    private void DisplayDungeonOverviewMap()
    {
        // return if fading
        if (isFading)
            return;

        // Display dungeonOverviewMap
        DungeonMap.Instance.DisplayDungeonOverViewMap();
    }


    private void PlayDungeonLevel(int dungeonLevelListIndex)
    {
        // Build dungeon for level
        bool dungeonBuiltSucessfully = DungeonBuilder.Instance.GenerateDungeon(dungeonLevelList[dungeonLevelListIndex]);

        if (!dungeonBuiltSucessfully)
        {
            Debug.LogError("Couldn't build dungeon from specified rooms and node graphs");
        }

        // Call static event that room has changed.
        StaticEventHandler.CallRoomChangedEvent(currentRoom);

        // Set player roughly mid-room
        player.gameObject.transform.position = new Vector3((currentRoom.lowerBounds.x + currentRoom.upperBounds.x) / 2f, (currentRoom.lowerBounds.y + currentRoom.upperBounds.y) / 2f, 0f);

        // Get nearest spawn point in room nearest to player
        player.gameObject.transform.position = HelperUtilities.GetSpawnPositionNearestToPlayer(player.gameObject.transform.position);

        // Display Dungeon Level Text
        StartCoroutine(DisplayDungeonLevelText());

        //// ** Demo code
        //RoomEnemiesDefeated();
    }

    /// <summary>
    /// Display the dungeon level text
    /// </summary>
    private IEnumerator DisplayDungeonLevelText()
    {
        // Set screen to black
        StartCoroutine(Fade(0f, 1f, 0f, Color.black));

        GetPlayer().playerControl.DisablePlayer();

        string messageText = "LEVEL " + (currentDungeonLevelListIndex + 1).ToString() + "\n\n" + dungeonLevelList[currentDungeonLevelListIndex].levelName.ToUpper();

        yield return StartCoroutine(DisplayMessageRoutine(messageText, Color.white, 2f));

        GetPlayer().playerControl.EnablePlayer();

        // Fade In
        yield return StartCoroutine(Fade(1f, 0f, 2f, Color.black));

    }

    /// <summary>
    /// Display the message text for displaySeconds  - if displaySeconds =0 then the message is displayed until the return key is pressed
    /// </summary>
    private IEnumerator DisplayMessageRoutine(string text, Color textColor, float displaySeconds)
    {
        // Set text
        messageTextTMP.SetText(text);
        messageTextTMP.color = textColor;

        // Display the message for the given time
        if (displaySeconds > 0f)
        {
            float timer = displaySeconds;

            while (timer > 0f && !Input.GetKeyDown(KeyCode.Return))
            {
                timer -= Time.deltaTime;
                yield return null;
            }
        }
        else
        // else display the message until the return button is pressed
        {
            while (!Input.GetKeyDown(KeyCode.Return))
            {
                yield return null;
            }
        }

        yield return null;

        // Clear text
        messageTextTMP.SetText("");
    }

    /// <summary>
    /// Enter boss stage
    /// </summary>
    private IEnumerator BossStage()
    {
        // Activate boss room
        bossRoom.gameObject.SetActive(true);

        // Unlock boss room
        bossRoom.UnlockDoors(0f);

        // Wait 2 seconds
        yield return new WaitForSeconds(2f);

        // Fade in canvas to display text message
        yield return StartCoroutine(Fade(0f, 1f, 2f, new Color(0f, 0f, 0f, 0.4f)));

        // Display boss message
        yield return StartCoroutine(DisplayMessageRoutine("WELL DONE  " + GameResources.Instance.currentPlayer.playerName + "!  YOU'VE SURVIVED ....SO FAR\n\nNOW FIND AND DEFEAT THE BOSS....GOOD LUCK!", Color.white, 5f));

        // Fade out canvas
        yield return StartCoroutine(Fade(1f, 0f, 2f, new Color(0f, 0f, 0f, 0.4f)));

    }

    /// <summary>
    /// Show level as being completed - load next level
    /// </summary>
    private IEnumerator LevelCompleted()
    {
        // Play next level
        gameState = GameState.playingLevel;

        // Wait 2 seconds
        yield return new WaitForSeconds(2f);

        // Fade in canvas to display text message
        yield return StartCoroutine(Fade(0f, 1f, 2f, new Color(0f, 0f, 0f, 0.4f)));

        // Display level completed
        yield return StartCoroutine(DisplayMessageRoutine("WELL DONE " + GameResources.Instance.currentPlayer.playerName + "! \n\nYOU'VE SURVIVED THIS DUNGEON LEVEL", Color.white, 5f));

        yield return StartCoroutine(DisplayMessageRoutine("COLLECT ANY LOOT ....THEN PRESS RETURN\n\nTO DESCEND FURTHER INTO THE DUNGEON", Color.white, 5f));

        // Fade out canvas
        yield return StartCoroutine(Fade(1f, 0f, 2f, new Color(0f, 0f, 0f, 0.4f)));

        // When player presses the return key proceed to the next level
        while (!Input.GetKeyDown(KeyCode.Return))
        {
            yield return null;
        }

        yield return null; // to avoid enter being detected twice

        // Increase index to next level
        currentDungeonLevelListIndex++;

        PlayDungeonLevel(currentDungeonLevelListIndex);
    }

    /// <summary>
    /// Fade Canvas Group
    /// </summary>
    public IEnumerator Fade(float startFadeAlpha, float targetFadeAlpha, float fadeSeconds, Color backgroundColor)
    {
        isFading = true;

        Image image = canvasGroup.GetComponent<Image>();
        image.color = backgroundColor;

        float time = 0;

        while (time <= fadeSeconds)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startFadeAlpha, targetFadeAlpha, time / fadeSeconds);
            yield return null;
        }

        isFading = false;

    }


    /// <summary>
    /// Game Won
    /// </summary>
    private IEnumerator GameWon()
    {
        previousGameState = GameState.gameWon;

        // Disable player
        GetPlayer().playerControl.DisablePlayer();

        int rank = HighScoreManager.Instance.GetRank(gameScore);

        string rankText;

        // Test if the score is in the rankings
        if (rank > 0 && rank <= Settings.numberOfHighScoresToSave)
        {
            rankText = "YOUR SCORE IS RANKED " + rank.ToString("#0") + " IN THE TOP " + Settings.numberOfHighScoresToSave.ToString("#0");

            string name = GameResources.Instance.currentPlayer.playerName;

            if (name == "")
            {
                name = playerDetails.playerCharacterName.ToUpper();
            }

            // Update scores
            HighScoreManager.Instance.AddScore(new Score() { playerName = name, levelDescription = "LEVEL " + (currentDungeonLevelListIndex + 1).ToString() + " - " + GetCurrentDungeonLevel().levelName.ToUpper(), playerScore = gameScore }, rank);


        }
        else
        {
            rankText = "YOUR SCORE ISN'T RANKED IN THE TOP " + Settings.numberOfHighScoresToSave.ToString("#0");
        }

        // Wait 1 seconds
        yield return new WaitForSeconds(1f);

        // Fade Out
        yield return StartCoroutine(Fade(0f, 1f, 2f, Color.black));

        // Display game won
        yield return StartCoroutine(DisplayMessageRoutine("WELL DONE " + GameResources.Instance.currentPlayer.playerName + "! YOU HAVE DEFEATED THE DUNGEON", Color.white, 3f));

        yield return StartCoroutine(DisplayMessageRoutine("YOU SCORED " + gameScore.ToString("###,###0") + "\n\n" + rankText, Color.white, 4f));

        yield return StartCoroutine(DisplayMessageRoutine("PRESS RETURN TO RESTART THE GAME", Color.white, 0f));

        // Set game state to restart game
        gameState = GameState.restartGame;
    }

    /// <summary>
    /// Game Lost
    /// </summary>
    private IEnumerator GameLost()
    {
        previousGameState = GameState.gameLost;

        // Disable player
        GetPlayer().playerControl.DisablePlayer();


        // Get rank
        int rank = HighScoreManager.Instance.GetRank(gameScore);
        string rankText;

        // Test if the score is in the rankings
        if (rank > 0 && rank <= Settings.numberOfHighScoresToSave)
        {
            rankText = "YOUR SCORE IS RANKED " + rank.ToString("#0") + " IN THE TOP " + Settings.numberOfHighScoresToSave.ToString("#0");

            string name = GameResources.Instance.currentPlayer.playerName;

            if (name == "")
            {
                name = playerDetails.playerCharacterName.ToUpper();
            }

            // Update scores
            HighScoreManager.Instance.AddScore(new Score() { playerName = name, levelDescription = "LEVEL " + (currentDungeonLevelListIndex + 1).ToString() + " - " + GetCurrentDungeonLevel().levelName.ToUpper(), playerScore = gameScore }, rank);
        }
        else
        {
            rankText = "YOUR SCORE ISN'T RANKED IN THE TOP " + Settings.numberOfHighScoresToSave.ToString("#0");
        }


        // Wait 1 seconds
        yield return new WaitForSeconds(1f);

        // Fade Out
        yield return StartCoroutine(Fade(0f, 1f, 2f, Color.black));

        // Disable enemies (FindObjectsOfType is resource hungry - but ok to use in this end of game situation)
        Enemy[] enemyArray = GameObject.FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in enemyArray)
        {
            enemy.gameObject.SetActive(false);
        }

        // Display game lost
        yield return StartCoroutine(DisplayMessageRoutine("BAD LUCK " + GameResources.Instance.currentPlayer.playerName + "! YOU HAVE SUCCUMBED TO THE DUNGEON", Color.white, 2f));

        yield return StartCoroutine(DisplayMessageRoutine("YOU SCORED " + gameScore.ToString("###,###0") + "\n\n" + rankText, Color.white, 4f));

        yield return StartCoroutine(DisplayMessageRoutine("PRESS RETURN TO RESTART THE GAME", Color.white, 0f));

        // Set game state to restart game
        gameState = GameState.restartGame;
    }

    /// <summary>
    /// Restart the game
    /// </summary>
    private void RestartGame()
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    /// <summary>
    /// Get the player
    /// </summary>
    public Player GetPlayer()
    {
        return player;
    }


    /// <summary>
    /// Get the player minimap icon
    /// </summary>
    public Sprite GetPlayerMiniMapIcon()
    {
        return playerDetails.playerMiniMapIcon;
    }


    /// <summary>
    /// Get the current room the player is in
    /// </summary>
    public Room GetCurrentRoom()
    {
        return currentRoom;
    }

    /// <summary>
    /// Get the current dungeon level
    /// </summary>
    public DungeonLevelSO GetCurrentDungeonLevel()
    {
        return dungeonLevelList[currentDungeonLevelListIndex];
    }


    #region Validation

#if UNITY_EDITOR

    private void OnValidate()
    {
        HelperUtilities.ValidateCheckNullValue(this, nameof(pauseMenu), pauseMenu);
        HelperUtilities.ValidateCheckNullValue(this, nameof(messageTextTMP), messageTextTMP);
        HelperUtilities.ValidateCheckNullValue(this, nameof(canvasGroup), canvasGroup);
        HelperUtilities.ValidateCheckEnumerableValues(this, nameof(dungeonLevelList), dungeonLevelList);
    }

#endif

    #endregion Validation

}