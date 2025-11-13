/*  This file is part of the "UniPay" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using UnityEngine;

namespace UniPay
{
    [Serializable]
    public class IAPExchangeObject
    {
        public ExchangeType type = ExchangeType.RealMoney;
        public enum ExchangeType
        {
            RealMoney,
            VirtualCurrency,
            VirtualProduct
        }

        [SerializeReference]
        public IAPCurrency currency;

        [SerializeReference]
        public IAPProduct product;

        public int amount;

        public string realPrice;
    }
}
