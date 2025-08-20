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
    // Keep only essential UI panels
    [Header("UI Panels")]
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private GameObject playersPanel;

    [Header("Host UI")]
    [SerializeField] private TMP_Text hostNameText;
    [SerializeField] private TMP_Text AnswerText;
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
    public void updateRounds(TMP_InputField num)
    {
        roundField = num;
        int numm = Convert.ToInt32(num);

        if(numm > 20)
        {
            numm = 20;
        }

        totalRounds = numm;
    }
    public bool CheckHostInputs()
    {
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
    private Dictionary<string, List<int>> playerRoundGuesses = new Dictionary<string, List<int>>();
    private Dictionary<string, int> playerTotalScores = new Dictionary<string, int>();
    private List<int> hostRoundNumbers = new List<int>();
    private List<string> hostRoundHints = new List<string>();

    // Player Guesses
    private Dictionary<string, int> playerGuesses = new Dictionary<string, int>();

    [Header("Player Profiles")]
    [SerializeField] private GameProfileUpdate[] playerProfiles; // Pre-assigned in inspector
    [SerializeField] private Transform profilesContainer;

    private Dictionary<string, GameProfileUpdate> activeProfiles = new Dictionary<string, GameProfileUpdate>();
    private List<Player> sortedPlayers = new List<Player>();

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        photonView = GetComponent<PhotonView>();
        isLocalHost = false;

        if (!ValidateUIReferences()) return;

        voteButton.onClick.AddListener(SubmitVote);
        startGameButton.onClick.AddListener(SubmitHostHint);
        replayButton.onClick.AddListener(RequestRestartGame);

        replayButton.gameObject.SetActive(false);
        voteButton.interactable = true;
        playerGuessInput.interactable = true;

        ClearLeaderboard();
        roundText.text = $"Round: {currentRound}/{totalRounds}";

        // Initialize all profiles as inactive
        foreach (var profile in playerProfiles)
        {
            profile.gameObject.SetActive(false);
            profile.hideAnswers();
        }
    }

    private bool ValidateUIReferences()
    {
        bool valid = true;
        if (hostPanel == null) Debug.LogError("Host Panel reference missing!");
        if (gameplayPanel == null) Debug.LogError("Gameplay Panel reference missing!");
        if (hostNameText == null) Debug.LogError("Host Name Text reference missing!");
        if (AnswerText == null) Debug.LogError("Answer Text reference missing!");
        if (hostHintText == null) Debug.LogError("Host Hint Text reference missing!");
        if (hostHintInput == null) Debug.LogError("Host Hint Input reference missing!");
        if (startGameButton == null) Debug.LogError("Start Game Button reference missing!");
        if (playerGuessInput == null) Debug.LogError("Player Guess Input reference missing!");
        if (voteButton == null) Debug.LogError("Vote Button reference missing!");
        if (leaderboardEntryPrefab == null) Debug.LogError("Leaderboard Entry Prefab reference missing!");
        if (winnerLog == null) Debug.LogError("Winner Log reference missing!");
        if (replayButton == null) Debug.LogError("Replay Button reference missing!");
        if (roundText == null) Debug.LogError("Round Text reference missing!");
        return valid;
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
        hostPanel.SetActive(false);
        isLocalHost = false;

        hostPlayerName = hostName;
        hostNameText.text = $"{hostName}'s guess";

        // Only set number if we're the new host
        if (PhotonNetwork.NickName == hostName)
        {
            isLocalHost = true;

            // Use number from master if available, otherwise generate locally
            hostNumber = hostNum > -1 ? hostNum : Random.Range(0, 11);
            AnswerText.text = hostNumber.ToString();
            hostPanel.SetActive(true);
        }

        gameplayPanel.SetActive(true);
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
        hostHintText.text = hostHint;

        // Deactivate host panel for everyone
        hostPanel.SetActive(false);

        // Disable voting for host only
        if (PhotonNetwork.NickName == hostPlayerName)
        {
            playerGuessInput.interactable = false;
            voteButton.interactable = false;
            playerGuessInput.text = "Host cannot vote";
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

        playerGuessInput.interactable = false;
        voteButton.interactable = false;

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

    // Add this RPC method for showing leaderboard
    [PunRPC]
    private void ShowOverallLeaderboardRPC(object[] scoreData)
    {
        ShowOverallLeaderboard(scoreData);
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
            // Move to next round after 5 seconds
            Invoke("NextRound", 5f);
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
    private void NextRound()
    {
        currentRound++;
        StartRound();
    }

    private void CalculateTotalScores()
    {
        // Reset scores
        foreach (var player in playerTotalScores.Keys.ToList())
        {
            playerTotalScores[player] = 0;
        }

        // Create a dictionary to track round scores if needed
        Dictionary<string, List<int>> playerRoundPoints = new Dictionary<string, List<int>>();
        foreach (var player in playerRoundGuesses.Keys)
        {
            playerRoundPoints[player] = new List<int>();
        }

        for (int round = 0; round < totalRounds; round++)
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
                    playerRoundPoints[player.Key].Add(points);

                    //  Trigger UI effect
                    if (activeProfiles.TryGetValue(player.Key, out GameProfileUpdate profile))
                    {
                        profile.Correct(points > 0);
                    }
                }
                else
                {
                    playerRoundPoints[player.Key].Add(0);

                    if (activeProfiles.TryGetValue(player.Key, out GameProfileUpdate profile))
                    {
                        profile.Correct(false);
                    }
                }
            }

            // Host always gets 0 points for the round
            if (playerRoundPoints.ContainsKey(hostPlayerName))
            {
                playerRoundPoints[hostPlayerName].Add(0);
            }
        }

        // Debug output to verify scoring
        Debug.Log("Final Scores:");
        foreach (var score in playerTotalScores)
        {
            Debug.Log($"{score.Key}: {score.Value} points");

            // Optional: Log round-by-round points
            if (playerRoundPoints.ContainsKey(score.Key))
            {
                string roundPoints = string.Join(", ", playerRoundPoints[score.Key]);
                Debug.Log($"{score.Key} round points: [{roundPoints}]");
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
        }

        // Update players' answers
        foreach (var guess in playerGuesses)
        {
            if (activeProfiles.TryGetValue(guess.Key, out GameProfileUpdate profile))
            {
                profile.numberGuessed(guess.Value.ToString());
                profile.showAnswers();
            }
        }
    }

    private void StartNextRound()
    {
        currentRound++;
        playerGuesses.Clear();
        ResetRoundState();

        // Hide previous answers
        HideAllAnswers();

        ChooseRandomHost();
    }

    private void ShowOverallLeaderboard(object[] scoreData)
    {
        leaderboardPanel.SetActive(true);
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

            GameObject entryObj = Instantiate(leaderboardEntryPrefab, leaderboardContainer.transform);
            LeaderboardEntry entryUm = entryObj.GetComponent<LeaderboardEntry>();

            if (entryUm != null)
            {
                entryUm.SetForOverall(
                    rank,
                    playerName,
                    score,
                    allPlayersScore,   // <<<< NEW PARAM
                    isHost
                );

                entryUm.SetAvatar(PlayerListUI.instance.GetAvatarSprite(avatarIndex));
            }
        }

        replayButton.gameObject.SetActive(true);
    }


    private void ClearLeaderboard()
    {
        foreach (Transform child in leaderboardContainer.transform)
            Destroy(child.gameObject);
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
        leaderboardPanel.SetActive(false);
        gameplayPanel.SetActive(true);

        hostPanel.SetActive(false);
        isLocalHost = false;

        hostHintInput.text = "";
        playerGuessInput.text = "";
        playerGuessInput.interactable = true;
        voteButton.interactable = true;
        hostHintText.text = "";
        winnerLog.text = "";
        roundText.text = $"Round: {currentRound}/{totalRounds}";

        HideAllAnswers();
    }

    private void ResetGameState()
    {
        ResetRoundState();
        replayButton.gameObject.SetActive(false);
        roundText.text = $"Round: {currentRound}/{totalRounds}";

        // Reset host status
        isLocalHost = false;
        hostPanel.SetActive(false);
    }

    public override void OnJoinedRoom()
    {
        playersPanel.gameObject.SetActive(true);
        UpdatePlayerProfiles();
    }

    public override void OnLeftRoom()
    {
        leaderboardPanel.SetActive(false);
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
        leaderboardPanel.SetActive(false);
        gameplayPanel.SetActive(false);
        hostPanel.SetActive(false);
        playersPanel.SetActive(false);
        PhotonNetwork.LeaveRoom();
    }

    private void UpdatePlayerProfiles()
    {
        // Clear existing active profiles
        activeProfiles.Clear();

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


}