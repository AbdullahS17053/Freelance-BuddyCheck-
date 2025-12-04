using UnityEngine;
using GoogleMobileAds.Api;
using System;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance;

    private InterstitialAd interstitial;
    private InterstitialAd interstitialBuffer;

    private RewardedAd rewarded;

    public GameObject noAdLoadedPanel;
    public GameObject boughtButton;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        MobileAds.Initialize(_ => {
            PreloadAds();
        });
    }

    void PreloadAds()
    {
        LoadInterstitialMain();
        LoadInterstitialBuffer();
        LoadRewarded();
    }

    // -----------------------------------------------------
    // LOAD MAIN INTERSTITIAL
    // -----------------------------------------------------
    void LoadInterstitialMain()
    {
        string adId = "ca-app-pub-3940256099942544/1033173712";

        InterstitialAd.Load(adId, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("Main interstitial failed to load: " + error);
                return;
            }

            interstitial = ad;

            interstitial.OnAdFullScreenContentClosed += () =>
            {
                Time.timeScale = 1f;
                LoadInterstitialMain();
            };
        });
    }

    // -----------------------------------------------------
    // LOAD BUFFER INTERSTITIAL
    // -----------------------------------------------------
    void LoadInterstitialBuffer()
    {
        string adId = "ca-app-pub-3940256099942544/1033173712";

        InterstitialAd.Load(adId, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("Buffer interstitial failed to load: " + error);
                return;
            }

            interstitialBuffer = ad;

            interstitialBuffer.OnAdFullScreenContentClosed += () =>
            {
                Time.timeScale = 1f;
                LoadInterstitialBuffer();
            };
        });
    }

    // -----------------------------------------------------
    // SHOW INTERSTITIAL
    // -----------------------------------------------------
    public void ShowInterstitial()
    {
        if (interstitial != null)
        {
            Time.timeScale = 0f;
            interstitial.Show();
            interstitial = null;
            return;
        }

        if (interstitialBuffer != null)
        {
            Time.timeScale = 0f;
            interstitialBuffer.Show();
            interstitialBuffer = null;
            return;
        }

        if (noAdLoadedPanel) noAdLoadedPanel.SetActive(true);

        Debug.Log("No interstitial loaded.");
    }

    // -----------------------------------------------------
    // LOAD REWARDED
    // -----------------------------------------------------
    void LoadRewarded()
    {
        string adId = "ca-app-pub-3940256099942544/5224354917";

        RewardedAd.Load(adId, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("Rewarded failed to load: " + error);
                return;
            }

            rewarded = ad;

            rewarded.OnAdFullScreenContentClosed += () =>
            {
                Time.timeScale = 1f;
                LoadRewarded();
            };
        });
    }

    // -----------------------------------------------------
    // SHOW REWARDED
    // -----------------------------------------------------
    public void ShowRewarded(Action onReward)
    {
        if (rewarded == null)
        {
            if (noAdLoadedPanel) noAdLoadedPanel.SetActive(true);
            Debug.Log("Rewarded not loaded.");
            return;
        }

        Time.timeScale = 0f;

        rewarded.Show(reward =>
        {
            onReward?.Invoke();
        });

        rewarded = null;
    }


    public void BuyAds()
    {
        boughtButton.SetActive(true);
        LoginManager.Instance.BuyFullVersion();
    }
}
