using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class GameProfileUpdate : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject hostCrown;
    public GameObject guessedAnswer;
    public Image pfp;
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI guessedNumber;

    [Header("Chat Indicator")]
    public GameObject chatIndicator;
    public TMP_Text lastMessageText;

    [Header("Animation")]
    [SerializeField] private float appearDelay = 0.1f;
    [SerializeField] private float appearDuration = 0.5f;
    [SerializeField]
    private AnimationCurve appearCurve =
        AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField] private GameObject confetti;
    [SerializeField] private GameObject clouds;
    [SerializeField] private Slider scoreMeter;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        // Start hidden
        if (canvasGroup) canvasGroup.alpha = 0;
    }

    public void SetChatActivity(bool isActive, string lastMessage = "")
    {
        if (chatIndicator != null)
            chatIndicator.SetActive(isActive);

        if (lastMessageText != null && !string.IsNullOrEmpty(lastMessage))
        {
            // Show abbreviated last message
            lastMessageText.text = lastMessage.Length > 15 ?
                lastMessage.Substring(0, 15) + "..." : lastMessage;
        }
    }

    public void updatePlayer(int playerPFP, string pName, bool host)
    {
        // Set player avatar
        if (PlayerListUI.instance && pfp)
        {
            pfp.sprite = PlayerListUI.instance.GetAvatarSprite(playerPFP);
        }

        // Set player name
        if (playerName)
        {
            playerName.text = pName;
        }

        // Set host status
        if (hostCrown)
        {
            hostCrown.SetActive(host);
        }
    }

    public void numberGuessed(string number)
    {
        if (guessedNumber)
        {
            guessedNumber.text = number;
        }
    }

    public void showAnswers()
    {
        scoreMeter.gameObject.SetActive(false);
        if (guessedAnswer)
        {
            guessedAnswer.SetActive(true);
        }

        // Animate appearance
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(AnimateAppearance());
        }
    }

    private IEnumerator AnimateAppearance()
    {
        if (!canvasGroup) yield break;
        if (!gameObject.activeInHierarchy) yield break;

        yield return new WaitForSeconds(appearDelay);

        float elapsed = 0;

        while (elapsed < appearDuration)
        {
            float t = elapsed / appearDuration;
            float curveValue = appearCurve.Evaluate(t);

            canvasGroup.alpha = Mathf.Lerp(0, 1, curveValue);

            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 1;
    }

    public void hideAnswers()
    {
        if (guessedAnswer)
        {
            guessedAnswer.SetActive(false);
        }
        scoreMeter.gameObject.SetActive(true);
        confetti.SetActive(false);
        clouds.SetActive(false);

        if (canvasGroup) canvasGroup.alpha = 0;
    }

    public void Correct(bool correct)
    {
        if (correct)
        {
            if(scoreMeter.value < scoreMeter.maxValue)
            {
                scoreMeter.value++;
            }
            else
            {
                scoreMeter.transform.DOScale(new Vector3(2f, 2f, 2f), 0.5f);
            }
                confetti.SetActive(true);
        }
        else
        {
            if (scoreMeter.value > scoreMeter.minValue)
            {
                scoreMeter.value--;

                scoreMeter.transform.DOScale(new Vector3(1.5f, 1.5f, 1.5f), 0.5f);
            }
                clouds.SetActive(true);
        }
    }

    public void ResetAll()
    {

        scoreMeter.gameObject.SetActive(true);
        scoreMeter.transform.DOScale(new Vector3(1.5f, 1.5f, 1.5f), 0.5f);
        scoreMeter.value = Mathf.Round(scoreMeter.maxValue / 2);

        confetti.SetActive(false);
        clouds.SetActive(false);
    }
}