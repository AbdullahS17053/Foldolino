using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;

[Serializable]
public class InAppProduct
{
    [Tooltip("Product ID exactly as in Google Play / App Store")]
    public string id;

    [Tooltip("Friendly name for your own reference")]
    public string name;

    [Tooltip("True = Consumable (coins, gems, etc). False = Non-consumable (remove ads, premium).")]
    public bool isConsumable;
}

public class InAppManager : MonoBehaviour
{
    public static InAppManager Instance;

    [Header("Products configured in Play Console / App Store Connect")]
    public List<InAppProduct> products = new List<InAppProduct>();

    private StoreController _store; 
    private bool _isConnected;
    private bool _productsFetched;

    private async void Awake()
    {

        Instance = this;
        DontDestroyOnLoad(gameObject);

        await InitializeIapAsync();

    }

    private void Start()
    {
        if (PlayerPrefs.GetInt("BuyPremium", 0) == 1)
            LocalDrawManager.instance.premiumBtn.SetActive(false);
    }
    private async Task InitializeIapAsync()
    {
        Debug.Log("[IAP] Initializing…");

        // 1) Get StoreController (v5 way)
        _store = UnityIAPServices.StoreController();

        // 2) Attach event handlers BEFORE calling Connect/Fetch
        _store.OnStoreDisconnected += OnStoreDisconnected;
        _store.OnProductsFetched += OnProductsFetched;
        _store.OnProductsFetchFailed += OnProductsFetchFailed;
        _store.OnPurchasesFetched += OnPurchasesFetched;
        _store.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
        _store.OnPurchasePending += OnPurchasePending;
        _store.OnPurchaseFailed += OnPurchaseFailed;
        _store.OnPurchaseConfirmed += OnPurchaseConfirmed;

        try
        {
            // 3) Connect to store (async)
            await _store.Connect();
            _isConnected = true;
            Debug.Log("[IAP] Connected to store.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[IAP] Connect failed: {e.Message}");
            return;
        }

        // 4) Build ProductDefinition list from inspector data
        var defs = new List<ProductDefinition>();
        foreach (var p in products)
        {
            if (string.IsNullOrEmpty(p.id))
                continue;

            var type = p.isConsumable ? ProductType.Consumable : ProductType.NonConsumable;
            // Uses constructor where id == storeSpecificId :contentReference[oaicite:3]{index=3}
            defs.Add(new ProductDefinition(p.id, type));
        }

        if (defs.Count == 0)
        {
            Debug.LogWarning("[IAP] No products configured in IAPManagerV5.");
            return;
        }

        // 5) Fetch product info (price, title, etc.) from store
        _store.FetchProductsWithNoRetries(defs);
    }

    // Called when products metadata comes back from store
    private void OnProductsFetched(List<Product> fetchedProducts)
    {
        _productsFetched = true;

        Debug.Log($"[IAP] Products fetched: {fetchedProducts.Count}");
        foreach (var p in fetchedProducts)
        {
            Debug.Log($"[IAP] {p.definition.id} | {p.metadata.localizedTitle} | {p.metadata.localizedPriceString}");
        }

        // Optionally also fetch existing purchases (non-consumables / subs)
        _store.FetchPurchases();
    }

    private void OnProductsFetchFailed(ProductFetchFailed failed)
    {
        Debug.LogError($"[IAP] Products fetch failed: {failed.FailureReason}");
    }

    // Called when existing purchases are fetched (restore flow) :contentReference[oaicite:4]{index=4}
    private void OnPurchasesFetched(Orders orders)
    {
        // Restore non-consumables / subscriptions
        foreach (var order in orders.ConfirmedOrders)
        {
            GrantProductsFromOrder(order, isRestore: true);
        }
    }

    private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failed)
    {
        Debug.LogError($"[IAP] Purchases fetch failed: {failed.FailureReason} | {failed.Message}");
    }

    // 🔹 PUBLIC API – call this from your UI button in Editor / Mobile
    public void PurchaseInApp(string productId)
    {
        if (_store == null || !_isConnected || !_productsFetched)
        {
            Debug.LogWarning("[IAP] Not ready yet. Wait for initialization.");
            return;
        }

        if (string.IsNullOrEmpty(productId))
        {
            Debug.LogWarning("[IAP] Empty productId.");
            return;
        }

        Debug.Log("[IAP] Purchasing product: " + productId);
        _store.PurchaseProduct(productId);   // New v5 API :contentReference[oaicite:5]{index=5}
    }

    /// <summary>
    /// Helper if you want to purchase by index from inspector list.
    /// (Optional – you can remove this if you don't need it.)
    /// </summary>
    public void PurchaseInAppByIndex(int index)
    {
        if (index < 0 || index >= products.Count)
        {
            Debug.LogWarning("[IAP] Invalid product index.");
            return;
        }

        PurchaseInApp(products[index].id);
    }

    // 🔹 PUBLIC API – Restore button (mainly for iOS)
    public void RestoreInApp()
    {
        if (_store == null || !_isConnected)
        {
            Debug.LogWarning("[IAP] Not ready yet. Wait for initialization.");
            return;
        }

#if UNITY_IOS
        // Explicit restore for iOS :contentReference[oaicite:6]{index=6}
        Debug.Log("[IAP] RestoreTransactions called (iOS).");
        _store.RestoreTransactions((success, error) =>
        {
            Debug.Log($"[IAP] RestoreTransactions result: success={success}, error={error}");
        });
#else
        // On Android / others, FetchPurchases usually enough for non-consumables / subs :contentReference[oaicite:7]{index=7}
        Debug.Log("[IAP] FetchPurchases called (non-iOS restore).");
        _store.FetchPurchases();
#endif
    }


    // New purchase (or re-processed pending purchase)
    private void OnPurchasePending(PendingOrder pending)
    {
        Debug.Log("[IAP] Purchase pending, granting entitlement then confirming…");

        // Your old ProcessPurchase logic goes here: grant items/flags from order
        GrantProductsFromOrder(pending, isRestore: false);

        // IMPORTANT: confirm the purchase so the store finishes the transaction :contentReference[oaicite:8]{index=8}
        _store.ConfirmPurchase(pending);
    }

    // Called after ConfirmPurchase (success or failed)
    private void OnPurchaseConfirmed(Order order)
    {
        OnPurchaseSuccess();
    }

    public void OnPurchaseSuccess()
    {
        PlayerPrefs.SetInt("BuyPremium", 1);
        LocalDrawManager.instance.premiumPanel.SetActive(false);
        LocalDrawManager.instance.premiumBtn.SetActive(false);
    }

    private void OnPurchaseFailed(FailedOrder failed)
    {
        Debug.LogError($"[IAP] Purchase FAILED. TxID: {failed.Info.TransactionID}, Details: {failed.Details}");
    }

    private void OnStoreDisconnected(StoreConnectionFailureDescription desc)
    {
        _isConnected = false;
        Debug.LogError($"[IAP] Store disconnected. Retryable={desc.IsRetryable}, Message={desc.Message}");
    }

    // 🔸 Shared helper – grant items based on productId(s) in an Order
    private void GrantProductsFromOrder(Order order, bool isRestore)
    {
        if (order.Info == null || order.Info.PurchasedProductInfo == null)
            return;

        foreach (var purchased in order.Info.PurchasedProductInfo)
        {
            string productId = purchased.productId;
            var cfg = products.Find(p => p.id == productId);

            if (cfg == null)
            {
                Debug.LogWarning("[IAP] Purchased product not found in local list: " + productId);
                continue;
            }

            if (cfg.isConsumable)
            {
                if (isRestore)
                {
                    // Consumables are normally NOT restored, so we usually skip here
                    Debug.Log("[IAP] Skipping restore for consumable: " + cfg.name);
                }
                else
                {
                    Debug.Log("[IAP] Granting consumable: " + cfg.name);
                    // TODO: Add your coins/gems/etc here
                }
            }
            else
            {
                Debug.Log($"[IAP] {(isRestore ? "Restoring" : "Granting")} non-consumable: " + cfg.name);
                // TODO: Set your "owns premium" / "ads removed" flag here and save
            }
        }
    }

    public void HostGame()
    {
        if (PlayerPrefs.GetInt("BuyPremium", 0) == 1)
            LocalDrawManager.instance.hostPanel.SetActive(true);
        else
            LocalDrawManager.instance.premiumPanel.SetActive(true);
    }
}
