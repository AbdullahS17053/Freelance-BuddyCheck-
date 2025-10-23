using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using System;

public class GameplayManager : MonoBehaviourPunCallbacks
{
    public static GameplayManager instance;

    private PhotonView photonView;

    [Header("UI Panels")]
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private GameObject playersPanel;

    [Header("Simple Chat System")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private Button sendChatButton;
    [SerializeField] private TMP_Text[] chatDisplayText;

    [Header("Host UI")]
    [SerializeField] private TMP_Text hostNameText;
    [SerializeField] private TMP_InputField hostAnswerInput;
    [SerializeField] private Button submitHostAnswerButton;
    [SerializeField] private TMP_Text hostStatusText;

    [Header("Player UI")]
    [SerializeField] private TMP_InputField playerGuessInput;
    [SerializeField] private Button voteButton;
    [SerializeField] private TMP_Text playerStatusText;

    [Header("Game State UI")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text gamePhaseText;

    [Header("Leaderboard UI")]
    [SerializeField] private GameObject leaderboardContainer;
    [SerializeField] private GameObject leaderboardEntryPrefab;
    [SerializeField] private TMP_Text winnerLog;
    [SerializeField] private Button replayButton;

    // Game State
    private string currentHostPlayerName;
    private int currentHostAnswer = -1;
    private bool isLocalHost = false;
    private bool isVotingPhase = false;
    private bool gameActive = false;
    private bool isRestarting = false;

    // Round Tracking
    private int currentRound = 0;
    public int totalRounds = 2;

    // Simple Chat System
    private List<string> chatMessages = new List<string>();
    private const int MAX_CHAT_LINES = 5;

    // Player Data - FIXED: Proper initialization and access
    private Dictionary<string, List<int>> playerRoundGuesses = new Dictionary<string, List<int>>();
    private Dictionary<string, int> playerTotalScores = new Dictionary<string, int>();
    private List<int> hostRoundAnswers = new List<int>();
    private Dictionary<string, int> currentRoundGuesses = new Dictionary<string, int>();

    [Header("Player Profiles")]
    [SerializeField] private GameProfileUpdate[] playerProfiles;

    private Dictionary<string, GameProfileUpdate> activeProfiles = new Dictionary<string, GameProfileUpdate>();
    private List<Player> sortedPlayers = new List<Player>();

    // Synchronization properties
    private const string GAME_PHASE_KEY = "GamePhase";
    private const string CURRENT_HOST_KEY = "CurrentHost";
    private const string CURRENT_ROUND_KEY = "CurrentRound";
    private const string TOTAL_ROUNDS_KEY = "TotalRounds";

    public enum GamePhase
    {
        WaitingForHost,
        Voting,
        ShowingResults,
        GameOver
    }

    private GamePhase currentGamePhase = GamePhase.WaitingForHost;

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
        isLocalHost = false;

        if (!ValidateUIReferences()) return;

        // Button listeners
        voteButton.onClick.AddListener(SubmitVote);
        submitHostAnswerButton.onClick.AddListener(SubmitHostAnswer);
        replayButton.onClick.AddListener(RequestRestartGame);

        // Simple chat system listeners
        sendChatButton.onClick.AddListener(SendChatMessage);
        chatInputField.onSubmit.AddListener(delegate { SendChatMessage(); });

        // Initial UI state
        replayButton.gameObject.SetActive(false);
        ResetUIForNewRound();

        ClearLeaderboard();

        // Initialize chat
        InitializeChat();

        // Initialize all profiles as inactive
        foreach (var profile in playerProfiles)
        {
            profile.gameObject.SetActive(false);
            profile.hideAnswers();
        }

        // Sync with room state if joining mid-game
        if (PhotonNetwork.InRoom)
        {
            SyncWithRoomState();
        }
    }

    private void SyncWithRoomState()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(CURRENT_HOST_KEY, out object hostObj))
        {
            currentHostPlayerName = (string)hostObj;
        }
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(GAME_PHASE_KEY, out object phaseObj))
        {
            currentGamePhase = (GamePhase)phaseObj;
        }
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(CURRENT_ROUND_KEY, out object roundObj))
        {
            currentRound = (int)roundObj;
        }
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(TOTAL_ROUNDS_KEY, out object totalRoundsObj))
        {
            totalRounds = (int)totalRoundsObj;
        }

        UpdateGamePhaseUI();
        UpdatePlayerProfiles();

        if (roundText != null)
            roundText.text = $"Round: {currentRound}/{totalRounds}";
    }

    private void UpdateGamePhaseUI()
    {
        if (gamePhaseText != null)
        {
            gamePhaseText.text = $"Phase: {currentGamePhase}";
        }

        switch (currentGamePhase)
        {
            case GamePhase.WaitingForHost:
                SetChatActive(false);
                break;
            case GamePhase.Voting:
                SetChatActive(true);
                break;
            case GamePhase.ShowingResults:
            case GamePhase.GameOver:
                SetChatActive(false);
                break;
        }
    }

    private void SetGamePhase(GamePhase phase)
    {
        currentGamePhase = phase;

        // Update room properties for synchronization
        if (PhotonNetwork.IsMasterClient)
        {
            var props = new Hashtable
            {
                [GAME_PHASE_KEY] = phase
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        UpdateGamePhaseUI();
    }

    private void InitializeChat()
    {
        chatMessages.Clear();
        UpdateChatDisplay();

        if (chatInputField != null)
            chatInputField.interactable = false;

        if (sendChatButton != null)
            sendChatButton.interactable = false;
    }

    // Simple Chat System Methods
    public void SendChatMessage()
    {
        string message = chatInputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        photonView.RPC("ReceiveChatMessageRPC", RpcTarget.All, PhotonNetwork.NickName, message);
        chatInputField.text = "";
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

        if (!active && chatInputField != null)
        {
            chatInputField.text = "";
            var placeholder = chatInputField.placeholder as TMP_Text;
            if (placeholder != null)
                placeholder.text = "Chat disabled";
        }
        else if (chatInputField != null)
        {
            chatInputField.text = "";
            var placeholder = chatInputField.placeholder as TMP_Text;
            if (placeholder != null)
                placeholder.text = "Type message...";
        }
    }

    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient && !gameActive)
        {
            gameActive = true;
            currentRound = 0;

            // Sync total rounds with all clients
            if (PhotonNetwork.IsMasterClient)
            {
                var props = new Hashtable
                {
                    [TOTAL_ROUNDS_KEY] = totalRounds
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            }

            InitializePlayerTracking();
            StartNewRound();
        }
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
                [CURRENT_ROUND_KEY] = currentRound
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        ResetUIForNewRound();
        HideAllAnswers();
        ChooseRandomHost();
    }

    private void ResetUIForNewRound()
    {
        if (roundText != null)
            roundText.text = $"Round: {currentRound}/{totalRounds}";

        if (hostStatusText != null)
            hostStatusText.text = "Waiting for host answer...";

        if (playerStatusText != null)
            playerStatusText.text = "Waiting for voting...";

        // Reset all player profiles
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
        playerRoundGuesses.Clear();
        playerTotalScores.Clear();
        hostRoundAnswers.Clear();

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            string nick = player.NickName;
            playerRoundGuesses[nick] = new List<int>();
            playerTotalScores[nick] = 0; // FIXED: Initialize scores to 0
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
            var props = new Hashtable
            {
                [CURRENT_HOST_KEY] = hostName
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        UpdateHostUI();
        UpdatePlayerProfiles();
        SetGamePhase(GamePhase.WaitingForHost);
    }

    private void UpdateHostUI()
    {
        // Always reset host panel first
        if (hostPanel != null)
            hostPanel.SetActive(false);

        if (hostNameText != null)
            hostNameText.text = $"{currentHostPlayerName}'s Round";

        if (isLocalHost)
        {
            // Current player is the host
            if (hostPanel != null)
                hostPanel.SetActive(true);

            if (hostStatusText != null)
                hostStatusText.text = "Enter your answer number";

            if (hostAnswerInput != null)
            {
                hostAnswerInput.text = "";
                hostAnswerInput.interactable = true;
            }
        }
        else
        {
            // Current player is not host
            if (playerStatusText != null)
                playerStatusText.text = $"Waiting for {currentHostPlayerName} to submit answer";
        }

        if (gameplayPanel != null)
            gameplayPanel.SetActive(true);
    }

    public void SubmitHostAnswer()
    {
        if (!isLocalHost) return;

        if (!int.TryParse(hostAnswerInput.text, out int answer))
        {
            if (hostStatusText != null)
                hostStatusText.text = "Please enter a valid number!";
            return;
        }

        currentHostAnswer = answer;

        if (hostAnswerInput != null)
            hostAnswerInput.interactable = false;

        if (hostStatusText != null)
            hostStatusText.text = "Answer submitted!";

        photonView.RPC("StartVotingPhaseRPC", RpcTarget.All, currentHostAnswer);
    }

    [PunRPC]
    private void StartVotingPhaseRPC(int hostAnswer)
    {
        currentHostAnswer = hostAnswer;
        isVotingPhase = true;
        SetGamePhase(GamePhase.Voting);

        // Disable host panel for everyone
        if (hostPanel != null)
            hostPanel.SetActive(false);

        // Enable voting for non-host players
        if (!isLocalHost)
        {
            if (playerGuessInput != null)
            {
                playerGuessInput.interactable = true;
                playerGuessInput.text = "";
            }
            if (voteButton != null)
                voteButton.interactable = true;

            if (playerStatusText != null)
                playerStatusText.text = "Enter your guess!";
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

            if (playerStatusText != null)
                playerStatusText.text = "Wait for players to vote";
        }

        SetChatActive(true);
    }

    public void SubmitVote()
    {
        if (isLocalHost) return;

        if (!int.TryParse(playerGuessInput.text, out int guessedNumber))
        {
            if (playerStatusText != null)
                playerStatusText.text = "Please enter a valid number!";
            return;
        }

        if (playerGuessInput != null)
            playerGuessInput.interactable = false;

        if (voteButton != null)
            voteButton.interactable = false;

        if (playerStatusText != null)
            playerStatusText.text = "Vote submitted!";

        SetChatActive(false);

        photonView.RPC("SubmitPlayerGuessRPC", RpcTarget.MasterClient, PhotonNetwork.NickName, guessedNumber);
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
            photonView.RPC("FinalizeRoundRPC", RpcTarget.All, currentHostAnswer);
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
    private void FinalizeRoundRPC(int hostAnswer)
    {
        currentHostAnswer = hostAnswer;
        hostRoundAnswers.Add(hostAnswer);
        isVotingPhase = false;
        SetGamePhase(GamePhase.ShowingResults);

        // Record guesses for scoring
        foreach (var guess in currentRoundGuesses)
        {
            if (playerRoundGuesses.ContainsKey(guess.Key))
            {
                playerRoundGuesses[guess.Key].Add(guess.Value);
            }
        }

        ShowPlayerAnswers();
        CalculateRoundScores();

        if (playerStatusText != null)
            playerStatusText.text = "Round complete!";

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
        // FIXED: Safe dictionary access with TryGetValue
        foreach (var guess in currentRoundGuesses)
        {
            string playerName = guess.Key;
            int playerGuess = guess.Value;

            // FIXED: Use TryGetValue to avoid KeyNotFoundException
            if (!playerTotalScores.TryGetValue(playerName, out int currentScore))
            {
                // Initialize if not present
                currentScore = 0;
                playerTotalScores[playerName] = currentScore;
            }

            int difference = Mathf.Abs(playerGuess - currentHostAnswer);
            int points = 0;

            if (difference == 0) points = 3;
            else if (difference == 1) points = 2;
            else if (difference == 2) points = 1;

            playerTotalScores[playerName] = currentScore + points;
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
                bool isCorrect = difference <= 2; // Within 2 points is "correct"
                profile.Correct(isCorrect);
            }
        }
    }

    private void ShowOverallLeaderboard()
    {
        SetGamePhase(GamePhase.GameOver);

        // FIXED: Use RPC to ensure all clients show leaderboard simultaneously
        if (PhotonNetwork.IsMasterClient)
        {
            var leaderboardData = PrepareLeaderboardData();
            photonView.RPC("ShowLeaderboardRPC", RpcTarget.All, leaderboardData);
        }
    }
    private void ForceUIUpdate()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(leaderboardContainer.transform as RectTransform);
    }

    [PunRPC]
    private void ShowLeaderboardRPC(object[] leaderboardData)
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(true);

        if (gameplayPanel != null)
            gameplayPanel.SetActive(false);

        ClearLeaderboard();

        // Parse leaderboard data
        int allPlayersScore = 0;
        var playerDataList = new List<(string name, int score, bool isHost, int avatarIndex)>();

        for (int i = 0; i < leaderboardData.Length; i += 4)
        {
            string playerName = (string)leaderboardData[i];
            int score = (int)leaderboardData[i + 1];
            bool isHost = (bool)leaderboardData[i + 2];
            int avatarIndex = (int)leaderboardData[i + 3];

            playerDataList.Add((playerName, score, isHost, avatarIndex));
            allPlayersScore += score;
        }

        // Sort by score and display
        var sortedPlayers = playerDataList.OrderByDescending(p => p.score).ToList();

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            var playerData = sortedPlayers[i];

            if (leaderboardContainer != null && leaderboardEntryPrefab != null)
            {
                GameObject entryObj = Instantiate(leaderboardEntryPrefab, leaderboardContainer.transform);
                LeaderboardEntry entry = entryObj.GetComponent<LeaderboardEntry>();

                if (entry != null)
                {
                    entry.SetForOverall(
                        i + 1,
                        playerData.name,
                        playerData.score,
                        allPlayersScore,
                        playerData.isHost
                    );

                    if (PlayerListUI.instance != null)
                        entry.SetAvatar(PlayerListUI.instance.GetAvatarSprite(playerData.avatarIndex));
                }
            }
        }

        if (replayButton != null)
            replayButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);

        ForceUIUpdate();
    }

    private object[] PrepareLeaderboardData()
    {
        var data = new List<object>();

        foreach (var playerScore in playerTotalScores)
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
        if (isRestarting) return;
        isRestarting = true;

        // Reset game state
        currentRound = 0;
        currentRoundGuesses.Clear();
        gameActive = true;
        isRestarting = false;

        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);

        if (gameplayPanel != null)
            gameplayPanel.SetActive(true);

        InitializePlayerTracking();
        StartNewRound();
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
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);

        if (playersPanel != null)
            playersPanel.gameObject.SetActive(false);

        // Reset local state
        gameActive = false;
        isLocalHost = false;
        currentRound = 0;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerProfiles();

        // Sync game state with new player
        if (PhotonNetwork.IsMasterClient && gameActive)
        {
            var gameStateData = new object[]
            {
                currentHostPlayerName,
                currentRound,
                (int)currentGamePhase,
                totalRounds
            };
            photonView.RPC("SyncGameStateRPC", newPlayer, gameStateData);
        }
    }

    [PunRPC]
    private void SyncGameStateRPC(object[] gameStateData)
    {
        currentHostPlayerName = (string)gameStateData[0];
        currentRound = (int)gameStateData[1];
        currentGamePhase = (GamePhase)gameStateData[2];
        totalRounds = (int)gameStateData[3];

        isLocalHost = (PhotonNetwork.NickName == currentHostPlayerName);

        UpdateGamePhaseUI();
        UpdatePlayerProfiles();

        if (roundText != null)
            roundText.text = $"Round: {currentRound}/{totalRounds}";
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        currentRoundGuesses.Remove(otherPlayer.NickName);

        if (PhotonNetwork.CurrentRoom == null) return;

        if (PhotonNetwork.CurrentRoom.PlayerCount <= 1)
        {
            CancelMatch();
        }
        else if (otherPlayer.NickName == currentHostPlayerName && gameActive && PhotonNetwork.IsMasterClient)
        {
            // Host left, choose new host
            ChooseRandomHost();
        }
        else if (PhotonNetwork.IsMasterClient && isVotingPhase)
        {
            // Check if we can finalize round after player left
            int expectedVotes = PhotonNetwork.CurrentRoom.PlayerCount - 1;
            if (currentRoundGuesses.Count >= expectedVotes)
            {
                photonView.RPC("FinalizeRoundRPC", RpcTarget.All, currentHostAnswer);
            }
        }

        UpdatePlayerProfiles();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // Sync room property changes
        if (propertiesThatChanged.ContainsKey(CURRENT_HOST_KEY))
        {
            currentHostPlayerName = (string)propertiesThatChanged[CURRENT_HOST_KEY];
            isLocalHost = (PhotonNetwork.NickName == currentHostPlayerName);
            UpdateHostUI();
        }
        if (propertiesThatChanged.ContainsKey(GAME_PHASE_KEY))
        {
            currentGamePhase = (GamePhase)propertiesThatChanged[GAME_PHASE_KEY];
            UpdateGamePhaseUI();
        }
        if (propertiesThatChanged.ContainsKey(CURRENT_ROUND_KEY))
        {
            currentRound = (int)propertiesThatChanged[CURRENT_ROUND_KEY];
            if (roundText != null)
                roundText.text = $"Round: {currentRound}/{totalRounds}";
        }
        if (propertiesThatChanged.ContainsKey(TOTAL_ROUNDS_KEY))
        {
            totalRounds = (int)propertiesThatChanged[TOTAL_ROUNDS_KEY];
            if (roundText != null)
                roundText.text = $"Round: {currentRound}/{totalRounds}";
        }

        UpdatePlayerProfiles();
    }

    public void CancelMatch()
    {
        gameActive = false;
        isRestarting = true;
        isVotingPhase = false;

        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);

        if (gameplayPanel != null)
            gameplayPanel.SetActive(false);

        if (hostPanel != null)
            hostPanel.SetActive(false);

        if (playersPanel != null)
            playersPanel.SetActive(false);

        PhotonNetwork.LeaveRoom();
    }

    private void UpdatePlayerProfiles()
    {
        activeProfiles.Clear();

        if (PhotonNetwork.PlayerList == null) return;

        sortedPlayers = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToList();

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

    private bool ValidateUIReferences()
    {
        bool valid = true;
        if (hostPanel == null) { Debug.LogError("Host Panel reference missing!"); valid = false; }
        if (gameplayPanel == null) { Debug.LogError("Gameplay Panel reference missing!"); valid = false; }
        if (hostNameText == null) { Debug.LogError("Host Name Text reference missing!"); valid = false; }
        if (hostAnswerInput == null) { Debug.LogError("Host Answer Input reference missing!"); valid = false; }
        if (submitHostAnswerButton == null) { Debug.LogError("Submit Host Answer Button reference missing!"); valid = false; }
        if (playerGuessInput == null) { Debug.LogError("Player Guess Input reference missing!"); valid = false; }
        if (voteButton == null) { Debug.LogError("Vote Button reference missing!"); valid = false; }
        if (leaderboardEntryPrefab == null) { Debug.LogError("Leaderboard Entry Prefab reference missing!"); valid = false; }
        if (winnerLog == null) { Debug.LogError("Winner Log reference missing!"); valid = false; }
        if (replayButton == null) { Debug.LogError("Replay Button reference missing!"); valid = false; }
        if (roundText != null) { } // Just check if it exists, don't error if null

        if (chatInputField == null) { Debug.LogError("Chat Input Field reference missing!"); valid = false; }
        if (sendChatButton == null) { Debug.LogError("Send Chat Button reference missing!"); valid = false; }
        if (chatDisplayText == null) { Debug.LogError("Chat Display Text reference missing!"); valid = false; }

        return valid;
    }
}