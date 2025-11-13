/*  This file is part of the "UniPay" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace UniPay
{
    /// <summary>
    /// Simple script to handle restoring purchases on platforms requiring it. Restoring purchases is a
    /// requirement by e.g. Apple and your app will be rejected if you do not provide it.
    /// </summary>
    public class UIButtonRestore : MonoBehaviour
    {
        /// <summary>
        /// Calls our RestoreTransactions implementation.
        /// It makes sense to add this to an UI button event.
        /// </summary>
        public void Restore()
        {
            IAPManager.RestoreTransactions();
        }
    }
}
