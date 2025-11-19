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
    [SerializeField] private float appearDuration = 0.5f;
    [SerializeField] private GameObject confetti;
    [SerializeField] private GameObject clouds;
    [SerializeField] private Slider scoreMeter;

    private CanvasGroup canvasGroup;
    private Coroutine chatIndicatorCoroutine;
    private Coroutine autoHideParticlesCoroutine;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup) canvasGroup.alpha = 0;

        // Initialize chat indicator as hidden
        if (chatIndicator != null)
            chatIndicator.SetActive(false);

        // Ensure particles are off at start
        if (confetti != null)
        {
            confetti.SetActive(false);
            ParticleSystem confettiPS = confetti.GetComponent<ParticleSystem>();
            if (confettiPS != null) confettiPS.Stop();
        }
        if (clouds != null)
        {
            clouds.SetActive(false);
            ParticleSystem cloudsPS = clouds.GetComponent<ParticleSystem>();
            if (cloudsPS != null) cloudsPS.Stop();
        }
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

    public void updatePlayer(int playerPFP, string pName)
    {
        if (PlayerListUI.instance && pfp)
            pfp.sprite = PlayerListUI.instance.GetAvatarSprite(playerPFP);

        if (playerName)
            playerName.text = pName;

        /*
        if (hostCrown)
            hostCrown.SetActive(host);*/
    }

    public void numberGuessed(string number)
    {
        if (guessedNumber)
            guessedNumber.text = number;
    }

    public void showAnswers()
    {
        scoreMeter.gameObject.SetActive(false);
        if (guessedAnswer)
            guessedAnswer.SetActive(true);

        if (gameObject.activeInHierarchy)
            StartCoroutine(AnimateAppearance());
    }

    private IEnumerator AnimateAppearance()
    {
        if (!canvasGroup) yield break;

        float elapsed = 0;
        while (elapsed < appearDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / appearDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 1;
    }

    public void hideAnswers()
    {
        if (guessedAnswer)
            guessedAnswer.SetActive(false);

        scoreMeter.gameObject.SetActive(true);

        // Stop and hide particles
        StopAllParticles();

        if (canvasGroup) canvasGroup.alpha = 0;

        // Reset chat indicator
        if (chatIndicator != null)
            chatIndicator.SetActive(false);
    }

    private void StopAllParticles()
    {
        // Stop any running auto-hide coroutine
        if (autoHideParticlesCoroutine != null)
        {
            StopCoroutine(autoHideParticlesCoroutine);
            autoHideParticlesCoroutine = null;
        }

        // Stop and hide confetti
        if (confetti != null)
        {
            ParticleSystem ps = confetti.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            confetti.SetActive(false);
        }

        // Stop and hide clouds
        if (clouds != null)
        {
            ParticleSystem ps = clouds.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            clouds.SetActive(false);
        }
    }

    public void Correct(bool correct)
    {
        // Stop any previous particle effects
        StopAllParticles();

        if (scoreMeter == null) return;

        if (correct)
        {
            if(scoreMeter.value < 10)
            {
                // Increment meter
                scoreMeter.value = Mathf.Min(scoreMeter.value + 1, scoreMeter.maxValue);
            }

            // Show confetti
            if (confetti != null)
            {
                confetti.SetActive(true);
                ParticleSystem ps = confetti.GetComponent<ParticleSystem>();
                if (ps != null) ps.Play();

                autoHideParticlesCoroutine = StartCoroutine(AutoHideParticlesAfterDelay(2f));
            }
        }
        else
        {
            if(scoreMeter.value > 0)
            {
                // Decrement meter
                scoreMeter.value = Mathf.Max(scoreMeter.value - 1, scoreMeter.minValue);
            }

            // Show clouds
            if (clouds != null)
            {
                clouds.SetActive(true);
                ParticleSystem ps = clouds.GetComponent<ParticleSystem>();
                if (ps != null) ps.Play();

                autoHideParticlesCoroutine = StartCoroutine(AutoHideParticlesAfterDelay(2f));
            }
        }
    }

    public void ResetSlider()
    {
        scoreMeter.value = 5;
    }

    private IEnumerator AutoHideParticlesAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StopAllParticles();
    }

    void OnDisable()
    {
        // Ensure particles are stopped when object is disabled
        StopAllParticles();
    }
}