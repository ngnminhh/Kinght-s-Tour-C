/*  This file is part of the "UniPay" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UniPay
{
    using UnityEngine.Purchasing;
    using UnityEngine.Purchasing.Security;

    /// <summary>
    /// IAP receipt validation on the client (local, on the device) using Unity IAPs validator class.
    /// This is a lot less secure than server-validation, but better than nothing.
    /// </summary>
    public class ReceiptValidatorClient : ReceiptValidator
    {
        #pragma warning disable 0067,0414
        [Header("Apple App Store")]
        /// <summary>
        /// Whether StoreKit should use a test configuration instead of an Apple Root certificate.
        /// </summary>
        public bool useStoreKitTest = false;

        Dictionary<string, string> introductory_info_dict;
        #pragma warning restore 0067,0414


        #if !RECEIPT_VALIDATION
        void Start()
        {
            #if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_TVOS
                Debug.LogError("You are using " + GetType() + " but did not add the 'RECEIPT_VALIDATION' define to 'Project Settings > Player > Scripting Define Symbols'. Validation code will not compile!");
            #endif
        }
        #endif


        #if RECEIPT_VALIDATION
        //subscribe to IAPManager events
        void Start()
        {
            if (!CanValidate() || !IAPManager.GetInstance())
                return;

            IAPManager.receiptValidationInitializeEvent += Validate;
            IAPManager.receiptValidationPurchaseEvent += Validate;
        }


        #if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_TVOS)
        public override bool CanValidate()
        {
            //when running on Android, validation is only supported on Google Play
            if (Application.platform == RuntimePlatform.Android && StandardPurchasingModule.Instance().appStore != AppStore.GooglePlay)
                return false;

            return true;
        }
        #endif


        private void Validate()
        {
            IAppleExtensions m_AppleExtensions = IAPManager.extensions.GetExtension<IAppleExtensions>();
            introductory_info_dict = m_AppleExtensions.GetIntroductoryPriceDictionary();

            Validate(null as Product);
        }


        /// <summary>
        /// Overriding the base method for constructing Unity IAP's CrossPlatformValidator and passing in purchase receipts.
        /// The validation result will either grant the item (success) or remove it from the inventory if granted already (failed).
        /// </summary>
        public override void Validate(Product p = null)
        {
            Product[] products = new Product[]{ p };
            bool withEvent = p != null;

            if (p == null)
                products = IAPManager.controller.products.all;

            CrossPlatformValidator validator = null;
            try
            {
                if (useStoreKitTest) validator = new CrossPlatformValidator(GooglePlayTangle.Data(), AppleStoreKitTestTangle.Data(), Application.identifier);
                else validator = new CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), Application.identifier);
            }
            catch (NotImplementedException) { }

            for (int i = 0; i < products.Length; i++)
            {
                //products should contain a receipt on a real device
                if (!products[i].hasReceipt)
                {
                    #if !UNITY_EDITOR
                    RemovePurchase(products[i].definition.id);
                    #endif

                    continue;
                }

                //we found a receipt for this product on the device, initiate client receipt validation
                //if the purchase is pending validation will throw an exception and retry on the next app launch
                try
                {
                    // On Google Play, result will have a single product Id.
                    // On Apple stores receipts can contain multiple products.
                    validator.Validate(products[i].receipt);
                    UpdateCustomData(null, products[i]);

                    if (IAPManager.isDebug) Debug.Log("Client Receipt Validation passed for: '" + products[i].definition.id + "'.");
                    IAPManager.GetInstance().CompletePurchase(products[i].definition.id, withEvent);
                }
                catch (Exception ex)
                {                   
                    #if UNITY_EDITOR
                    //complete fake store = test mode purchases successfully anyway, but only in the editor
                    if(ex is NullReferenceException || ex.Message.Contains("fake"))
                    {
                        if (IAPManager.isDebug) Debug.Log("Test Mode. Client Receipt Validation passed for: '" + products[i].definition.id + "'.");
                        IAPManager.GetInstance().CompletePurchase(products[i].definition.id, withEvent);
                        continue;
                    }
                    #endif

                    if (IAPManager.isDebug) Debug.Log("Client Receipt Validation failed for: '" + products[i].definition.id + "'. Exception: " + ex + ", " + ex.Message);

                    if ((ex is NullReferenceException || ex is IAPSecurityException))
                    {
                        RemovePurchase(products[i].definition.id);
                    }
                };

                IAPManager.controller.ConfirmPendingPurchase(products[i]);
            }

            void RemovePurchase(string productID)
            {
                if (!DBManager.IsPurchased(productID))
                    return;

                ShopItem2D item = null;
                if (IAPManager.GetInstance()) item = IAPManager.GetShopItem(productID);
                if (item) item.Purchased(false);
                DBManager.ConsumePurchase(productID);
            }
        }

        
        private void UpdateCustomData(IAPProduct product, Product p)
        {
            if (product == null) product = IAPManager.GetIAPProduct(p.definition.id);
            if (product == null) return;

            if (product.type != ProductType.Subscription || !IsAvailableForSubscriptionManager(p.receipt))
                return;
                  
            string intro_json = (introductory_info_dict == null || !introductory_info_dict.ContainsKey(p.definition.storeSpecificId)) ? null : introductory_info_dict[p.definition.storeSpecificId];
            SubscriptionManager sub = new SubscriptionManager(p, intro_json);
            SubscriptionInfo info = sub.getSubscriptionInfo();

            DateTime exDate = info.getExpireDate().ToLocalTime();
            if ((exDate - DateTime.Now).TotalDays > 0)
            {
                product.customData.Remove("expiration");
                product.customData.Add("expiration", exDate.ToUniversalTime().ToString("u"));
            }
        }


        //modified from the Unity IAP sample
        //developerPayload is not supported anymore
        private bool IsAvailableForSubscriptionManager(string receipt)
        {
            Hashtable hash = UniPay.MiniJson.JsonDecode(receipt) as Hashtable;

            if (!hash.ContainsKey("Store") || !hash.ContainsKey("Payload"))
            {
                if (IAPManager.isDebug) Debug.Log("The product receipt does not contain enough information");
                return false;
            }

            string store = hash["Store"] as string;
            Hashtable payload = UniPay.MiniJson.JsonDecode(hash["Payload"] as string) as Hashtable;

            switch (store)
            {
                case GooglePlay.Name:
                {
                    if (payload == null)
                    {
                        if (IAPManager.isDebug) Debug.Log("The product receipt does not contain enough information, payload is empty.");
                        return false;
                    }

                    if (!payload.ContainsKey("json"))
                    {
                        if (IAPManager.isDebug) Debug.Log("The product receipt does not contain enough information, the 'json' field is missing");
                        return false;
                    }

                    Hashtable json = UniPay.MiniJson.JsonDecode(payload["json"] as string) as Hashtable;

                    if (json == null)
                    {
                        if (IAPManager.isDebug) Debug.Log("The product receipt does not contain enough information, json is empty.");
                        return false;
                    }

                    return true;
                }

                case AppleAppStore.Name:
                case AmazonApps.Name:
                case MacAppStore.Name:
                {
                    return true;
                }

                default:
                {
                    return false;
                }
            }
        }
        #endif
    }
}
