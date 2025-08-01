using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class GameplayManager : MonoBehaviourPunCallbacks
{
    public GameObject hostPanel;
    public GameObject gameplayPanel;
    public TMP_Text hostNameText;
    public TMP_Text hostHintText;
    public TMP_InputField hostHintInput;
    public Button startGameButton;
    public TMP_InputField playerGuessInput;
    public Button voteButton;
    public GameObject leaderboardContainer;
    public GameObject leaderboardEntryPrefab;
    public TMP_Text winnerLog;
    public Button replayButton;

    private string hostPlayerName;
    private int hostNumber;
    private string hostHint;
    private bool isLocalHost = false;
    private Dictionary<string, int> playerGuesses = new Dictionary<string, int>();

    void Start()
    {

        voteButton.onClick.AddListener(SubmitVote);
        startGameButton.onClick.AddListener(SubmitHostHint);
        replayButton.onClick.AddListener(RestartGame);
    }

    [PunRPC]
    void ChooseRandomHost()
    {
        List<Player> players = PhotonNetwork.PlayerList.ToList();
        Player selectedHost = players[Random.Range(0, players.Count)];
        hostPlayerName = selectedHost.NickName;
        photonView.RPC("SetHost", RpcTarget.All, hostPlayerName);
    }

    [PunRPC]
    void SetHost(string hostName)
    {
        hostPlayerName = hostName;
        isLocalHost = (PhotonNetwork.NickName == hostName);
        hostNameText.text = $"Host: {hostName}";

        if (isLocalHost)
        {
            hostNumber = Random.Range(0, 11); // Assigned secretly
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
        hostHintText.text = $"Hint from Host: {hint}";
        hostPanel.SetActive(false);
        gameplayPanel.SetActive(true);
    }

    public void SubmitVote()
    {
        if (!int.TryParse(playerGuessInput.text, out int guessedNumber) || guessedNumber < 0 || guessedNumber > 10)
        {
            Debug.LogError("Invalid guess. Must be 0–10.");
            return;
        }
        photonView.RPC("SubmitPlayerGuess", RpcTarget.MasterClient, PhotonNetwork.NickName, guessedNumber);
    }

    [PunRPC]
    void SubmitPlayerGuess(string playerName, int guess)
    {
        if (!playerGuesses.ContainsKey(playerName))
        {
            playerGuesses[playerName] = guess;
        }

        if (playerGuesses.Count == PhotonNetwork.CurrentRoom.PlayerCount - 1)
        {
            photonView.RPC("ShowLeaderboard", RpcTarget.All);
        }
    }

    [PunRPC]
    void ShowLeaderboard()
    {
        leaderboardContainer.SetActive(true);

        foreach (Transform child in leaderboardContainer.transform)
        {
            Destroy(child.gameObject);
        }

        // Sort player guesses by proximity to hostNumber
        var sorted = playerGuesses.OrderBy(p => Mathf.Abs(p.Value - hostNumber));
        int rank = 1;

        foreach (var entry in sorted)
        {
            string playerName = entry.Key;
            int guessedNumber = entry.Value;

            // Find Photon Player by name
            Player photonPlayer = PhotonNetwork.PlayerList.FirstOrDefault(p => p.NickName == playerName);

            // Get avatar index from custom properties
            int avatarIndex = 0;
            if (photonPlayer != null && photonPlayer.CustomProperties.ContainsKey("avatarIndex"))
            {
                avatarIndex = (int)photonPlayer.CustomProperties["avatarIndex"];
            }

            // Instantiate leaderboard entry
            GameObject newEntry = Instantiate(leaderboardEntryPrefab, leaderboardContainer.transform);

            // Get UI references
            TMP_Text[] texts = newEntry.GetComponentsInChildren<TMP_Text>(); // 0: Rank, 1: Name, 2: Guess
            Image avatarImage = newEntry.transform.Find("Mask/AvatarImage").GetComponent<Image>();

            if (texts.Length >= 3)
            {
                texts[0].text = $"#{rank}";
                texts[1].text = playerName;
                texts[2].text = guessedNumber.ToString();
            }

            if (avatarImage != null && avatarIndex >= 0 && avatarIndex < PlayerListUI.instance.avatarSprites.Length)
            {
                avatarImage.sprite = PlayerListUI.instance.avatarSprites[avatarIndex];
            }

            rank++;
        }

        // Show correct answer and host info
        winnerLog.text = $"Correct Answer: {hostNumber}\nHost: {hostPlayerName}\nHint: {hostHint}";
        hostHintText.text = "";
        replayButton.gameObject.SetActive(true);
    }
    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("ChooseRandomHost", RpcTarget.All);
        }
    }

    public void RestartGame()
    {
        playerGuesses.Clear();

        foreach (Transform child in leaderboardContainer.transform)
        {
            Destroy(child.gameObject);
        }

        leaderboardContainer.SetActive(false);
        replayButton.gameObject.SetActive(false);
        hostHintInput.text = "";
        playerGuessInput.text = "";
        hostHintText.text = "";

        StartGame();
    }

    public void CancelMatch()
    {
        PhotonNetwork.LeaveRoom();
        Debug.Log("Leaving room and returning to main menu...");
        // Optionally: Show loading UI
        gameplayPanel.SetActive(false);
        hostPanel.SetActive(false);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"{otherPlayer.NickName} left the room.");

        // Remove their guess or avatar if stored
        if (playerGuesses.ContainsKey(otherPlayer.NickName))
            playerGuesses.Remove(otherPlayer.NickName);

        PlayerListUI.instance.RefreshPlayerList();

        // If only 1 player remains, end match
        if (PhotonNetwork.CurrentRoom.PlayerCount <= 1)
        {
            Debug.Log("Only one player left. Cancelling match.");
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            // If host left, Photon will auto-assign MasterClient
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("ChooseRandomHost", RpcTarget.All);
            }
        }
    }

}
