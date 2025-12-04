using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Purchasing;
using static NUnit.Framework.Internal.OSPlatform;

public class PurchaseManager : MonoBehaviour, IStoreListener
{
    public static PurchaseManager Instance;

    private static IStoreController controller;
    private static IExtensionProvider extensions;

    [Header("Product Settings")]
    public string fullGameProductID = "fullgame_removeads";   // Set in inspector
    public bool useDummyMode = false;                        // Force dummy mode

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        // If product ID missing ? enable dummy mode
        if (string.IsNullOrEmpty(fullGameProductID))
        {
            Debug.LogWarning("No Product ID set ? Using DUMMY PURCHASE MODE.");
            useDummyMode = true;
            return;
        }

        InitializeIAP();
    }

    // -------------------------------------------------------
    // IAP INIT
    // -------------------------------------------------------
    void InitializeIAP()
    {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        builder.AddProduct(fullGameProductID, ProductType.NonConsumable);

        UnityPurchasing.Initialize(this, builder);
    }

    // -------------------------------------------------------
    // BUY FULL GAME
    // -------------------------------------------------------
    public void BuyFullGame()
    {
        // Dummy purchase mode
        if (useDummyMode || string.IsNullOrEmpty(fullGameProductID))
        {
            Debug.Log("<color=yellow>DUMMY PURCHASE SUCCESS: Full Game Unlocked</color>");
            FakePurchaseSuccess();
            return;
        }

        if (controller == null)
        {
            Debug.LogWarning("IAP not initialized yet.");
            return;
        }

        controller.InitiatePurchase(fullGameProductID);
    }

    // -------------------------------------------------------
    // REAL PURCHASE SUCCESS
    // -------------------------------------------------------
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        if (args.purchasedProduct.definition.id == fullGameProductID)
        {
            CompleteFullGamePurchase();
        }

        return PurchaseProcessingResult.Complete;
    }

    // -------------------------------------------------------
    // PURCHASE FAILED
    // -------------------------------------------------------
    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
    {
        Debug.LogWarning("Purchase Failed: " + reason);
    }

    public void OnInitialized(IStoreController c, IExtensionProvider e)
    {
        controller = c;
        extensions = e;
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError("IAP Initialization Failed: " + error);
    }

    // -------------------------------------------------------
    // HANDLE SUCCESS
    // -------------------------------------------------------
    private void CompleteFullGamePurchase()
    {
        PlayerPrefs.SetInt("full_version", 1);
        PlayerPrefs.Save();

        LoginManager.Instance.fullVersion = 1;
        AdCommunicator.Instance.OnFullGamePurchased();

        Debug.Log("<color=green>REAL PURCHASE SUCCESS</color>");
    }

    // -------------------------------------------------------
    // DUMMY SUCCESS (Editor / Testing / Missing ID)
    // -------------------------------------------------------
    private void FakePurchaseSuccess()
    {
        PlayerPrefs.SetInt("full_version", 1);
        PlayerPrefs.Save();

        LoginManager.Instance.fullVersion = 1;
        AdCommunicator.Instance.OnFullGamePurchased();

        Debug.Log("<color=cyan>DUMMY PURCHASE COMPLETED — Full Game Unlocked</color>");
    }
}
