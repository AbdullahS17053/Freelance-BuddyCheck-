using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

[System.Serializable]
public class FriendStats
{
    public int friendID;
    public string friendName;
    public int totalPointsAtoB;   // YOU → Friend
    public int totalPointsBtoA;   // Friend → YOU
    public int totalPossibleAtoB;     // For calculating % later
    public int totalPossibleBtoA;     // For calculating % later
    public int avatarIndex;
    public string lastPlayed;
}

public class PlayerStatistics : MonoBehaviour
{

    [System.Serializable]
    public class FriendStatsWrapper
    {
        public List<FriendStats> list = new List<FriendStats>();
    }


    public List<FriendStats> friendsStats = new List<FriendStats>();

    public static PlayerStatistics instance;

    [Header("Reset Options")]
    public bool hardResetOnStart = false;
    private string savePath => Application.persistentDataPath + "/friend_stats.json";

    [Header("UI for display")]
    public Image[] avatars;
    public TextMeshProUGUI[] names;
    public TextMeshProUGUI[] AtoB;
    public Slider[] AtoBslider;
    public TextMeshProUGUI[] AtoBtotal;
    public TextMeshProUGUI[] AtoBtotalPercent;
    public TextMeshProUGUI[] BtoA;
    public Slider[] BtoAslider;
    public TextMeshProUGUI[] BtoAtotal;
    public TextMeshProUGUI[] BtoAtotalPercent;
    public TextMeshProUGUI[] lastPlayed;

    private void Awake()
    {
        instance = this;

        if (hardResetOnStart)
        {
            HardReset();
        }

        LoadStats();
        UpdateDisplay();
    }

    public void UpdateAtoB(FriendStats FData)
    {
        // Try to find the friend
        FriendStats friend = friendsStats.Find(f => f.friendID == FData.friendID);

        // If friend does NOT exist → create and add a new one
        if (friend == null)
        {
            friend = new FriendStats()
            {
                friendID = FData.friendID,
                friendName = FData.friendName,
                avatarIndex = FData.avatarIndex,
                lastPlayed = System.DateTime.Now.ToString("dd MMM yyyy"),
                totalPointsAtoB = FData.totalPointsAtoB,
                totalPointsBtoA = FData.totalPointsBtoA,
                totalPossibleAtoB = FData.totalPossibleAtoB,
                totalPossibleBtoA = FData.totalPossibleBtoA
            };

            friendsStats.Add(friend);

            // Sort after adding
            friendsStats = friendsStats
                .OrderByDescending(f => f.totalPointsAtoB)
                .ToList();

            SaveStats();
            UpdateDisplay();
            return;
        }

        // If friend exists → update
        friend.friendName = FData.friendName;
        friend.avatarIndex = FData.avatarIndex;

        // System updates today's date
        friend.lastPlayed = System.DateTime.Now.ToString("dd MMM yyyy");

        // Increment values
        friend.totalPointsAtoB += FData.totalPointsAtoB;
        friend.totalPointsBtoA += FData.totalPointsBtoA;
        friend.totalPossibleAtoB += FData.totalPossibleAtoB;
        friend.totalPossibleBtoA += FData.totalPossibleBtoA;

        // Sort after updating
        friendsStats = friendsStats
            .OrderByDescending(f => f.totalPointsAtoB)
            .ToList();

        SaveStats();
        UpdateDisplay();
    }


    private void SaveStats()
    {
        FriendStatsWrapper wrapper = new FriendStatsWrapper();
        wrapper.list = friendsStats;

        string json = JsonUtility.ToJson(wrapper, true);
        System.IO.File.WriteAllText(savePath, json);

        Debug.Log("Stats saved to: " + savePath);
    }

    private void LoadStats()
    {
        if (!System.IO.File.Exists(savePath))
        {
            Debug.Log("No save file found, creating new data.");
            friendsStats = new List<FriendStats>();
            return;
        }

        string json = System.IO.File.ReadAllText(savePath);
        FriendStatsWrapper wrapper = JsonUtility.FromJson<FriendStatsWrapper>(json);

        friendsStats = wrapper.list;

        // sort after loading
        friendsStats = friendsStats.OrderByDescending(f => f.totalPointsAtoB).ToList();

        Debug.Log("Stats loaded from: " + savePath);
    }

    private void UpdateDisplay()
    {
        for (int i = 0; i < names.Length; i++)
        {
            if (i < friendsStats.Count)
            {
                FriendStats f = friendsStats[i];

                // avatar
                avatars[i].sprite = PlayerListUI.instance.GetAvatarSprite(f.avatarIndex);

                // name
                names[i].text = f.friendName;

                // A → B
                AtoB[i].text = f.totalPointsAtoB.ToString();
                AtoBslider[i].maxValue = f.totalPossibleAtoB;
                AtoBslider[i].value = f.totalPointsAtoB;
                AtoBtotal[i].text = f.totalPossibleAtoB.ToString();
                AtoBtotalPercent[i].text = CalculatePercent(f.totalPointsAtoB, f.totalPossibleAtoB);

                // B → A
                BtoA[i].text = f.totalPointsBtoA.ToString();
                BtoAslider[i].maxValue = f.totalPossibleBtoA;
                BtoAslider[i].value = f.totalPointsBtoA;
                BtoAtotal[i].text = f.totalPossibleBtoA.ToString();
                BtoAtotalPercent[i].text = CalculatePercent(f.totalPointsBtoA, f.totalPossibleBtoA);

                // Last Played
                lastPlayed[i].text = f.lastPlayed;

                // ensure UI is visible
                names[i].transform.parent.gameObject.SetActive(true);
            }
            else
            {
                // hide empty rows
                names[i].transform.parent.gameObject.SetActive(false);
            }
        }
    }
    private string CalculatePercent(int points, int possible)
    {
        if (possible <= 0) return "0%";

        float percent = (points / (float)possible) * 100f;
        return Mathf.RoundToInt(percent) + "%";
    }

    public void HardReset()
    {
        if (System.IO.File.Exists(savePath))
        {
            System.IO.File.Delete(savePath);
            Debug.Log("Hard reset: Deleted save file.");
        }

        friendsStats = new List<FriendStats>();
    }

}
