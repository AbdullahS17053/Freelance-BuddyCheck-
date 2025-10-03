using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using System;
using Random = UnityEngine.Random;

// Add this at the top of the class
[System.Serializable]
public class PlayerScoreData
{
    public string playerName;
    public int score;
    public bool isHost;
    public int avatarIndex;
}

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
    [SerializeField] private TMP_Text chatDisplayText; // Single text area for all chat
    [SerializeField] private ScrollRect chatScrollRect;

    [Header("Host UI")]
    [SerializeField] private TMP_Text hostNameText;
    [SerializeField] private TMP_Text answerText;
    [SerializeField] private TMP_Text hostHintText;
    [SerializeField] private TMP_InputField hostHintInput;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TMP_Text roundText;

    [Header("Player UI")]
    [SerializeField] private TMP_InputField playerGuessInput;
    [SerializeField] private Button voteButton;

    [Header("Leaderboard UI")]
    [SerializeField] private GameObject leaderboardContainer;
    [SerializeField] private GameObject leaderboardEntryPrefab;
    [SerializeField] private TMP_Text winnerLog;
    [SerializeField] private Button replayButton;

    // Game State
    private string hostPlayerName;
    private int hostNumber = -1;
    private string hostHint;
    private bool isLocalHost = false;
    private bool isVotingPhase = false;
    private bool gameActive = false;
    private bool isRestarting = false;

    // Round Tracking
    private int currentRound = 1;
    public int totalRounds = 2;
    private TMP_InputField roundField;

    // Simple Chat System
    private List<string> chatMessages = new List<string>();
    private const int MAX_CHAT_LINES = 5;

    // Player Data
    private Dictionary<string, List<int>> playerRoundGuesses = new Dictionary<string, List<int>>();
    private Dictionary<string, int> playerTotalScores = new Dictionary<string, int>();
    private List<int> hostRoundNumbers = new List<int>();
    private List<string> hostRoundHints = new List<string>();
    private Dictionary<string, int> playerGuesses = new Dictionary<string, int>();

    [Header("Player Profiles")]
    [SerializeField] private GameProfileUpdate[] playerProfiles;
    [SerializeField] private Transform profilesContainer;

    private Dictionary<string, GameProfileUpdate> activeProfiles = new Dictionary<string, GameProfileUpdate>();
    private List<Player> sortedPlayers = new List<Player>();

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
        startGameButton.onClick.AddListener(SubmitHostHint);
        replayButton.onClick.AddListener(RequestRestartGame);

        // Simple chat system listeners
        sendChatButton.onClick.AddListener(SendChatMessage);
        chatInputField.onSubmit.AddListener(delegate { SendChatMessage(); });

        // Initial UI state
        replayButton.gameObject.SetActive(false);
        voteButton.interactable = true;
        playerGuessInput.interactable = true;

        ClearLeaderboard();
        roundText.text = $"Round: {currentRound}/{totalRounds}";

        // Initialize chat
        InitializeChat();

        // Initialize all profiles as inactive
        foreach (var profile in playerProfiles)
        {
            profile.gameObject.SetActive(false);
            profile.hideAnswers();
        }
    }

    private void InitializeChat()
    {
        chatMessages.Clear();
        UpdateChatDisplay();

        if (chatInputField != null)
            chatInputField.interactable = false; // Start disabled until voting phase

        if (sendChatButton != null)
            sendChatButton.interactable = false;
    }

    // Simple Chat System Methods
    public void SendChatMessage()
    {
        string message = chatInputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        // Send via RPC to all players
        photonView.RPC("ReceiveChatMessageRPC", RpcTarget.All, PhotonNetwork.NickName, message);
        chatInputField.text = "";
        chatInputField.ActivateInputField(); // Keep focus on input
    }

    [PunRPC]
    private void ReceiveChatMessageRPC(string sender, string message)
    {
        AddChatMessage(sender, message);
    }

    private void AddChatMessage(string sender, string message)
    {
        // Format: "name: message"
        string formattedMessage = $"{sender}: {message}";
        chatMessages.Add(formattedMessage);

        // Keep only last 5 messages
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
            // Join all messages with newlines
            chatDisplayText.text = string.Join("\n", chatMessages);
        }

        // Scroll to bottom
        if (chatScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // Enable/disable chat based on game phase
    public void SetChatActive(bool active)
    {
        if (chatInputField != null)
            chatInputField.interactable = active && isVotingPhase;

        if (sendChatButton != null)
            sendChatButton.interactable = active && isVotingPhase;

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

    public void updateRounds(TMP_InputField num)
    {
        roundField = num;

        int numm;
        if (!int.TryParse(num.text, out numm))
        {
            numm = 2;
        }

        if (numm > 20)
        {
            numm = 20;
        }

        totalRounds = numm;

        // Sync the rounds value with all clients
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("SyncTotalRounds", RpcTarget.All, totalRounds);
        }
    }

    [PunRPC]
    private void SyncTotalRounds(int rounds)
    {
        totalRounds = rounds;
        if (roundText != null)
            roundText.text = $"Round: {currentRound}/{totalRounds}";
    }

    public bool CheckHostInputs()
    {
        if (roundField == null || LocalPlayer.Instance == null || LocalPlayer.Instance.nameField == null)
            return false;

        if (string.IsNullOrEmpty(roundField.text) || string.IsNullOrEmpty(LocalPlayer.Instance.nameField.text))
        {
            return false;
        }
        else
        {
            roundField.text = string.Empty;
            LocalPlayer.Instance.nameField.text = string.Empty;
            return true;
        }
    }

    public bool CheckClientInputs()
    {
        if (LocalPlayer.Instance == null || LocalPlayer.Instance.nameField == null)
            return false;

        if (string.IsNullOrEmpty(LocalPlayer.Instance.nameField.text))
        {
            return false;
        }
        else
        {
            LocalPlayer.Instance.nameField.text = string.Empty;
            return true;
        }
    }

    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient && !gameActive)
        {
            gameActive = true;
            InitializePlayerTracking();
            StartRound();
        }
    }

    private void StartRound()
    {
        playerGuesses.Clear();
        ResetRoundState();
        HideAllAnswers();
        ChooseRandomHost();
    }

    private void InitializePlayerTracking()
    {
        playerRoundGuesses.Clear();
        playerTotalScores.Clear();
        hostRoundNumbers.Clear();
        hostRoundHints.Clear();

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            string nick = player.NickName;
            playerRoundGuesses[nick] = new List<int>();
            playerTotalScores[nick] = 0;
        }
    }

    private void ChooseRandomHost()
    {
        List<Player> players = PhotonNetwork.PlayerList.ToList();
        if (players.Count == 0) return;

        Player selectedHost = players[Random.Range(0, players.Count)];

        // Only master client sets the host number
        if (PhotonNetwork.IsMasterClient)
        {
            int newHostNumber = Random.Range(0, 11);
            photonView.RPC("SetHost", RpcTarget.All, selectedHost.NickName, newHostNumber);
        }
        else
        {
            photonView.RPC("SetHost", RpcTarget.All, selectedHost.NickName, -1);
        }
    }

    [PunRPC]
    private void SetHost(string hostName, int hostNum)
    {
        // Always reset host panel first
        if (hostPanel != null)
            hostPanel.SetActive(false);

        isLocalHost = false;

        hostPlayerName = hostName;
        if (hostNameText != null)
            hostNameText.text = $"{hostName}'s guess";

        // Only set number if we're the new host
        if (PhotonNetwork.NickName == hostName)
        {
            isLocalHost = true;

            // Use number from master if available, otherwise generate locally
            hostNumber = hostNum > -1 ? hostNum : Random.Range(0, 11);
            if (answerText != null)
                answerText.text = hostNumber.ToString();

            if (hostPanel != null)
                hostPanel.SetActive(true);
        }

        if (gameplayPanel != null)
            gameplayPanel.SetActive(true);

        // Update round text with current values
        if (roundText != null)
            roundText.text = $"Round: {currentRound}/{totalRounds}";

        UpdatePlayerProfiles();
    }

    public void SubmitHostHint()
    {
        if (string.IsNullOrWhiteSpace(hostHintInput.text))
        {
            Debug.LogError("Hint cannot be empty.");
            return;
        }

        hostHint = hostHintInput.text;

        // Only host should broadcast the hint
        if (isLocalHost)
        {
            photonView.RPC("BroadcastHostHint", RpcTarget.All, hostHint, hostNumber);
        }
    }

    [PunRPC]
    private void BroadcastHostHint(string hint, int number)
    {
        hostHint = hint;
        hostNumber = number;

        if (hostHintText != null)
            hostHintText.text = hostHint;

        // Deactivate host panel for everyone
        if (hostPanel != null)
            hostPanel.SetActive(false);

        // Enable chat for voting phase
        SetChatActive(true);

        // Disable voting for host only
        if (PhotonNetwork.NickName == hostPlayerName)
        {
            if (playerGuessInput != null)
            {
                playerGuessInput.interactable = false;
                playerGuessInput.text = "Host cannot vote";
            }
            if (voteButton != null)
                voteButton.interactable = false;

            SetChatActive(false); // Host can't chat during voting
        }

        isVotingPhase = true;
    }

    public void SubmitVote()
    {
        if (!int.TryParse(playerGuessInput.text, out int guessedNumber) || guessedNumber < 0 || guessedNumber > 10)
        {
            Debug.LogError("Invalid guess. Must be 0-10.");
            return;
        }

        if (playerGuessInput != null)
            playerGuessInput.interactable = false;

        if (voteButton != null)
            voteButton.interactable = false;

        SetChatActive(false); // Disable chat after voting

        photonView.RPC("SubmitPlayerGuess", RpcTarget.MasterClient, PhotonNetwork.NickName, guessedNumber);
    }

    [PunRPC]
    private void SubmitPlayerGuess(string playerName, int guess)
    {
        if (playerGuesses.ContainsKey(playerName)) return;

        playerGuesses[playerName] = guess;
        photonView.RPC("SyncPlayerGuess", RpcTarget.Others, playerName, guess);

        if (PhotonNetwork.IsMasterClient &&
            playerGuesses.Count >= PhotonNetwork.CurrentRoom.PlayerCount - 1)
        {
            photonView.RPC("FinalizeGuesses", RpcTarget.All);
        }
    }

    [PunRPC]
    private void SyncPlayerGuess(string playerName, int guess)
    {
        if (!playerGuesses.ContainsKey(playerName))
        {
            playerGuesses[playerName] = guess;
        }
    }

    [PunRPC]
    private void FinalizeGuesses()
    {
        hostRoundNumbers.Add(hostNumber);
        hostRoundHints.Add(hostHint);

        foreach (var guess in playerGuesses)
        {
            if (playerRoundGuesses.ContainsKey(guess.Key))
            {
                playerRoundGuesses[guess.Key].Add(guess.Value);
            }
        }

        // Show guesses on player profiles
        ShowPlayerAnswers();

        Invoke("NextRound", 5f);
    }

    private void NextRound()
    {
        if (currentRound >= totalRounds)
        {
            CalculateTotalScores();

            // Only master sends leaderboard data
            if (PhotonNetwork.IsMasterClient)
            {
                // Prepare data to send to all clients
                var scoreData = PrepareScoreData();
                photonView.RPC("ShowOverallLeaderboardRPC", RpcTarget.All, scoreData);
            }
        }
        else
        {
            currentRound++;

            // Update round text
            if (roundText != null)
                roundText.text = $"Round: {currentRound}/{totalRounds}";

            StartRound();
        }
    }

    private object[] PrepareScoreData()
    {
        var sortedPlayers = playerTotalScores
            .OrderByDescending(p => p.Value)
            .ToList();

        List<object> data = new List<object>();

        foreach (var playerScore in sortedPlayers)
        {
            Player player = PhotonNetwork.PlayerList.FirstOrDefault(p => p.NickName == playerScore.Key);
            int avatarIndex = 0;

            if (player != null && player.CustomProperties.TryGetValue("avatarIndex", out object indexObj))
            {
                avatarIndex = (int)indexObj;
            }

            // Add data in fixed order: name, score, isHost, avatarIndex
            data.Add(playerScore.Key);
            data.Add(playerScore.Value);
            data.Add(playerScore.Key == hostPlayerName);
            data.Add(avatarIndex);
        }

        return data.ToArray();
    }

    [PunRPC]
    private void ShowOverallLeaderboardRPC(object[] scoreData)
    {
        ShowOverallLeaderboard(scoreData);
    }

    private void CalculateTotalScores()
    {
        // Reset scores
        foreach (var player in playerTotalScores.Keys.ToList())
        {
            playerTotalScores[player] = 0;
        }

        // Calculate scores for each round
        for (int round = 0; round < hostRoundNumbers.Count; round++)
        {
            int hostNumber = hostRoundNumbers[round];

            foreach (var player in playerRoundGuesses)
            {
                if (player.Value.Count > round)
                {
                    int guess = player.Value[round];
                    int difference = Mathf.Abs(guess - hostNumber);
                    int points = 0;

                    if (difference == 0) points = 3;
                    else if (difference == 1) points = 2;
                    else if (difference == 2) points = 1;

                    playerTotalScores[player.Key] += points;
                }
            }
        }
    }

    private void ShowPlayerAnswers()
    {
        // Update host's answer
        if (activeProfiles.TryGetValue(hostPlayerName, out GameProfileUpdate hostProfile))
        {
            hostProfile.numberGuessed(hostNumber.ToString());
            hostProfile.showAnswers();
            hostProfile.Correct(false); // Host never earns points
        }

        // Update players' answers + scoring
        foreach (var guess in playerGuesses)
        {
            if (activeProfiles.TryGetValue(guess.Key, out GameProfileUpdate profile))
            {
                profile.numberGuessed(guess.Value.ToString());
                profile.showAnswers();

                // Scoring logic here
                int difference = Mathf.Abs(guess.Value - hostNumber);
                int points = 0;

                if (difference == 0) points = 3;
                else if (difference == 1) points = 2;
                else if (difference == 2) points = 1;

                // Update totals
                if (!playerTotalScores.ContainsKey(guess.Key))
                    playerTotalScores[guess.Key] = 0;

                playerTotalScores[guess.Key] += points;

                // Trigger UI
                profile.Correct(points > 0);
            }
        }
    }

    private void ShowOverallLeaderboard(object[] scoreData)
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(true);

        if (gameplayPanel != null)
            gameplayPanel.SetActive(false);

        ClearLeaderboard();

        // Calculate total of all players' scores
        int allPlayersScore = 0;
        for (int i = 1; i < scoreData.Length; i += 4)
        {
            allPlayersScore += (int)scoreData[i];
        }

        // Parse player data
        for (int i = 0; i < scoreData.Length; i += 4)
        {
            string playerName = (string)scoreData[i];
            int score = (int)scoreData[i + 1];
            bool isHost = (bool)scoreData[i + 2];
            int avatarIndex = (int)scoreData[i + 3];

            int rank = (i / 4) + 1;

            if (leaderboardContainer != null && leaderboardEntryPrefab != null)
            {
                GameObject entryObj = Instantiate(leaderboardEntryPrefab, leaderboardContainer.transform);
                LeaderboardEntry entryUm = entryObj.GetComponent<LeaderboardEntry>();

                if (entryUm != null)
                {
                    entryUm.SetForOverall(
                        rank,
                        playerName,
                        score,
                        allPlayersScore,
                        isHost
                    );

                    if (PlayerListUI.instance != null)
                        entryUm.SetAvatar(PlayerListUI.instance.GetAvatarSprite(avatarIndex));
                }
            }
        }

        if (replayButton != null)
            replayButton.gameObject.SetActive(true);
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

        playerGuesses.Clear();
        currentRound = 1;
        ResetGameState();
        InitializePlayerTracking();

        if (gameActive && PhotonNetwork.IsMasterClient)
            Invoke("ActuallyRestartGame", 0.5f);
    }

    private void ActuallyRestartGame()
    {
        isRestarting = false;
        ChooseRandomHost();
    }

    private void ResetRoundState()
    {
        isVotingPhase = false;

        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);

        if (gameplayPanel != null)
            gameplayPanel.SetActive(true);

        if (hostPanel != null)
            hostPanel.SetActive(false);

        isLocalHost = false;

        if (hostHintInput != null)
            hostHintInput.text = "";

        if (playerGuessInput != null)
        {
            playerGuessInput.text = "";
            playerGuessInput.interactable = true;
        }

        if (voteButton != null)
            voteButton.interactable = true;

        if (hostHintText != null)
            hostHintText.text = "";

        if (winnerLog != null)
            winnerLog.text = "";

        if (roundText != null)
            roundText.text = $"Round: {currentRound}/{totalRounds}";

        // Reset chat
        SetChatActive(false);
        chatMessages.Clear();
        UpdateChatDisplay();

        HideAllAnswers();
    }

    private void ResetGameState()
    {
        ResetRoundState();

        if (replayButton != null)
            replayButton.gameObject.SetActive(false);

        if (roundText != null)
            roundText.text = $"Round: {currentRound}/{totalRounds}";

        // Reset host status
        isLocalHost = false;

        if (hostPanel != null)
            hostPanel.SetActive(false);
    }

    public override void OnJoinedRoom()
    {
        if (playersPanel != null)
            playersPanel.gameObject.SetActive(true);

        UpdatePlayerProfiles();

        // Request the current rounds value from the master client
        if (!PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RequestRoundsSync", RpcTarget.MasterClient);
        }
    }

    [PunRPC]
    private void RequestRoundsSync()
    {
        // Master client sends the current rounds value to the requesting client
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("SyncTotalRounds", RpcTarget.Others, totalRounds);
        }
    }

    public override void OnLeftRoom()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);

        if (playersPanel != null)
            playersPanel.gameObject.SetActive(false);

        UpdatePlayerProfiles();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerProfiles();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (playerGuesses.ContainsKey(otherPlayer.NickName))
            playerGuesses.Remove(otherPlayer.NickName);

        if (PhotonNetwork.CurrentRoom == null) return;

        if (PhotonNetwork.CurrentRoom.PlayerCount <= 1)
        {
            CancelMatch();
        }
        else if (otherPlayer.NickName == hostPlayerName && !isRestarting && PhotonNetwork.IsMasterClient)
        {
            ChooseRandomHost();
        }
        else if (PhotonNetwork.IsMasterClient && isVotingPhase &&
                 playerGuesses.Count >= PhotonNetwork.CurrentRoom.PlayerCount - 1)
        {
            photonView.RPC("FinalizeGuesses", RpcTarget.All);
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
        // Clear existing active profiles
        activeProfiles.Clear();

        if (PhotonNetwork.PlayerList == null) return;

        // Sort players by actor number for consistent ordering
        sortedPlayers = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToList();

        // Activate and assign profiles
        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            if (i < playerProfiles.Length)
            {
                Player player = sortedPlayers[i];
                GameProfileUpdate profile = playerProfiles[i];

                profile.gameObject.SetActive(true);
                activeProfiles[player.NickName] = profile;

                // Update profile data
                UpdateSingleProfile(player);

                // Set position in container
                profile.transform.SetSiblingIndex(i);
            }
        }

        // Deactivate unused profiles
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

            // Use the current global host name
            bool isHost = (player.NickName == hostPlayerName);
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
        if (answerText == null) { Debug.LogError("Answer Text reference missing!"); valid = false; }
        if (hostHintText == null) { Debug.LogError("Host Hint Text reference missing!"); valid = false; }
        if (hostHintInput == null) { Debug.LogError("Host Hint Input reference missing!"); valid = false; }
        if (startGameButton == null) { Debug.LogError("Start Game Button reference missing!"); valid = false; }
        if (playerGuessInput == null) { Debug.LogError("Player Guess Input reference missing!"); valid = false; }
        if (voteButton == null) { Debug.LogError("Vote Button reference missing!"); valid = false; }
        if (leaderboardEntryPrefab == null) { Debug.LogError("Leaderboard Entry Prefab reference missing!"); valid = false; }
        if (winnerLog == null) { Debug.LogError("Winner Log reference missing!"); valid = false; }
        if (replayButton == null) { Debug.LogError("Replay Button reference missing!"); valid = false; }
        if (roundText == null) { Debug.LogError("Round Text reference missing!"); valid = false; }

        // Simple chat validation
        if (chatInputField == null) { Debug.LogError("Chat Input Field reference missing!"); valid = false; }
        if (sendChatButton == null) { Debug.LogError("Send Chat Button reference missing!"); valid = false; }
        if (chatDisplayText == null) { Debug.LogError("Chat Display Text reference missing!"); valid = false; }

        return valid;
    }
}