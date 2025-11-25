using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

public class Menus : MonoBehaviour
{
    // Language settings
    public enum LanguageOption { English, German, Spanish }
    public LanguageOption currentLanguage = LanguageOption.English;

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

    void Start()
    {
        LoadSettings();

        if (PlayerPrefs.HasKey("EULA"))
        {
            if(PlayerPrefs.GetInt("EULA") == 1)
            {
                EULA.SetActive(true);
            }
            else
            {
                EULA.SetActive(false);
            }
        }
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

        SaveSettings();
    }

    public void ToggleAudio()
    {
        
    }

    public void ToggleMusic()
    {
        MusicManager.instance.StartStopMusic(musicToggle.isOn);
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
