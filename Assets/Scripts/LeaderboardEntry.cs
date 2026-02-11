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

            if (!StatsManager.instance.allPlayersInfo.TryGetValue(
                guess.guesserID, out PlayerInfo info))
                continue;

            // Fill UI
            guessers[uiIndex]._name.gameObject.SetActive(true);
            guessers[uiIndex]._name.text = info.playerName;
            guessers[uiIndex]._score.text = guess.guessScore.ToString();

            uiIndex++;
        }
    }

    private void SetScores(int thisPlayerID)
    {
        // Get all round data once
        List<RoundData> rounds = StatsManager.instance.GetAllRoundsData();

        // Local temporary values
        int AtoB = 0; // thisPlayer → me
        int BtoA = 0; // me → thisPlayer

        // Loop once through all rounds
        foreach (var r in rounds)
        {
            // CASE 1: this player gave the hint → I guessed
            if (r.hintGiverID == thisPlayerID)
            {
                foreach (var g in r.guesses)
                {
                    if (g.guesserID == StatsManager.instance.myID)
                    {
                        BtoA = g.guessScore;
                    }
                }
            }

            // CASE 2: I gave the hint → this player guessed
            if (r.hintGiverID == StatsManager.instance.myID)
            {
                foreach (var g in r.guesses)
                {
                    if (g.guesserID == thisPlayerID)
                    {
                        AtoB = g.guessScore;
                    }
                }
            }
        }

        // Update UI
        AtoBScore.text = AtoB.ToString();   // This player guessed my hint (A→B)
        BtoAScore.text = BtoA.ToString();   // I guessed this player’s hint (B→A)


        ShowGuessesForHintGiver(StatsManager.instance.myID);

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

        // Set total score - CRITICAL FIX HERE
        if (scoreText != null)
        {
            scoreText.text = totalScore.ToString(); // Display the actual score
            scoreText.color = isHost ? hostColor : defaultColor;
            scoreText.gameObject.SetActive(true); // Ensure it's active
        }

        // Calculate % of total
        if (perceText != null)
        {
            if (allPlayersScore > 0)
            {
                float percentage = (float)totalScore / allPlayersScore * 100f;
                perceText.text = $"{percentage:F1}%"; // one decimal place
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