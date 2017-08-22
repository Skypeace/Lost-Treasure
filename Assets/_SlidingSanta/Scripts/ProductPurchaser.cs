using UnityEngine;
using System.Collections;
using EasyMobile;

namespace SgLib
{
    public class ProductPurchaser : MonoBehaviour
    {

        void OnEnable()
        {
            IAPManager.PurchaseCompleted += OnPurchaseCompleted;
            IAPManager.RestoreCompleted += OnRestoreCompleted;
        }

        void OnDisable()
        {
            IAPManager.PurchaseCompleted -= OnPurchaseCompleted;
            IAPManager.RestoreCompleted -= OnRestoreCompleted;
        }

        public void Purchase(string productName)
        {
            if (IAPManager.Instance.IsInitialized())
            {
                IAPManager.Instance.PurchaseWithName(productName);
            }
            else
            {
                MobileNativeAlert.CreateOneButtonAlert("Service Unavailable", "Please check your internet connection.", "OK");
            }
        }

        public void RestorePurchases()
        {
            if (IAPManager.Instance.IsInitialized())
            {
                IAPManager.Instance.RestorePurchases();
            }
            else
            {
                MobileNativeAlert.CreateOneButtonAlert("Service Unavailable", "Please check your internet connection.", "OK");
            }
        }

        void OnPurchaseCompleted(IAPProduct product)
        {
            switch (product.name)
            {
                case "Remove_Ads":
                    AdManager.Instance.RemoveAds();
                    break;
                case "200_Gifts":
                    CoinManager.Instance.AddCoins(200);
                    break;
                case "450_Gifts":
                    CoinManager.Instance.AddCoins(450);
                    break;
                case "1000_Gifts":
                    CoinManager.Instance.AddCoins(1000);
                    break;
                case "3000_Gifts":
                    CoinManager.Instance.AddCoins(3000);
                    break;
            }
        }

        // RestoreCompleted event is only fired on iOS devices.
        void OnRestoreCompleted()
        {
            MobileNativeAlert.CreateOneButtonAlert("Restore Completed", "Your purchases were restored successfully.", "OK");
        }
    }
}
