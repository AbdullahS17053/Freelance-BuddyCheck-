using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Video;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;
using System.Collections;

public class Menus : MonoBehaviour
{
    public static Menus instance;

    // Language settings
    public enum LanguageOption { English, German, Spanish }
    public LanguageOption currentLanguage = LanguageOption.English;
    private Locale GetLocaleFromEnum(LanguageOption lang)
    {
        switch (lang)
        {
            case LanguageOption.English:
                return LocalizationSettings.AvailableLocales.Locales.Find(l => l.Identifier.Code == "en");
            case LanguageOption.German:
                return LocalizationSettings.AvailableLocales.Locales.Find(l => l.Identifier.Code == "de");
            case LanguageOption.Spanish:
                return LocalizationSettings.AvailableLocales.Locales.Find(l => l.Identifier.Code == "es");
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
    public VideoPlayer videoPlayer;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        LoadSettings();

        if (PlayerPrefs.HasKey("EULA"))
        {
            EULA.SetActive(false);
        }
        else
        {
            EULA.SetActive(true);
        }

        videoPlayer.loopPointReached += OnVideoEnd;
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        StartCoroutine(DisableAfterDelay(vp));
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
        // Load language
        if (PlayerPrefs.HasKey("language"))
        {
            currentLanguage = (LanguageOption)PlayerPrefs.GetInt("language");
            if (languageDropdown != null)
                languageDropdown.value = (int)currentLanguage;
            Debug.Log("Loaded language: " + currentLanguage);
        }

        // Load region
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
