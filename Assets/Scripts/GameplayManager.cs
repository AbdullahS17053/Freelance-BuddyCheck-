using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class GameplayManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private GameObject playersPanel;
    [SerializeField] private TMP_Text hostNameText;
    [SerializeField] private TMP_Text AnswerText;
    [SerializeField] private TMP_Text hostHintText;
    [SerializeField] private TMP_InputField hostHintInput;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TMP_InputField playerGuessInput;
    [SerializeField] private Button voteButton;
    [SerializeField] private GameObject leaderboardContainer;
    [SerializeField] private GameObject leaderboardEntryPrefab;
    [SerializeField] private TMP_Text winnerLog;
    [SerializeField] private Button replayButton;

    private string hostPlayerName;
    private int hostNumber = -1;
    private string hostHint;
    private bool isLocalHost = false;
    private Dictionary<string, int> playerGuesses = new Dictionary<string, int>();
    private bool isVotingPhase = false;
    private bool gameActive = false;
    private bool isRestarting = false; // New flag to track restart state

    void Start()
    {
        if (!ValidateUIReferences()) return;

        voteButton.onClick.AddListener(SubmitVote);
        startGameButton.onClick.AddListener(SubmitHostHint);
        replayButton.onClick.AddListener(RequestRestartGame);
        replayButton.gameObject.SetActive(false);

        voteButton.interactable = true;
        playerGuessInput.interactable = true;

        // Ensure leaderboard is cleared on start
        ClearLeaderboard();
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
        return valid;
    }

    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient && !gameActive)
        {
            gameActive = true;
            ChooseRandomHost();
        }
    }

    private void ChooseRandomHost()
    {
        List<Player> players = PhotonNetwork.PlayerList.ToList();
        if (players.Count == 0) return;

        Player selectedHost = players[Random.Range(0, players.Count)];
        hostPlayerName = selectedHost.NickName;
        photonView.RPC("SetHost", RpcTarget.All, hostPlayerName);
    }

    [PunRPC]
    void SetHost(string hostName)
    {
        ResetGameState();
        hostPlayerName = hostName;
        isLocalHost = (PhotonNetwork.NickName == hostName);
        hostNameText.text = $"Host: {hostName}";

        if (isLocalHost)
        {
            hostNumber = Random.Range(0, 11);
            AnswerText.text = hostNumber.ToString();
            hostPanel.SetActive(true);
            gameplayPanel.SetActive(true);
        }
        else
        {
            hostPanel.SetActive(false);
            gameplayPanel.SetActive(true);
        }
    }

    public void SubmitHostHint()
    {
        if (string.IsNullOrWhiteSpace(hostHintInput.text))
        {
            Debug.LogError("Hint cannot be empty.");
            return;
        }

        hostHint = hostHintInput.text;
        photonView.RPC("BroadcastHostHint", RpcTarget.All, hostHint, hostNumber);
    }

    [PunRPC]
    void BroadcastHostHint(string hint, int number)
    {
        hostHint = hint;
        hostNumber = number;
        hostHintText.text = hostHint;
        hostPanel.SetActive(false);
        gameplayPanel.SetActive(true);

        // Disable voting for host
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

        // Disable voting after submission
        playerGuessInput.interactable = false;
        voteButton.interactable = false;

        photonView.RPC("SubmitPlayerGuess", RpcTarget.MasterClient, PhotonNetwork.NickName, guessedNumber);
    }

    [PunRPC]
    void SubmitPlayerGuess(string playerName, int guess)
    {
        if (playerGuesses.ContainsKey(playerName)) return;

        playerGuesses[playerName] = guess;

        // Update the guess dictionary for all clients
        photonView.RPC("SyncPlayerGuess", RpcTarget.Others, playerName, guess);

        if (PhotonNetwork.IsMasterClient &&
            playerGuesses.Count >= PhotonNetwork.CurrentRoom.PlayerCount - 1)
        {
            // First sync all guesses to all clients
            photonView.RPC("FinalizeGuesses", RpcTarget.All);
        }
    }

    [PunRPC]
    void SyncPlayerGuess(string playerName, int guess)
    {
        if (!playerGuesses.ContainsKey(playerName))
        {
            playerGuesses[playerName] = guess;
        }
    }

    [PunRPC]
    void FinalizeGuesses()
    {
        // Now that all guesses are synced, show the leaderboard
        ShowLeaderboard();
    }

    private void ShowLeaderboard()
    {
        // Only process if we're not already showing the leaderboard
        if (leaderboardPanel.activeSelf) return;

        isVotingPhase = false;
        leaderboardPanel.SetActive(true);
        gameplayPanel.SetActive(false);
        ClearLeaderboard();

        Debug.Log($"Showing leaderboard with {playerGuesses.Count} guesses");

        // First create host entry
        CreateHostLeaderboardEntry();

        // Then create player entries
        CreatePlayerLeaderboardEntries();

        replayButton.gameObject.SetActive(true);
    }

    private void ClearLeaderboard()
    {
        foreach (Transform child in leaderboardContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }

    private void CreateHostLeaderboardEntry()
    {


        // Create special entry for host at top
        GameObject hostEntry = Instantiate(leaderboardEntryPrefab, leaderboardContainer.transform);
        LeaderboardEntry entryUI = hostEntry.GetComponent<LeaderboardEntry>();

        if (entryUI != null)
        {
            entryUI.Initialize(
                rank: 0,
                playerName: hostPlayerName,
                guess: hostNumber,
                isHost: true,
                hint: hostHint
            );

            // Set host avatar
            Player hostPlayer = PhotonNetwork.PlayerList.FirstOrDefault(p => p.NickName == hostPlayerName);
            if (hostPlayer != null && hostPlayer.CustomProperties.ContainsKey("avatarIndex"))
            {
                int avatarIndex = (int)hostPlayer.CustomProperties["avatarIndex"];
                entryUI.SetAvatar(PlayerListUI.instance.avatarSprites[avatarIndex]);
            }
        }

        // Add separator
        if (winnerLog != null)
        {
            winnerLog.text = $"Correct Answer: {hostNumber} | Hint: {hostHint}";
        }
    }

    private void CreatePlayerLeaderboardEntries()
    {
        var players = PhotonNetwork.PlayerList
            .Where(p => p.NickName != hostPlayerName)
            .ToList();

        var playersWithGuesses = players
            .Select(p => new {
                Player = p,
                Guess = playerGuesses.ContainsKey(p.NickName) ? playerGuesses[p.NickName] : -1
            })
            .Where(x => x.Guess >= 0)
            .OrderBy(x => Mathf.Abs(x.Guess - hostNumber))
            .ThenBy(x => x.Player.NickName)
            .ToList();

        int rank = 1;
        foreach (var playerData in playersWithGuesses)
        {
            GameObject entryObj = Instantiate(leaderboardEntryPrefab, leaderboardContainer.transform);
            LeaderboardEntry entryUI = entryObj.GetComponent<LeaderboardEntry>();

            if (entryUI != null)
            {
                entryUI.Initialize(
                    rank: rank,
                    playerName: playerData.Player.NickName,
                    guess: playerData.Guess,
                    isHost: false,
                    hint: ""
                );

                // Get avatar with proper synchronization
                int avatarIndex = 0;
                if (playerData.Player.CustomProperties.TryGetValue("avatarIndex", out object indexObj))
                {
                    avatarIndex = (int)indexObj;
                }

                Sprite avatarSprite = PlayerListUI.instance.GetAvatarSprite(avatarIndex);
                entryUI.SetAvatar(avatarSprite);
            }
            rank++;
        }
    }

    public void RequestRestartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RestartGameRPC", RpcTarget.All);
        }
    }

    [PunRPC]
    void RestartGameRPC()
    {
        if (isRestarting) return; // Prevent multiple restarts
        isRestarting = true;

        playerGuesses.Clear();
        ResetGameState();

        if (gameActive && PhotonNetwork.IsMasterClient)
        {
            // Delay the restart slightly to ensure all clients have reset
            Invoke("ActuallyRestartGame", 0.5f);
        }
    }

    private void ActuallyRestartGame()
    {
        isRestarting = false;
        ChooseRandomHost();
    }

    private void ResetGameState()
    {
        isVotingPhase = false;
        leaderboardPanel.SetActive(false);
        gameplayPanel.SetActive(true); // Show gameplay panel again
        ClearLeaderboard();
        replayButton.gameObject.SetActive(false);
        hostHintInput.text = "";
        playerGuessInput.text = "";
        playerGuessInput.interactable = true;
        voteButton.interactable = true;
        hostHintText.text = "";
        winnerLog.text = "";
    }

    public override void OnJoinedRoom()
    {
        playersPanel.gameObject.SetActive(true);
    }
    public override void OnLeftRoom()
    {
        leaderboardPanel.SetActive(false);

        playersPanel.gameObject.SetActive(false);
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
        else if (otherPlayer.NickName == hostPlayerName && !isRestarting)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                ChooseRandomHost();
            }
        }
        else if (PhotonNetwork.IsMasterClient && isVotingPhase &&
                 playerGuesses.Count >= PhotonNetwork.CurrentRoom.PlayerCount - 1)
        {
            photonView.RPC("ShowLeaderboard", RpcTarget.All);
        }
    }

    public void CancelMatch()
    {
        // Set all flags to prevent automatic restart
        gameActive = false;
        isRestarting = true;
        isVotingPhase = false;

        // Reset UI
        leaderboardPanel.SetActive(false);
        gameplayPanel.SetActive(false);
        hostPanel.SetActive(false);
        playersPanel.SetActive(false);

        // Leave room
        PhotonNetwork.LeaveRoom();
    }
}