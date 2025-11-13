/*  This file is part of the "UniPay" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniPay
{
    /// <summary>
    /// IAP unlock requirement, matched with the database.
    /// </summary>
    [Serializable]
    public class IAPRequirement
    {
        /// <summary>
        /// Database key name for the target value. Value to reach for unlocking this requirement.
        /// </summary>
        [SerializeField]
        public List<KeyValuePairStringInt> pairs = new List<KeyValuePairStringInt>();

        /// <summary>
        /// Optional label text that describes the requirement.
        /// </summary>
        public string label;


        /// <summary>
        /// 
        /// </summary>
        public bool Exists()
        {
            return pairs.Count > 0;
        }
    }
}
