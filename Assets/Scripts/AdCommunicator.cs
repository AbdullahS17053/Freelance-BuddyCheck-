using System;
using ExitGames.Client.Photon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class AdCommunicator : MonoBehaviourPunCallbacks
{
    public static AdCommunicator Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ----------------------------
    // Public interface
    // ----------------------------

    /// <summary>
    /// Call this at the start of a game
    /// </summary>
    public void TryStartGame(Action onGameStart)
    {
        if(LoginManager.Instance.fullVersion == 1)
        {
            Debug.Log("Buyer in room → No ads, start game immediately");
            onGameStart?.Invoke();
            return;
        }
        else
        {
            ShowLongAd(() =>
            {
                onGameStart?.Invoke();
            });
        }
    }


    /// <summary>
    /// Call this at the end of a round
    /// </summary>
    public void ShowEndOfRoundAd()
    {
        // Show short interstitial ad (5-7 sec skippable)
        ShowShortAd();
    }


    // ----------------------------
    // Mock ad methods
    // ----------------------------
    private void ShowLongAd(Action onComplete)
    {
        Debug.Log("Showing LONG rewarded ad...");

        if(LoginManager.Instance.fullVersion != 1)
        {
            // Replace with actual ad SDK call
            AdManager.Instance.ShowRewarded(() =>
            {
                onComplete?.Invoke();
            });
        }
    }

    private void ShowShortAd()
    {
        Debug.Log("Showing SHORT interstitial ad...");

        // Replace with actual ad SDK call
        AdManager.Instance.ShowInterstitial();
    }
}
