/*  This file is part of the "UniPay" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace UniPay
{
    using UnityEngine.Purchasing;

    /// <summary>
    /// Base class for receipt validation implementations.
    /// </summary>
	public class ReceiptValidator : MonoBehaviour
    {    
        /// <summary>
        /// Determines if a validation is possible on this platform.
        /// </summary>
        public virtual bool CanValidate()
        {
            return false;
        }


        /// <summary>
        /// Validation for all products.
        /// </summary>
        public virtual void Validate(Product p = null)
        {
            
        }


        /// <summary>
        /// Validation for unified receipts i.e. Apple App Store.
        /// </summary>
        public virtual void Validate(string receipt)
        {

        }
    }
}