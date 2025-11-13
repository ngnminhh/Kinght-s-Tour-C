/*  This file is part of the "UniPay" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UniPay
{
    [CustomEditor(typeof(ShopItem2D))]
    public class ShopItemEditor : Editor
    {
        private ShopItem2D script;
        private IAPScriptableObject asset;

        private string[] dropdownNames;
        private bool foldout = true;


        void OnEnable()
        {
            script = (ShopItem2D)target;
            asset = IAPScriptableObject.GetOrCreateSettings();
            dropdownNames = new string[] { "Add Currency...", "Real Money" }.Union(asset.currencyList.Select(element => element.ID)).ToArray();
        }


        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Separator();
            foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, "Cost Labels");

            if (foldout)
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    int dropdownIndex = EditorGUILayout.Popup(0, dropdownNames);
                    if (dropdownIndex == 1)
                    {
                        script.costs.Insert(0, new ShopItemCost() { type = IAPExchangeObject.ExchangeType.RealMoney });
                    }
                    else if (dropdownIndex > 1)
                    {
                        IAPCurrency newElement = asset.currencyList.Single(x => x.ID == dropdownNames[dropdownIndex]);
                        script.costs.Add(new ShopItemCost() { type = IAPExchangeObject.ExchangeType.VirtualCurrency, currency = newElement });
                    }

                    foreach (ShopItemCost cost in script.costs)
                    {
                        EditorGUILayout.BeginHorizontal();

                        switch(cost.type)
                        {
                            case IAPExchangeObject.ExchangeType.RealMoney:
                                EditorGUILayout.LabelField(dropdownNames[1]);
                                break;
                            case IAPExchangeObject.ExchangeType.VirtualCurrency:
                                if (cost.currency == null) //v6.0 upgrade notification
                                    Debug.LogWarning("ShopItem2D: " + script.productID + " has missing currency reference! See inspector.");
                                else
                                    EditorGUILayout.LabelField(cost.currency.ID);
                                break;
                        }

                        cost.label = ( Text )EditorGUILayout.ObjectField(cost.label, typeof( Text ), true);

                        if (GUILayout.Button("", IAPSettingsStyles.deleteButtonStyle))
                        {
                            script.costs.Remove(cost);
                            break;
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    if (check.changed)
                    {
                        Undo.RecordObject(script, "Change ShopItem Costs");
                        EditorUtility.SetDirty(script);
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}