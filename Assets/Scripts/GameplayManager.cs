using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Simple Chat System")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private Button sendChatButton;
    [SerializeField] private TMP_Text[] chatDisplayText;

    [Header("Hint UI")]
    [SerializeField] private TMP_Text hintNameText;
    [SerializeField] private TMP_InputField hintAnswerInput;
    [SerializeField] private Button submitHintAnswerButton;

    [Header("Player UI")]
    [SerializeField] private TMP_InputField playerGuessInput;
    [SerializeField] private Button voteButton;

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
    private bool gameActive = false;


    private int tempPlayerChecks = 0;
    private int tempScore;
    private int hintRoundEach = 0;
    private int voting = 0;
    private int hintRound = 0;
    private int currentRound = 1;
    public int totalRounds = 2;
    private TMP_InputField roundsInputField;

    private List<string> chatMessages = new List<string>();
    private const int MAX_CHAT_LINES = 8;

    // --- Reworked ready-system: separate ready-for-hints
    private Dictionary<string, bool> readyForHints = new Dictionary<string, bool>();
    [SerializeField] private GameObject waitingForPlayersPanel;

    private Dictionary<string, int> playerTotalScores = new Dictionary<string, int>();
    private Dictionary<string, int> currentRoundGuesses = new Dictionary<string, int>();
    private Dictionary<string, GameProfileUpdate> activeProfiles = new Dictionary<string, GameProfileUpdate>();

    public FriendStats friendStatsRound;

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
    }

    void Start()
    {
        photonView = GetComponent<PhotonView>();


        foreach (var b in exampleButton) b.onClick.AddListener(() => ToggleExampleButton(true));
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
        if (PhotonNetwork.IsMasterClient)
        {
            hintRoundEach = totalRounds / Mathf.CeilToInt((float)PhotonNetwork.CurrentRoom.PlayerCount);

            var props = new Hashtable { [EACH_ROUNDS_KEY] = hintRoundEach };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            voting = 0;
            var props2 = new Hashtable { [VOTING] = voting };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props2);
        }

        HintCollection(); // Local for master
        photonView.RPC("HintCollection", RpcTarget.Others);
    }

    [PunRPC]
    private void HintCollection()
    {
        Debug.Log("New Hint Round of " + hintRoundEach);

        if (PhotonNetwork.IsMasterClient)
        {
            hintRoundEach = totalRounds / Mathf.CeilToInt((float)PhotonNetwork.CurrentRoom.PlayerCount);

            var props = new Hashtable { [EACH_ROUNDS_KEY] = hintRoundEach };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        // Reset ready states so match can start after hints
        readyForHints.Clear();
        foreach (Player p in PhotonNetwork.PlayerList)
            readyForHints[p.NickName] = false;


        waitingForPlayersPanel.SetActive(true);
        hintPanel.SetActive(true);
        HintNewCategory();
    }
    private void StartNewRound()
    {
        ResetRoundState();

        realAnswer.SetActive(false);
        currentRoundGuesses.Clear();

        NewCategory();
        ResetUIForNewRound();
        HideAllAnswers();
    }

    private void ProceedToNextPhase()
    {
        currentRound++;

        UpdatePlayerProfiles();

        CalculateRoundScores();

        Debug.Log("Proceeding to next phase..." + currentRound + " ||| " + totalRounds);

        StatsManager.instance.UpdatePlayerStatistics();

        if (currentRound > totalRounds) ShowOverallLeaderboard();
        else StartNewRound();

        StatsManager.instance.ResetAllRounds();
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

    public void HintNewCategory()
    {
        hintCategoryID = UnityEngine.Random.Range(0, categories[0].categories.Length);

        foreach (var t in bad) t.text = categories[0].categories[hintCategoryID].bad;
        foreach (var t in example) t.text = categories[0].categories[hintCategoryID].example;
        foreach (var t in good) t.text = categories[0].categories[hintCategoryID].good;

        tempScore = UnityEngine.Random.Range(0, 10);

        categories[0].categories[hintCategoryID].score = tempScore;

        foreach (var t in scores) t.text = tempScore.ToString();
    }

    public void NewCategory()
    {
        // Only the host picks the category
        if (PhotonNetwork.IsMasterClient)
        {
            // Sort lists first
            hintCatories = hintCatories.OrderBy(c => c.playerID).ToList();
            hintStoredCatories = hintStoredCatories.OrderBy(c => c.playerID).ToList();

            // Pick category AFTER sorting
            int newID = UnityEngine.Random.Range(0, hintCatories.Count);

            // Broadcast to all clients
            photonView.RPC("GameNewCategoryAll", RpcTarget.All, newID);
        }
    }


    [PunRPC]
    private void UpdatePlayerCountAdd()
    {
        tempPlayerChecks++;
    }

    [PunRPC]
    private void GameNewCategoryAll(int ID)
    {
        // Every client sorts the same way
        hintCatories = hintCatories.OrderBy(c => c.playerID).ToList();
        hintStoredCatories = hintStoredCatories.OrderBy(c => c.playerID).ToList();

        // Debug the order
        string ids = string.Join(", ", hintCatories.Select(c => c.playerID));
        Debug.Log("hintCatories sorted order: " + ids);
        Debug.Log("ID: " + ID);

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

        // Reset the counter
        tempPlayerChecks = 0;

        categoryID = ID;

        // Now ID matches the sorted list correctly
        foreach (var t in bad) t.text = hintCatories[ID].bad;
        foreach (var t in example) t.text = hintCatories[ID].example;
        foreach (var t in good) t.text = hintCatories[ID].good;
        foreach (var t in username) t.text = hintCatories[ID].player + "'s hint:";
        foreach (var t in hint) t.text = hintCatories[ID].hint;
        foreach (var t in scores) t.text = hintCatories[ID].score.ToString();

        currentHostAnswer = hintCatories[ID].score;
    }



    public void SubmitHint()
    {
        hintRound++;

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
        Categories newCat = new Categories();
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

        if (!int.TryParse(playerGuessInput.text, out int guessedNumber))
            return;

        playerGuessInput.interactable = false;
        voteButton.interactable = false;

        SendChatMessage(PhotonNetwork.NickName + " voted");

        photonView.RPC("SubmitPlayerGuessRPC", RpcTarget.All, PhotonNetwork.NickName, guessedNumber);
    }

    [PunRPC]
    private void SubmitPlayerGuessRPC(string playerName, int guess)
    {
        RemoveHint();
        Debug.Log("Correct Answer = " + currentHostAnswer);

        if (currentRoundGuesses.ContainsKey(playerName)) return;

        currentRoundGuesses[playerName] = guess;

        // ---------------- StatsManager integration ----------------
        int points = CalculatePoints(guess, currentHostAnswer); // helper function
        int hinterID = hintStoredCatories[categoryID].playerID; // track who gave hint
        StatsManager.instance.SubmitGuess(hinterID, points);
        // ----------------------------------------------------------

        photonView.RPC("SyncPlayerGuessRPC", RpcTarget.Others, playerName, guess);

        int expectedVotes = PhotonNetwork.CurrentRoom.PlayerCount - 1;
        if (currentRoundGuesses.Count >= expectedVotes)
        {
            realAnswer.SetActive(true);
            ShowPlayerAnswers();
            Invoke("ProceedToNextPhase", 3f);
        }
    }
    private int CalculatePoints(int guess, int answer)
    {
        int diff = Mathf.Abs(guess - answer);
        return diff == 0 ? 3 : diff == 1 ? 2 : diff == 2 ? 1 : 0;
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
            int points = diff == 0 ? 3 : diff == 1 ? 2 : diff == 2 ? 1 : 0;

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
        ClearLeaderboard();

        // STEP BY 4 (because each player has 4 values)
        for (int i = 0; i < leaderboardData.Length; i += 4)
        {
            string playerName = (string)leaderboardData[i];
            int score = (int)leaderboardData[i + 1];
            int avatarIndex = (int)leaderboardData[i + 2];
            int totalScoreAllPlayers = (int)leaderboardData[i + 3];

            GameObject entryObj = Instantiate(leaderboardEntryPrefab, leaderboardContainer.transform);
            LeaderboardEntry entry = entryObj.GetComponent<LeaderboardEntry>();

            if (entry != null)
            {
                int rank = (i / 4) + 1;
                entry.SetForOverall(rank, playerName, score, totalScoreAllPlayers, false);

                if (PlayerListUI.instance != null)
                    entry.SetAvatar(PlayerListUI.instance.GetAvatarSprite(avatarIndex));
            }
        }

        // Update the persistent stats
        StatsManager.instance.UpdatePlayerStatistics();
    }


    private object[] PrepareLeaderboardData()
    {
        List<object> data = new List<object>();
        var sortedScores = playerTotalScores.OrderByDescending(p => p.Value).ToList();

        int totalScoreAllPlayers = playerTotalScores.Values.Sum(); // total of all players

        foreach (var playerScore in sortedScores)
        {
            Player player = PhotonNetwork.PlayerList.FirstOrDefault(p => p.NickName == playerScore.Key);

            int avatarIndex = player != null && player.CustomProperties.TryGetValue("avatarIndex", out object idx)
                                ? (int)idx
                                : 0;

            data.Add(playerScore.Key);
            data.Add(playerScore.Value);
            data.Add(avatarIndex);
            data.Add(totalScoreAllPlayers); // <-- Add total score here
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
            //message = "<color=FF3200>msg</color>";
            message = chatInputField.text.Trim();
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
        HandleRoomLeave();
    }

    private void HandleRoomLeave()
    {
        leaderboardPanel.SetActive(false);
        gameplayPanel.SetActive(false);
        playersPanel.SetActive(false);
        hostPanel.SetActive(false);

        gameActive = false;
        currentRound = 1;

        playerTotalScores.Clear();
        currentRoundGuesses.Clear();
        readyForHints.Clear();

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
    }


    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerProfiles();

        if (PhotonNetwork.IsMasterClient && gameActive)
        {
            photonView.RPC("SyncGameStateRPC", newPlayer);
        }
    }

    [PunRPC]
    private void SyncGameStateRPC(object[] data)
    {
        totalRounds = (int)data[0];

        UpdatePlayerProfiles();
        UpdateRoundText();
    }


    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"{otherPlayer.NickName} left. Ending game for everyone.");

        if (PhotonNetwork.NetworkClientState == ClientState.Joined)
        {
            photonView.RPC("EndGameForAllRPC", RpcTarget.All);
        }
    }

    [PunRPC]
    private void EndGameForAllRPC()
    {
        messagePanel.SetActive(true);

        CancelMatch();
    }

    [PunRPC]
    private void RestartRoundAfterPlayerLeaveRPC()
    {
        ResetRoundState();

        currentRoundGuesses.Clear();
        ResetUIForNewRound();
        HideAllAnswers();

        Invoke("DelayedHostSelection", 0.5f);
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
            CancelMatch();
        else
        {
            playersPanel?.SetActive(false);
            gameplayPanel?.SetActive(false);
            leaderboardPanel?.SetActive(false);
            hostPanel?.SetActive(false);
        }
    }

    public void UpdateRounds(TMP_InputField roundsInput)
    {
        roundsInputField = roundsInput;

        string raw = roundsInput.text.Trim(); 

        if (string.IsNullOrWhiteSpace(raw))
        {
            totalRounds = 2;
        }
        else if (int.TryParse(raw, out int rounds))
        {
            totalRounds = Mathf.Clamp(rounds, 2, 20);
        }
        else
        {
            totalRounds = 2;
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
        gameActive = false;
        if (PhotonNetwork.NetworkClientState == ClientState.Joined)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            Debug.Log("Not leaving room because client is not ready. Current state: " + PhotonNetwork.NetworkClientState);
        }
    }

    public void RequestRestartGame()
    {
        if (PhotonNetwork.NetworkClientState == ClientState.Joined)
        {
            PhotonNetwork.LeaveRoom();
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

    [PunRPC]
    private void RestartGameRPC()
    {
        gameActive = true;
        currentRound = 1;
        currentRoundGuesses.Clear();

        leaderboardPanel.SetActive(false);
        gameplayPanel.SetActive(true);

        ResetRoundState();

        chatMessages.Clear();
        UpdateChatDisplay();

        InitializePlayerTracking();
        StartGame();
    }


    public void OpenWebpageIntroVideo()
    {
        UnityEngine.Application.OpenURL("https://drive.google.com/file/d/1bobCLbFbLqbYDcIjoj-Y87e8YZcMeNDb/views");
    }

    #endregion
    // -----------------------------------------------------------
}
