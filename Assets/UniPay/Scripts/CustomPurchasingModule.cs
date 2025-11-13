/*  This file is part of the "UniPay" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System.Collections.Generic;

namespace UniPay
{
    using UnityEngine.Purchasing.Extension;

    /// <summary>
    /// Custom Unity IAP purchasing module for overwriting default store subsystems.
    /// </summary>
    public class CustomPurchasingModule : IPurchasingModule
    {
        /// <summary>
        /// The currently selected App Store.
        /// </summary>
        public string appStore = "";

        /// <summary>
        /// Custom Store implementations that require access to its class instance.
        /// </summary>
        public Dictionary<string, IStore> customStores = new Dictionary<string, IStore>();


        /*
        public SISPurchasingModule(IAPManager callback)
        {
            if(callback.asset.customStoreConfig.PayPal.enabled)
                customStores.Add(IAPPlatform.PayPal.ToString(), new PayPalStore(callback));
        }
        */

        public void Configure(IPurchasingBinder binder)
		{
            //Native
            #if STEAM_IAP
                appStore = IAPPlatform.SteamStore.ToString();
                binder.RegisterStore(appStore, new SteamStore());
            #endif

            #if PAYPAL_IAP
                appStore = IAPPlatform.PayPal.ToString();
                binder.RegisterStore(appStore, new PayPalStore(IAPManager.GetInstance()));
            #endif

            //VR
            #if OCULUS_IAP
                appStore = IAPPlatform.OculusStore.ToString();
                binder.RegisterStore(appStore, new OculusStore());
            #endif
        }
    }
}