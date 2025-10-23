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
    [SerializeField] private AnimationCurve appearCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField] private GameObject confetti;
    [SerializeField] private GameObject clouds;
    [SerializeField] private Slider scoreMeter;

    private CanvasGroup canvasGroup;
    private Coroutine chatIndicatorCoroutine;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup) canvasGroup.alpha = 0;

        // Initialize chat indicator as hidden
        if (chatIndicator != null)
            chatIndicator.SetActive(false);
    }

    public void SetChatActivity(bool isActive, string lastMessage = "")
    {
        if (chatIndicatorCoroutine != null)
            StopCoroutine(chatIndicatorCoroutine);

        if (chatIndicator != null)
            chatIndicator.SetActive(isActive);

        if (lastMessageText != null && !string.IsNullOrEmpty(lastMessage))
        {
            lastMessageText.text = lastMessage.Length > 15 ?
                lastMessage.Substring(0, 15) + "..." : lastMessage;
        }

        // Auto-hide chat indicator after 3 seconds
        if (isActive)
        {
            chatIndicatorCoroutine = StartCoroutine(HideChatIndicatorAfterDelay(3f));
        }
    }

    private IEnumerator HideChatIndicatorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (chatIndicator != null)
            chatIndicator.SetActive(false);
    }

    public void updatePlayer(int playerPFP, string pName, bool host)
    {
        if (PlayerListUI.instance && pfp)
        {
            pfp.sprite = PlayerListUI.instance.GetAvatarSprite(playerPFP);
        }

        if (playerName)
        {
            playerName.text = pName;
        }

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

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(AnimateAppearance());
        }
    }

    private IEnumerator AnimateAppearance()
    {
        if (!canvasGroup || !gameObject.activeInHierarchy) yield break;

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

        if (confetti) confetti.SetActive(false);
        if (clouds) clouds.SetActive(false);

        if (canvasGroup) canvasGroup.alpha = 0;

        // Reset chat indicator
        if (chatIndicator != null)
            chatIndicator.SetActive(false);
    }

    public void Correct(bool correct)
    {
        if (correct)
        {
            if (scoreMeter.value < scoreMeter.maxValue)
            {
                scoreMeter.value++;
            }
            else
            {
                scoreMeter.transform.DOScale(new Vector3(2f, 2f, 2f), 0.5f);
            }
            if (confetti) confetti.SetActive(true);
            if (clouds) clouds.SetActive(false);
        }
        else
        {
            if (scoreMeter.value > scoreMeter.minValue)
            {
                scoreMeter.value--;
                scoreMeter.transform.DOScale(new Vector3(1.5f, 1.5f, 1.5f), 0.5f);
            }
            if (clouds) clouds.SetActive(true);
            if (confetti) confetti.SetActive(false);
        }
    }

    public void ResetAll()
    {
        scoreMeter.gameObject.SetActive(true);
        scoreMeter.transform.DOScale(Vector3.one, 0.5f);
        scoreMeter.value = Mathf.Round(scoreMeter.maxValue / 2);

        if (confetti) confetti.SetActive(false);
        if (clouds) clouds.SetActive(false);

        if (chatIndicator) chatIndicator.SetActive(false);
    }
}