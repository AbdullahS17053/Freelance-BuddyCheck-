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

    private string currentHostPlayerName;
    private int currentHostAnswer = -1;
    private bool isLocalHost = false;
    private bool gameActive = false;

    private int tempScore;
    private int hintRoundEach = 0;
    private int hintRound = 0;
    private int currentRound = 0;
    public int totalRounds = 2;
    private TMP_InputField roundsInputField;

    private List<string> chatMessages = new List<string>();
    private const int MAX_CHAT_LINES = 8;

    private Dictionary<string, bool> playersReadyForRounds = new Dictionary<string, bool>();
    [SerializeField] private GameObject waitingForPlayersPanel;

    private Dictionary<string, int> playerTotalScores = new Dictionary<string, int>();
    private Dictionary<string, int> currentRoundGuesses = new Dictionary<string, int>();
    private Dictionary<string, GameProfileUpdate> activeProfiles = new Dictionary<string, GameProfileUpdate>();

    private const string CURRENT_ROUND_KEY = "CurrentRound";
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

        voteButton.onClick.AddListener(SubmitVote);
        submitHostAnswerButton.onClick.AddListener(SubmitHostAnswer);
        submitHintAnswerButton.onClick.AddListener(SubmitHint);
        replayButton.onClick.AddListener(RequestRestartGame);
        sendChatButton.onClick.AddListener(SendChatMessage);

        chatInputField.onSubmit.AddListener(delegate { SendChatMessage(); });

        replayButton.gameObject.SetActive(false);

        ResetUIForNewRound();
        ClearLeaderboard();
        InitializeChat();

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
        SetChatActive(false);
    }

    private void ResetUIForNewRound()
    {
        UpdateRoundText();

        if (hostAnswerInput != null) hostAnswerInput.text = "";
        if (playerGuessInput != null) playerGuessInput.text = "";

        SetChatActive(false);

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
        }

        HintCollection(); // Local for master
        photonView.RPC("HintCollection", RpcTarget.Others);
    }

    [PunRPC]
    private void HintCollection()
    {
        Debug.Log("New Hint Round of " + hintRoundEach);

        // FIX: Reset ready states so match can start after hints
        playersReadyForRounds.Clear();
        foreach (Player p in PhotonNetwork.PlayerList)
            playersReadyForRounds[p.NickName] = false;

        hintPanel.SetActive(true);
        HintNewCategory();
    }


    private void StartNewRound()
    {

        if(PhotonNetwork.IsMasterClient)
        {
            currentRound++;
            var props = new Hashtable { [CURRENT_ROUND_KEY] = currentRound };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        else
        {
            currentRound = (int)PhotonNetwork.CurrentRoom.CustomProperties[CURRENT_ROUND_KEY];
        }


        currentRoundGuesses.Clear();

        if (PhotonNetwork.IsMasterClient)
        {
            var props = new Hashtable
            {
                [CURRENT_ROUND_KEY] = currentRound,
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        NewCategory();
        ResetUIForNewRound();
        HideAllAnswers();
    }

    private void ProceedToNextPhase()
    {
        if (currentRound >= totalRounds) ShowOverallLeaderboard();
        else StartNewRound();
    }

    #endregion
    // -----------------------------------------------------------



    // -----------------------------------------------------------
    #region HINT SYSTEM
    // -----------------------------------------------------------

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
        categoryID = UnityEngine.Random.Range(0, hintCatories.Count);
        photonView.RPC("GameNewCategoryAll", RpcTarget.All, categoryID);
    }

    [PunRPC]
    private void GameNewCategoryAll(int ID)
    {
        categoryID = ID;

        foreach (var t in bad) t.text = hintCatories[ID].bad;
        foreach (var t in example) t.text = hintCatories[ID].example;
        foreach (var t in good) t.text = hintCatories[ID].good;
        foreach (var t in username) t.text = hintCatories[ID].player;
        foreach (var t in hint) t.text = hintCatories[ID].hint;
    }

    public void SubmitHint()
    {
        hintRound++;

        photonView.RPC("AddHintList", RpcTarget.All,
            hintCategoryID,
            tempScore,
            categories[0].categories[hintCategoryID].hint,
            categories[0].categories[hintCategoryID].player);

        photonView.RPC("PlayerReadyForRoundsRPC", RpcTarget.MasterClient, PhotonNetwork.NickName);

        hintAnswerInput.text = "";
        HintNewCategory();

        if (hintRound >= hintRoundEach)
        {
            hintRound = 0;
            hintPanel.SetActive(false);
        }
    }

    [PunRPC]
    private void AddHintList(int ID, int score, string hint, string player)
    {
        Categories newCat = new Categories();
        newCat.bad = categories[0].categories[ID].bad;
        newCat.good = categories[0].categories[ID].good;
        newCat.example = categories[0].categories[ID].example;
        newCat.score = score;
        newCat.hint = hint;
        newCat.player = player;

        categories[0].categories[ID].score = score;

        hintCatories.Add(newCat);
        hintStoredCatories.Add(newCat);
    }

    public void RemoveHint()
    {
        photonView.RPC("RemoveHintList", RpcTarget.All, categoryID);
    }

    [PunRPC]
    private void RemoveHintList(int ID)
    {
        categoryID = ID;
        hintCatories.RemoveAt(ID);
    }

    #endregion
    // -----------------------------------------------------------



    #region PLAYER READY SYSTEM
    [PunRPC]
    private void PlayerReadyForRoundsRPC(string playerName)
    {
        if (!playersReadyForRounds.ContainsKey(playerName))
            playersReadyForRounds[playerName] = true;

        CheckAllPlayersReady();
    }

    private void CheckAllPlayersReady()
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!playersReadyForRounds.ContainsKey(p.NickName))
                playersReadyForRounds[p.NickName] = false;

            if (!playersReadyForRounds[p.NickName])
            {
                waitingForPlayersPanel.SetActive(true);
                return;
            }
        }

        waitingForPlayersPanel.SetActive(false);

        if (PhotonNetwork.IsMasterClient)
        {
            gameActive = true;
            currentRound = 0;
            InitializePlayerTracking();
            StartNewRound();

            playersReadyForRounds.Clear();
        }
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

        if (hostPanel) hostPanel.SetActive(false);

        if (!isLocalHost)
        {
            playerGuessInput.interactable = true;
            voteButton.interactable = true;
        }
        else
        {
            playerGuessInput.interactable = false;
            playerGuessInput.text = "Host cannot vote";
            voteButton.interactable = false;
        }

        SetChatActive(true);
    }

    public void SubmitVote()
    {
        if (isLocalHost) return;

        if (!int.TryParse(playerGuessInput.text, out int guessedNumber))
            return;

        playerGuessInput.interactable = false;
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
            currentRoundGuesses[playerName] = guess;
    }

    [PunRPC]
    private void FinalizeRoundRPC()
    {
        SetChatActive(false);

        CalculateRoundScores();
        ShowPlayerAnswers();

        Invoke("ProceedToNextPhase", 3f);
    }

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
        if (activeProfiles.TryGetValue(currentHostPlayerName, out GameProfileUpdate hostProfile))
        {
            hostProfile.numberGuessed(currentHostAnswer.ToString());
            hostProfile.showAnswers();
        }

        foreach (var guess in currentRoundGuesses)
        {
            if (activeProfiles.TryGetValue(guess.Key, out GameProfileUpdate profile))
            {
                profile.numberGuessed(guess.Value.ToString());
                profile.showAnswers();

                int difference = Mathf.Abs(guess.Value - currentHostAnswer);
                profile.Correct(difference <= 2);
            }
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

        ClearLeaderboard();

        for (int i = 0; i < leaderboardData.Length; i += 4)
        {
            string playerName = (string)leaderboardData[i];
            int score = (int)leaderboardData[i + 1];
            bool isHost = (bool)leaderboardData[i + 2];
            int avatarIndex = (int)leaderboardData[i + 3];

            GameObject entryObj = Instantiate(leaderboardEntryPrefab, leaderboardContainer.transform);
            LeaderboardEntry entry = entryObj.GetComponent<LeaderboardEntry>();

            if (entry != null)
            {
                entry.SetForOverall(i + 1, playerName, score, 0, isHost);

                if (PlayerListUI.instance != null)
                    entry.SetAvatar(PlayerListUI.instance.GetAvatarSprite(avatarIndex));
            }
        }

        replayButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
    }

    private object[] PrepareLeaderboardData()
    {
        List<object> data = new List<object>();
        var sortedScores = playerTotalScores.OrderByDescending(p => p.Value).ToList();

        foreach (var playerScore in sortedScores)
        {
            Player player = PhotonNetwork.PlayerList.FirstOrDefault(p => p.NickName == playerScore.Key);

            int avatarIndex = player != null && player.CustomProperties.TryGetValue("avatarIndex", out object idx)
                                ? (int)idx
                                : 0;

            bool isHost = playerScore.Key == currentHostPlayerName;

            data.Add(playerScore.Key);
            data.Add(playerScore.Value);
            data.Add(isHost);
            data.Add(avatarIndex);
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

        bool isHost = player.NickName == currentHostPlayerName;
        profile.updatePlayer(avatarIndex, player.NickName, isHost);
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
            totalRounds = (int)PhotonNetwork.CurrentRoom.CustomProperties[TOTAL_ROUNDS_KEY];
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

        isLocalHost = PhotonNetwork.NickName == currentHostPlayerName;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(CURRENT_ROUND_KEY, out object roundObj))
            currentRound = (int)roundObj;

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
        isLocalHost = false;
        currentRound = 0;

        playerTotalScores.Clear();
        currentRoundGuesses.Clear();

        chatMessages.Clear();
        UpdateChatDisplay();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerProfiles();

        if (PhotonNetwork.IsMasterClient && gameActive)
        {
            object[] data = { currentHostPlayerName, currentRound, totalRounds };
            photonView.RPC("SyncGameStateRPC", newPlayer, data);
        }
    }

    [PunRPC]
    private void SyncGameStateRPC(object[] data)
    {
        currentHostPlayerName = (string)data[0];
        currentRound = (int)data[1];
        totalRounds = (int)data[2];
        isLocalHost = PhotonNetwork.NickName == currentHostPlayerName;

        UpdatePlayerProfiles();
        UpdateRoundText();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        currentRoundGuesses.Remove(otherPlayer.NickName);
        playerTotalScores.Remove(otherPlayer.NickName);

        if (PhotonNetwork.CurrentRoom == null) return;

        UpdatePlayerProfiles();

        if (PhotonNetwork.CurrentRoom.PlayerCount <= 1)
        {
            CancelMatch();
            return;
        }

        if (gameActive && PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RestartRoundAfterPlayerLeaveRPC", RpcTarget.All);
        }
    }

    [PunRPC]
    private void RestartRoundAfterPlayerLeaveRPC()
    {
        currentRoundGuesses.Clear();
        ResetUIForNewRound();
        HideAllAnswers();

        Invoke("DelayedHostSelection", 0.5f);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {

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
            Debug.Log("Updated hintRoundEach = " + hintRoundEach);
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

        if (int.TryParse(roundsInput.text, out int rounds))
        {
            totalRounds = rounds;

            if (PhotonNetwork.IsMasterClient)
            {
                var props = new Hashtable { [TOTAL_ROUNDS_KEY] = totalRounds };
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            }

            roundsInput.text = "";
            UpdateRoundText();
        }
    }

    private void UpdateRoundText()
    {
        if (roundText != null)
            roundText.text = $"Round: {currentRound}/{totalRounds}";
    }

    public void CancelMatch()
    {
        gameActive = false;
        PhotonNetwork.LeaveRoom();
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

        leaderboardPanel.SetActive(false);
        gameplayPanel.SetActive(true);

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
