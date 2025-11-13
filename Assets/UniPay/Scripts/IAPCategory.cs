/*  This file is part of the "UniPay" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniPay
{
    /// <summary>
    /// IAP Settings editor group properties. Each group holds a list of IAPObject.
    /// </summary>
    [Serializable]
    public class IAPCategory
    {
        /// <summary>
        /// The unique group id for identifying mappings to an IAPContainer in the scene.
        /// </summary>
        [HideInInspector]
        public string referenceID;

        /// <summary>
        /// The unique name of the group.
        /// </summary>
        public string ID;

        /// <summary>
        /// Overrides for category to disable it on selected store platforms.
        /// </summary>
        public List<StoreMetaDefinition> storeIDs = new List<StoreMetaDefinition>();
    }
}
