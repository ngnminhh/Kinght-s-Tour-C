/*  This file is part of the "UniPay" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UniPay
{
    using UnityEngine.Purchasing;

    /// <summary>
    /// IAP object properties. This is a meta-class for an IAP item.
    /// </summary>
    [Serializable]
    public class IAPProduct
    {
        [HideInInspector]
        public string referenceID;

        public string ID;
        public List<StoreMetaDefinition> storeIDs = new List<StoreMetaDefinition>();

        [SerializeReference]
        public IAPCategory category;

        public ProductType type = ProductType.Consumable;
        public string title;
        public string description;
        public bool discount = false;
        public bool fetch = false;
        public Sprite icon;

        public Dictionary<string, string> customData = new Dictionary<string, string>();

        public List<IAPExchangeObject> priceList = new List<IAPExchangeObject>();
        public List<IAPExchangeObject> rewardList = new List<IAPExchangeObject>();

        public IAPRequirement requirement = new IAPRequirement();

        /// <summary>
        ///	Product reference for the following upgrade.
        /// </summary>
        [SerializeReference]
        public IAPProduct nextUpgrade;


        public IDs GetIDs()
        {
            IDs productIDs = new IDs();

            foreach (StoreMetaDefinition definition in storeIDs)
            {
                if(definition.active && !string.IsNullOrEmpty(definition.ID))
                    productIDs.Add(definition.ID, definition.store);
            }

            if (productIDs.Any()) return productIDs;
            else return null;
        }


        public bool IsVirtual()
        {
            return !priceList.Exists(x => x.type == IAPExchangeObject.ExchangeType.RealMoney);
        }


        public string GetPriceString()
        {
            for(int i = 0; i < priceList.Count; i++)
            {
                if (priceList[i].type == IAPExchangeObject.ExchangeType.RealMoney)
                    return priceList[i].realPrice;
            }

            return string.Empty;
        }
    }
}
