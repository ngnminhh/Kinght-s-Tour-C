/*  This file is part of the "UniPay" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace UniPay
{
    using UniPay.SimpleJSON;

    /// <summary>
    /// Represents the public interface of the underlying store system for PayPal.
    /// </summary>
    class PayPalStore : IStore
    {
        /// <summary>
        /// Reference to this store class, since the user needs to confirm the purchase
        /// transaction manually in-game, thus calling the confirm method of this script.
        /// </summary>
        public static PayPalStore instance { get; private set; }

        /// <summary>
        /// Callback for hooking into the custom Unity IAP logic.
        /// </summary>
        public IStoreCallback callback;

        /// <summary>
        /// List of products which are declared and retrieved by the billing system.
        /// </summary>
        public Dictionary<string, ProductDescription> products;

        /// <summary>
        /// Callback for hooking into IAPManager methods.
        /// This is basically a stripped down version of the IStoreCallback.
        /// </summary>
        public IAPManager iapManager;

        //configuration file for client and secret keys
        private PayPalStoreConfig config;

        //generated OAuth acess token returned by PayPal servers on client validation
        private AccessToken accessToken;

        //keeping track of the order that is currently being processed, so we can confirm and finish it later on.
        private string orderId;

        //keeping track of the product that is currently being processed
        private string currentProduct = "";


        public PayPalStore(IAPManager iapManager)
        {
            instance = this;
            this.iapManager = iapManager;

            config = iapManager.asset.customStoreConfig.PayPal;
        }


        /// <summary>
        /// Initialize the instance using the specified IStoreCallback.
        /// </summary>
        public virtual void Initialize(IStoreCallback callback)
        {
            this.callback = callback;
            this.products = new Dictionary<string, ProductDescription>();
        }


        public virtual void RetrieveProducts(ReadOnlyCollection<ProductDefinition> products)
        {
            string productID = null;
            string storeID = null;
            IAPProduct product = null;
            string priceString = null;

            for (int i = 0; i < products.Count; i++)
            {
                productID = products[i].id;
                storeID = products[i].storeSpecificId;
                product = IAPManager.GetIAPProduct(productID);
                priceString = product.GetPriceString();

                if (!this.products.ContainsKey(productID))
                {
                    this.products.Add(productID, new ProductDescription(storeID, new ProductMetadata(priceString, product.title, product.description, "USD", 1)));
                }
            }

            iapManager.StartCoroutine(OnInitialized(this.products.Values.ToList()));
        }


        public virtual void Purchase(ProductDefinition definition, string developerPayload)
        {
            iapManager.StartCoroutine(Purchase(definition));
        }


        IEnumerator Purchase(ProductDefinition definition)
        {
            if (accessToken == null || !accessToken.IsValid())
                yield return iapManager.StartCoroutine(GetAccessToken());

            if (accessToken == null || !accessToken.IsValid())
            {
                iapManager.OnPurchaseFailed(null, PurchaseFailureReason.SignatureInvalid);
                yield break;
            }

            IAPProduct product = IAPManager.GetIAPProduct(definition.id);
            if (product == null)
            {
                iapManager.OnPurchaseFailed(null, PurchaseFailureReason.ProductUnavailable);
                yield break;
            }

            string postData = GetPostData(product);
            using (UnityWebRequest www = UnityWebRequest.Post(GetUrl("order", product.type), string.Empty, "application/json"))
            {
                UploadHandlerRaw uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(postData));
                uploadHandler.contentType = "application/json";
                www.uploadHandler = uploadHandler;

                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", "Bearer " + accessToken.token);
                www.SetRequestHeader("PayPal-Request-Id", System.Guid.NewGuid().ToString());

                yield return www.SendWebRequest();
                
                if (www.result != UnityWebRequest.Result.Success)
                {
                    if (IAPManager.isDebug)
                    {
                        Debug.Log("PayPalStore purchase error: " + www.error);
                    }

                    iapManager.OnPurchaseFailed(null, PurchaseFailureReason.PurchasingUnavailable);
                }
                else
                {
                    JSONNode response = JSON.Parse(www.downloadHandler.text);
                    orderId = response["id"];
                    currentProduct = definition.storeSpecificId;

                    //get checkout link from HATEOAS links in response
                    string checkoutUrl = string.Empty;
                    JSONArray links = response["links"].AsArray;
                    for(int i = 0; i < links.Count; i++)
                    {
                        if(links[i]["rel"].Value == "payer-action" || links[i]["rel"].Value == "approve")
                        {
                            checkoutUrl = links[i]["href"].Value;
                            break;
                        }
                    }

                    //only show one waiting window at a time
                    UIShopFeedback.ShowLoading(false);
                    //show confirmation pending window and open website
                    UIShopFeedback.ShowConfirmation();
                    Application.OpenURL(checkoutUrl);
                }
            }
        }


        /// <summary>
        /// Manually triggering purchase confirmation after a PayPal payment has been made.
        /// This is so that the transaction gets finished and PayPal actually substracts funds.
        /// </summary>
        public void ConfirmPurchase()
        {
            if (string.IsNullOrEmpty(orderId))
            {
                OnPurchaseFailed("Purchase invalid or already claimed.", (int)PurchaseFailureReason.DuplicateTransaction);
                return;
            }

            OnPurchaseSucceeded();
        }


        /// <summary>
        /// Callback from the billing system when a purchase completes (be it successful or not).
        /// </summary>
        #pragma warning disable 0162
        public void OnPurchaseSucceeded()
        {
            //without IAPGUARD: finish transaction myself (capture)
            #if !RECEIPT_VALIDATION
                iapManager.StartCoroutine(FinishTransaction());
                return;
            #endif

            //with IAPGUARD: do not finish, let callback > ReceiptValidatorServer do it
            UnifiedReceipt receipt = new UnifiedReceipt()
            {
                Payload = string.Empty,
                Store = IAPPlatform.PayPal.ToString(),
                TransactionID = orderId
            };

            callback.OnPurchaseSucceeded(currentProduct, JsonUtility.ToJson(receipt), receipt.TransactionID);
        }
        #pragma warning restore 0162


        public virtual void FinishTransaction(ProductDefinition product, string transactionId)
        {
            //nothing to do here since the billing system has its own callback
        }


        IEnumerator FinishTransaction()
        {
            IAPProduct product = IAPManager.GetIAPProduct(currentProduct);
            if (product == null)
            {
                if(IAPManager.isDebug) Debug.Log("PayPalStore finish error could not load product: " + currentProduct);
                iapManager.OnPurchaseFailed(null, PurchaseFailureReason.ProductUnavailable);
                yield break;
            }

            UnityWebRequest www = product.type == ProductType.Subscription ?
                UnityWebRequest.Get(GetUrl("capture", product.type)) : UnityWebRequest.Post(GetUrl("capture", product.type), string.Empty, "application/json");
            
            using (www)
            {
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", "Bearer " + accessToken.token);
                www.SetRequestHeader("PayPal-Request-Id", orderId);

                yield return www.SendWebRequest();

                //payment could still be outstanding when initiating the capture call
                if (www.downloadHandler.text.Contains("APPROVAL_PENDING") || www.downloadHandler.text.Contains("ORDER_NOT_APPROVED"))
                {
                    if (UIShopFeedback.GetInstance() != null)
                        UIShopFeedback.ShowMessage("Order is not approved yet. Please confirm the transaction in your browser.");

                    yield break;
                }
                
                if (www.result != UnityWebRequest.Result.Success)
                {
                    if (IAPManager.isDebug)
                    {
                        Debug.Log("PayPalStore finish error: " + www.error);
                    }

                    iapManager.OnPurchaseFailed(null, PurchaseFailureReason.PaymentDeclined);
                }
                else
                {
                    JSONNode response = JSON.Parse(www.downloadHandler.text);
                    if (response["status"].Value == "COMPLETED" || response["status"].Value == "ACTIVE")
                    {
                        iapManager.CompletePurchase(product.ID);
                        orderId = currentProduct = string.Empty;

                        if (UIShopFeedback.GetInstance() != null && UIShopFeedback.GetInstance().confirmWindow != null)
                            UIShopFeedback.GetInstance().confirmWindow.SetActive(false);
                    }
                }
            }
        }


        string GetPostData(IAPProduct product)
        {
            switch(product.type)
            {
                case ProductType.Subscription:
                    return GetPostDataSubscription(product);
                default:
                    return GetPostDataOneTime(product);
            }
        }


        string GetPostDataOneTime(IAPProduct product)
        {
            JSONNode data = new JSONClass();
            data["intent"] = "CAPTURE";

            StoreMetaDefinition storeDefinition = product.storeIDs.Find(x => x.store == "PayPal" && x.active);
            string price = product.priceList.Find(x => x.type == IAPExchangeObject.ExchangeType.RealMoney).realPrice;
            price = Regex.Match(price, @"[0-9]+\.?[0-9,]*").Value;

            JSONNode unit = new JSONClass();
            JSONNode amount = new JSONClass();
            amount["currency_code"] = config.currencyCode;
            amount["value"] = price;

            JSONNode total = new JSONClass();
            total["currency_code"] = amount["currency_code"].Value;
            total["value"] = amount["value"].Value;

            JSONNode breakdown = new JSONClass();
            breakdown["item_total"] = total;
            amount["breakdown"] = breakdown;

            unit["amount"] = amount;
            unit["description"] = "Goods for " + Application.productName;

            JSONNode item = new JSONClass();
            item["name"] = string.IsNullOrEmpty(product.title) ? product.ID : product.title;
            item["description"] = product.type == ProductType.NonConsumable ? "Non-Consumable" : product.type.ToString();
            item["unit_amount"] = amount;
            item["quantity"] = "1";
            item["sku"] = storeDefinition == null ? product.ID : storeDefinition.ID;

            unit["items"] = new JSONArray();
            unit["items"].Add(item);

            data["purchase_units"] = new JSONArray();
            data["purchase_units"].Add(unit);

            JSONNode paymentSource = new JSONClass();
            JSONNode paypal = new JSONClass();
            JSONNode context = new JSONClass();
            context["return_url"] = config.returnUrl;
            paypal["experience_context"] = context;
            paymentSource["paypal"] = paypal;

            data["payment_source"] = paymentSource;

            return data.ToString();
        }


        string GetPostDataSubscription(IAPProduct product)
        {
            JSONNode data = new JSONClass();

            StoreMetaDefinition storeDefinition = product.storeIDs.Find(x => x.store == "PayPal" && x.active);
            data["plan_id"] = storeDefinition == null ? product.ID : storeDefinition.ID;
            data["quantity"] = "1";

            JSONNode context = new JSONClass();
            context["return_url"] = config.returnUrl;

            #if RECEIPT_VALIDATION
                context["user_action"] = "CONTINUE";
            #endif

            data["application_context"] = context;

            return data.ToString();
        }


        string GetUrl(string api, ProductType type = ProductType.NonConsumable)
        {
            switch(api)
            {
                case "token":
                    if (IAPManager.isDebug) return "https://api-m.sandbox.paypal.com/v1/oauth2/token";
                    else return "https://api-m.paypal.com/v1/oauth2/token";
                case "order":
                    switch (type)
                    {
                        case ProductType.Subscription:
                            if (IAPManager.isDebug) return "https://api-m.sandbox.paypal.com/v1/billing/subscriptions";
                            else return "https://api-m.paypal.com/v1/billing/subscriptions";

                        default:
                            if (IAPManager.isDebug) return "https://api-m.sandbox.paypal.com/v2/checkout/orders";
                            else return "https://api-m.paypal.com/v2/checkout/orders";
                    }
                case "capture":
                    switch(type)
                    {
                        case ProductType.Subscription:
                            return GetUrl("order", type) + "/" + orderId;

                        default:
                            return GetUrl("order") + "/" + orderId + "/capture";
                    }
            }

            return string.Empty;
        }


        IEnumerator GetAccessToken()
        {
            WWWForm form = new WWWForm();
            form.AddField("grant_type", "client_credentials");

            using (UnityWebRequest www = UnityWebRequest.Post(GetUrl("token"), form))
            {
                string auth = IAPManager.isDebug ? (config.sandbox.clientID + ":" + config.sandbox.secretKey) : (config.live.clientID + ":" + config.live.secretKey);
                auth = Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(auth));
                auth = "Basic " + auth;
                www.SetRequestHeader("Authorization", auth);

                yield return www.SendWebRequest();

                if (IAPManager.isDebug && www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("PayPalStore token error: " + www.error);
                }
                else
                {
                    JSONNode response = JSON.Parse(www.downloadHandler.text);
                    accessToken = new AccessToken(response["access_token"], response["expires_in"].AsInt);
                }
            }
        }


        [Serializable]
        public class AccessToken
        {
            public string token;
            public long expirationTime;

            public AccessToken(string token, long time)
            {
                this.token = token;
                expirationTime = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds() + time;
            }

            public bool IsValid()
            {
                return new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds() < expirationTime;
            }
        }


        //delay initialization until after Start, otherwise it finishes instantly
        //this fixes an issue with script not being subscribed to receiptValidationInitializeEvent in Start() yet
        IEnumerator OnInitialized(List<ProductDescription> products)
        {
            yield return new WaitForEndOfFrame();

            callback.OnProductsRetrieved(products);
        }


        /// <summary>
        /// Method we are calling for any failed results in the billing interaction.
        /// Here error codes are mapped to more user-friendly descriptions shown to them.
        /// </summary>
        public void OnPurchaseFailed(string error, int code)
        {
            PurchaseFailureReason reason = PurchaseFailureReason.Unknown;
            switch (code)
            {
                case 1:
                    reason = PurchaseFailureReason.ExistingPurchasePending;
                    break;
                case 2:
                    reason = PurchaseFailureReason.UserCancelled;
                    break;
                case 3:
                    reason = PurchaseFailureReason.PurchasingUnavailable;
                    break;
                case 4:
                    reason = PurchaseFailureReason.ProductUnavailable;
                    break;
                case 5:
                    reason = PurchaseFailureReason.SignatureInvalid;
                    break;
            }

            callback.OnPurchaseFailed(new PurchaseFailureDescription(currentProduct, reason, error));
        }
    }
}
