using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardEntry : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text guessText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private GameObject hostBadge;
    [SerializeField] private Color hostColor = Color.yellow;

    public void Initialize(int rank, string playerName, int guess, bool isHost, string hint)
    {
        // Set rank
        if (rankText != null)
        {
            rankText.text = rank > 0 ? $"#{rank}" : "HOST";
            rankText.color = isHost ? hostColor : Color.white;
        }

        // Set player name
        if (nameText != null)
        {
            nameText.text = playerName;
            nameText.color = isHost ? hostColor : Color.white;
        }

        // Set guessed number
        if (guessText != null)
        {
            guessText.text = guess.ToString();
            guessText.color = isHost ? hostColor : Color.white;
        }

        // Show host badge
        if (hostBadge != null)
        {
            hostBadge.SetActive(isHost);
        }
    }

    public void SetAvatar(Sprite avatar)
    {
        if (avatarImage != null)
        {
            avatarImage.sprite = avatar;
        }
    }
}