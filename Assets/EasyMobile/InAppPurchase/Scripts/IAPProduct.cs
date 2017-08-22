using System;
using UnityEngine;

using UnityEngine.Purchasing;

namespace EasyMobile
{
    [Serializable]
    public class IAPProduct
    {
        [Tooltip("Product name, can be used when making purchase.")]
        public string name;
        [Tooltip("Unity IAP product ID. Potentially independent of store IDs.")]
        public string id;
        public ProductType type;
        [Tooltip("Add store-specific product ID if you need to use different IDs among stores.")]
        public StoreSpecificId[] storeSpecificIds;

        [Serializable]
        public class StoreSpecificId
        {
            public IAPSupportedStore store;
            public string id;
        }
    }
}

