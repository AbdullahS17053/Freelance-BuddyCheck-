using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

[System.Serializable]
public class FriendStats
{
    public int friendID;
    public string friendName;
    public int totalPointsAtoB;   // YOU → Friend (points you earned guessing their hints)
    public int totalPointsBtoA;   // Friend → YOU (points they earned guessing your hints)
    public int totalPossibleAtoB; // Max possible YOU could have earned
    public int totalPossibleBtoA; // Max possible they could have earned
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
    public int displayIndex;
    public GameObject removeFriendDisplay;
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

    [Header("Buddy Score UI")]
    public TextMeshProUGUI[] buddyScoreText;
    public TextMeshProUGUI[] buddyRankText;
    public TextMeshProUGUI[] buddyRankExtraText;


    private void Start()
    {
        instance = this;

        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

        if (hardResetOnStart)
            HardReset();

        LoadStats();
        UpdateDisplay();
    }

    private void OnLocaleChanged(Locale obj)
    {
        UpdateDisplay();
    }


    // ----------------------------
    // UPDATE / ADD FRIEND STATS
    // ----------------------------

    public void UpdateAtoB(FriendStats FData)
    {
        FriendStats friend = friendsStats.Find(f => f.friendID == FData.friendID);

        if (friend == null)
        {
            // New friend — create entry
            friend = new FriendStats()
            {
                friendID = FData.friendID,
                friendName = FData.friendName,
                avatarIndex = FData.avatarIndex,
                lastPlayed = DateTime.Now.ToString("dd MMM yyyy"),
                totalPointsAtoB = FData.totalPointsAtoB,
                totalPointsBtoA = FData.totalPointsBtoA,
                totalPossibleAtoB = FData.totalPossibleAtoB,
                totalPossibleBtoA = FData.totalPossibleBtoA
            };

            // ✅ Clamp on write so data is never corrupt from the start
            ClampFriendStats(friend);
            friendsStats.Add(friend);
        }
        else
        {
            // Existing friend — accumulate values
            friend.friendName = FData.friendName;
            friend.avatarIndex = FData.avatarIndex;
            friend.lastPlayed = DateTime.Now.ToString("dd MMM yyyy");

            friend.totalPointsAtoB += FData.totalPointsAtoB;
            friend.totalPointsBtoA += FData.totalPointsBtoA;
            friend.totalPossibleAtoB += FData.totalPossibleAtoB;
            friend.totalPossibleBtoA += FData.totalPossibleBtoA;

            // ✅ Clamp after accumulation so points never exceed possible
            ClampFriendStats(friend);
        }

        // ✅ Sort by buddy score
        friendsStats = friendsStats.OrderByDescending(f => GetBuddyScore(f)).ToList();

        SaveStats();
        UpdateDisplay();
    }

    // ✅ Points can never exceed possible — prevents >100% at the data level
    private void ClampFriendStats(FriendStats f)
    {
        if (f.totalPossibleAtoB > 0)
            f.totalPointsAtoB = Mathf.Min(f.totalPointsAtoB, f.totalPossibleAtoB);
        if (f.totalPossibleBtoA > 0)
            f.totalPointsBtoA = Mathf.Min(f.totalPointsBtoA, f.totalPossibleBtoA);
    }


    // ----------------------------
    // SAVE & LOAD
    // ----------------------------

    private void SaveStats()
    {
        try
        {
            FriendStatsWrapper wrapper = new FriendStatsWrapper { list = friendsStats };
            string json = JsonUtility.ToJson(wrapper, true);
            System.IO.File.WriteAllText(savePath, json);
            Debug.Log("Stats saved to: " + savePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save stats: " + e.Message);
        }
    }

    private void LoadStats()
    {
        try
        {
            if (!System.IO.File.Exists(savePath))
            {
                Debug.Log("No save file found, starting fresh.");
                friendsStats = new List<FriendStats>();
                return;
            }

            string json = System.IO.File.ReadAllText(savePath);
            FriendStatsWrapper wrapper = JsonUtility.FromJson<FriendStatsWrapper>(json);

            // ✅ Guard against corrupt or empty file
            friendsStats = wrapper?.list ?? new List<FriendStats>();

            // ✅ Clamp any corrupt values from older saves
            foreach (var f in friendsStats)
                ClampFriendStats(f);

            // ✅ Sort by buddy score
            friendsStats = friendsStats.OrderByDescending(f => GetBuddyScore(f)).ToList();

            // ✅ Persist sorted + clamped version back to disk immediately
            SaveStats();

            Debug.Log("Stats loaded. Friends count: " + friendsStats.Count);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to load stats: " + e.Message);
            friendsStats = new List<FriendStats>();
        }
    }


    // ----------------------------
    // DISPLAY
    // ----------------------------

    private void UpdateDisplay()
    {
        // ✅ FIX: Never mutate friendsStats — use a filtered local list for display only
        var displayList = friendsStats
            .Where(f => f.friendID != StatsManager.instance.myID)
            .ToList();

        for (int i = 0; i < names.Length; i++)
        {
            if (i < displayList.Count)
            {
                FriendStats f = displayList[i];

                // Avatar
                avatars[i].sprite = PlayerListUI.instance.GetAvatarSprite(f.avatarIndex);

                // Name
                names[i].text = f.friendName;

                // A → B (YOU guessing their hints)
                AtoB[i].text = f.totalPointsAtoB.ToString();
                AtoBslider[i].maxValue = Mathf.Max(f.totalPossibleAtoB, 1);
                AtoBslider[i].value = f.totalPointsAtoB;
                AtoBtotal[i].text = f.totalPossibleAtoB.ToString();
                // ✅ FIX: Use per-friend possible, not global maxScore
                AtoBtotalPercent[i].text = CalculatePercent(f.totalPointsAtoB, f.totalPossibleAtoB);

                // B → A (them guessing your hints)
                BtoA[i].text = f.totalPointsBtoA.ToString();
                BtoAslider[i].maxValue = Mathf.Max(f.totalPossibleBtoA, 1);
                BtoAslider[i].value = f.totalPointsBtoA;
                BtoAtotal[i].text = f.totalPossibleBtoA.ToString();
                // ✅ FIX: Use per-friend possible, not global maxScore
                BtoAtotalPercent[i].text = CalculatePercent(f.totalPointsBtoA, f.totalPossibleBtoA);

                // Last Played
                lastPlayed[i].text = f.lastPlayed;

                // Buddy Score + Rank
                float score = GetBuddyScore(f);
                BuddyRankInfo rank = GetRank(score);

                buddyScoreText[i].text = score + "%";

                bool isGerman = (Menus.instance.GetLanguageIndex() == 1);
                buddyRankText[i].text = isGerman ? $"#{i + 1} {rank.rankDE}" : $"#{i + 1} {rank.rankEN}";
                buddyRankExtraText[i].text = isGerman ? rank.extraDE : rank.extraEN;

                names[i].transform.parent.gameObject.SetActive(true);
                avatars[i].transform.parent.gameObject.transform.parent.gameObject.SetActive(true);
            }
            else
            {
                names[i].transform.parent.gameObject.SetActive(false);
                avatars[i].transform.parent.gameObject.transform.parent.gameObject.SetActive(false);
            }
        }
    }

    // ✅ Hard capped at 100%
    private string CalculatePercent(int points, int possible)
    {
        if (possible <= 0) return "0%";
        float percent = Mathf.Min(100f, (points / (float)possible) * 100f);
        return Mathf.RoundToInt(percent) + "%";
    }


    // ---------------------------------------------------------
    // BUDDY SCORE SYSTEM
    // ---------------------------------------------------------

    [System.Serializable]
    public class BuddyRankInfo
    {
        public string rankEN;
        public string rankDE;
        public string extraEN;
        public string extraDE;
        public int min;
        public int max;
    }

    public BuddyRankInfo[] buddyRanks = new BuddyRankInfo[]
    {
        new BuddyRankInfo {
            rankEN="Brobuddy",             rankDE="Brobuddy",
            extraEN="You two are basically sharing one brain.",
            extraDE="Ihr teilt euch im Grunde dasselbe Gehirn.",
            min=80, max=100
        },
        new BuddyRankInfo {
            rankEN="Co-Pilot Buddy",       rankDE="Beifahrerbuddy",
            extraEN="Always there, always on track.",
            extraDE="Immer da, immer auf Kurs.",
            min=65, max=79
        },
        new BuddyRankInfo {
            rankEN="Parallel-Class Buddy", rankDE="Parallelclassbuddy",
            extraEN="You know each other surprisingly well.",
            extraDE="Ihr kennt euch überraschend gut.",
            min=50, max=64
        },
        new BuddyRankInfo {
            rankEN="Bronze Buddy",         rankDE="Bronze buddy",
            extraEN="Not bad — but still some mysteries left.",
            extraDE="Nicht schlecht — aber es gibt noch einige Geheimnisse.",
            min=35, max=49
        },
        new BuddyRankInfo {
            rankEN="Spare Tire Buddy",     rankDE="Ersatzrad buddy",
            extraEN="Reliable… in emergencies.",
            extraDE="Zuverlässig… in Notfällen.",
            min=20, max=34
        },
        new BuddyRankInfo {
            rankEN="Mr. Nobuddy",          rankDE="Mr. Nobuddy",
            extraEN="Time for a coffee? You two could need it.",
            extraDE="Zeit für einen Kaffee? Ihr könntet ihn gebrauchen.",
            min=0, max=19
        }
    };

    private BuddyRankInfo GetRank(float score)
    {
        foreach (var r in buddyRanks)
            if (score >= r.min && score <= r.max)
                return r;

        return buddyRanks[buddyRanks.Length - 1];
    }

    // ✅ Every ratio and the final average are clamped to 100
    private float GetBuddyScore(FriendStats f)
    {
        float aToB = f.totalPossibleAtoB > 0
            ? Mathf.Min(100f, (f.totalPointsAtoB / (float)f.totalPossibleAtoB) * 100f)
            : 0f;

        float bToA = f.totalPossibleBtoA > 0
            ? Mathf.Min(100f, (f.totalPointsBtoA / (float)f.totalPossibleBtoA) * 100f)
            : 0f;

        return Mathf.Round(Mathf.Min(100f, (aToB + bToA) / 2f));
    }


    // ----------------------------
    // RESET
    // ----------------------------
    public void DeleteFriendByIndex(int displayIndex_)
    {
        displayIndex = displayIndex_;
        removeFriendDisplay.SetActive(true);
    }

    public void RemoveFriend()
    {
        var displayList = friendsStats
            .Where(f => f.friendID != StatsManager.instance.myID)
            .ToList();

        if (displayIndex < 0 || displayIndex >= displayList.Count)
        {
            Debug.LogWarning($"DeleteFriendByIndex: index {displayIndex} out of range");
            return;
        }

        int friendID = displayList[displayIndex].friendID;
        friendsStats.RemoveAll(f => f.friendID == friendID);
        friendsStats = friendsStats.OrderByDescending(f => GetBuddyScore(f)).ToList();
        SaveStats();
        UpdateDisplay();
        Debug.Log($"Deleted friend at display index {displayIndex} (ID: {friendID})");
    }

    public void HardReset()
    {
        if (System.IO.File.Exists(savePath))
        {
            System.IO.File.Delete(savePath);
            Debug.Log("Hard reset: save file deleted.");
        }
        friendsStats = new List<FriendStats>();
    }
}