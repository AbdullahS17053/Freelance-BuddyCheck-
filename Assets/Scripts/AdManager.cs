using UnityEngine;
using GoogleMobileAds.Api;
using System;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance;

    public bool disableAds = false;
    public GameObject testAds;

    private InterstitialAd interstitial;
    private RewardedAd rewarded;

    public GameObject noAdLoadedPanel;
    public GameObject boughtButton;

    private bool initializing = false;

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
        if (disableAds)
        {
            Debug.LogWarning("Ads are disabled via inspector.");
            return;
        }
        // Prevent activity race crash
        StartCoroutine(InitializeAdsDelayed());
    }

    private System.Collections.IEnumerator InitializeAdsDelayed()
    {
        if (initializing) yield break;
        initializing = true;

        yield return new WaitForSeconds(10f);

        MobileAds.Initialize(initStatus =>
        {
            LoadInterstitial();
            LoadRewarded();
        });
    }

    void OnApplicationPause(bool pause)
    {

    }

    // ------------------- INTERSTITIAL -------------------
    private void LoadInterstitial()
    {
        if (interstitial != null)
            return;

        string adId = "ca-app-pub-3940256099942544/1033173712"; // Test ID

        InterstitialAd.Load(adId, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("Failed to load interstitial: " + error);
                return;
            }

            interstitial = ad;

            interstitial.OnAdFullScreenContentClosed += () =>
            {
                DestroyInterstitial();
                LoadInterstitial();
            };

            interstitial.OnAdFullScreenContentFailed += (err) =>
            {
                DestroyInterstitial();
                LoadInterstitial();
            };

            Debug.Log("Interstitial loaded.");
        });
    }

    public void ShowInterstitial()
    {
        if (disableAds)
        {
            Debug.LogWarning("Ads are disabled via inspector.");
            testAds.SetActive(true);
            return;
        }
        if (interstitial != null)
        {
            interstitial.Show();
            if (noAdLoadedPanel) noAdLoadedPanel.SetActive(false);
        }
        else
        {
            Debug.Log("Interstitial not loaded yet.");
            if (noAdLoadedPanel) noAdLoadedPanel.SetActive(true);
            LoadInterstitial();
        }
    }

    private void DestroyInterstitial()
    {
        if (interstitial != null)
        {
            interstitial.Destroy();
            interstitial = null;
        }
    }


    // ------------------- REWARDED -------------------
    private void LoadRewarded()
    {
        if (disableAds)
        {
            Debug.LogWarning("Ads are disabled via inspector.");
            testAds.SetActive(true);
            return;
        }
        if (rewarded != null)
            return;

        string adId = "ca-app-pub-3940256099942544/5224354917"; // Test ID

        RewardedAd.Load(adId, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("Failed to load rewarded: " + error);
                if (noAdLoadedPanel) noAdLoadedPanel.SetActive(true);
                return;
            }

            rewarded = ad;

            rewarded.OnAdFullScreenContentClosed += () =>
            {
                DestroyRewarded();
                LoadRewarded();
            };

            rewarded.OnAdFullScreenContentFailed += (err) =>
            {
                DestroyRewarded();
                LoadRewarded();
            };

            Debug.Log("Rewarded loaded.");
        });
    }

    public void ShowRewarded(Action onReward = null)
    {
        if (disableAds)
        {
            Debug.LogWarning("Ads are disabled via inspector.");
            testAds.SetActive(true);
            return;
        }
        if (rewarded != null && rewarded.CanShowAd())
        {
            rewarded.Show(reward =>
            {
                onReward?.Invoke();
            });

            if (noAdLoadedPanel) noAdLoadedPanel.SetActive(false);
        }
        else
        {
            Debug.Log("Rewarded not loaded yet.");
            if (noAdLoadedPanel) noAdLoadedPanel.SetActive(true);
            LoadRewarded();
        }
    }

    private void DestroyRewarded()
    {
        if (rewarded != null)
        {
            rewarded.Destroy();
            rewarded = null;
        }
    }


    // ------------------- PURCHASE -------------------
    public void BuyAds()
    {
        // unchanged
        LoginManager.Instance.BuyFullVersion();
    }

    public void PurchasedSuccess()
    {
        // unchanged
        if (boughtButton) boughtButton.SetActive(true);
    }
}
