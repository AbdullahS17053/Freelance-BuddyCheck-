using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Text.RegularExpressions;
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
    public string userEmail;
    public string userPassword;
    public int fullVersion;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
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
            loginPanel.SetActive(true);
        }
    }

    // ================================
    // LOGIN BUTTON
    // ================================
    public void OnLoginButton()
    {
        string email = loginEmailField.text.Trim().ToLower();
        string password = loginPasswordField.text.Trim();

        if (!Utils.IsValidEmail(email))
        {
            Debug.Log("Invalid email format!");
            loginErrorUI.SetActive(true);
            return;
        }

        StartCoroutine(LoginRoutine(email, password, false));
    }

    IEnumerator LoginRoutine(string email, string password, bool silentLogin)
    {
        WWWForm form = new WWWForm();
        form.AddField("action", "login");
        form.AddField("email", email);
        form.AddField("password", password);
        UI_Loading.SetActive(true);

        UnityWebRequest www = UnityWebRequest.Post(webAppUrl, form);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            if (!silentLogin) loginErrorUI.SetActive(true);
            loginPanel.SetActive(true);
            yield break;
        }

        string json = www.downloadHandler.text;
        var data = JsonUtility.FromJson<LoginResponse>(json);

        if (data.success.ToLower() == "true")
        {
            loginEmailField.text = "";
            loginPasswordField.text = "";
            userEmail = data.email;
            userPassword = password;
            fullVersion = int.Parse(data.fullVersion);

            SavePlayerData();
            OpenMainMenu();
        }
        else
        {
            if (!silentLogin) loginErrorUI.SetActive(true);
            loginPanel.SetActive(true);
        }

        Debug.Log(data.error);

        UI_Loading.SetActive(false);
    }

    // ================================
    // REGISTER BUTTON
    // ================================
    public void OnRegisterButton()
    {
        string email = registerEmailField.text.Trim().ToLower();
        string password = registerPasswordField.text.Trim();

        if (!Utils.IsValidEmail(email))
        {
            Debug.Log("Invalid email format!");
            registerErrorUI.SetActive(true);
            return;
        }

        StartCoroutine(RegisterRoutine(email, password));
    }

    IEnumerator RegisterRoutine(string email, string password)
    {
        WWWForm form = new WWWForm();
        form.AddField("action", "register");
        form.AddField("email", email);
        form.AddField("password", password);
        UI_Loading.SetActive(true);

        UnityWebRequest www = UnityWebRequest.Post(webAppUrl, form);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            registerErrorUI.SetActive(true);
            yield break;
        }

        string json = www.downloadHandler.text;
        var data = JsonUtility.FromJson<LoginResponse>(json);

        if (data.success.ToLower() == "true")
        {
            userEmail = email;
            userPassword = password;
            fullVersion = int.Parse(data.fullVersion);

            registerEmailField.text = "";
            registerPasswordField.text = "";

            SavePlayerData();
            OpenMainMenu();
        }
        else
        {
            registerErrorUI.SetActive(true);
        }
        UI_Loading.SetActive(false);
    }

    // ================================
    // PURCHASE FUNCTIONS
    // ================================
    public void BuyFullVersion()
    {
        StartCoroutine(BuyFullVersionRoutine());
    }

    IEnumerator BuyFullVersionRoutine()
    {
        WWWForm form = new WWWForm();
        form.AddField("action", "buy");
        form.AddField("email", userEmail);

        UnityWebRequest www = UnityWebRequest.Post(webAppUrl, form);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string json = www.downloadHandler.text;
            var data = JsonUtility.FromJson<LoginResponse>(json);

            if (data.success.ToLower() == "true")
            {
                fullVersion = 1;
                SavePlayerData();
                Debug.Log("Purchase successful! Full version unlocked.");
            }
        }
    }

    public void CheckPurchaseStatus()
    {
        StartCoroutine(CheckPurchaseRoutine());
    }

    IEnumerator CheckPurchaseRoutine()
    {
        WWWForm form = new WWWForm();
        form.AddField("action", "getPurchase");
        form.AddField("email", userEmail);

        UnityWebRequest www = UnityWebRequest.Post(webAppUrl, form);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string json = www.downloadHandler.text;
            var data = JsonUtility.FromJson<LoginResponse>(json);

            if (data.success.ToLower() == "true")
            {
                fullVersion = int.Parse(data.fullVersion);
                SavePlayerData();
            }
        }
    }

    // ================================
    // PASSWORD RECOVERY
    // ================================
    public void OnForgotPasswordButton()
    {
        Application.OpenURL($"mailto:{recoveryEmail}?subject=Password Recovery Request");
    }

    // ================================
    // SAVE PLAYER DATA LOCALLY
    // ================================
    private void SavePlayerData()
    {
        PlayerPrefs.SetString("email", userEmail);
        PlayerPrefs.SetString("password", userPassword);
        PlayerPrefs.SetInt("fullVersion", fullVersion);
        PlayerPrefs.Save();
    }

    // ================================
    // OPEN MAIN MENU
    // ================================
    private void OpenMainMenu()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    // ================================
    // DATA STRUCT FOR JSON PARSING
    // ================================
    [System.Serializable]
    private class LoginResponse
    {
        public string success;
        public string email;
        public string fullVersion;
        public string error;
    }
}