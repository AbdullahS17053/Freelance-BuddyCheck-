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

    [Header("UI Panels")]
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private GameObject playersPanel;

    [Header("Simple Chat System")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private Button sendChatButton;
    [SerializeField] private TMP_Text[] chatDisplayText;

    [Header("Host UI")]
    [SerializeField] private TMP_InputField hostAnswerInput;
    [SerializeField] private Button submitHostAnswerButton;

    [Header("Hint UI")]
    [SerializeField] private TMP_Text hintNameText;
    [SerializeField] private TMP_InputField hintAnswerInput;
    [SerializeField] private Button submitHintAnswerButton;

    [Header("Player UI")]
    [SerializeField] private TMP_InputField playerGuessInput;
    [SerializeField] private Button voteButton;

    [Header("Game State UI")]
    [SerializeField] private TMP_Text roundText;

    [Header("Leaderboard UI")]
    [SerializeField] private GameObject leaderboardContainer;
    [SerializeField] private GameObject leaderboardEntryPrefab;
    [SerializeField] private Button replayButton;

    [Header("Gameplay Quiz")]
    [SerializeField] private TextMeshProUGUI[] bad;
    [SerializeField] private TextMeshProUGUI[] good;
    [SerializeField] private TextMeshProUGUI[] example;
    [SerializeField] private TextMeshProUGUI[] hint;
    [SerializeField] private TextMeshProUGUI[] username;
    [SerializeField] private CategoryCatalog[] categories;
    public List<Categories> hintCatories;
    private int hintCategoryID;
    private int categoryID;
    [System.Serializable]
    public class CategoryCatalog
    {
        public Categories[] categories;
    }

    [Header("Player Profiles")]
    [SerializeField] private GameProfileUpdate[] playerProfiles;

    // Game State
    private string currentHostPlayerName;
    private int currentHostAnswer = -1;
    private bool isLocalHost = false;
    private bool gameActive = false;

    // Round Tracking
    private int hintRoundEach = 0;
    private int hintRound = 0;
    private int currentRound = 0;
    public int totalRounds = 2;
    private TMP_InputField roundsInputField; // To track rounds input

    // Chat System
    private List<string> chatMessages = new List<string>();
    private const int MAX_CHAT_LINES = 8;

    // Player Data
    private Dictionary<string, bool> playersReadyForRounds = new Dictionary<string, bool>();
    [SerializeField] private GameObject waitingForPlayersPanel; // assign in inspector

    private Dictionary<string, int> playerTotalScores = new Dictionary<string, int>();
    private Dictionary<string, int> currentRoundGuesses = new Dictionary<string, int>();
    private Dictionary<string, GameProfileUpdate> activeProfiles = new Dictionary<string, GameProfileUpdate>();

    // Synchronization
    private const string CURRENT_HOST_KEY = "CurrentHost";
    private const string CURRENT_ROUND_KEY = "CurrentRound";
    private const string TOTAL_ROUNDS_KEY = "TotalRounds";
    private const string EACH_ROUNDS_KEY = "EachRounds";

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        photonView = GetComponent<PhotonView>();

        // Button listeners
        voteButton.onClick.AddListener(SubmitVote);
        submitHostAnswerButton.onClick.AddListener(SubmitHostAnswer);
        submitHintAnswerButton.onClick.AddListener(SubmitHint);
        replayButton.onClick.AddListener(RequestRestartGame);
        sendChatButton.onClick.AddListener(SendChatMessage);

        // Chat input listener
        chatInputField.onSubmit.AddListener(delegate { SendChatMessage(); });

        // Initial UI state
        replayButton.gameObject.SetActive(false);
        ResetUIForNewRound();
        ClearLeaderboard();
        InitializeChat();

        // Sync with room state if joining mid-game
        if (PhotonNetwork.InRoom)
        {
            SyncWithRoomState();
        }
    }

    // New method to handle leaving the game
    public void LeaveGame()
    {
        if (PhotonNetwork.InRoom)
        {
            CancelMatch();
        }
        else
        {
            // If not in a room, just go back to menu
            if (playersPanel != null) playersPanel.SetActive(false);
            if (gameplayPanel != null) gameplayPanel.SetActive(false);
            if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
            if (hostPanel != null) hostPanel.SetActive(false);
        }
    }

    // Method to update rounds from UI input
    public void UpdateRounds(TMP_InputField roundsInput)
    {
        roundsInputField = roundsInput;

        if (int.TryParse(roundsInput.text, out int rounds))
        {
            totalRounds = rounds; // Limit between 1-20 rounds

            // Sync with all clients if master
            if (PhotonNetwork.IsMasterClient)
            {
                var props = new Hashtable { [TOTAL_ROUNDS_KEY] = totalRounds };
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            }

            roundsInput.text = string.Empty;

            UpdateRoundText();
        }
    }

    private void InitializeChat()
    {
        chatMessages.Clear();
        UpdateChatDisplay();
        SetChatActive(false);
    }

    /// <summary>
    /// Question Display Categories
    /// </summary>
    /// 

    public void HintNewCategory()
    {
        hintCategoryID = UnityEngine.Random.Range(0, categories[0].categories.Length);

        foreach (TextMeshProUGUI text in bad)
        {
            text.text = categories[0].categories[hintCategoryID].bad;
        }
        foreach (TextMeshProUGUI text in example)
        {
            text.text = categories[0].categories[hintCategoryID].example;
        }
        foreach (TextMeshProUGUI text in good)
        {
            text.text = categories[0].categories[hintCategoryID].good;
        }

        // photonView.RPC("HintNewCategoryAll", RpcTarget.All, hintCategoryID);
    }
    public void NewCategory()
    {
        categoryID = UnityEngine.Random.Range(0, hintCatories.Count);

        photonView.RPC("GameNewCategoryAll", RpcTarget.All, categoryID);
    }

    [PunRPC]
    private void HintNewCategoryAll(int ID)
    {
        hintCategoryID = ID;

        foreach(TextMeshProUGUI text in bad)
        {
            text.text = hintCatories[ID].bad;
        }
        foreach(TextMeshProUGUI text in example)
        {
            text.text = hintCatories[ID].example;
        }
        foreach(TextMeshProUGUI text in good)
        {
            text.text = hintCatories[ID].good;
        }
    }
    [PunRPC]
    private void GameNewCategoryAll(int ID)
    {
        categoryID = ID;

        foreach (TextMeshProUGUI text in bad)
        {
            text.text = hintCatories[ID].bad;
        }
        foreach (TextMeshProUGUI text in example)
        {
            text.text = hintCatories[ID].example;
        }
        foreach (TextMeshProUGUI text in good)
        {
            text.text = hintCatories[ID].good;
        }
        foreach (TextMeshProUGUI text in username)
        {
            text.text = hintCatories[ID].player;
        }
        foreach (TextMeshProUGUI text in hint)
        {
            text.text = hintCatories[ID].hint;
        }
    }
    public void SubmitHint()
    {
        hintRound++;

        // Add hint to all clients
        photonView.RPC("AddHintList", RpcTarget.All,
            hintCategoryID,
            categories[0].categories[hintCategoryID].hint,
            categories[0].categories[hintCategoryID].player);

        // Mark this player as ready
        photonView.RPC("PlayerReadyForRoundsRPC", RpcTarget.MasterClient, PhotonNetwork.NickName);

        hintAnswerInput.text = "";

        HintNewCategory();

        if (hintRound >= hintRoundEach)
        {
            hintRound = 0;
            hintPanel.SetActive(false);
        }
    }

    #region Readiness
    [PunRPC]
    private void PlayerReadyForRoundsRPC(string playerName)
    {
        if (!playersReadyForRounds.ContainsKey(playerName))
            playersReadyForRounds[playerName] = true;

        CheckAllPlayersReady();
    }
    private void CheckAllPlayersReady()
    {
        // Everyone in the room?
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!playersReadyForRounds.ContainsKey(player.NickName) || !playersReadyForRounds[player.NickName])
            {
                // Still waiting for some players
                waitingForPlayersPanel.SetActive(true);
                return;
            }
        }

        // All players ready, hide waiting panel
        waitingForPlayersPanel.SetActive(false);

        // Start the rounds if master client
        if (PhotonNetwork.IsMasterClient)
        {
            gameActive = true;
            currentRound = 0;
            InitializePlayerTracking();
            StartNewRound();

            // Reset ready tracker for next hint phase
            playersReadyForRounds.Clear();
        }
    }


    #endregion

    public void RemoveHint()
    {
        photonView.RPC("RemoveHintList", RpcTarget.All, categoryID);
    }
    [PunRPC]
    private void AddHintList(int ID, string hint, string player)
    {
        Categories newCat = new Categories();
        newCat.bad = categories[0].categories[ID].bad;
        newCat.good = categories[0].categories[ID].good;
        newCat.example = categories[0].categories[ID].example;

        newCat.hint = hint;
        newCat.player = player;

        hintCatories.Add(newCat);
    }

    [PunRPC]
    private void RemoveHintList(int ID)
    {
        categoryID = ID;

        hintCatories.RemoveAt(ID);
    }

    #region Chat System

    // Simple Chat System Methods
    public void SendChatMessage()
    {
        string message = chatInputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        photonView.RPC("ReceiveChatMessageRPC", RpcTarget.All, PhotonNetwork.NickName, message);
        chatInputField.text = "";
        chatInputField.ActivateInputField();
    }

    [PunRPC]
    private void ReceiveChatMessageRPC(string sender, string message)
    {
        AddChatMessage(sender, message);

        // Update chat indicator on player profile
        if (activeProfiles.TryGetValue(sender, out GameProfileUpdate profile))
        {
            profile.SetChatActivity(true, message);
        }
    }

    private void AddChatMessage(string sender, string message)
    {
        string formattedMessage = $"{sender}: {message}";
        chatMessages.Add(formattedMessage);

        // Keep only last 8 messages
        while (chatMessages.Count > MAX_CHAT_LINES)
        {
            chatMessages.RemoveAt(0);
        }

        UpdateChatDisplay();
    }

    private void UpdateChatDisplay()
    {
        if (chatDisplayText != null)
        {
            for (int i = 0; i < chatDisplayText.Length; i++)
            {
                chatDisplayText[i].text = string.Join("\n", chatMessages);
            }
        }
    }

    public void SetChatActive(bool active)
    {
        if (chatInputField != null)
            chatInputField.interactable = active;

        if (sendChatButton != null)
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

    private void SyncWithRoomState()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(CURRENT_HOST_KEY, out object hostObj))
        {
            currentHostPlayerName = (string)hostObj;
            isLocalHost = (PhotonNetwork.NickName == currentHostPlayerName);
        }
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(CURRENT_ROUND_KEY, out object roundObj))
        {
            currentRound = (int)roundObj;
        }
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(TOTAL_ROUNDS_KEY, out object totalRoundsObj))
        {
            totalRounds = (int)totalRoundsObj;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(EACH_ROUNDS_KEY, out object eachRoundsObj))
        {
            hintRoundEach = (int)eachRoundsObj;
        }

        UpdatePlayerProfiles();
        UpdateRoundText();
    }

    private void UpdateRoundText()
    {
        if (roundText != null)
            roundText.text = $"Round: {currentRound}/{totalRounds}";
    }

    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            hintRoundEach = Mathf.CeilToInt((float)PhotonNetwork.CurrentRoom.PlayerCount / totalRounds);
            var props = new Hashtable { [EACH_ROUNDS_KEY] = hintRoundEach };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        photonView.RPC("HintCollection", RpcTarget.All);
        // Hint System Added
        //StartNewRound();
    }

    [PunRPC]
    private void HintCollection()
    {
        hintPanel.SetActive(true);

        HintNewCategory();
    }

    private void StartNewRound()
    {
        currentRound++;
        currentRoundGuesses.Clear();

        // Update room properties
        if (PhotonNetwork.IsMasterClient)
        {
            var props = new Hashtable
            {
                [CURRENT_ROUND_KEY] = currentRound,
                [TOTAL_ROUNDS_KEY] = totalRounds
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        NewCategory();
        ResetUIForNewRound();
        HideAllAnswers();
        ChooseRandomHost();
    }

    private void ResetUIForNewRound()
    {
        UpdateRoundText();

        if (hostAnswerInput != null)
            hostAnswerInput.text = "";

        if (playerGuessInput != null)
            playerGuessInput.text = "";

        // Enable chat during voting phase
        SetChatActive(false);

        // Reset all player profiles and ensure particles are stopped
        foreach (var profile in playerProfiles)
        {
            if (profile.gameObject.activeSelf)
            {
                profile.hideAnswers();
                profile.SetChatActivity(false);
            }
        }
    }

    private void InitializePlayerTracking()
    {
        playerTotalScores.Clear();
        currentRoundGuesses.Clear();

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            playerTotalScores[player.NickName] = 0;
        }
    }

    private void ChooseRandomHost()
    {
        List<Player> players = PhotonNetwork.PlayerList.ToList();
        if (players.Count == 0) return;

        Player selectedHost = players[UnityEngine.Random.Range(0, players.Count)];
        photonView.RPC("SetHostRPC", RpcTarget.All, selectedHost.NickName);
    }

    [PunRPC]
    private void SetHostRPC(string hostName)
    {
        currentHostPlayerName = hostName;
        isLocalHost = (PhotonNetwork.NickName == hostName);

        // Update room properties
        if (PhotonNetwork.IsMasterClient)
        {
            var props = new Hashtable { [CURRENT_HOST_KEY] = hostName };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        UpdateHostUI();
        UpdatePlayerProfiles();
    }

    private void UpdateHostUI()
    {
        // Always reset host panel first
        if (hostPanel != null)
            hostPanel.SetActive(false);

        if (isLocalHost)
        {
            // Current player is the host
            if (hostPanel != null)
                hostPanel.SetActive(true);

            if (hostAnswerInput != null)
            {
                hostAnswerInput.interactable = true;
            }
        }

        if (gameplayPanel != null)
            gameplayPanel.SetActive(true);
    }

    public void SubmitHostAnswer()
    {
        if (!isLocalHost) return;

        if (!int.TryParse(hostAnswerInput.text, out int answer))
            return;

        currentHostAnswer = answer;

        if (hostAnswerInput != null)
            hostAnswerInput.interactable = false;

        photonView.RPC("StartVotingPhaseRPC", RpcTarget.All, currentHostAnswer);

    }

    [PunRPC]
    private void StartVotingPhaseRPC(int hostAnswer)
    {
        currentHostAnswer = hostAnswer;

        // Disable host panel for everyone
        if (hostPanel != null)
            hostPanel.SetActive(false);

        // Enable voting for non-host players
        if (!isLocalHost)
        {
            if (playerGuessInput != null)
                playerGuessInput.interactable = true;
            if (voteButton != null)
                voteButton.interactable = true;
        }
        else
        {
            // Host cannot vote
            if (playerGuessInput != null)
            {
                playerGuessInput.interactable = false;
                playerGuessInput.text = "Host cannot vote";
            }
            if (voteButton != null)
                voteButton.interactable = false;
        }

        // Enable chat during voting phase
        SetChatActive(true);
    }

    public void SubmitVote()
    {
        if (isLocalHost) return;

        if (!int.TryParse(playerGuessInput.text, out int guessedNumber))
            return;

        if (playerGuessInput != null)
            playerGuessInput.interactable = false;

        if (voteButton != null)
            voteButton.interactable = false;

        photonView.RPC("SubmitPlayerGuessRPC", RpcTarget.MasterClient, PhotonNetwork.NickName, guessedNumber);
        RemoveHint();
    }

    [PunRPC]
    private void SubmitPlayerGuessRPC(string playerName, int guess)
    {
        if (currentRoundGuesses.ContainsKey(playerName)) return;

        currentRoundGuesses[playerName] = guess;
        photonView.RPC("SyncPlayerGuessRPC", RpcTarget.Others, playerName, guess);

        // Check if all players have voted (excluding host)
        int expectedVotes = PhotonNetwork.CurrentRoom.PlayerCount - 1;
        if (currentRoundGuesses.Count >= expectedVotes)
        {
            photonView.RPC("FinalizeRoundRPC", RpcTarget.All);
        }
    }

    [PunRPC]
    private void SyncPlayerGuessRPC(string playerName, int guess)
    {
        if (!currentRoundGuesses.ContainsKey(playerName))
        {
            currentRoundGuesses[playerName] = guess;
        }
    }

    [PunRPC]
    private void FinalizeRoundRPC()
    {
        // Disable chat when round ends
        SetChatActive(false);

        CalculateRoundScores();
        ShowPlayerAnswers();

        Invoke("ProceedToNextPhase", 3f);
    }

    private void ProceedToNextPhase()
    {
        if (currentRound >= totalRounds)
        {
            ShowOverallLeaderboard();
        }
        else
        {
            StartNewRound();
        }
    }

    private void CalculateRoundScores()
    {
        foreach (var guess in currentRoundGuesses)
        {
            string playerName = guess.Key;
            int playerGuess = guess.Value;

            // Safe dictionary access
            if (!playerTotalScores.ContainsKey(playerName))
                playerTotalScores[playerName] = 0;

            int difference = Mathf.Abs(playerGuess - currentHostAnswer);
            int points = 0;

            if (difference == 0) points = 3;
            else if (difference == 1) points = 2;
            else if (difference == 2) points = 1;

            playerTotalScores[playerName] += points;
        }
    }

    private void ShowPlayerAnswers()
    {
        // Show host answer
        if (activeProfiles.TryGetValue(currentHostPlayerName, out GameProfileUpdate hostProfile))
        {
            hostProfile.numberGuessed(currentHostAnswer.ToString());
            hostProfile.showAnswers();
        }

        // Show player answers with scoring
        foreach (var guess in currentRoundGuesses)
        {
            if (activeProfiles.TryGetValue(guess.Key, out GameProfileUpdate profile))
            {
                profile.numberGuessed(guess.Value.ToString());
                profile.showAnswers();

                int difference = Mathf.Abs(guess.Value - currentHostAnswer);
                bool isCorrect = difference <= 2;
                profile.Correct(isCorrect);
            }
        }
    }

    private void ShowOverallLeaderboard()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            var leaderboardData = PrepareLeaderboardData();
            photonView.RPC("ShowLeaderboardRPC", RpcTarget.All, leaderboardData);
        }
    }

    [PunRPC]
    private void ShowLeaderboardRPC(object[] leaderboardData)
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(true);

        if (gameplayPanel != null)
            gameplayPanel.SetActive(false);

        ClearLeaderboard();

        // Parse and display leaderboard data
        for (int i = 0; i < leaderboardData.Length; i += 4)
        {
            string playerName = (string)leaderboardData[i];
            int score = (int)leaderboardData[i + 1];
            bool isHost = (bool)leaderboardData[i + 2];
            int avatarIndex = (int)leaderboardData[i + 3];

            if (leaderboardContainer != null && leaderboardEntryPrefab != null)
            {
                GameObject entryObj = Instantiate(leaderboardEntryPrefab, leaderboardContainer.transform);
                LeaderboardEntry entry = entryObj.GetComponent<LeaderboardEntry>();

                if (entry != null)
                {
                    entry.SetForOverall(i + 1, playerName, score, 0, isHost);
                    if (PlayerListUI.instance != null)
                        entry.SetAvatar(PlayerListUI.instance.GetAvatarSprite(avatarIndex));
                }
            }
        }

        if (replayButton != null)
            replayButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);

    }

    private object[] PrepareLeaderboardData()
    {
        var data = new List<object>();
        var sortedScores = playerTotalScores.OrderByDescending(p => p.Value).ToList();

        foreach (var playerScore in sortedScores)
        {
            Player player = PhotonNetwork.PlayerList.FirstOrDefault(p => p.NickName == playerScore.Key);
            int avatarIndex = 0;
            bool isHost = (playerScore.Key == currentHostPlayerName);

            if (player != null && player.CustomProperties.TryGetValue("avatarIndex", out object indexObj))
            {
                avatarIndex = (int)indexObj;
            }

            data.Add(playerScore.Key);
            data.Add(playerScore.Value);
            data.Add(isHost);
            data.Add(avatarIndex);
        }

        return data.ToArray();
    }

    private void ClearLeaderboard()
    {
        if (leaderboardContainer != null)
        {
            foreach (Transform child in leaderboardContainer.transform)
                Destroy(child.gameObject);
        }
    }

    public void RequestRestartGame()
    {
        if (PhotonNetwork.IsMasterClient)
            photonView.RPC("RestartGameRPC", RpcTarget.All);
    }

    [PunRPC]
    private void RestartGameRPC()
    {
        gameActive = true;
        currentRound = 0;
        currentRoundGuesses.Clear();

        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);

        if (gameplayPanel != null)
            gameplayPanel.SetActive(true);

        // Clear chat for new game
        chatMessages.Clear();
        UpdateChatDisplay();

        InitializePlayerTracking();
        StartGame();
    }

    public override void OnJoinedRoom()
    {
        if (playersPanel != null)
            playersPanel.gameObject.SetActive(true);

        UpdatePlayerProfiles();
        SyncWithRoomState();
    }

    public override void OnLeftRoom()
    {
        HandleRoomLeave();
    }

    private void HandleRoomLeave()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);

        if (gameplayPanel != null)
            gameplayPanel.SetActive(false);

        if (playersPanel != null)
            playersPanel.gameObject.SetActive(false);

        if (hostPanel != null)
            hostPanel.SetActive(false);

        gameActive = false;
        isLocalHost = false;
        currentRound = 0;

        // Clear all data
        playerTotalScores.Clear();
        currentRoundGuesses.Clear();
        chatMessages.Clear();
        UpdateChatDisplay();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerProfiles();

        // Sync game state with new player
        if (PhotonNetwork.IsMasterClient && gameActive)
        {
            var gameStateData = new object[] { currentHostPlayerName, currentRound, totalRounds };
            photonView.RPC("SyncGameStateRPC", newPlayer, gameStateData);
        }
    }

    [PunRPC]
    private void SyncGameStateRPC(object[] gameStateData)
    {
        currentHostPlayerName = (string)gameStateData[0];
        currentRound = (int)gameStateData[1];
        totalRounds = (int)gameStateData[2];
        isLocalHost = (PhotonNetwork.NickName == currentHostPlayerName);

        UpdatePlayerProfiles();
        UpdateRoundText();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // Remove player from all tracking
        currentRoundGuesses.Remove(otherPlayer.NickName);
        playerTotalScores.Remove(otherPlayer.NickName);

        if (PhotonNetwork.CurrentRoom == null) return;

        UpdatePlayerProfiles();

        // If we're the last player, leave the room automatically
        if (PhotonNetwork.CurrentRoom.PlayerCount <= 1)
        {
            // Last player standing - auto leave
            CancelMatch();
            return;
        }

        // If game is active and there are still players, restart the round
        if (gameActive && PhotonNetwork.IsMasterClient)
        {
            // Don't increment round count - just restart the current round
            photonView.RPC("RestartRoundAfterPlayerLeaveRPC", RpcTarget.All);
        }
    }

    [PunRPC]
    private void RestartRoundAfterPlayerLeaveRPC()
    {
        // Reset current round without incrementing round count
        currentRoundGuesses.Clear();
        ResetUIForNewRound();
        HideAllAnswers();

        // Add a small delay before choosing new host to ensure smooth transition
        Invoke("DelayedHostSelection", 0.5f);
    }

    private void DelayedHostSelection()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            ChooseRandomHost();
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(CURRENT_HOST_KEY))
        {
            currentHostPlayerName = (string)propertiesThatChanged[CURRENT_HOST_KEY];
            isLocalHost = (PhotonNetwork.NickName == currentHostPlayerName);
            UpdateHostUI();
        }
        if (propertiesThatChanged.ContainsKey(CURRENT_ROUND_KEY))
        {
            currentRound = (int)propertiesThatChanged[CURRENT_ROUND_KEY];
            UpdateRoundText();
        }
        if (propertiesThatChanged.ContainsKey(TOTAL_ROUNDS_KEY))
        {
            totalRounds = (int)propertiesThatChanged[TOTAL_ROUNDS_KEY];
            UpdateRoundText();
        }
        if (propertiesThatChanged.ContainsKey(EACH_ROUNDS_KEY))
        {
            hintRoundEach = (int)propertiesThatChanged[EACH_ROUNDS_KEY];
            Debug.Log("Synced hintRoundEach = " + hintRoundEach);
        }

        UpdatePlayerProfiles();
    }

    public void CancelMatch()
    {
        gameActive = false;
        PhotonNetwork.LeaveRoom();
    }

    private void UpdatePlayerProfiles()
    {
        activeProfiles.Clear();

        if (PhotonNetwork.PlayerList == null) return;

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
        if (activeProfiles.TryGetValue(player.NickName, out GameProfileUpdate profile))
        {
            int avatarIndex = 0;
            if (player.CustomProperties.TryGetValue("avatarIndex", out object indexObj))
                avatarIndex = (int)indexObj;

            bool isHost = (player.NickName == currentHostPlayerName);
            profile.updatePlayer(avatarIndex, player.NickName, isHost);
        }
    }

    private void HideAllAnswers()
    {
        foreach (var profile in playerProfiles)
        {
            if (profile.gameObject.activeSelf)
            {
                profile.hideAnswers();
            }
        }
    }

    public void OpenWebpageIntroVideo()
    {
        UnityEngine.Application.OpenURL("https://drive.google.com/file/d/1bobCLbFbLqbYDcIjoj-Y87e8YZcMeNDb/views");
    }
}