/*  This file is part of the "UniPay" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace UniPay
{
    public class EnumHelper
    {
        /// <summary>
        /// Returns an array of string values for a given enum type.
        /// This will only work if you assign the Description attribute to the enum items.
        /// </summary>
        public static string[] GetEnumDescriptions(Enum e)
        {
            List<string> list = new List<string>();
            foreach (Enum en in Enum.GetValues(e.GetType()))
            { list.Add(GetEnumDescription(en)); }

            return list.ToArray();
        }

        /// <summary>
        /// Returns the string value for a given enum value.
        /// This will only work if you assign the Description attribute to the enum items.
        /// </summary>
        public static string GetEnumDescription(Enum e)
        {
            FieldInfo fieldInfo = e.GetType().GetField(e.ToString());

            DescriptionAttribute[] enumAttributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (enumAttributes.Length > 0)
            {
                return enumAttributes[0].Description;
            }
            return e.ToString();
        }
    }


    /// <summary>
    /// Plugin selector for the User Interface.
    /// </summary>
    public enum UIAssetPlugin
    {
        UnityUI,
        TextMeshPro
    }


    /// <summary>
    /// Plugin selector for the store implementation on standalone.
    /// </summary>
    public enum DesktopPlugin
    {
        [Description("")]
        UnityIAP = 0,

        [Description("OCULUS_IAP")]
        Oculus = 1,

        [Description("STEAM_IAP")]
        Steam = 2,

        [Description("PAYPAL_IAP")]
        PayPal = 3
    }


    /// <summary>
    /// Plugin selector for the store implementation on WebGL.
    /// </summary>
    public enum WebPlugin
    {
        [Description("")]
        UnityIAP = 0,

        [Description("PAYPAL_IAP")]
        PayPal = 1
    }


    /// <summary>
    /// Plugin selector for the store implementation on Android.
    /// </summary>
    public enum AndroidPlugin
    {
        [Description("")]
        UnityIAP = 0,

        [Description("OCULUS_IAP")]
        Oculus = 1
    }


    /// <summary>
    /// Supported billing stores.
    /// </summary>
    public enum IAPPlatform
    {
        NotSpecified = 0,
        GooglePlay = 1,
        AmazonAppStore = 2,
        //SamsungApps = 3, obsolete
        UDP = 4,
        MacAppStore = 5,
        AppleAppStore = 6,
        WinRT = 7,
        //FacebookStore = 8, obsolete
        fake = 9,

        OculusStore = 20,
        SteamStore = 21,
        PayPal = 30
    }


    /// <summary>
    /// Location of where DBManager data is stored.
    /// </summary>
    public enum StorageTarget
    {
        PlayerPrefs,
        PersistentDataPath,
        Memory
    }


    /// <summary>
    /// Type of encryption applied on local storage.
    /// </summary>
    public enum EncryptionType
    {
        None,
        Internal,
        AntiCheatToolkit
    }
}
