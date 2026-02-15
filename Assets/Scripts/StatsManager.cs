using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class StatsManager : MonoBehaviourPunCallbacks
{
    [System.Serializable]
    public class RoundGuess
    {
        public int guesserID;  // who guessed
        public int guessScore; // points given
    }

    [System.Serializable]
    public class RoundData
    {
        public int hintGiverID;        // who gave the hint
        public List<RoundGuess> guesses = new List<RoundGuess>();
    }

    [System.Serializable]
    public class tempRounds
    {
        public int hinter;
        public int guesser;
        public int point;
    }

    public List<tempRounds> tempRoundList = new List<tempRounds>();

    [System.Serializable]
    public class PlayerInfo
    {
        public string playerName;
        public int avatarIndex;
    }

    public static StatsManager instance;

    public int maxScore;

    public int myID = 0;
    // ✅ All player IDs in room
    public List<int> allPlayerIDs = new List<int>();
    public Dictionary<int, PlayerInfo> allPlayersInfo = new Dictionary<int, PlayerInfo>();

    // ✅ Track round data temporarily
    public List<RoundData> roundsData = new List<RoundData>();

    #region stuff

    private void Awake()
    {
        instance = this;
        if (!PlayerPrefs.HasKey("ID"))
        {
            myID = UnityEngine.Random.Range(0, 1000);
            PlayerPrefs.SetInt("ID", myID);
        }
        else
        {
            myID = PlayerPrefs.GetInt("ID");
        }
    }
    public List<int> GetAllPlayerIDsInRoom()
    {
        List<int> ids = new List<int>();

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue("myID", out object idObj))
            {
                int id = (int)idObj;
                ids.Add(id);
            }
        }

        return ids;
    }
    public void SyncAllPlayersInfo()
    {
        allPlayersInfo.Clear();

        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (!p.CustomProperties.TryGetValue("myID", out object idObj)) continue;

            int id = (int)idObj;
            string name = p.NickName;
            int avatar = 0;
            if (p.CustomProperties.TryGetValue("avatarIndex", out object idx))
                avatar = (int)idx;

            allPlayersInfo[id] = new PlayerInfo { playerName = name, avatarIndex = avatar };
        }

        Debug.Log("Synced all player info: " + string.Join(", ", allPlayersInfo.Keys));
    }

    // Called on the new player after joining
    public override void OnJoinedRoom()
    {
        Hashtable props = new Hashtable
        {
            ["myID"] = myID,
            ["avatarIndex"] = LocalPlayer.Instance.defaultAvatarIndex
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        // Ask all other players to send their info
        photonView.RPC("SendMyInfoToNewPlayer", RpcTarget.Others, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    // RPC called on all existing players
    [PunRPC]
    private void SendMyInfoToNewPlayer(int newPlayerActorNumber)
    {
        // Use your own custom properties, not NickName
        Hashtable myProps = PhotonNetwork.LocalPlayer.CustomProperties;
        if (!myProps.TryGetValue("myID", out object idObj)) return;
        int id = (int)idObj;

        int avatar = myProps.ContainsKey("avatarIndex") ? (int)myProps["avatarIndex"] : 0;
        string name = PhotonNetwork.LocalPlayer.NickName; // only this client’s NickName

        photonView.RPC("ReceivePlayerInfo", PhotonNetwork.CurrentRoom.GetPlayer(newPlayerActorNumber),
            id, name, avatar);
    }


    [PunRPC]
    private void ReceivePlayerInfo(int playerID, string name, int avatar)
    {
        allPlayersInfo[playerID] = new StatsManager.PlayerInfo
        {
            playerName = name,
            avatarIndex = avatar
        };

        Debug.Log($"Received info for player {playerID}: {name}");
        SyncAllPlayersInfo();
    }



    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        SyncAllPlayersInfo();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        SyncAllPlayersInfo();
    }

    // ----------------------------
    // ✅ Submit a guess from GameplayManager
    // ----------------------------
    public void SubmitGuess(int hintGiverID, int points)
    {
        // Send guess to all clients
        photonView.RPC("SyncRoundData", RpcTarget.All, hintGiverID, myID, points);
    }

    #endregion

    public void FinalizeRound()
    {
        SyncToMain();
    }


    private void SyncToMain()
    {
        roundsData.Clear();

        foreach (var temp in tempRoundList)
        {
            // 1️⃣ Find RoundData for this hinter
            RoundData round = roundsData.Find(r => r.hintGiverID == temp.hinter);

            // If not found → create new
            if (round == null)
            {
                round = new RoundData();
                round.hintGiverID = temp.hinter;
                roundsData.Add(round);
            }

            // 2️⃣ Find guess inside this hinter
            RoundGuess guess = round.guesses.Find(g => g.guesserID == temp.guesser);

            // If guess not found → create new
            if (guess == null)
            {
                guess = new RoundGuess();
                guess.guesserID = temp.guesser;
                guess.guessScore = temp.point;
                round.guesses.Add(guess);
            }
            else
            {
                // 3️⃣ If same hinter + guesser → add points
                guess.guessScore += temp.point;
            }
        }

        tempRoundList.Clear();
    }


    // ✅ Add logging to SyncRoundData
    private void SyncRoundData(int hintGiverID, int guesserID, int points)
    {
        tempRounds temp = new tempRounds
        {
            hinter = hintGiverID,
            guesser = guesserID,
            point = points
        };

        tempRoundList.Add(temp);
    }


    // ----------------------------
    // Store guesses on your own hint
    private void StoreGuessForHint(int guesserID, int points)
    {
        Debug.Log($"[Hint Giver] Player {guesserID} guessed {points} points on my hint.");
        // TODO: Add your logic to store locally, e.g., temporary dictionary or UI update
    }

    // Store how you judged another player's hint
    private void StoreYourJudgement(int hinterID, int points)
    {
        Debug.Log($"[Guesser] I gave {points} points to hint from player {hinterID}.");
        // TODO: Add your logic to store locally, e.g., temporary dictionary
    }

    public void UpdatePlayerStatistics()
    {
        foreach (var round in roundsData)
        {
            int hinterID = round.hintGiverID;

            foreach (var guess in round.guesses)
            {
                int guesserID = guess.guesserID;
                int points = guess.guessScore;

                PlayerInfo guesserInfo = allPlayersInfo[guesserID];
                PlayerInfo hinterInfo = allPlayersInfo[hinterID];

                FriendStats fs = new FriendStats
                {
                    friendID = guesserID,
                    friendName = guesserInfo.playerName,
                    totalPointsAtoB = points,
                    totalPointsBtoA = 0,
                    totalPossibleAtoB = 2,
                    totalPossibleBtoA = 0,
                    avatarIndex = guesserInfo.avatarIndex
                };

                PlayerStatistics.instance.UpdateAtoB(fs);

                FriendStats fs2 = new FriendStats
                {
                    friendID = hinterID,
                    friendName = hinterInfo.playerName,
                    totalPointsAtoB = 0,
                    totalPointsBtoA = points,
                    totalPossibleAtoB = 0,
                    totalPossibleBtoA = 2,
                    avatarIndex = hinterInfo.avatarIndex
                };

                PlayerStatistics.instance.UpdateAtoB(fs2);

            }
        }

        Debug.Log("PlayerStatistics updated from StatsManager!");
    }


    // ----------------------------
    // Get all round data for leaderboard
    public List<RoundData> GetAllRoundsData()
    {
        return roundsData;
    }

    // ----------------------------
    // Optional: Reset stats for new match
    public void ResetAllRounds()
    {
        roundsData.Clear();
        tempRoundList.Clear();
        Debug.Log("All rounds data cleared!");
    }


    // ✅ FIX: Make this public so LeaderboardEntry can call it directly
    public void SyncRoundDataLocal(int hintGiverID, int guesserID, int points)
    {
        SyncRoundData(hintGiverID, guesserID, points);
    }
    public int GetMaxPossibleScore(int playerID)
    {
        int roundsAsGuesser = 0;

        // ✅ Count rounds where this player ACTUALLY SUBMITTED A GUESS
        foreach (var round in roundsData)
        {
            // Check if this player has a guess entry in this round
            bool playerGuessedInThisRound = round.guesses.Exists(g => g.guesserID == playerID);

            if (playerGuessedInThisRound)
            {
                roundsAsGuesser++;
            }
        }

        int maxScore = roundsAsGuesser * 2;
        Debug.Log($"GetMaxPossibleScore: Player {playerID} was guesser in {roundsAsGuesser} rounds, max possible = {maxScore}");
        return maxScore;
    }

    public int GetPointsFromHintGiver(int guesserID, int hintGiverID)
    {
        int totalPoints = 0;

        foreach (var round in roundsData)
        {
            if (round.hintGiverID == hintGiverID)
            {
                RoundGuess guess = round.guesses.Find(g => g.guesserID == guesserID);
                if (guess != null)
                {
                    totalPoints += guess.guessScore;
                    Debug.Log($"Found: Guesser {guesserID} earned {guess.guessScore} from HintGiver {hintGiverID}");
                }
            }
        }

        Debug.Log($"GetPointsFromHintGiver: Guesser {guesserID} earned total {totalPoints} from HintGiver {hintGiverID}");
        return totalPoints;
    }

    

}
