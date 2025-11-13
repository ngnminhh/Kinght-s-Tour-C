/*  This file is part of the "UniPay" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniPay
{
    /// <summary>
    /// Simple script that contains methods for testing purposes.
    /// You shouldn't implement this script in production versions.
    /// <summary>
    public class DebugCalls : MonoBehaviour
    {
        /// <summary>
        /// Allows initializing the IAPManager at some point later manually.
        /// </summary>
        [ContextMenu("Initialize")]
        public void Initialize()
        {
            if (IAPManager.GetInstance())
            {
                IAPManager.GetInstance().Initialize();
            }
        }


        /// <summary>
        /// Deletes all data saved in prefs, for ensuring a clean test state.
        /// <summary>
        [ContextMenu("Reset")]
        public void Reset()
        {
            if (DBManager.GetInstance())
            {
                try { UnityEngine.Purchasing.UnityPurchasing.ClearTransactionLog(); }
                catch (Exception) {}

                DBManager.ClearAll();
                DBManager.GetInstance().Init();
            }
        }


        /// <summary>
        /// Increases player level by 1 which unlocks new shop items.
        /// <summary>
        [ContextMenu("LevelUp")]
        public void LevelUp()
        {
            if (DBManager.GetInstance())
            {
                int level = DBManager.AddPlayerData("level", 1);

                IAPManager.GetInstance().RefreshShopItemAll();

                if (UIShopFeedback.GetInstance())
                {
                    UIShopFeedback.ShowMessage("Leveled up to level: " + level + "! Tried to unlock new items.");
                }
            }
        }


        /// <summary>
        /// Consumes product purchase by 1.
        /// </summary>
        [ContextMenu("Consume")]
        public void ConsumeItem()
        {
            IAPManager.Consume("energy");
        }


        /*
        /// <summary>
        /// PlayFab Azure Call for SetItem to update an item.
        /// </summary>
        [ContextMenu("SetItem")]
        public void SetItem()
        {
            PlayFabManager.GetInstance().StartCoroutine(PlayFabManager.SetPurchase(new System.Collections.Generic.Dictionary<string, int>() { { "energy", 5 } }));
        }


        /// <summary>
        /// PlayFab Azure Call for SetItem to update currency.
        /// </summary>
        [ContextMenu("SetCurrency")]
        public void SetCurrency()
        {
            DBManager.AddCurrency("coins", 50);
            PlayFabManager.SetCurrency();
        }
        */
    }
}
