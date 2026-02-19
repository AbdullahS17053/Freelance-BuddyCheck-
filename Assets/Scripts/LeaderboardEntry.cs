using NUnit.Framework;
using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StatsManager;

public class LeaderboardEntry : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text perceText;
    [SerializeField] private TMP_Text subText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text AtoBScore;
    [SerializeField] private TMP_Text BtoAScore;
    [SerializeField] private GameObject hostBadge;
    [SerializeField] private GameObject crown;
    [SerializeField] private GameObject container;
    [SerializeField] private Color hostColor = Color.yellow;
    [SerializeField] private Color defaultColor = Color.black;


    [System.Serializable]
    public class UIEL
    {
        public TextMeshProUGUI _name;
        public TextMeshProUGUI _score;
    }

    public UIEL[] guessers;

    /// <summary>
    /// Shows what THIS player earned from each hint giver
    /// </summary>
    public void ShowPlayerBreakdown(int playerID)
    {
        Debug.Log("====================================");
        Debug.Log($"ShowPlayerBreakdown CALLED for PlayerID: {playerID}");

        // 🔎 Photon State (VERY useful for multiplayer debugging)
        Debug.Log($"IsConnected: {PhotonNetwork.IsConnected}");
        Debug.Log($"IsMasterClient: {PhotonNetwork.IsMasterClient}");
        Debug.Log($"LocalPlayer ActorNumber: {PhotonNetwork.LocalPlayer.ActorNumber}");

        Debug.Log($"Total players in allPlayersInfo: {StatsManager.instance.allPlayersInfo.Count}");
        Debug.Log($"UI Slots Available: {guessers.Length}");

        // Hide all UI slots first
        for (int i = 0; i < guessers.Length; i++)
        {
            guessers[i]._name.gameObject.SetActive(false);
            Debug.Log($"Hiding UI Slot {i}");
        }

        // ✅ Get list of all OTHER players (potential hint givers)
        List<int> otherPlayerIDs = new List<int>();

        foreach (var kvp in StatsManager.instance.allPlayersInfo)
        {
            Debug.Log($"Checking player in dictionary: ID = {kvp.Key}");

            if (kvp.Key != playerID)
            {
                otherPlayerIDs.Add(kvp.Key);
                Debug.Log($" -> Added as OTHER player: {kvp.Key}");
            }
            else
            {
                Debug.Log($" -> Skipped self player: {kvp.Key}");
            }
        }

        Debug.Log($"Total other players found: {otherPlayerIDs.Count}");

        int uiIndex = 0;

        Debug.Log($"=== Breakdown for Player {playerID} ===");

        // ✅ Loop through each other player and show points earned from them
        foreach (int hintGiverID in otherPlayerIDs)
        {
            Debug.Log($"Processing HintGiverID: {hintGiverID}");

            if (uiIndex >= guessers.Length)
            {
                Debug.LogWarning("UI slots exhausted! Breaking loop.");
                break;
            }

            // Get total points THIS player earned from this hint giver
            int pointsFromThisHintGiver =
                StatsManager.instance.GetPointsFromHintGiver(playerID, hintGiverID);

            Debug.Log($"Points from HintGiver {hintGiverID} to Player {playerID}: {pointsFromThisHintGiver}");

            if (StatsManager.instance.allPlayersInfo.TryGetValue(hintGiverID, out PlayerInfo hintGiverInfo))
            {
                Debug.Log($"Found PlayerInfo for HintGiver {hintGiverID} -> Name: {hintGiverInfo.playerName}");

                guessers[uiIndex]._name.gameObject.SetActive(true);
                guessers[uiIndex]._name.text = hintGiverInfo.playerName;
                guessers[uiIndex]._score.text = pointsFromThisHintGiver.ToString();

                Debug.Log($"UI Slot {uiIndex} SET -> Name: {hintGiverInfo.playerName}, Score: {pointsFromThisHintGiver}");

                uiIndex++;
            }
            else
            {
                Debug.LogError($"PlayerInfo NOT FOUND for HintGiver {hintGiverID}");
            }
        }

        Debug.Log($"Final UI slots used: {uiIndex}");
        Debug.Log("====================================");
    }


    private void SetScores(int thisPlayerID)
    {
        Debug.Log("====================================");
        Debug.Log($"SetScores CALLED for PlayerID: {thisPlayerID}");

        // 🔎 Photon State (very useful in multiplayer debugging)
        Debug.Log($"IsConnected: {PhotonNetwork.IsConnected}");
        Debug.Log($"IsMasterClient: {PhotonNetwork.IsMasterClient}");
        Debug.Log($"LocalPlayer ActorNumber: {PhotonNetwork.LocalPlayer.ActorNumber}");
        Debug.Log($"Frame: {Time.frameCount}");

        int totalEarned = 0;
        int totalGiven = 0;

        List<RoundData> rounds = StatsManager.instance.GetAllRoundsData();

        Debug.Log($"Total rounds retrieved: {rounds.Count}");

        int roundIndex = 0;

        foreach (var round in rounds)
        {
            Debug.Log($"--- Processing Round {roundIndex} ---");
            Debug.Log($"HintGiverID: {round.hintGiverID}");
            Debug.Log($"Total guesses in this round: {round.guesses.Count}");

            // 🟢 1️⃣ When THIS player was a guesser
            if (round.hintGiverID != thisPlayerID)
            {
                Debug.Log("Checking if this player guessed in this round...");

                RoundGuess thisPlayerGuess =
                    round.guesses.Find(g => g.guesserID == thisPlayerID);

                if (thisPlayerGuess != null)
                {
                    Debug.Log($"Player was guesser. Score found: {thisPlayerGuess.guessScore}");
                    totalEarned += thisPlayerGuess.guessScore;
                    Debug.Log($"Updated totalEarned: {totalEarned}");
                }
                else
                {
                    Debug.Log("Player did NOT guess in this round.");
                }
            }

            // 🔵 2️⃣ When THIS player was the hint giver
            if (round.hintGiverID == thisPlayerID)
            {
                Debug.Log("Player WAS hint giver in this round.");

                foreach (var guess in round.guesses)
                {
                    Debug.Log($"Adding guess score from guesser {guess.guesserID}: {guess.guessScore}");
                    totalGiven += guess.guessScore;
                    Debug.Log($"Updated totalGiven: {totalGiven}");
                }
            }

            roundIndex++;
        }

        Debug.Log("=== FINAL TOTALS ===");
        Debug.Log($"Player {thisPlayerID} TOTAL EARNED: {totalEarned}");
        Debug.Log($"Player {thisPlayerID} TOTAL GIVEN: {totalGiven}");

        // 🖥 UI Update
        if (AtoBScore != null)
        {
            AtoBScore.text = totalEarned.ToString();
            Debug.Log($"AtoBScore UI updated -> {totalEarned}");
        }
        else
        {
            Debug.LogWarning("AtoBScore UI reference is NULL!");
        }

        if (BtoAScore != null)
        {
            BtoAScore.text = totalGiven.ToString();
            Debug.Log($"BtoAScore UI updated -> {totalGiven}");
        }
        else
        {
            Debug.LogWarning("BtoAScore UI reference is NULL!");
        }

        Debug.Log("Calling ShowPlayerBreakdown...");
        ShowPlayerBreakdown(thisPlayerID);

        Debug.Log("SetScores FINISHED");
        Debug.Log("====================================");
    }



    public void SetForOverall(int rank, string playerName, int totalScore, int allPlayersScore, bool isHost, int otherID)
    {
        Debug.Log($"=== SetForOverall: {playerName} (ID: {otherID}) - Score: {totalScore} ===");
        // ✅ Crown and scale based on rank
        if (crown != null) crown.SetActive(rank == 1);
        if (container != null) container.transform.localScale = rank == 1 ? Vector3.one * 1.2f : Vector3.one;

        SetScores(otherID);

        // Set rank
        if (rankText != null)
        {
            switch (rank)
            {
                case 1:
                    rankText.text = $"#{rank}st Place: {playerName} |";
                    if (subText != null) subText.text = $"BROBUDDY";
                    break;
                case 2:
                    rankText.text = $"{rank}nd Place: {playerName} |";
                    if (subText != null) subText.text = $"Co-pilot buddy";
                    break;
                case 3:
                    rankText.text = $"{rank}rd Place: {playerName} |";
                    if (subText != null) subText.text = $"Parallel class buddy";
                    break;
                case 4:
                    rankText.text = $"{rank}th Place: {playerName} |";
                    if (subText != null) subText.text = $"Bronze buddy";
                    break;
                case 5:
                    rankText.text = $"{rank}th Place: {playerName} |";
                    if (subText != null) subText.text = $"Spare tire buddy";
                    break;
                case 6:
                    rankText.text = $"{rank}th Place: {playerName} |";
                    if (subText != null) subText.text = $"Farmer buddy";
                    break;
                case 7:
                    rankText.text = $"{rank}th Place: {playerName} |";
                    if (subText != null) subText.text = $"Chill buddy";
                    break;
                case 8:
                    rankText.text = $"{rank}th Place: {playerName} |";
                    if (subText != null) subText.text = $"bit on the other side buddy";
                    break;
                case 9:
                    rankText.text = $"{rank}th Place: {playerName} |";
                    if (subText != null) subText.text = $"no words buddy";
                    break;
                case 10:
                    rankText.text = $"{rank}th Place: {playerName} |";
                    if (subText != null) subText.text = $"Mr. Nobuddy";
                    break;
                default:
                    rankText.text = $"{rank} Place: {playerName} |";
                    if (subText != null) subText.text = $"Mr. Nobuddy";
                    break;
            }
        }

        // Set player name
        if (nameText != null)
        {
            nameText.text = playerName;
            nameText.color = isHost ? hostColor : defaultColor;
            nameText.gameObject.SetActive(true);
        }

        // Set total score
        if (scoreText != null)
        {
            scoreText.text = totalScore.ToString();
            scoreText.color = isHost ? hostColor : defaultColor;
            scoreText.gameObject.SetActive(true);
        }

        // ✅ FIX: Calculate CORRECT percentage using THIS player's max possible
        if (perceText != null)
        {
            // Get max possible score for THIS specific player
            int maxPossible = StatsManager.instance.GetMaxPossibleScore(otherID);

            Debug.Log($"Player {otherID} ({playerName}): {totalScore}/{maxPossible} points");

            if (StatsManager.instance.maxScore >= 0)
            {
                float percentage = ((float)totalScore / StatsManager.instance.maxScore) * 100f;
                percentage = Mathf.CeilToInt(percentage);
                perceText.text = $"{percentage}%";
            }
            else
            {
                perceText.text = $"0%";
            }

            perceText.color = isHost ? hostColor : defaultColor;
            perceText.gameObject.SetActive(true);
        }

        // Show host badge
        if (hostBadge != null) hostBadge.SetActive(isHost);
    }

    public void SetAvatar(Sprite avatar)
    {
        if (avatarImage != null)
        {
            avatarImage.sprite = avatar;
        }
    }
}