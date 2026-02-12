using NUnit.Framework;
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
        // Hide all UI slots first
        for (int i = 0; i < guessers.Length; i++)
        {
            guessers[i]._name.gameObject.SetActive(false);
        }

        // ✅ Get list of all OTHER players (potential hint givers)
        List<int> otherPlayerIDs = new List<int>();
        foreach (var kvp in StatsManager.instance.allPlayersInfo)
        {
            if (kvp.Key != playerID)
            {
                otherPlayerIDs.Add(kvp.Key);
            }
        }

        int uiIndex = 0;

        Debug.Log($"=== Breakdown for Player {playerID} ===");

        // ✅ Loop through each other player and show points earned from them
        foreach (int hintGiverID in otherPlayerIDs)
        {
            if (uiIndex >= guessers.Length)
                break;

            // Get total points THIS player earned from this hint giver
            int pointsFromThisHintGiver = StatsManager.instance.GetPointsFromHintGiver(playerID, hintGiverID);

            // ✅ ALWAYS show, even if 0 points
            if (StatsManager.instance.allPlayersInfo.TryGetValue(hintGiverID, out PlayerInfo hintGiverInfo))
            {
                guessers[uiIndex]._name.gameObject.SetActive(true);
                guessers[uiIndex]._name.text = hintGiverInfo.playerName;
                guessers[uiIndex]._score.text = pointsFromThisHintGiver.ToString();

                Debug.Log($"  {hintGiverInfo.playerName}: {pointsFromThisHintGiver} points");

                uiIndex++;
            }
        }

        Debug.Log($"===================");
    }

    private void SetScores(int thisPlayerID)
    {
        // ✅ FIX: Calculate THIS player's total scores (not relationship with me)
        int totalEarned = 0;  // Total points THIS player earned by guessing correctly
        int totalGiven = 0;   // Total points given to THIS player's hints

        List<RoundData> rounds = StatsManager.instance.GetAllRoundsData();

        foreach (var round in rounds)
        {
            // Calculate points THIS player earned (when they were a guesser)
            if (round.hintGiverID != thisPlayerID)
            {
                RoundGuess thisPlayerGuess = round.guesses.Find(g => g.guesserID == thisPlayerID);
                if (thisPlayerGuess != null)
                {
                    totalEarned += thisPlayerGuess.guessScore;
                }
            }

            // Calculate points given to THIS player's hints (when they were the hint giver)
            if (round.hintGiverID == thisPlayerID)
            {
                foreach (var guess in round.guesses)
                {
                    totalGiven += guess.guessScore;
                }
            }
        }

        // Update UI
        if (AtoBScore != null)
        {
            AtoBScore.text = totalEarned.ToString();
            Debug.Log($"Player {thisPlayerID} total earned: {totalEarned}");
        }

        if (BtoAScore != null)
        {
            BtoAScore.text = totalGiven.ToString();
            Debug.Log($"Player {thisPlayerID} total given: {totalGiven}");
        }

        // ✅ Show breakdown of where points came from
        ShowPlayerBreakdown(thisPlayerID);
    }


    public void SetForOverall(int rank, string playerName, int totalScore, int allPlayersScore, bool isHost, int otherID)
    {
        Debug.Log($"=== SetForOverall: {playerName} (ID: {otherID}) - Score: {totalScore} ===");

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

            if (StatsManager.instance.maxScore > 0)
            {
                float percentage = ((float)totalScore / StatsManager.instance.maxScore) * 100f;
                perceText.text = $"{percentage}%";
            }
            else
            {
                perceText.text = $"0% ({totalScore}/0)";
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