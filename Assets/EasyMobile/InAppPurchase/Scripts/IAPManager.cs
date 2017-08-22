using UnityEngine;
using System.Collections;
using System;

using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

namespace EasyMobile
{
    public enum IAPSupportedStore
    {
        AppleAppStore,
        GooglePlay,
        AmazonApps,
        SamsungApps,
        WindowStore
    }

    // Deriving the Purchaser class from IStoreListener enables it to receive messages from Unity Purchasing.
    public class IAPManager : MonoBehaviour, IStoreListener
    {
        public static IAPManager Instance { get; private set; }

        public static event Action<IAPProduct> PurchaseCompleted = delegate {};
        public static event Action<IAPProduct> PurchaseFailed = delegate {};
        // Restore events are fired on iOS or MacOSX only
        public static event Action RestoreCompleted = delegate {};
        public static event Action RestoreFailed = delegate {};

        public bool verboseDebugLog = false;
        
        #if UNITY_ANDROID
        public AndroidStore targetAndroidStore = AndroidStore.GooglePlay;
        #endif
        
        [Header("PRODUCT LIST")]
        public IAPProduct[] products;
        
        // The Unity Purchasing system
        private IStoreController storeController;
        
        // The store-specific Purchasing subsystems
        private IExtensionProvider storeExtensionProvider;

        void Awake()
        {
            if (Instance)
            {
                DestroyImmediate(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        void Start()
        {
            // If we haven't set up the Unity Purchasing reference
            if (storeController == null)
            {
                // Begin to configure our connection to Purchasing
                InitializePurchasing();
            }
        }

        public void InitializePurchasing()
        {
            // If we have already connected to Purchasing ...
            if (IsInitialized())
            {
                // ... we are done here.
                return;
            }
        
            // Create a builder, first passing in a suite of Unity provided stores.
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        
            // Add products
            foreach (IAPProduct pd in products)
            {
                if (pd.storeSpecificIds != null && pd.storeSpecificIds.Length > 0)
                {
                    // Add store-specific id if any
                    IDs storeIDs = new IDs();
                    foreach (IAPProduct.StoreSpecificId sId in pd.storeSpecificIds)
                    {
                        storeIDs.Add(sId.id, new string[] { GetStoreName(sId.store) });
                    }
        
                    // Add product with store-specific ids
                    builder.AddProduct(pd.id, pd.type, storeIDs);
                }
                else
                {
                    // Add product using store-independent id
                    builder.AddProduct(pd.id, pd.type);
                }
            }
        
            // Kick off the remainder of the set-up with an asynchrounous call, passing the configuration
            // and this class' instance. Expect a response either in OnInitialized or OnInitializeFailed.
            UnityPurchasing.Initialize(this, builder);
        }

        /// <summary>
        /// Determines whether UnityIAP is initialized. All further actions like purchasing
        /// or restoring can only be done if UnityIAP is initialized.
        /// </summary>
        /// <returns><c>true</c> if initialized; otherwise, <c>false</c>.</returns>
        public bool IsInitialized()
        {
            // Only say we are initialized if both the Purchasing references are set.
            return storeController != null && storeExtensionProvider != null;
        }

        /// <summary>
        /// Purchase the specified product.
        /// </summary>
        /// <param name="product">Product.</param>
        public void Purchase(IAPProduct product)
        {
            if (product != null && product.id != null)
            {
                PurchaseWithId(product.id);
            }
            else
            {
                Debug.Log("Purchase: FAIL. Either product or its id is NULL.");
            }
        }

        /// <summary>
        /// Purchases the product with specified name.
        /// </summary>
        /// <param name="productName">Product name.</param>
        public void PurchaseWithName(string productName)
        {
            IAPProduct pd = GetIAPProductByName(productName);
        
            if (pd != null && pd.id != null)
            {
                PurchaseWithId(pd.id);
            }
            else
            {
                Debug.Log("PurchaseWithName: FAIL. Not found product with name: " + productName + " or its id is NULL.");
            }
        }

        /// <summary>
        /// Purchase the product with specified productId.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        public void PurchaseWithId(string productId)
        {
            // If Purchasing has been initialized ...
            if (IsInitialized())
            {
                // ... look up the Product reference with the general product identifier and the Purchasing
                // system's products collection.
                Product product = storeController.products.WithID(productId);
        
                // If the look up found a product for this device's store and that product is ready to be sold ...
                if (product != null && product.availableToPurchase)
                {
                    Debug.Log(string.Format("Purchasing product asychronously: '{0}'", product.definition.id));
                    // ... buy the product. Expect a response either through ProcessPurchase or OnPurchaseFailed
                    // asynchronously.
                    storeController.InitiatePurchase(product);
                }
                        // Otherwise ...
                        else
                {
                    // ... report the product look-up failure situation
                    Debug.Log("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
                }
            }
                    // Otherwise ...
                    else
            {
                // ... report the fact Purchasing has not succeeded initializing yet. Consider waiting longer or retrying initialization.
                Debug.Log("BuyProductID FAIL. Not initialized.");
            }
        }

        /// <summary>
        /// Restore purchases previously made by this customer. Some platforms automatically restore purchases, like Google.
        /// Apple currently requires explicit purchase restoration for IAP, conditionally displaying a password prompt.
        /// This method only has effect on iOS and MacOSX apps.
        /// </summary>
        public void RestorePurchases()
        {
            // If Purchasing has not yet been set up ...
            if (!IsInitialized())
            {
                // ... report the situation and stop restoring. Consider either waiting longer, or retrying initialization.
                Debug.Log("RestorePurchases FAIL. Not initialized.");
                return;
            }
        
            // If we are running on an Apple device ...
            if (Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                // ... begin restoring purchases
                Debug.Log("RestorePurchases started ...");
        
                // Fetch the Apple store-specific subsystem.
                var apple = storeExtensionProvider.GetExtension<IAppleExtensions>();
        
                // Begin the asynchronous process of restoring purchases. Expect a confirmation response in
                // the Action<bool> below, and ProcessPurchase if there are previously purchased products to restore.
                apple.RestoreTransactions((result) =>
                    {
                        // The first phase of restoration. If no more responses are received on ProcessPurchase then
                        // no purchases are available to be restored.
                        Debug.Log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
        
                        if (result)
                        {
                            // Fire restore complete event.
                            RestoreCompleted();
                        }
                        else
                        {
                            // Fire event failed event.
                            RestoreFailed();
                        }
                    });
            }
                    // Otherwise ...
                    else
            {
                // We are not running on an Apple device. No work is necessary to restore purchases.
                Debug.Log("RestorePurchases FAIL. Not supported on this platform. Current = " + Application.platform);
            }
        }

        /// <summary>
        /// Gets the IAP product with the specified name.
        /// </summary>
        /// <returns>The IAP product.</returns>
        /// <param name="productName">Product name.</param>
        public IAPProduct GetIAPProductByName(string productName)
        {
            foreach (IAPProduct pd in products)
            {
                if (pd.name.Equals(productName))
                    return pd;
            }
        
            return null;
        }

        /// <summary>
        /// Gets the IAP product by identifier.
        /// </summary>
        /// <returns>The IAP product by identifier.</returns>
        /// <param name="pId">P identifier.</param>
        public IAPProduct GetIAPProductById(string productId)
        {
            foreach (IAPProduct pd in products)
            {
                if (pd.id.Equals(productId))
                    return pd;
            }
        
            return null;
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            // Purchasing has succeeded initializing. Collect our Purchasing references.
            Debug.Log("IAPManager OnInitialized: PASS");
        
            // Overall Purchasing system, configured with products for this application.
            storeController = controller;
            // Store specific subsystem, for accessing device-specific store features.
            storeExtensionProvider = extensions;
        }

        
        public void OnInitializeFailed(InitializationFailureReason error)
        {
            // Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
            Debug.Log("OnInitializeFailed InitializationFailureReason:" + error);
        }

        
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            IAPProduct pd = GetIAPProductById(args.purchasedProduct.definition.id);
        
            // Fire purchase success event
            PurchaseCompleted(pd);
        
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            // A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing
            // this reason with the user to guide their troubleshooting actions.
            Debug.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
        
            // Fire purchase failure event
            IAPProduct pd = GetIAPProductById(product.definition.id);
            PurchaseFailed(pd);
        }

        string GetStoreName(IAPSupportedStore store)
        {
            switch (store)
            {
                case IAPSupportedStore.AppleAppStore:
                    return AppleAppStore.Name;
                case IAPSupportedStore.GooglePlay:
                    return GooglePlay.Name;
                case IAPSupportedStore.AmazonApps:
                    return AmazonApps.Name;
                case IAPSupportedStore.SamsungApps:
                    return SamsungApps.Name;
                case IAPSupportedStore.WindowStore:
                    return WindowsStore.Name;
                default:
                    return null;
            }
        }
    }
}