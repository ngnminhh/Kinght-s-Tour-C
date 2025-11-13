/*  This file is part of the "UniPay" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;

namespace UniPay
{
    /// <summary>
    /// Override options for store settings on specific products.
    /// </summary>
    [Serializable]
    public class StoreMetaDefinition
    {
        public bool active = true;

        public string store;

        public string ID;
    }
}
