using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatMessageUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text messageText;
    public TMP_Text senderText;
    public TMP_Text timeText;
    public Image backgroundImage;

    [Header("Colors")]
    public Color localPlayerColor = new Color(0.2f, 0.6f, 1f, 0.3f);
    public Color otherPlayerColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
    public Color hostPlayerColor = new Color(1f, 0.8f, 0.2f, 0.3f);

    public void Initialize(string sender, string message, string time, bool isLocalPlayer, bool isHost)
    {
        if (messageText != null) messageText.text = message;
        if (senderText != null) senderText.text = sender;
        if (timeText != null) timeText.text = time;

        // Set background color based on player type
        if (backgroundImage != null)
        {
            if (isLocalPlayer)
                backgroundImage.color = localPlayerColor;
            else if (isHost)
                backgroundImage.color = hostPlayerColor;
            else
                backgroundImage.color = otherPlayerColor;
        }

        // Auto-size the layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }
}