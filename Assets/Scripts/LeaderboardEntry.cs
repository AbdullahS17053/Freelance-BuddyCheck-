using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardEntry : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text perceText;
    [SerializeField] private TMP_Text subText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private GameObject hostBadge;
    [SerializeField] private Color hostColor = Color.yellow;
    [SerializeField] private Color defaultColor = Color.black;


    public void Initialize(int rank, string playerName, int guess, bool isHost, string hint)
    {
        SetForRound(rank, playerName, guess, isHost, hint);
    }

    public void SetForRound(int rank, string playerName, int guess, bool isHost, string hint)
    {
        /*
        // Set rank
        if (rankText != null)
        {
            rankText.text = rank > 0 ? $"#{rank}" : "HOST";
            rankText.color = isHost ? hostColor : defaultColor;
            rankText.gameObject.SetActive(true);
        }

        // Set player name
        if (nameText != null)
        {
            nameText.text = playerName;
            nameText.color = isHost ? hostColor : defaultColor;
            nameText.gameObject.SetActive(true);
        }

        // Set guessed number
        if (guessText != null)
        {
            guessText.text = guess.ToString();
            guessText.color = isHost ? hostColor : defaultColor;
            guessText.gameObject.SetActive(true);
        }

        // Set hint
        if (hintText != null)
        {
            hintText.text = hint;
            hintText.color = isHost ? hostColor : defaultColor;
            hintText.gameObject.SetActive(true);
        }

        // Hide score for round view
        if (scoreText != null) scoreText.gameObject.SetActive(false);

        // Show host badge
        if (hostBadge != null) hostBadge.SetActive(isHost);*/
    }

    public void SetForOverall(int rank, string playerName, int totalScore, int allPlayersScore, bool isHost)
    {
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


            rankText.text = $"#{rank}";
            rankText.color = isHost ? hostColor : defaultColor;
            rankText.gameObject.SetActive(true);
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