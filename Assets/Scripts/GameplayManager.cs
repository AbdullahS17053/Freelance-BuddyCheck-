using DG.Tweening;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Pun.Demo.PunBasics;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GameplayManager : MonoBehaviourPunCallbacks
{
    public static GameplayManager instance;

    private PhotonView photonView;

    // -----------------------------------------------------------
    #region INSPECTOR REFERENCES
    // -----------------------------------------------------------

    [Header("UI Panels")]
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private GameObject playersPanel;
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private GameObject waitingPlayerReconnectPanel;
    [SerializeField] private float waitForReconnectTime;
    [SerializeField] private GameObject PlayersTryingReconnect;

    [Header("Simple Chat System")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private Button sendChatButton;
    [SerializeField] private TMP_Text[] chatDisplayText;

    [Header("Hint UI")]
    [SerializeField] private TMP_Text hintNameText;
    [SerializeField] private TMP_InputField hintAnswerInput;
    [SerializeField] private Button submitHintAnswerButton;
    [SerializeField] private Button reniewHintAnswerButton;
    [SerializeField] private Button reniewHintAnswerConfirmButton;
    [SerializeField] private TextMeshProUGUI reniewHintAnswerConfirmText;

    [Header("Player UI")]
    [SerializeField] private TMP_InputField playerGuessInput;
    [SerializeField] private Button voteButton;
    [SerializeField] private GameObject voteWarning;

    [Header("Game State UI")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private Button[] exampleButton;
    [SerializeField] private GameObject[] exampleText;

    [Header("Leaderboard UI")]
    [SerializeField] private GameObject leaderboardContainer;
    [SerializeField] private GameObject leaderboardEntryPrefab;
    [SerializeField] private Button replayButton;

    [Header("Gameplay Quiz")]
    [SerializeField] private GameObject realAnswer;
    [SerializeField] private TextMeshProUGUI[] bad;
    [SerializeField] private TextMeshProUGUI[] good;
    [SerializeField] private TextMeshProUGUI[] example;
    [SerializeField] private TextMeshProUGUI[] hint;
    [SerializeField] private TextMeshProUGUI[] username;
    [SerializeField] private TextMeshProUGUI[] scores;
    [SerializeField] private CategoryCatalog[] categories;

    public List<Categories> hintCatories;
    public List<Categories> hintStoredCatories;
    private int hintCategoryID;
    private int hintChance = 0;
    private int categoryID;

    [System.Serializable]
    public class CategoryCatalog
    {
        public Categories[] categories;
    }

    [Header("Player Profiles")]
    [SerializeField] private GameProfileUpdate[] playerProfiles;

    #endregion
    // -----------------------------------------------------------



    // -----------------------------------------------------------
    #region GAME STATE DATA
    // -----------------------------------------------------------

    private int currentHostAnswer = -1;
    public bool gameActive = false;


    private int tempPlayerChecks = 0;
    private int tempScore;
    private int hintRoundEach = 0;
    private int voting = 0;
    private int[] hinters;
    private int hintRound = 0;
    private int currentRound = 1;
    public int totalRounds = 4;
    private TMP_InputField roundsInputField;

    private HashSet<int> usedCategoryIDs = new HashSet<int>();

    private List<string> chatMessages = new List<string>();
    private const int MAX_CHAT_LINES = 8;

    // --- Reworked ready-system: separate ready-for-hints
    private Dictionary<string, bool> readyForHints = new Dictionary<string, bool>();
    public GameObject waitingForPlayersPanel;

    private Dictionary<string, int> playerTotalScores = new Dictionary<string, int>();
    private Dictionary<string, int> currentRoundGuesses = new Dictionary<string, int>();
    private Dictionary<string, GameProfileUpdate> activeProfiles = new Dictionary<string, GameProfileUpdate>();

    public FriendStats friendStatsRound;
    private FusionRoomManager roomManager;
    private const string VOTING = "Voting";
    private const string TOTAL_ROUNDS_KEY = "TotalRounds";
    private const string EACH_ROUNDS_KEY = "EachRounds";

    #endregion
    // -----------------------------------------------------------



    // -----------------------------------------------------------
    #region SINGLETON & INITIALIZATION
    // -----------------------------------------------------------

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else Destroy(gameObject);

        roomManager = GetComponent<FusionRoomManager>();

    }

    void Start()
    {
        photonView = GetComponent<PhotonView>();


        foreach (var b in exampleButton) b.onClick.AddListener(() => ToggleExampleButton(true));
        reniewHintAnswerButton.onClick.AddListener(ReniewCategoryLimit);
        reniewHintAnswerConfirmButton.onClick.AddListener(HintNewCategory);
        reniewHintAnswerConfirmButton.onClick.AddListener(increaseHintChance);
        voteButton.onClick.AddListener(SubmitVote);
        submitHintAnswerButton.onClick.AddListener(SubmitHint);
        replayButton.onClick.AddListener(RequestRestartGame);
        sendChatButton.onClick.AddListener(() => SendChatMessage());

        chatInputField.onSubmit.AddListener(delegate { SendChatMessage(); });

        ResetUIForNewRound();
        ClearLeaderboard();
        InitializeChat();
        SetChatActive(true);

        if (PhotonNetwork.InRoom) SyncWithRoomState();
    }

    #endregion
    // -----------------------------------------------------------



    // -----------------------------------------------------------
    #region UI INITIALIZATION
    // -----------------------------------------------------------

    private void InitializeChat()
    {
        chatMessages.Clear();
        UpdateChatDisplay();
    }

    private void ResetUIForNewRound()
    {
        UpdateRoundText();

        if (playerGuessInput != null) playerGuessInput.text = "";

        foreach (var profile in playerProfiles)
        {
            if (profile.gameObject.activeSelf)
            {
                profile.hideAnswers();
                profile.SetChatActivity(false);
            }
        }
    }

    #endregion
    // -----------------------------------------------------------



    // -----------------------------------------------------------
    #region GAME START & ROUND FLOW
    // -----------------------------------------------------------

    public void StartGame()
    {
        // This only runs after ad unlock or immediately if no ad required
        StatsManager.instance.SyncAllPlayersInfo();

        hintRoundEach = totalRounds;
        var props = new Hashtable { [EACH_ROUNDS_KEY] = hintRoundEach };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        voting = 0;
        hintChance = 0;
        var props2 = new Hashtable { [VOTING] = voting };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props2);

        photonView.RPC("CheckFullVersions", RpcTarget.All);


    }
    [PunRPC]
    private void CheckFullVersions()
    {
        if (LoginManager.Instance.fullVersion == 1)
        {

            Hashtable props3 = new Hashtable
            {
                ["my_purchase"] = 1        // Buyer marker
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props3);

        }

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(WaitForAllPlayersReadyForHints());
        }
    }

    IEnumerator WaitForAllPlayersReadyForHints()
    {
        yield return new WaitForSeconds(0.5f);

        bool someonePurchased = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("my_purchase");

        if (someonePurchased)
        {
            Debug.Log("Buyer in room → No ads, start game immediately");

            photonView.RPC("HintCollection", RpcTarget.All, false);
            yield break;
        }

        photonView.RPC("HintCollection", RpcTarget.All, true);

    }

    [PunRPC]
    private void HintCollection(bool ad)
    {
        Debug.Log("New Hint Round of " + hintRoundEach);

        if (PhotonNetwork.IsMasterClient)
        {
            int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;

            hintRoundEach = totalRounds / Mathf.CeilToInt((float)playerCount);

            if (hintRoundEach < 3)
                hintRoundEach = 3;

            // Every hint submitted must be played — no hints lost.
            // totalRounds must equal hints-per-player × player count.
            totalRounds = playerCount * hintRoundEach;

            var props = new Hashtable
            {
                [EACH_ROUNDS_KEY] = hintRoundEach,
                [TOTAL_ROUNDS_KEY] = totalRounds
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        StatsManager.instance.maxScore = totalRounds * 2;
        hinters = new int[PhotonNetwork.CurrentRoom.PlayerCount];
        // Reset used categories for fresh game
        usedCategoryIDs.Clear();
        // Reset ready states so match can start after hints
        readyForHints.Clear();
        foreach (Player p in PhotonNetwork.PlayerList)
            readyForHints[p.NickName] = false;


        waitingForPlayersPanel.SetActive(true);
        hintPanel.SetActive(true);
        HintNewCategory();
        FusionRoomManager.Instance.Fpause(false); // ✅ Queue RUNNING - game is starting


        if (ad)
        {
            AdCommunicator.Instance.TryStartGame();
        }
    }
    private void StartNewRound()
    {
        photonView.RPC("SetPlayerOfflineRPC", RpcTarget.All, PhotonNetwork.NickName, false);

        ResetRoundState();


        realAnswer.SetActive(false);
        currentRoundGuesses.Clear();

        ResetUIForNewRound();
        HideAllAnswers();
        NewCategory();

        // AdCommunicator.Instance.ShowEndOfRoundAd();
    }

    private void ProceedToNextPhase()
    {
        currentRound++;

        UpdatePlayerProfiles();

        CalculateRoundScores();

        Debug.Log("Proceeding to next phase..." + currentRound + " ||| " + totalRounds);

        // StatsManager.instance.UpdatePlayerStatistics();
        if (currentRound > totalRounds)
        {
            ShowOverallLeaderboard();

        }
        else
        {
            StartNewRound();
        }
    }

    #endregion
    // -----------------------------------------------------------



    // -----------------------------------------------------------
    #region HINT SYSTEM
    // -----------------------------------------------------------

    public void ToggleExampleButton(bool toggle = true)
    {
        if (toggle)
        {
            foreach (var g in exampleText) g.SetActive(!g.activeSelf);
        }
        else
        {
            foreach (var g in exampleText) g.SetActive(toggle);
        }
    }
    /// <summary>
    /// /////////////////// YEAHHHHHHHHHHHHHH
    /// </summary>
    
    private int lastTempScore = -1;
    public void HintNewCategory()
    {
        int totalCategories = categories[0].categories.Length;

        // If every category has been used, reset so player isn't stuck
        if (usedCategoryIDs.Count >= totalCategories)
            usedCategoryIDs.Clear();

        // Pick a random category that this player hasn't used yet this game
        int attempts = 0;
        do
        {
            hintCategoryID = UnityEngine.Random.Range(0, totalCategories);
            attempts++;
        }
        while (usedCategoryIDs.Contains(hintCategoryID) && attempts < totalCategories * 3);

        foreach (var t in bad)
            t.text = categories[0].categories[hintCategoryID].bad[Menus.instance.GetLanguageIndex()];
        foreach (var t in example)
            t.text = categories[0].categories[hintCategoryID].example[Menus.instance.GetLanguageIndex()];
        foreach (var t in good)
            t.text = categories[0].categories[hintCategoryID].good[Menus.instance.GetLanguageIndex()];

        // Prevent same score appearing twice in a row
        int newScore;
        int scoreAttempts = 0;
        do
        {
            newScore = UnityEngine.Random.Range(0, 10);
            scoreAttempts++;
        }
        while (newScore == lastTempScore && scoreAttempts < 10);

        tempScore = newScore;
        lastTempScore = tempScore;

        categories[0].categories[hintCategoryID].score = tempScore;

        foreach (var t in scores)
            t.text = tempScore.ToString();

        // Disable renew button if free user has used up their renewals
        if (LoginManager.Instance.fullVersion != 1 && hintChance >= 1)
        {
            reniewHintAnswerButton.interactable = false;
        }
        else
        {
            reniewHintAnswerButton.interactable = true;
        }
    }
    public void increaseHintChance()
    {
        // ✅ THEN: Increment usage counter
        hintChance++;
        // ✅ FINALLY: Disable button if free user has used up their renewals
        if (LoginManager.Instance.fullVersion != 1 && hintChance >= 1)
        {
            reniewHintAnswerButton.interactable = false;
        }
        else
        {
            reniewHintAnswerButton.interactable = true;
        }
        Debug.Log(hintChance);
    }
    public void ReniewCategoryLimit()
    {
        if (LoginManager.Instance.fullVersion == 1)
        {
            reniewHintAnswerConfirmText.text = "Do you really want to skip ?";
        }
        else
        {
            reniewHintAnswerConfirmText.text = "Do you really want to skip ? : " + (1 - hintChance) + " Skip Left";
        }
    }
    private int hintRoundIndex = 0;
    public void NewCategory()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        FusionRoomManager.Instance.Fpause(true);

        // Sort lists consistently across all clients
        hintCatories = hintCatories.OrderBy(c => c.playerID).ToList();
        hintStoredCatories = hintStoredCatories.OrderBy(c => c.playerID).ToList();

        // Get distinct player IDs in a fixed sorted order
        List<int> sortedPlayerIDs = hintCatories
            .Select(c => c.playerID)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        if (sortedPlayerIDs.Count == 0)
        {
            Debug.LogWarning("NewCategory: No players in hintCatories!");
            FusionRoomManager.Instance.Fpause(false);
            return;
        }

        // ✅ Round-robin: A B C D A B C D — no randomness
        int selectedPlayerID = sortedPlayerIDs[hintRoundIndex % sortedPlayerIDs.Count];

        // Find first available hint from this player
        // Round-robin by index directly on the remaining list
        int categoryIndex = hintCatories.FindIndex(c => c.playerID == selectedPlayerID);

        if (categoryIndex == -1)
        {
            Debug.LogWarning($"NewCategory: No hints left for player {selectedPlayerID}, skipping.");
            hintRoundIndex++;
            FusionRoomManager.Instance.Fpause(false);
            return;
        }

        int slot = hintRoundIndex % hinters.Length;
        hinters[slot] = selectedPlayerID;

        Debug.Log($"[Round-Robin] hintRoundIndex={hintRoundIndex} → Player {selectedPlayerID} at categoryIndex={categoryIndex}, slot={slot}");

        hintRoundIndex++;

        photonView.RPC("GameNewCategoryAll", RpcTarget.All, categoryIndex, slot);
    }



    [PunRPC]
    private void UpdatePlayerCountAdd()
    {
        tempPlayerChecks++;
    }
    private int currentHinterPlayerID = -1;
    [PunRPC]
    private void GameNewCategoryAll(int ID, int playerID)
    {
        // Every client sorts the same way
        hintCatories = hintCatories.OrderBy(c => c.playerID).ToList();
        hintStoredCatories = hintStoredCatories.OrderBy(c => c.playerID).ToList();

        hinters[playerID] = hintCatories[ID].playerID;
        currentHinterPlayerID = hintCatories[ID].playerID;


        /*
        // Clear crown from all profiles first
        foreach (var profile in activeProfiles.Values)
            profile.SetCrown(false);


        // Set crown on current hinter
        string hinterName = hintCatories[ID].player;
        if (activeProfiles.TryGetValue(hinterName, out GameProfileUpdate hinterProfile))
            hinterProfile.SetCrown(true);
        */

        // Debug the order
        string ids = string.Join(", ", hintCatories.Select(c => c.playerID));
        Debug.Log("hintCatories sorted order: " + ids);
        Debug.Log("ID: " + ID);

        

        // Reset the counter
        tempPlayerChecks = 0;

        categoryID = ID;

        // Now ID matches the sorted list correctly
        foreach (var t in bad) t.text = hintCatories[ID].bad[Menus.instance.GetLanguageIndex()];
        foreach (var t in example) t.text = hintCatories[ID].example[Menus.instance.GetLanguageIndex()];
        foreach (var t in good) t.text = hintCatories[ID].good[Menus.instance.GetLanguageIndex()];
        foreach (var t in username) t.text = hintCatories[ID].player + "'s hint:";
        foreach (var t in hint) t.text = hintCatories[ID].hint;
        foreach (var t in scores) t.text = hintCatories[ID].score.ToString();

        currentHostAnswer = hintCatories[ID].score;

        if (hintCatories[ID].playerID == StatsManager.instance.myID)
        {
            playerGuessInput.interactable = false;
            voteButton.interactable = false;
            SetChatActive(true);
        }
        else
        {
            playerGuessInput.interactable = true;
            voteButton.interactable = true;
        }

        // Crown — just use name directly
        foreach (var profile in activeProfiles.Values)
            profile.SetCrown(false);

        if (activeProfiles.TryGetValue(hintCatories[ID].player, out GameProfileUpdate hinterProfile))
            hinterProfile.SetCrown(true);
        /*
        foreach (var profile in playerProfiles)
            profile.SetCrown(profile.playerID == hintCatories[ID].playerID);
        */
        FusionRoomManager.Instance.Fpause(false); // ✅ Queue RUNNING - category ready
    }


    bool IsInvalidHintAnswer(TMP_InputField input)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.text))
            return true;

        if (!int.TryParse(input.text, out int value))
            return true;

        return value <= -1 || value >= 11;
    }

    public void SubmitHint()
    {
        /*if (IsInvalidHintAnswer(hintAnswerInput))
        {
            Debug.Log("Invalid hint answer. Must be a number between 0 and 10.");
            hintAnswerInput.text = "";
            return; // ⛔ STOP submission
        }*/

        hintRound++;

        FusionRoomManager.Instance.Fpause(true); // ✅ Queue PAUSED - submitting hint

        // Mark this category as used so it won't appear again this game for this player
        usedCategoryIDs.Add(hintCategoryID);

        photonView.RPC("AddHintList", RpcTarget.All,
            hintCategoryID,
            tempScore,
            hintAnswerInput.text,
            PhotonNetwork.NickName,
            StatsManager.instance.myID
            );

        hintAnswerInput.text = "";
        ToggleExampleButton(false);
        HintNewCategory();

        if (hintRound >= hintRoundEach)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(VOTING, out object vote))
                voting = (int)vote;

            voting++;

            var props2 = new Hashtable { [VOTING] = voting };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props2);

            hintRound = 0;
            hintPanel.SetActive(false);
            gameplayPanel.SetActive(true);
            waitingForPlayersPanel.SetActive(true);


            if (voting >= PhotonNetwork.CurrentRoom.PlayerCount)
            {
                voting = 0;

                var props1 = new Hashtable { [VOTING] = voting };
                PhotonNetwork.CurrentRoom.SetCustomProperties(props1);

                photonView.RPC("VotingDone", RpcTarget.All);
            }

        }
    }

    [PunRPC]
    private void AddHintList(int ID, int score, string hint, string player, int id)
    {
        Categories newCat = ScriptableObject.CreateInstance<Categories>();
        newCat.bad = categories[0].categories[ID].bad;
        newCat.good = categories[0].categories[ID].good;
        newCat.example = categories[0].categories[ID].example;
        newCat.score = score;
        newCat.hint = hint;
        newCat.player = player;
        newCat.playerID = id;

        categories[0].categories[ID].score = score;

        hintCatories.Add(newCat);
        hintStoredCatories.Add(newCat);

        // ✅ CRITICAL FIX: Resume queue after hint is added
        FusionRoomManager.Instance.Fpause(false);
    }

    public void RemoveHint()
    {
        if (categoryID >= 0 && categoryID < hintCatories.Count)
        {
            hintCatories.RemoveAt(categoryID);
        }
        else
        {
            Debug.LogWarning($"RemoveHint: index {categoryID} out of range (list count: {hintCatories.Count})");
        }
    }

    #endregion
    // -----------------------------------------------------------



    #region PLAYER READY SYSTEM


    [PunRPC]
    private void VotingDone()
    {
        waitingForPlayersPanel.SetActive(false);
        StartNewRound();
    }


    #endregion



    // -----------------------------------------------------------
    #region HOST LOGIC
    // -----------------------------------------------------------

    public void SubmitHostAnswer()
    {
        photonView.RPC("StartVotingPhaseRPC", RpcTarget.All, hintStoredCatories[categoryID].score);
    }

    #endregion
    // -----------------------------------------------------------



    // -----------------------------------------------------------
    #region VOTING SYSTEM
    // -----------------------------------------------------------

    [PunRPC]
    private void StartVotingPhaseRPC(int Answer)
    {
        currentHostAnswer = Answer;

        Debug.Log("Real Answers " + currentHostAnswer + hintStoredCatories[categoryID].score);

        if (hostPanel) hostPanel.SetActive(false);

        // 🛑 Disable voting if YOU are the hint giver
        if (hintStoredCatories[categoryID].playerID == StatsManager.instance.myID)
        {
            playerGuessInput.interactable = false;
            voteButton.interactable = false;
            SetChatActive(true);
        }
        else
        {
            playerGuessInput.interactable = true;
            voteButton.interactable = true;
        }

        SetChatActive(true);
    }

    public void SubmitVote()
    {
        // Hard guard: you cannot vote on your own hint, even if UI glitches
        if (currentHinterPlayerID == StatsManager.instance.myID)
        {
            Debug.LogWarning("SubmitVote blocked: you are the current hinter.");
            endGameAll();
            return;
        }

        if (IsInvalidHintAnswer(playerGuessInput))
        {
            Debug.Log("Invalid hint answer. Must be a number between 0 and 10.");
            playerGuessInput.text = "";
            DOTween.CompleteAll();
            voteWarning.SetActive(true);
            voteWarning.transform.DOScale(1f, 0.2f).OnComplete(() =>
            {
                voteWarning.transform.DOScale(0f, 0.2f).SetDelay(3).OnComplete(() => voteWarning.SetActive(false));
            }); 
            return; // ⛔ STOP submission
        }

        FusionRoomManager.Instance.Fpause(true); // ✅ Queue PAUSED - submitting vote

        if (!int.TryParse(playerGuessInput.text, out int guessedNumber))
            return;

        playerGuessInput.interactable = false;
        voteButton.interactable = false;

        SendChatMessage(PhotonNetwork.NickName + ": voted");

        photonView.RPC("SubmitPlayerGuessRPC", RpcTarget.All, PhotonNetwork.NickName, guessedNumber);
    }

    [PunRPC]
    private void SubmitPlayerGuessRPC(string playerName, int guess)
    {
        // ✅ Only remove hint on first guess, not every time
        if (!currentRoundGuesses.ContainsKey(playerName))
        {
            if (currentRoundGuesses.Count == 0) // first guesser triggers removal
                RemoveHint();
        }
        else
        {
            FusionRoomManager.Instance.Fpause(false);
            return;
        }

        currentRoundGuesses[playerName] = guess;

        Player guesser = PhotonNetwork.PlayerList.FirstOrDefault(p => p.NickName == playerName);
        int guesserID = -1;
        if (guesser != null && guesser.CustomProperties.TryGetValue("myID", out object idObj))
            guesserID = (int)idObj;
        else
        {
            Debug.LogError($"Could not find player ID for {playerName}");
            FusionRoomManager.Instance.Fpause(false);
            return;
        }

        int points = CalculatePoints(guess, currentHostAnswer);
        int hinterID = currentHinterPlayerID;

        // Hard guard: hinter cannot score points on their own hint
        if (guesserID == hinterID)
        {
            Debug.LogWarning($"SubmitPlayerGuessRPC: blocked self-score for player {guesserID}");
            FusionRoomManager.Instance.Fpause(false);

            if (currentRoundGuesses.Count >= PhotonNetwork.CurrentRoom.PlayerCount - 1)
            {
                realAnswer.SetActive(true);
                ShowPlayerAnswers();
                Invoke("ProceedToNextPhase", 3f);
            }
            return;
        }

        StatsManager.instance.SyncRoundDataLocal(hinterID, guesserID, points);

        FusionRoomManager.Instance.Fpause(false);
        photonView.RPC("SyncPlayerGuessRPC", RpcTarget.Others, playerName, guess);

        if (currentRoundGuesses.Count >= PhotonNetwork.CurrentRoom.PlayerCount - 1)
        {
            realAnswer.SetActive(true);
            ShowPlayerAnswers();
            Invoke("ProceedToNextPhase", 3f);
        }
    }

    private int CalculatePoints(int guess, int answer)
    {
        int diff = Mathf.Abs(guess - answer);
        return diff == 0 ? 2 : diff == 1 ? 1 : 0;
    }



    [PunRPC]
    private void SyncPlayerGuessRPC(string playerName, int guess)
    {
        if (!currentRoundGuesses.ContainsKey(playerName))
            currentRoundGuesses[playerName] = guess;
    }

    #endregion
    // -----------------------------------------------------------


    // -----------------------------------------------------------
    #region Player Stats
    // -----------------------------------------------------------



    #endregion
    // -----------------------------------------------------------


    // -----------------------------------------------------------
    #region SCORE & LEADERBOARD
    // -----------------------------------------------------------

    private void CalculateRoundScores()
    {
        foreach (var guess in currentRoundGuesses)
        {
            string playerName = guess.Key;
            int playerGuess = guess.Value;

            if (!playerTotalScores.ContainsKey(playerName))
                playerTotalScores[playerName] = 0;

            int diff = Mathf.Abs(playerGuess - currentHostAnswer);
            int points = diff == 0 ? 2 : diff == 1 ? 1 : 0;

            playerTotalScores[playerName] += points;
        }
    }

    private void ShowPlayerAnswers()
    {
        // Check if all players (except host) have guessed
        int expectedGuesses = PhotonNetwork.CurrentRoom.PlayerCount - 1; // exclude host
        if (currentRoundGuesses.Count < expectedGuesses)
        {
            Debug.Log("Not all players have guessed yet. Waiting...");
            return;
        }

        // All players guessed, show answers
        foreach (var guess in currentRoundGuesses)
        {
            if (!activeProfiles.TryGetValue(guess.Key, out GameProfileUpdate profile))
            {
                Debug.LogWarning($"Player profile not found: {guess.Key}");
                continue;
            }

            profile.numberGuessed(guess.Value.ToString());
            profile.showAnswers();

            int difference = Mathf.Abs(guess.Value - currentHostAnswer);
            profile.Correct(difference <= 2);
        }
    }



    private void ShowOverallLeaderboard()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            object[] data = PrepareLeaderboardData();
            photonView.RPC("ShowLeaderboardRPC", RpcTarget.All, data);
        }
    }

    [PunRPC]
    private void ShowLeaderboardRPC(object[] leaderboardData)
    {
        leaderboardPanel.SetActive(true);
        gameplayPanel.SetActive(false);

        foreach (GameProfileUpdate profile in activeProfiles.Values)
        {
            profile.ResetSlider();
        }
        StatsManager.instance.FinalizeRound();
        ClearLeaderboard();

        // STEP BY 5 (because each player has 5 values) - ✅ FIXED
        for (int i = 0; i < leaderboardData.Length; i += 5)
        {
            string playerName = (string)leaderboardData[i];
            int score = (int)leaderboardData[i + 1];
            int avatarIndex = (int)leaderboardData[i + 2];
            int totalScoreAllPlayers = (int)leaderboardData[i + 3];
            int otherID = (int)leaderboardData[i + 4];

            GameObject entryObj = Instantiate(leaderboardEntryPrefab, leaderboardContainer.transform);
            LeaderboardEntry entry = entryObj.GetComponent<LeaderboardEntry>();

            if (entry != null)
            {
                int rank = (i / 5) + 1; // ✅ FIXED from (i / 4)
                entry.SetForOverall(rank, playerName, score, totalScoreAllPlayers, false, otherID);

                if (PlayerListUI.instance != null)
                    entry.SetAvatar(PlayerListUI.instance.GetAvatarSprite(avatarIndex));
            }
        }

        // Update the persistent stats
        StatsManager.instance.UpdatePlayerStatistics();

        AdCommunicator.Instance.ShowEndOfRoundAd();

    }


    private object[] PrepareLeaderboardData()
    {
        List<object> data = new List<object>();
        var sortedScores = playerTotalScores.OrderByDescending(p => p.Value).ToList();

       // int totalScoreAllPlayers = playerTotalScores.Values.Sum(); // total of all players
        int totalScoreAllPlayers = StatsManager.instance.maxScore; // total of all players * 2

        foreach (var playerScore in sortedScores)
        {
            Player player = PhotonNetwork.PlayerList.FirstOrDefault(p => p.NickName == playerScore.Key);

            int avatarIndex = player != null && player.CustomProperties.TryGetValue("avatarIndex", out object idx)
                                ? (int)idx
                                : 0;
            int playerID = 0;
            if (PhotonNetwork.PlayerList.FirstOrDefault(p => p.NickName == playerScore.Key)?.CustomProperties.TryGetValue("myID", out object idObj) ?? false)
            {
                playerID = (int)idObj;
            }


            data.Add(playerScore.Key);
            data.Add(playerScore.Value);
            data.Add(avatarIndex);
            data.Add(totalScoreAllPlayers); // <-- Add total score here
            data.Add(playerID); // <-- PlayerID
        }

        return data.ToArray();
    }


    private void ClearLeaderboard()
    {
        foreach (Transform child in leaderboardContainer.transform) Destroy(child.gameObject);
    }

    #endregion
    // -----------------------------------------------------------



    // -----------------------------------------------------------
    #region PLAYER PROFILE MANAGEMENT
    // -----------------------------------------------------------

    private void InitializePlayerTracking()
    {
        playerTotalScores.Clear();
        currentRoundGuesses.Clear();

        foreach (Player p in PhotonNetwork.PlayerList)
            playerTotalScores[p.NickName] = 0;
    }

    private void UpdatePlayerProfiles()
    {
        activeProfiles.Clear();

        var sortedPlayers = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToList();

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            if (i < playerProfiles.Length)
            {
                Player player = sortedPlayers[i];
                GameProfileUpdate profile = playerProfiles[i];

                profile.gameObject.SetActive(true);
                activeProfiles[player.NickName] = profile;

                // Assign ID directly to profile
                if (player.CustomProperties.TryGetValue("myID", out object idObj))
                    profile.playerID = (int)idObj;

                UpdateSingleProfile(player);
                profile.transform.SetSiblingIndex(i);
            }
        }

        for (int i = sortedPlayers.Count; i < playerProfiles.Length; i++)
        {
            playerProfiles[i].gameObject.SetActive(false);
        }
    }

    private void UpdateSingleProfile(Player player)
    {
        if (!activeProfiles.TryGetValue(player.NickName, out GameProfileUpdate profile))
            return;

        int avatarIndex = 0;
        if (player.CustomProperties.TryGetValue("avatarIndex", out object idxObj))
            avatarIndex = (int)idxObj;

        profile.updatePlayer(avatarIndex, player.NickName);
    }

    private void HideAllAnswers()
    {
        foreach (var profile in playerProfiles)
        {
            if (profile.gameObject.activeSelf)
                profile.hideAnswers();
        }
    }

    #endregion
    // -----------------------------------------------------------



    // -----------------------------------------------------------
    #region CHAT SYSTEM
    // -----------------------------------------------------------

    public void SendChatMessage(string msg = null)
    {
        string message;

        if (msg != null)
        {
            message = msg;
        }
        else
        {
            message = chatInputField.text.Trim();
        }

        if (string.IsNullOrEmpty(message)) return;

        photonView.RPC("ReceiveChatMessageRPC", RpcTarget.All, PhotonNetwork.NickName, message);

        chatInputField.text = "";
        chatInputField.ActivateInputField();
    }

    [PunRPC]
    private void ReceiveChatMessageRPC(string sender, string message)
    {
        AddChatMessage(sender, message);

        if (activeProfiles.TryGetValue(sender, out GameProfileUpdate profile))
        {
            profile.SetChatActivity(true, message);
        }
    }

    private void AddChatMessage(string sender, string message)
    {
        string formatted = $"{sender}: {message}";
        chatMessages.Add(formatted);

        while (chatMessages.Count > MAX_CHAT_LINES)
            chatMessages.RemoveAt(0);

        UpdateChatDisplay();
    }

    private void UpdateChatDisplay()
    {
        foreach (var t in chatDisplayText)
            t.text = string.Join("\n", chatMessages);
    }

    public void SetChatActive(bool active)
    {
        chatInputField.interactable = active;
        sendChatButton.interactable = active;

        if (active)
        {
            chatInputField.text = "";
            chatInputField.placeholder.GetComponent<TMP_Text>().text = "Type message...";
            chatInputField.ActivateInputField();
        }
        else
        {
            chatInputField.text = "";
            chatInputField.placeholder.GetComponent<TMP_Text>().text = "Chat disabled";
        }
    }

    #endregion
    // -----------------------------------------------------------



    // -----------------------------------------------------------
    #region PHOTON CALLBACKS
    // -----------------------------------------------------------

    public override void OnJoinedRoom()
    {
        if(PhotonNetwork.LocalPlayer.HasRejoined)
        {
            Debug.Log("Player rejoined room, skipping OnJoinedRoom logic.");
            return; // Skip the rest of this method if we're rejoining
        }

        Debug.Log("OnJoinedRoom triggered Gameplay Manager");
        if (!PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(TOTAL_ROUNDS_KEY, out object tot))
                totalRounds = (int)tot;
        }
        else
        {
            var total = new Hashtable { [TOTAL_ROUNDS_KEY] = totalRounds };
            PhotonNetwork.CurrentRoom.SetCustomProperties(total);
        }
        photonView.RPC("SyncGameStateRPC", RpcTarget.All);
        playersPanel.SetActive(true);

        UpdatePlayerProfiles();
        SyncWithRoomState();
    }

    private void SyncWithRoomState()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(TOTAL_ROUNDS_KEY, out object totalRoundsObj))
            totalRounds = (int)totalRoundsObj;

        UpdatePlayerProfiles();
        UpdateRoundText();
    }




    public override void OnLeftRoom()
    {
        Debug.Log("OnLeftRoom triggered Gameplay Manager");

    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("OnPlayerEnteredRoom triggered Gameplay Manager");

        UpdatePlayerProfiles(); // ← MUST be first, rebuilds activeProfiles
        //photonView.RPC("SyncGameStateRPC", RpcTarget.All);

        if (newPlayer.HasRejoined)
        {
            Debug.Log($"Player {newPlayer.NickName} rejoined. Resyncing game state to them.");

            tryReconnect();
            photonView.RPC("SetPlayerOfflineRPC", RpcTarget.All, newPlayer.NickName, false);
            return; // Don't treat as new join during active game
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("OnPlayerLeftRoom triggered Gameplay Manager");

        UpdatePlayerProfiles(); // ← MUST be first, rebuilds activeProfiles
        if (otherPlayer.IsInactive)
        {
            Debug.Log("Player temporarily disconnected: " + otherPlayer.NickName);
            playerDisconnects();
            photonView.RPC("SetPlayerOfflineRPC", RpcTarget.All, otherPlayer.NickName, true);
            return; // Ignore temporary leave
        }

        // Permanent leave — end game for everyone
        /*if (PhotonNetwork.IsMasterClient)
        {
            endGameAll();
        }*/
    }


    public override void OnDisconnected(DisconnectCause cause)
    {
        if (PhotonNetwork.ReconnectAndRejoin())
        {
            
        }
        else
        {
            PhotonNetwork.LeaveRoom(false);
            if(!PhotonNetwork.Reconnect())
            {
                FusionRoomManager.Instance.TryReconnect();
            }
            HandleRoomLeave();
        }
    }

    #region Game Ending Logic Calls

    [PunRPC]
    private void EndGameForAllRPC()
    {
        StopAllCoroutines();
        PlayersTryingReconnect.SetActive(false);
        leaderboardPanel.SetActive(false);
        gameplayPanel.SetActive(false);
        playersPanel.SetActive(true);
        hostPanel.SetActive(false);
        hintPanel.SetActive(false);

        // --- Reset simple state ---
        gameActive = false;
        currentRound = 1;
        currentHostAnswer = -1;
        hintRound = 0;
        voting = 0;
        hintChance = 0;
        currentHinterPlayerID = -1;
        hintRoundIndex = 0;
        lastTempScore = -1;
        StatsManager.instance.maxScore = 0;

        // --- Reset all lists ---
        playerTotalScores.Clear();
        currentRoundGuesses.Clear();
        readyForHints.Clear();
        hintCatories.Clear();
        hintStoredCatories.Clear();
        usedCategoryIDs.Clear();
        chatMessages.Clear();

        ResetRoundState();
        InitializePlayerTracking();

        chatMessages.Clear();
        UpdateChatDisplay();

        // Reset inputs
        playerGuessInput.text = "";
        playerGuessInput.interactable = true;
        voteButton.interactable = true;

        foreach (var profile in playerProfiles)
        {
            profile.numberGuessed("");
            profile.hideAnswers();
            profile.SetChatActivity(false);
        }
        for (int j = 0; j < hinters.Length; j++)
        {
            hinters[j] = 0;
        }
        messagePanel.SetActive(true);

        gameplayPanel.SetActive(false);
        leaderboardPanel.SetActive(false);

        // Show the room lobby UI again
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = true;
            roomManager.ShowHostPanel();
        }
        else
        {
            roomManager.ShowClientPanel();
        }

        StatsManager.instance.ResetAllRounds();
    }
    [PunRPC]
    private void RestartGameRPC()
    {
        StopAllCoroutines();
        PlayersTryingReconnect.SetActive(false);
        leaderboardPanel.SetActive(false);
        gameplayPanel.SetActive(false);
        playersPanel.SetActive(true);
        hostPanel.SetActive(false);
        hintPanel.SetActive(false);

        // --- Reset simple state ---
        gameActive = false;
        currentRound = 1;
        currentHostAnswer = -1;
        hintRound = 0;
        voting = 0;
        hintChance = 0;
        currentHinterPlayerID = -1;
        hintRoundIndex = 0;
        lastTempScore = -1;
        StatsManager.instance.maxScore = 0;

        // --- Reset all lists ---
        playerTotalScores.Clear();
        currentRoundGuesses.Clear();
        readyForHints.Clear();
        hintCatories.Clear();
        hintStoredCatories.Clear();
        usedCategoryIDs.Clear();
        chatMessages.Clear();

        ResetRoundState();
        InitializePlayerTracking();

        chatMessages.Clear();
        UpdateChatDisplay();

        // Reset inputs
        playerGuessInput.text = "";
        playerGuessInput.interactable = true;
        voteButton.interactable = true;

        foreach (var profile in playerProfiles)
        {
            profile.numberGuessed("");
            profile.hideAnswers();
            profile.SetChatActivity(false);
        }
        for (int j = 0; j < hinters.Length; j++)
        {
            hinters[j] = 0;
        }


        messagePanel.SetActive(false);

        gameplayPanel.SetActive(false);
        leaderboardPanel.SetActive(false);

        // Show the room lobby UI again
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = true;
            roomManager.ShowHostPanel();
        }
        else
        {
            roomManager.ShowClientPanel();
        }

        StatsManager.instance.ResetAllRounds();
    }

    public void HandleRoomLeave()
    {
        StopAllCoroutines();
        PlayersTryingReconnect.SetActive(false);
        leaderboardPanel.SetActive(false);
        gameplayPanel.SetActive(false);
        playersPanel.SetActive(false);
        hostPanel.SetActive(false);
        hintPanel.SetActive(false);

        // --- Reset simple state ---
        gameActive = false;
        currentRound = 1;
        currentHostAnswer = -1;
        hintRound = 0;
        voting = 0;
        hintChance = 0;
        currentHinterPlayerID = -1;
        hintRoundIndex = 0;
        lastTempScore = -1;
        StatsManager.instance.maxScore = 0;

        // --- Reset all lists ---
        playerTotalScores.Clear();
        currentRoundGuesses.Clear();
        readyForHints.Clear();
        hintCatories.Clear();
        hintStoredCatories.Clear();
        chatMessages.Clear();
        usedCategoryIDs.Clear();

        ResetRoundState();
        InitializePlayerTracking();

        chatMessages.Clear();
        UpdateChatDisplay();

        // Reset inputs
        playerGuessInput.text = "";
        playerGuessInput.interactable = true;
        voteButton.interactable = true;

        foreach (var profile in playerProfiles)
        {
            profile.numberGuessed("");
            profile.hideAnswers();
            profile.SetChatActivity(false);
        }
        if (hinters != null)
        {
            for (int j = 0; j < hinters.Length; j++)
            {
                hinters[j] = 0;
            }
        }


        //messagePanel.SetActive(false);

        gameplayPanel.SetActive(false);
        leaderboardPanel.SetActive(false);

        roomManager.ShowMenuPanel();

        StatsManager.instance.ResetAllRounds();
    }

    #endregion




    [PunRPC]
    private void SyncGameStateRPC()
    {
        SyncWithRoomState();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {

        if (propertiesThatChanged.ContainsKey(TOTAL_ROUNDS_KEY))
        {
            totalRounds = (int)propertiesThatChanged[TOTAL_ROUNDS_KEY];
            UpdateRoundText();
        }
        if (propertiesThatChanged.ContainsKey(EACH_ROUNDS_KEY))
        {
            hintRoundEach = (int)propertiesThatChanged[EACH_ROUNDS_KEY];
            Debug.Log("Updated hintRoundEach = " + hintRoundEach);
        }
        if (propertiesThatChanged.ContainsKey(VOTING))
        {
            voting = (int)propertiesThatChanged[VOTING];
            Debug.Log("Updated voting = " + voting);
        }

        UpdatePlayerProfiles();
    }

    #endregion
    // -----------------------------------------------------------



    // -----------------------------------------------------------
    #region UTILITY & WEB LINKS
    // -----------------------------------------------------------

    public void LeaveGame()
    {
        if (PhotonNetwork.InRoom)
        {
            photonView.RPC("RestartGameRPC", RpcTarget.All);
        }

    }

    public void UpdateRounds(TMP_InputField roundsInput)
    {
        roundsInputField = roundsInput;

        string raw = roundsInput.text.Trim();

        if (string.IsNullOrWhiteSpace(raw))
        {
            totalRounds = 6;
        }
        else if (int.TryParse(raw, out int rounds))
        {
            totalRounds = Mathf.Clamp(rounds, 6, 20);
        }
        else
        {
            totalRounds = 6;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            var props = new ExitGames.Client.Photon.Hashtable
            {
                [TOTAL_ROUNDS_KEY] = totalRounds
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        roundsInput.text = "";

        UpdateRoundText();
    }


    private void UpdateRoundText()
    {
        if (roundText != null)
            roundText.text = $"Round: {currentRound}/{totalRounds}";
    }

    public void CancelMatch()
    {
        endGameAll();
    }

    public void RequestRestartGame()
    {
        if (PhotonNetwork.NetworkClientState == ClientState.Joined)
        {
            photonView.RPC("RestartGameRPC", RpcTarget.All);
            //PhotonNetwork.LeaveRoom();
        }
        else
        {
            Debug.Log("Not leaving room because client is not ready. Current state: " + PhotonNetwork.NetworkClientState);
        }

        /*  ---- Game Restarts Another Round
        if (PhotonNetwork.IsMasterClient)
            photonView.RPC("RestartGameRPC", RpcTarget.All);
        */
    }
    private void ResetRoundState()
    {
        // Clear all crowns
        foreach (var profile in activeProfiles.Values)
            profile.SetCrown(false);

        currentRoundGuesses.Clear();

        playerGuessInput.text = "";
        playerGuessInput.interactable = true;
        voteButton.interactable = true;

        foreach (var profile in activeProfiles.Values)
        {
            profile.numberGuessed("");
            profile.hideAnswers();
        }

        SetChatActive(true);
    }

    


    public void OpenWebpageIntroVideo()
    {
        UnityEngine.Application.OpenURL("https://drive.google.com/file/d/1bobCLbFbLqbYDcIjoj-Y87e8YZcMeNDb/views");
    }

    #endregion
    // -----------------------------------------------------------

    // -----------------------------------------------------------
    #region Reconnection Logic

    //-> Disconnects
    public void playerDisconnects()
    {
        PlayersTryingReconnect.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(WaitForAutoGameEnd());
    }

    //-> TryReconnect
    public void tryReconnect()
    {
        StopAllCoroutines();
        if (isEveryoneOnline())
        {
            PlayersTryingReconnect.SetActive(false);
        }
        else
        {
            StartCoroutine(WaitForAutoGameEnd());
            PlayersTryingReconnect.SetActive(true);
        }
    }

    //-> RestartRoom
    public void restartRoom()
    {
        if (PhotonNetwork.IsMasterClient && isEveryoneOnline())
        {
            photonView.RPC("RestartGameRPC", RpcTarget.All);
        }
        else if (PhotonNetwork.IsMasterClient && !isEveryoneOnline())
        {
            endGameAll();
        }
    }

    //-> GameEnded
    public void endGameAll()
    {
        gameActive = false;
        // photonView.RPC("EndGameForAllRPC", RpcTarget.All);
        photonView.RPC("KickEveryone", RpcTarget.All);
    }

    //-> helper functions
    private bool isEveryoneOnline()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.IsInactive)
            {
                return false;
            }
        }
        return true;
    }

    IEnumerator WaitForAutoGameEnd()
    {
        yield return new WaitForSecondsRealtime(waitForReconnectTime + 5f);

        restartRoom();
    }

    [PunRPC]
    public void KickEveryone()
    {
        messagePanel.SetActive(true);
        HandleRoomLeave();
        PhotonNetwork.LeaveRoom(false);
    }

    [PunRPC]
    private void SetPlayerOfflineRPC(string playerName, bool isOffline)
    {
        if (activeProfiles.TryGetValue(playerName, out GameProfileUpdate profile))
        {
            profile.SetOffline(isOffline);
        }
    }

    public void ShowChatIndicator()
    {
        photonView.RPC("SetChatIndicatorRPC", RpcTarget.All, PhotonNetwork.NickName, true);
    }

    public void HideChatIndicator()
    {
        photonView.RPC("SetChatIndicatorRPC", RpcTarget.All, PhotonNetwork.NickName, false);
    }

    [PunRPC]
    private void SetChatIndicatorRPC(string playerName, bool on)
    {
        if (activeProfiles.TryGetValue(playerName, out GameProfileUpdate profile))
        {
            profile.SetChatOffline(on);
        }
    }
    #endregion
    // -----------------------------------------------------------
}