using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

/// <summary>
/// Small utility class (email validation)
/// </summary>
public static class Utils
{
    // Returns true if input looks like a valid email
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return false;
        // Simple regex for email validation
        string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, pattern);
    }
}

/// <summary>
/// Robust LoginManager that communicates with Google Apps Script endpoint.
/// Handles Editor and Build differences by cleaning server response and extracting fields safely.
/// </summary>
public class LoginManager : MonoBehaviour
{
    public static LoginManager Instance;

    [Header("Web App URL")]
    public string webAppUrl = "https://script.google.com/macros/s/AKfycbxTk_86k7ziXLH_I7RrNuE0h03BAbFnccZLZUgin5W32KEq_cR2JZ_GvO8QJ0smtiNi/exec";

    [Header("UI Panels")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject mainMenuPanel;
    public GameObject UI_Loading;
    public GameObject doneButton;

    [Header("Login Fields")]
    public TMP_InputField loginEmailField;
    public TMP_InputField loginPasswordField;
    public GameObject loginErrorUI;

    [Header("Register Fields")]
    public TMP_InputField registerEmailField;
    public TMP_InputField registerPasswordField;
    public GameObject registerErrorUI;

    [Header("Forgot Password")]
    public string recoveryEmail = "dash.clientwork@gmail.com";

    // Local Player Data
    [HideInInspector] public string userEmail;
    [HideInInspector] public string userPassword;
    [HideInInspector] public int fullVersion;
    public bool privilagedUser = false;

    // Regex cache
    private static readonly Regex keyRegexTemplate = new Regex("\"{0}\"\\s*:\\s*(?:\"(?<str>[^\"]*)\"|(?<word>[^,}\\s]+))", RegexOptions.Compiled);

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Try auto-login from PlayerPrefs
        if (PlayerPrefs.HasKey("email") && PlayerPrefs.HasKey("password"))
        {
            userEmail = PlayerPrefs.GetString("email");
            userPassword = PlayerPrefs.GetString("password");
            fullVersion = PlayerPrefs.GetInt("fullVersion", 0);

            StartCoroutine(LoginRoutine(userEmail, userPassword, true));
        }
        else
        {
            // show login UI if assigned
            if (loginPanel != null) loginPanel.SetActive(true);
        }
    }

    // ---------------------------
    // Public UI hooks
    // ---------------------------
    public void OnLoginButton()
    {
        string email = loginEmailField.text.Trim().ToLower();
        string password = loginPasswordField.text.Trim();

        if (!Utils.IsValidEmail(email))
        {
            Debug.Log("Invalid email format!");
            if (loginErrorUI != null) loginErrorUI.SetActive(true);
            return;
        }

        StartCoroutine(LoginRoutine(email, password, false));
    }

    public void OnRegisterButton()
    {
        string email = registerEmailField.text.Trim().ToLower();
        string password = registerPasswordField.text.Trim();

        if (!Utils.IsValidEmail(email))
        {
            Debug.Log("Invalid email format!");
            if (registerErrorUI != null) registerErrorUI.SetActive(true);
            return;
        }

        StartCoroutine(RegisterRoutine(email, password));
    }

    public void OnForgotPasswordButton()
    {
        Application.OpenURL($"mailto:{recoveryEmail}?subject=Password Recovery Request");
    }

    // ---------------------------
    // Network Routines
    // ---------------------------
    IEnumerator LoginRoutine(string email, string password, bool silentLogin)
    {
        if (UI_Loading != null) UI_Loading.SetActive(true);

        WWWForm form = new WWWForm();
        form.AddField("action", "login");
        form.AddField("email", email);
        form.AddField("password", password);

        using (UnityWebRequest www = UnityWebRequest.Post(webAppUrl, form))
        {
            www.timeout = 15;
            yield return www.SendWebRequest();

            // Always hide loading at the end
            try
            {
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("Login request failed: " + www.error);
                    if (!silentLogin && loginErrorUI != null) loginErrorUI.SetActive(true);
                    if (loginPanel != null) loginPanel.SetActive(true);
                    yield break;
                }

                string raw = www.downloadHandler.text;
                Debug.Log("[Login] Server response: " + raw);

                var parsed = ParseServerJson(raw);
                string successStr = GetParsedValue(parsed, "success");
                string emailResp = GetParsedValue(parsed, "email");
                string fullVerStr = GetParsedValue(parsed, "fullVersion");
                string errorResp = GetParsedValue(parsed, "error");

                bool ok = IsTrue(successStr);

                if (ok)
                {
                    // successful login
                    loginEmailField.text = "";
                    loginPasswordField.text = "";

                    userEmail = !string.IsNullOrEmpty(emailResp) ? emailResp : email;
                    userPassword = password;

                    if (!int.TryParse(fullVerStr, out fullVersion))
                        fullVersion = 0;

                    if (fullVersion == 1 && doneButton != null)
                        doneButton.SetActive(true);

                    SavePlayerData();
                    OpenMainMenu();
                }
                else
                {
                    Debug.LogWarning("[Login] Failed: " + errorResp);
                    if (!silentLogin && loginErrorUI != null) loginErrorUI.SetActive(true);
                    if (loginPanel != null) loginPanel.SetActive(true);
                }
            }
            finally
            {
                if (UI_Loading != null) UI_Loading.SetActive(false);
            }
        }
    }

    IEnumerator RegisterRoutine(string email, string password)
    {
        if (UI_Loading != null) UI_Loading.SetActive(true);

        WWWForm form = new WWWForm();
        form.AddField("action", "register");
        form.AddField("email", email);
        form.AddField("password", password);

        using (UnityWebRequest www = UnityWebRequest.Post(webAppUrl, form))
        {
            www.timeout = 15;
            yield return www.SendWebRequest();

            try
            {
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("Register request failed: " + www.error);
                    if (registerErrorUI != null) registerErrorUI.SetActive(true);
                    yield break;
                }

                string raw = www.downloadHandler.text;
                Debug.Log("[Register] Server response: " + raw);

                var parsed = ParseServerJson(raw);
                string successStr = GetParsedValue(parsed, "success");
                string fullVerStr = GetParsedValue(parsed, "fullVersion");
                string errorResp = GetParsedValue(parsed, "error");

                bool ok = IsTrue(successStr);

                if (ok)
                {
                    userEmail = email;
                    userPassword = password;

                    if (!int.TryParse(fullVerStr, out fullVersion))
                        fullVersion = 0;

                    registerEmailField.text = "";
                    registerPasswordField.text = "";

                    SavePlayerData();
                    OpenMainMenu();
                }
                else
                {
                    Debug.LogWarning("[Register] Failed: " + errorResp);
                    if (registerErrorUI != null) registerErrorUI.SetActive(true);
                }
            }
            finally
            {
                if (UI_Loading != null) UI_Loading.SetActive(false);
            }
        }
    }

    IEnumerator BuyFullVersionRoutine()
    {
        if (string.IsNullOrEmpty(userEmail))
        {
            Debug.LogWarning("BuyFullVersion: no user email set");
            yield break;
        }

        if (UI_Loading != null) UI_Loading.SetActive(true);

        WWWForm form = new WWWForm();
        form.AddField("action", "buy");
        form.AddField("email", userEmail);

        using (UnityWebRequest www = UnityWebRequest.Post(webAppUrl, form))
        {
            www.timeout = 15;
            yield return www.SendWebRequest();

            try
            {
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("Buy request failed: " + www.error);
                    yield break;
                }

                string raw = www.downloadHandler.text;
                Debug.Log("[Buy] Server response: " + raw);

                var parsed = ParseServerJson(raw);
                string successStr = GetParsedValue(parsed, "success");
                string fullVerStr = GetParsedValue(parsed, "fullVersion");
                string errorResp = GetParsedValue(parsed, "error");

                bool ok = IsTrue(successStr);

                if (ok)
                {
                    fullVersion = 1;
                    if (doneButton != null) doneButton.SetActive(true);
                    SavePlayerData();
                    Debug.Log("Purchase successful! Full version unlocked.");

                    AdManager.Instance.PurchasedSuccess();
                    FusionRoomManager.Instance.BoughtAdsRN();
                }
                else
                {
                    Debug.LogWarning("[Buy] Failed: " + errorResp);
                }
            }
            finally
            {
                if (UI_Loading != null) UI_Loading.SetActive(false);
            }
        }
    }

    IEnumerator CheckPurchaseRoutine()
    {
        if (string.IsNullOrEmpty(userEmail))
        {
            Debug.LogWarning("CheckPurchaseStatus: no user email set");
            yield break;
        }

        if (UI_Loading != null) UI_Loading.SetActive(true);

        WWWForm form = new WWWForm();
        form.AddField("action", "getPurchase");
        form.AddField("email", userEmail);

        using (UnityWebRequest www = UnityWebRequest.Post(webAppUrl, form))
        {
            www.timeout = 15;
            yield return www.SendWebRequest();

            try
            {
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("getPurchase failed: " + www.error);
                    yield break;
                }

                string raw = www.downloadHandler.text;
                Debug.Log("[GetPurchase] Server response: " + raw);

                var parsed = ParseServerJson(raw);
                string successStr = GetParsedValue(parsed, "success");
                string fullVerStr = GetParsedValue(parsed, "fullVersion");

                bool ok = IsTrue(successStr);
                if (ok && int.TryParse(fullVerStr, out int fv))
                {
                    fullVersion = fv;
                    SavePlayerData();
                }
            }
            finally
            {
                if (UI_Loading != null) UI_Loading.SetActive(false);
            }
        }
    }

    // ---------------------------
    // Helpers
    // ---------------------------

    /// <summary>
    /// Extracts a clean JSON object substring if the response contains HTML or extra text.
    /// Then extracts simple key:value pairs into a dictionary (string values).
    /// Works with values that are quoted strings or bare words/numbers/true/false.
    /// </summary>
    private Dictionary<string, string> ParseServerJson(string raw)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(raw))
            return result;

        // Trim BOM/Whitespace
        raw = raw.Trim();

        // Try to find the first { and the last } to extract the JSON object
        int first = raw.IndexOf('{');
        int last = raw.LastIndexOf('}');
        string json = (first >= 0 && last > first) ? raw.Substring(first, last - first + 1) : raw;

        // Now extract keys using regex (handles "key":"value" or "key":true or "key":123)
        // Pattern: "key" : "value" OR "key": value
        var keyPattern = new Regex("\"(?<key>[^\"\\\\]+)\"\\s*:\\s*(?:\"(?<sval>[^\"]*)\"|(?<wval>[^,}\\s]+))", RegexOptions.Compiled);

        var matches = keyPattern.Matches(json);
        foreach (Match m in matches)
        {
            var k = m.Groups["key"].Value;
            string val = null;
            if (m.Groups["sval"].Success) val = m.Groups["sval"].Value;
            else if (m.Groups["wval"].Success) val = m.Groups["wval"].Value;
            if (k != null && val != null)
                result[k] = val;
        }

        return result;
    }

    private string GetParsedValue(Dictionary<string, string> parsed, string key)
    {
        if (parsed == null) return null;
        if (parsed.TryGetValue(key, out string v)) return v;
        return null;
    }

    private bool IsTrue(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        value = value.Trim().ToLowerInvariant();
        return value == "true" || value == "1" || value == "yes";
    }

    private void SavePlayerData()
    {
        PlayerPrefs.SetString("email", userEmail ?? "");
        PlayerPrefs.SetString("password", userPassword ?? "");
        PlayerPrefs.SetInt("fullVersion", fullVersion);
        PlayerPrefs.Save();
    }

    private void OpenMainMenu()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    // ---------------------------
    // Public wrappers to start routines
    // ---------------------------
    public void BuyFullVersion()
    {
        StartCoroutine(BuyFullVersionRoutine());
    }

    public void CheckPurchaseStatus()
    {
        StartCoroutine(CheckPurchaseRoutine());
    }
}
