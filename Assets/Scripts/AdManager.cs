using GoogleMobileAds.Api;
using Photon.Pun.Demo.PunBasics;
using System;
using UnityEngine;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance;

    public bool fakePurchaseStore;
    public GameObject fakePurchaseStorePanel;




#if UNITY_ANDROID
    public string bannerAdUnitId = "ca-app-pub-2445870976172222/1336179037";
    public string interstitialAdUnitId = "ca-app-pub-2445870976172222/7789906744";
    public string rewardedAdUnitId = "ca-app-pub-2445870976172222/1899285818";
    private string testBannerId = "ca-app-pub-3940256099942544/6300978111";
    private string testInterstitialId = "ca-app-pub-3940256099942544/1033173712";
    private string testRewardedId = "ca-app-pub-3940256099942544/5224354917";
#elif UNITY_IPHONE
    public string bannerAdUnitId = "your_real_banner_id_ios";
    public string interstitialAdUnitId = "your_real_interstitial_id_ios";
    public string rewardedAdUnitId = "your_real_rewarded_id_ios";
    private string testBannerId = "ca-app-pub-3940256099942544/2934735716";
    private string testInterstitialId = "ca-app-pub-3940256099942544/4411468910";
    private string testRewardedId = "ca-app-pub-3940256099942544/1712485313";
#else
    private string bannerAdUnitId = "unused";
    private string interstitialAdUnitId = "unused";
    private string rewardedAdUnitId = "unused";
#endif

    private BannerView bannerView;
    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;

    private bool bannerLoaded = false;
    private bool bannerVisible = false;


    public GameObject adsBoughtButton;
    public bool noAds = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        MobileAds.Initialize(initStatus =>
        {
            LoadInterstitialAd(interstitialAdUnitId);
            LoadRewardedAd(rewardedAdUnitId);
        });

        if (PlayerPrefs.HasKey("NoAds"))
        {
            CompleteFakePurchase();
        }
    }
    public void RunFakePurchase()
    {
        if (fakePurchaseStore)
        {
            fakePurchaseStorePanel.SetActive(true);
        }
    }
    public void CompleteFakePurchase()
    {
        if (fakePurchaseStore)
        {
            fakePurchaseStorePanel.SetActive(false);
            AdManager.Instance.NoAds();
            PlayerPrefs.SetInt("NoAds", 1);
        }
    }

    public void OnPurchaseComplete()
    {
        // Go Ad Free Here
        Debug.Log("Game is free now");
    }
    #region Banner
    private void LoadBannerAd(string adUnit)
    {
        bannerView = new BannerView(adUnit, AdSize.Banner, AdPosition.Bottom);
        AdRequest request = new AdRequest();

        bannerView.OnBannerAdLoaded += () =>
        {
            bannerLoaded = true;
            bannerView.Hide();
        };

        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            // if (adUnit != testBannerId) LoadBannerAd(testBannerId);
        };

        bannerView.LoadAd(request);
    }

    public void ToggleBanner()
    {
        if (noAds) return;

        if (!bannerLoaded)
        {
            LoadBannerAd(bannerAdUnitId);
            return;
        }

        if (bannerVisible)
        {
            bannerView.Hide();
            bannerVisible = false;
        }
        else
        {
            bannerView.Show();
            bannerVisible = true;
        }
    }
    #endregion

    #region Interstitial
    private void LoadInterstitialAd(string adUnit)
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        InterstitialAd.Load(adUnit, new AdRequest(), (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
               // if (adUnit != testInterstitialId) LoadInterstitialAd(testInterstitialId);
                return;
            }

            interstitialAd = ad;

            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                LoadInterstitialAd(adUnit);
            };

            interstitialAd.OnAdFullScreenContentFailed += (error) =>
            {

            };
        });
    }

    public void ShowInterstitialAd()
    {
        if (noAds) return;

        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
        }
        else
        {
            LoadInterstitialAd(interstitialAdUnitId);
        }
    }
    #endregion

    #region Rewarded
    private void LoadRewardedAd(string adUnit)
    {
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        RewardedAd.Load(adUnit, new AdRequest(), (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
               // if (adUnit != testRewardedId) LoadRewardedAd(testRewardedId);
                return;
            }

            rewardedAd = ad;

            rewardedAd.OnAdFullScreenContentClosed += () =>
            {
                LoadRewardedAd(adUnit);
            };

            rewardedAd.OnAdFullScreenContentFailed += (error) =>
            {

            };
        });
    }

    public void ShowRewardedAd()
    {
        if (noAds) return;

        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show((Reward reward) =>
            {

            });
        }
        else
        {

            LoadRewardedAd(rewardedAdUnitId);
        }
    }

    public void NoAds()
    {
        adsBoughtButton.SetActive(true);
        noAds = true;
        BuyAds();
    }
    #endregion


    // ------------------- PURCHASE -------------------
    public void BuyAds()
    {
        // unchanged
        LoginManager.Instance.BuyFullVersion();
    }
}
