using Photon.Pun;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.Video;
using Image = UnityEngine.UI.Image;

public class Menus : MonoBehaviour
{
    public static Menus instance;

    // Language settings
    public enum LanguageOption { English, Deutsch, Español, Français }

    public LanguageOption currentLanguage = LanguageOption.English;
    private Locale GetLocaleFromEnum(LanguageOption lang)
    {
        switch (lang)
        {
            case LanguageOption.English:
                return LocalizationSettings.AvailableLocales.Locales.Find(l => l.Identifier.Code == "en");
            case LanguageOption.Deutsch:
                return LocalizationSettings.AvailableLocales.Locales.Find(l => l.Identifier.Code == "de");
            case LanguageOption.Español:
                return LocalizationSettings.AvailableLocales.Locales.Find(l => l.Identifier.Code == "es");
            case LanguageOption.Français:
                return LocalizationSettings.AvailableLocales.Locales.Find(l => l.Identifier.Code == "en");
            default:
                return LocalizationSettings.AvailableLocales.Locales[0]; // fallback
        }
    }


    public GameObject EULA;

    public UnityEngine.UI.Toggle audioToggle;
    public UnityEngine.UI.Toggle musicToggle;

    public int currentRegion;
    public Image regionFlag;
    public RegionOption[] regions;
    public TextMeshProUGUI currentRegionText;

    [System.Serializable]
    public class RegionOption
    {
        public string regions;
    }

    [Header("UI Elements")]

    public TMP_Dropdown languageDropdown;

    [Header("Links")]
    public string termsURL = "https://buddycheck.app/terms-and-conditions-english";
    public string privacyURL = "https://buddycheck.app/privacy-english";
    public string aboutURL = "https://buddycheck.app/impressum";
    public GameObject signIn;
    public VideoPlayer videoPlayer;

    private void Awake()
    {
        videoPlayer.gameObject.SetActive(true);

        LoadSettings();
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        PhotonNetwork.KeepAliveInBackground = 60f;
        Application.runInBackground = true;
    }

    void Start()
    {

        if (PlayerPrefs.HasKey("EULA"))
        {
            EULA.SetActive(false);
        }
        else
        {
            EULA.SetActive(true);
        }

        signIn.SetActive(true);
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        signIn.SetActive(true);
        
        
        //StartCoroutine(DisableAfterDelay(vp));
    }

    private IEnumerator DisableAfterDelay(VideoPlayer vp)
    {
        yield return new WaitForSeconds(1f);
        vp.gameObject.SetActive(false);
    }

    public void AcceptEULA()
    {
        PlayerPrefs.SetInt("EULA", 1);
        EULA.SetActive(false);
    }

    #region Settings Functions


    private void SaveSettings()
    {
        PlayerPrefs.SetInt("language", (int)currentLanguage);
        PlayerPrefs.SetInt("region", currentRegion);
        PlayerPrefs.Save();
        Debug.Log("Settings saved!");
    }

    private void LoadSettings()
    {
        // ---------------------------------------------------------
        // 1) FIRST APP LAUNCH → Detect system language
        // ---------------------------------------------------------
        if (!PlayerPrefs.HasKey("language"))
        {
            if (Application.systemLanguage == SystemLanguage.German)
            {
                currentLanguage = LanguageOption.Deutsch;
            }
            else if (Application.systemLanguage == SystemLanguage.Spanish)
            {
                currentLanguage = LanguageOption.Español;
            }
            else if (Application.systemLanguage == SystemLanguage.French)
            {
                currentLanguage = LanguageOption.Français;
            }
            else
            {
                currentLanguage = LanguageOption.English; // default
            }

            // Save immediately so future launches don't override user choice
            PlayerPrefs.SetInt("language", (int)currentLanguage);
            PlayerPrefs.Save();

            Debug.Log("Auto-detected device language: " + currentLanguage);
        }
        else
        {
            // ---------------------------------------------------------
            // 2) USER ALREADY PICKED A LANGUAGE BEFORE → LOAD IT
            // ---------------------------------------------------------
            currentLanguage = (LanguageOption)PlayerPrefs.GetInt("language");
            Debug.Log("Loaded saved language: " + currentLanguage);
        }

        // Apply to dropdown (if exists)
        if (languageDropdown != null)
            languageDropdown.value = (int)currentLanguage;


        // ---------------------------------------------------------
        // REGION STUFF (unchanged)
        // ---------------------------------------------------------
        if (PlayerPrefs.HasKey("region"))
        {
            currentRegion = PlayerPrefs.GetInt("region");
            if (currentRegion >= 0 && currentRegion <= regions.Length)
            {
                currentRegionText.text = regions[currentRegion].regions;
            }
            Debug.Log("Loaded region: " + currentRegion);
        }

        InitializeSettingsUI();

        // ---------------------------------------------------------
        // APPLY LANGUAGE TO LOCALIZATION SYSTEM
        // ---------------------------------------------------------
        Locale locale = GetLocaleFromEnum(currentLanguage);
        LocalizationSettings.SelectedLocale = locale;
    }


    void InitializeSettingsUI()
    {
        if (languageDropdown != null)
            languageDropdown.value = (int)currentLanguage;


        if (currentRegionText != null)
            currentRegionText.text = regions[currentRegion].regions;
    }

    public void SetLanguage(int languageIndex)
    {
        currentLanguage = (LanguageOption)languageIndex;

        // Set Unity Localization
        Locale locale = GetLocaleFromEnum(currentLanguage);
        if (locale != null)
        {
            LocalizationSettings.SelectedLocale = locale;
        }

        SaveSettings();
    }
    public int GetLanguageIndex()
    {
        return (int)currentLanguage;
    }


    public void ToggleAudio()
    {
        
    }

    public void ToggleMusic()
    {
        MusicManager.instance.StartStopMusic(!musicToggle.isOn);
    }

    public void SetRegion(int region)
    {
        currentRegion = region;
        currentRegionText.text = regions[region].regions;

        Debug.Log("Saved region: " + currentRegion);

        SaveSettings();
    }

    #endregion

    #region Links Functions

    public void OpenTerms()
    {
        Application.OpenURL(termsURL);
    }

    public void OpenPrivacy()
    {
        Application.OpenURL(privacyURL);
    }

    public void OpenAbout()
    {
        Application.OpenURL(aboutURL);
    }

    #endregion
}
