/*  This file is part of the "UniPay" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEditor;
using UnityEngine;

namespace UniPay
{
    /// <summary>
    /// Custom inspector for splineMove.
    /// <summary>
    [CustomEditor(typeof(DBManager))]
    public class DBManagerEditor : Editor
    {
        private SerializedObject m_Object;


        //called whenever this inspector window is loaded 
        public virtual void OnEnable()
        {
            //we create a reference to our script object by passing in the target
            m_Object = new SerializedObject(target);
        }


        //called whenever the inspector gui gets rendered
        public override void OnInspectorGUI()
        {
            //this pulls the relative variables from unity runtime and stores them in the object
            m_Object.Update();

            //draw custom variable properties by using serialized properties
            EditorGUILayout.PropertyField(m_Object.FindProperty("storageTarget"));

            SerializedProperty encryptionTypeValue = m_Object.FindProperty("encryptionType");
            int oldEncryptionTypeValue = encryptionTypeValue.enumValueIndex;
            EditorGUILayout.PropertyField(encryptionTypeValue);

            if(encryptionTypeValue.enumValueIndex == 1)
            {
                EditorGUILayout.PropertyField(m_Object.FindProperty("obfuscKey"));
            }

            if(encryptionTypeValue.enumValueIndex == 2)
            {
                #if !ACTK_IS_HERE
                Debug.LogWarning("Did you import ACTK? First enable the ACTK_IS_HERE Compatibility Symbol in ACTK Project Settings, then try again.");
                encryptionTypeValue.enumValueIndex = oldEncryptionTypeValue;
                #endif
            }

            //we push our modified variables back to our serialized object
            m_Object.ApplyModifiedProperties();
        }
    }
}
