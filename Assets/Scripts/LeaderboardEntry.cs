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

    public void ShowGuessesForHintGiver(int hintGiverID)
    {
        // Hide all UI slots first
        for (int i = 0; i < guessers.Length; i++)
        {
            guessers[i]._name.gameObject.SetActive(false);
        }

        RoundData round = StatsManager.instance.roundsData
            .Find(r => r.hintGiverID == hintGiverID);

        if (round == null)
        {
            Debug.Log("No guesses found for this hint giver.");
            return;
        }

        int uiIndex = 0;

        foreach (var guess in round.guesses)
        {
            // Skip self
            if (guess.guesserID == hintGiverID)
                continue;

            // Stop if UI slots are full
            if (uiIndex >= guessers.Length)
                break;

            // ✅ FIXED: Removed the ! negation - now checks if info EXISTS
            if (StatsManager.instance.allPlayersInfo.TryGetValue(
                guess.guesserID, out PlayerInfo info))
            {
                // Fill UI
                guessers[uiIndex]._name.gameObject.SetActive(true);
                guessers[uiIndex]._name.text = info.playerName;
                guessers[uiIndex]._score.text = guess.guessScore.ToString();

                uiIndex++; // ✅ FIXED: Moved inside the if block
            }
            // ✅ If player info not found, skip this guess (no continue needed)
        }
    }

    /// <summary>
    /// Shows what THIS player guessed on OTHER people's hints (when they were a guesser)
    /// </summary>
    public void ShowMyGuessesOnOthers(int myPlayerID)
    {
        // Hide all UI slots first
        for (int i = 0; i < guessers.Length; i++)
        {
            guessers[i]._name.gameObject.SetActive(false);
        }

        // ✅ FIX: Get list of all OTHER players
        List<int> otherPlayerIDs = new List<int>();
        foreach (var kvp in StatsManager.instance.allPlayersInfo)
        {
            if (kvp.Key != myPlayerID)
            {
                otherPlayerIDs.Add(kvp.Key);
            }
        }

        int uiIndex = 0;

        // ✅ FIX: Loop through each other player and calculate points from them
        foreach (int hintGiverID in otherPlayerIDs)
        {
            if (uiIndex >= guessers.Length)
                break;

            // Get total points I earned from this hint giver
            int pointsFromThisPlayer = StatsManager.instance.GetPointsFromHintGiver(myPlayerID, hintGiverID);

            // ✅ FIX: ALWAYS show, even if 0 points
            if (StatsManager.instance.allPlayersInfo.TryGetValue(hintGiverID, out PlayerInfo hintGiverInfo))
            {
                guessers[uiIndex]._name.gameObject.SetActive(true);
                guessers[uiIndex]._name.text = hintGiverInfo.playerName;
                guessers[uiIndex]._score.text = pointsFromThisPlayer.ToString(); // Will show "0" if no points

                uiIndex++;
            }
        }
    }

    private void SetScores(int thisPlayerID)
    {
        // This is for the AtoB/BtoA scores at the top
        // Keep your existing logic here or simplify it

        // Get all round data once
        List<RoundData> rounds = StatsManager.instance.GetAllRoundsData();

        int AtoB = 0; // I guessed on thisPlayer's hint
        int BtoA = 0; // thisPlayer guessed on my hint

        foreach (var r in rounds)
        {
            // CASE 1: thisPlayer gave the hint → I guessed on it
            if (r.hintGiverID == thisPlayerID)
            {
                foreach (var g in r.guesses)
                {
                    if (g.guesserID == StatsManager.instance.myID)
                    {
                        AtoB += g.guessScore;
                    }
                }
            }

            // CASE 2: I gave the hint → thisPlayer guessed on it
            if (r.hintGiverID == StatsManager.instance.myID)
            {
                foreach (var g in r.guesses)
                {
                    if (g.guesserID == thisPlayerID)
                    {
                        BtoA += g.guessScore;
                    }
                }
            }
        }

        // Update UI
        if (AtoBScore != null) AtoBScore.text = AtoB.ToString();
        if (BtoAScore != null) BtoAScore.text = BtoA.ToString();

        // ✅ Show breakdown
        ShowMyGuessesOnOthers(thisPlayerID);
    }


    public void SetForOverall(int rank, string playerName, int totalScore, int allPlayersScore, bool isHost, int otherID)
    {
        SetScores(otherID);

        // Set rank
        if (rankText != null)
        {
            switch (rank)
            {
                case 1:
                    rankText.text = $"#{rank}st Place: {playerName} |";
                    subText.text = $"BROBUDDY";
                    break;
                case 2:
                    rankText.text = $"{rank}nd Place: {playerName} |";
                    subText.text = $"Co-pilot buddy";
                    break;
                case 3:
                    rankText.text = $"{rank}rd Place: {playerName} |";
                    subText.text = $"Parallel class buddy";
                    break;
                case 4:
                    rankText.text = $"{rank}th Place: {playerName} |";
                    subText.text = $"Bronze buddy";
                    break;
                case 5:
                    rankText.text = $"{rank}th Place: {playerName} |";
                    subText.text = $"Spare tire buddy";
                    break;
                case 6:
                    rankText.text = $"{rank}th Place: {playerName} |";
                    subText.text = $"Farmer buddy";
                    break;
                case 7:
                    rankText.text = $"{rank}th Place: {playerName} |";
                    subText.text = $"Chill buddy";
                    break;
                case 8:
                    rankText.text = $"{rank}th Place: {playerName} |";
                    subText.text = $"bit on the other side buddy";
                    break;
                case 9:
                    rankText.text = $"{rank}th Place: {playerName} |";
                    subText.text = $"no words buddy";
                    break;
                case 10:
                    rankText.text = $"{rank}th Place: {playerName} |";
                    subText.text = $"Mr. Nobuddy";
                    break;
                default:
                    rankText.text = $"{rank} Place: {playerName} |";
                    subText.text = $"Mr. Nobuddy";
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

        // Calculate % of total
        if (perceText != null)
        {
            if (allPlayersScore > 0)
            {
                float percentage = (float)totalScore / allPlayersScore * 100f;
                perceText.text = $"{percentage:F1}%";
            }
            else
            {
                perceText.text = "0%";
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