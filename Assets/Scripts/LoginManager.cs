using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.Purchasing;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

public class LoginManager : MonoBehaviour
{
#if UNITY_ANDROID
    public string GooglePlayGamesToken { get; private set; }
#endif

    public static LoginManager Instance;

    [Header("Web App URL")]
    public string webAppUrl = "https://script.google.com/macros/s/YOUR_SCRIPT_ID/exec";

    public string playerID;
    public bool signedIn;

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject UI_Loading;
    public GameObject doneButton;
    public GameObject SignInFailed;

    [Header("Debug")]
    public TextMeshProUGUI debugText; // Assign in inspector

    [HideInInspector] public int fullVersion;
    [HideInInspector] public bool privilagedUser;
    [HideInInspector] public bool guestMode;

    private void Awake()
    {
        Instance = this;
#if UNITY_ANDROID
        PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Activate();
        //StartSignIn();
#else
        //EnterGuestMode();
#endif
    }

    private void StartSignIn()
    {
#if UNITY_ANDROID
        LogDebug("Attempting Google Play Games sign-in...");
        PlayGamesPlatform.Instance.Authenticate((status) =>
        {
            if (status == SignInStatus.Success)
            {
                LogDebug("Sign-in success.");
                PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
                {
                    GooglePlayGamesToken = code;
                    playerID = PlayGamesPlatform.Instance.GetUserId();
                    signedIn = true;
                    guestMode = false;

                    LogDebug("PlayerID: " + playerID);
                    StartCoroutine(RegisterOrFetchFullVersion(playerID));
                });
                SignInFailed.SetActive(false);
            }
            else
            {
                LogDebug("Sign-in failed. Entering guest mode.");
                SignInFailed.SetActive(true);
                EnterGuestMode();
            }
        });
#endif
    }

    IEnumerator RegisterOrFetchFullVersion(string id)
    {
        if (UI_Loading != null) UI_Loading.SetActive(true);

        // Try login first
        WWWForm form = new WWWForm();
        form.AddField("action", "login");
        form.AddField("email", id);

        using (UnityWebRequest www = UnityWebRequest.Post(webAppUrl, form))
        {
            yield return www.SendWebRequest();

            bool success = false;

            if (www.result == UnityWebRequest.Result.Success)
            {
                LogDebug("Login response: " + www.downloadHandler.text);
                var parsed = UnityEngine.Purchasing.MiniJSON.Json.Deserialize(www.downloadHandler.text) as Dictionary<string, object>;
                if (parsed != null && parsed.ContainsKey("success") && parsed["success"].ToString() == "true")
                {
                    fullVersion = int.Parse(parsed["fullVersion"].ToString());
                    LogDebug("Login fetched fullVersion: " + fullVersion);
                    success = true;
                }
            }

            // If login fails, register
            if (!success)
            {
                LogDebug("Login failed or first-time user. Registering...");
                WWWForm regForm = new WWWForm();
                regForm.AddField("action", "register");
                regForm.AddField("email", id);

                using (UnityWebRequest regWWW = UnityWebRequest.Post(webAppUrl, regForm))
                {
                    yield return regWWW.SendWebRequest();

                    if (regWWW.result == UnityWebRequest.Result.Success)
                    {
                        LogDebug("Register response: " + regWWW.downloadHandler.text);
                        var parsedReg = UnityEngine.Purchasing.MiniJSON.Json.Deserialize(regWWW.downloadHandler.text) as Dictionary<string, object>;
                        if (parsedReg != null && parsedReg.ContainsKey("success") && parsedReg["success"].ToString() == "true")
                        {
                            fullVersion = int.Parse(parsedReg["fullVersion"].ToString());
                            LogDebug("Registration complete. FullVersion: " + fullVersion);
                        }
                        else
                        {
                            LogDebug("Registration failed, entering guest mode.");
                            EnterGuestMode();
                        }
                    }
                    else
                    {
                        LogDebug("Register request failed: " + regWWW.error + " Entering guest mode.");
                        EnterGuestMode();
                    }
                }
            }

            OpenMainMenu();
            if (UI_Loading != null) UI_Loading.SetActive(false);
        }
    }

    private void EnterGuestMode()
    {
        guestMode = true;
        playerID = "Guest_" + System.Guid.NewGuid().ToString("N");
        fullVersion = 0;
        privilagedUser = false;
        LogDebug("Guest mode activated with ID: " + playerID);
        OpenMainMenu();
    }

    public void BuyFullVersion()
    {
        StartCoroutine(BuyFullVersionRoutine());
        /*
        if (guestMode)
        {
            LogDebug("Guest tried to purchase. Retrying sign-in...");
            StartSignIn();
        }
        else
        {
            StartCoroutine(BuyFullVersionRoutine());
        }*/
    }

    IEnumerator BuyFullVersionRoutine()
    {

        UI_Loading.SetActive(true);
        yield return new WaitForSeconds(2);
        fullVersion = 1;
        UI_Loading.SetActive(false);


        /*
        if (UI_Loading != null) UI_Loading.SetActive(true);
        LogDebug("Sending buy full version request...");

        WWWForm form = new WWWForm();
        form.AddField("action", "buy");
        form.AddField("email", playerID);

        using (UnityWebRequest www = UnityWebRequest.Post(webAppUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                LogDebug("Buy response: " + www.downloadHandler.text);
                var parsed = UnityEngine.Purchasing.MiniJSON.Json.Deserialize(www.downloadHandler.text) as Dictionary<string, object>;
                if (parsed != null && parsed.ContainsKey("success") && parsed["success"].ToString() == "true")
                {
                    fullVersion = 1;
                    LogDebug("Full version unlocked!");
                    if (doneButton != null) doneButton.SetActive(true);
                }
            }
            else
            {
                LogDebug("Buy request failed: " + www.error);
            }
        }

        if (UI_Loading != null) UI_Loading.SetActive(false);
        */
    }

    private void OpenMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        LogDebug("Main Menu opened.");
    }

    private void LogDebug(string msg)
    {
        Debug.Log(msg);
        if (debugText != null)
        {
            debugText.text += msg + "\n";
        }
    }
}
