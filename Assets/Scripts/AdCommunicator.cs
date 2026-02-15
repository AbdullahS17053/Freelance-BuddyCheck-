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
    public void TryStartGame()
    {
        if(LoginManager.Instance.fullVersion != 1)
        {
            Debug.Log("No buyer in room → ads");
            ShowLongAd();
        }
    }


    /// <summary>
    /// Call this at the end of a round
    /// </summary>
    public void ShowEndOfRoundAd()
    {

        ShowShortAd();
    }


    // ----------------------------
    // Mock ad methods
    // ----------------------------
    private void ShowLongAd()
    {
        Debug.Log("Showing LONG rewarded ad...");

        if(LoginManager.Instance.fullVersion != 1 || LoginManager.Instance.privilagedUser)
        {
            // Replace with actual ad SDK call
            AdManager.Instance.ShowRewardedAd();
        }
    }

    private void ShowShortAd()
    {
        if (LoginManager.Instance.fullVersion != 1 || LoginManager.Instance.privilagedUser)
        {
            // Replace with actual ad SDK call
            AdManager.Instance.ShowInterstitialAd();
        }
        Debug.Log("Showing SHORT interstitial ad...");
    }
}
