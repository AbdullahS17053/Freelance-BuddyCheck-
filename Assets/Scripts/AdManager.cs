using UnityEngine;
using GoogleMobileAds.Api;
using System;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance;

    private InterstitialAd interstitial;
    private bool isInterstitialLoading = false;

    private RewardedAd rewarded;
    private bool isRewardedLoading = false;

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
        MobileAds.Initialize(_ => PreloadAds());
    }

    private void PreloadAds()
    {
        LoadInterstitial();
        LoadRewarded();
    }

    // ------------------- INTERSTITIAL -------------------
    private void LoadInterstitial()
    {
        if (isInterstitialLoading || interstitial != null) return;

        isInterstitialLoading = true;
        string adId = "ca-app-pub-3940256099942544/1033173712"; // Test ID

        InterstitialAd.Load(adId, new AdRequest(), (ad, error) =>
        {
            isInterstitialLoading = false;

            if (error != null || ad == null)
            {
                Debug.LogWarning("Interstitial failed to load: " + error);
                return;
            }

            interstitial = ad;

            interstitial.OnAdFullScreenContentClosed += () =>
            {
                interstitial = null;
                LoadInterstitial();
            };

            Debug.Log("Interstitial loaded.");
        });
    }

    public void ShowInterstitial()
    {
        if (interstitial != null)
        {
            interstitial.Show();
            interstitial = null;
            LoadInterstitial();
            if (noAdLoadedPanel) noAdLoadedPanel.SetActive(false);
        }
        else
        {
            Debug.Log("No interstitial loaded yet.");
            if (noAdLoadedPanel) noAdLoadedPanel.SetActive(true);
            LoadInterstitial();
        }
    }

    // ------------------- REWARDED -------------------
    private void LoadRewarded()
    {
        if (isRewardedLoading || rewarded != null) return;

        isRewardedLoading = true;
        string adId = "ca-app-pub-3940256099942544/5224354917"; // Test ID

        RewardedAd.Load(adId, new AdRequest(), (ad, error) =>
        {
            isRewardedLoading = false;

            if (error != null || ad == null)
            {
                Debug.LogWarning("Rewarded failed to load: " + error);
                if (noAdLoadedPanel) noAdLoadedPanel.SetActive(true);
                return;
            }

            rewarded = ad;

            rewarded.OnAdFullScreenContentClosed += () =>
            {
                rewarded = null;
                LoadRewarded();
            };

            Debug.Log("Rewarded loaded.");
        });
    }

    public void ShowRewarded(Action onReward = null)
    {
        if (rewarded != null && rewarded.CanShowAd())
        {
            rewarded.Show(reward =>
            {
                onReward?.Invoke();
            });

            rewarded = null;
            LoadRewarded();
            if (noAdLoadedPanel) noAdLoadedPanel.SetActive(false);
        }
        else
        {
            Debug.Log("Rewarded not loaded yet.");
            if (noAdLoadedPanel) noAdLoadedPanel.SetActive(true);
            LoadRewarded();
        }
    }

    // ------------------- PURCHASE -------------------
    public void BuyAds()
    {
        LoginManager.Instance.BuyFullVersion();
    }
    public void PurchasedSuccess()
    {
        if (boughtButton) boughtButton.SetActive(true);
    }
}
