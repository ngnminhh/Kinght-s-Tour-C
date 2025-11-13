/*  This file is part of the "UniPay" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniPay
{
    #if ACTK_IS_HERE
    using CodeStage.AntiCheat.Storage;
    #endif

    class IAPSettingsProvider : SettingsProvider
    {
        private SerializedObject serializedObject;
        private IAPScriptableObject asset;

        int toolbarIndex = 0;
        string[] toolbarNames = new string[] { "Setup", "Tools", "About" };
        string errorMessage = "";

        public bool isChanged = false;
        public bool isPackageImported = false;
        public ListRequest pckList;

        private DesktopPlugin desktopPlugin = DesktopPlugin.UnityIAP;
        private WebPlugin webPlugin = WebPlugin.UnityIAP;
        private AndroidPlugin androidPlugin = AndroidPlugin.UnityIAP;

        private string packagesPath;
        private enum ExtensionPackages
        {
            VR = 0,
            PlayFab = 1
        }

        private UIAssetPlugin uiPlugin = UIAssetPlugin.UnityUI;

        private bool customStoreFoldout = false; 
        private string databaseContent;

        class Styles
        {
            public static GUIContent Info = new GUIContent("Welcome! This section contains the billing setup, store settings and other tools for UniPay. When you are happy with your billing configuration, " +
                                                           "expand this section in the menu on the left, in order to define your in-app purchase categories and products.");
        }

        public IAPSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }


        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            serializedObject = IAPScriptableObject.GetSerializedSettings();
            asset = serializedObject.targetObject as IAPScriptableObject;

            var script = MonoScript.FromScriptableObject(asset);
            string thisPath = AssetDatabase.GetAssetPath(script);
            packagesPath = thisPath.Replace("/Scripts/IAPScriptableObject.cs", "/Editor/Packages/");

            GetScriptingDefines();
            GetDatabaseContent();
        }


        public override void OnDeactivate()
        {
            AssetDatabase.SaveAssets();
        }


        public override void OnGUI(string searchContext)
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(Styles.Info.text, MessageType.None);
            GUILayout.Space(5);

            DrawListElement();

            EditorUtility.SetDirty(serializedObject.targetObject);
            serializedObject.ApplyModifiedProperties();
        }


        void DrawListElement()
        {
            toolbarIndex = GUILayout.Toolbar(toolbarIndex, toolbarNames);

            switch (toolbarIndex)
            {
                case 0:
                    DrawToolBar0();
                    break;
                case 1:
                    DrawToolBar1();
                    break;
                case 2:
                    DrawToolBar2();
                    break;
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();

                GUI.color = Color.yellow;
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                GUI.color = Color.white;
                EditorGUILayout.HelpBox(errorMessage, MessageType.Warning, true);
                if (GUILayout.Button("OK", GUILayout.Width(80), GUILayout.Height(38)))
                {
                    errorMessage = string.Empty;
                }

                EditorGUILayout.EndHorizontal();
                GUI.color = Color.white;
                EditorGUILayout.Space();
            }
        }


        void DrawToolBar0()
        {
            EditorGUILayout.Space();
            DrawBillingSetup();

            EditorGUILayout.Space(20);
            DrawCustomExtensions();

            EditorGUILayout.Space(20);
            DrawStoreExtensions();
        }


        void DrawBillingSetup()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Unity IAP", EditorStyles.boldLabel);
            Color defaultColor = GUI.contentColor;

            //Check Unity PackageManager package
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Package Imported:", GUILayout.MaxWidth(120));
            if (pckList == null || !pckList.IsCompleted)
            {
                GUI.contentColor = Color.yellow;
                EditorGUILayout.LabelField("CHECKING...", GUILayout.MaxWidth(80));
                GUI.contentColor = defaultColor;
            }
            else if (!isPackageImported)
            {
                PackageCollection col = pckList.Result;
                foreach (UnityEditor.PackageManager.PackageInfo info in col)
                {
                    if (info.packageId.StartsWith("com.unity.purchasing", System.StringComparison.Ordinal))
                    {
                        isPackageImported = true;
                        break;
                    }
                }
            }

            GUI.contentColor = isPackageImported ? Color.green : Color.red;
            if(pckList != null && pckList.IsCompleted)
                EditorGUILayout.LabelField(isPackageImported == true ? "OK" : "NOT OK", GUILayout.MaxWidth(20));
            GUI.contentColor = defaultColor;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                GUI.enabled = isPackageImported;

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Custom Stores", EditorStyles.boldLabel);
                desktopPlugin = (DesktopPlugin)EditorGUILayout.EnumPopup("Standalone:", desktopPlugin);
                webPlugin = (WebPlugin)EditorGUILayout.EnumPopup("WebGL:", webPlugin);
                androidPlugin = (AndroidPlugin)EditorGUILayout.EnumPopup("Android:", androidPlugin);

                EditorGUILayout.EndVertical();
                GUI.enabled = true;

                if (check.changed)
                {
                    isChanged = check.changed;
                }
            }

            EditorGUILayout.EndHorizontal();
            if (isChanged) GUI.color = Color.yellow;

            EditorGUILayout.Space();
            if (GUILayout.Button("Apply"))
            {
                ApplyScriptingDefines();
                isChanged = false;
            }
            GUI.color = Color.white;
            #if UNITY_6000_0_OR_NEWER
            EditorGUILayout.LabelField("Note: the Store target per Platform is only set in the general Player Settings, not in any custom Build Profiles.");
            #endif
        }


        void DrawCustomExtensions()
        {
            EditorGUILayout.LabelField("Custom Extensions", EditorStyles.boldLabel);

            if (GUILayout.Button("Virtual Reality") && EditorUtility.DisplayDialog("VR Package Import", "This imports an extension package into your project. Please confirm.", "Ok", "Cancel"))
            {
                AssetDatabase.ImportPackage(packagesPath + ExtensionPackages.VR.ToString() + ".unitypackage", true);
            }

            if (GUILayout.Button("PlayFab (External SDK)") && EditorUtility.DisplayDialog("PlayFab Package Import", "This imports an extension package into your project. Please confirm.", "Ok", "Cancel"))
            {
                AssetDatabase.ImportPackage(packagesPath + ExtensionPackages.PlayFab.ToString() + ".unitypackage", true);
            }
        }


        void DrawStoreExtensions()
        {
            EditorGUILayout.LabelField("Store Extensions", EditorStyles.boldLabel);
            customStoreFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(customStoreFoldout, "PayPal");
            if (customStoreFoldout)
            {
                asset.customStoreConfig.PayPal.currencyCode = EditorGUILayout.TextField("Currency Code:", asset.customStoreConfig.PayPal.currencyCode);
                asset.customStoreConfig.PayPal.sandbox.clientID = EditorGUILayout.TextField("Sandbox Client ID:", asset.customStoreConfig.PayPal.sandbox.clientID);
                asset.customStoreConfig.PayPal.sandbox.secretKey = EditorGUILayout.TextField("Sandbox Secret:", asset.customStoreConfig.PayPal.sandbox.secretKey);
                asset.customStoreConfig.PayPal.live.clientID = EditorGUILayout.TextField("Live Client ID:", asset.customStoreConfig.PayPal.live.clientID);
                asset.customStoreConfig.PayPal.live.secretKey = EditorGUILayout.TextField("Live Secret:", asset.customStoreConfig.PayPal.live.secretKey);
                asset.customStoreConfig.PayPal.returnUrl = EditorGUILayout.TextField("Return URL:", asset.customStoreConfig.PayPal.returnUrl);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }


        void DrawToolBar1()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Backup", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Import from JSON"))
            {
                string path = EditorUtility.OpenFolderPanel("Import IAP Settings from JSON", "", "");
                if (path.Length != 0)
                {
                    asset.currencyList = IAPSettingsExporter.FromJSONCurrency(File.ReadAllText(path + "/UniPay_Currencies.json"));
                    asset.categoryList = IAPSettingsExporter.FromJSONCategory(File.ReadAllText(path + "/UniPay_IAPSettings.json"));
                    asset.productList = IAPSettingsExporter.FromJSONProduct(File.ReadAllText(path + "/UniPay_IAPSettings.json"));
                    return;
                }
            }

            if (GUILayout.Button("Export to JSON"))
            {
                string path = EditorUtility.SaveFolderPanel("Save IAP Settings as JSON", "", "");
                if (path.Length != 0)
                {
                    File.WriteAllBytes(path + "/UniPay_IAPSettings.json", System.Text.Encoding.UTF8.GetBytes(IAPSettingsExporter.ToJSON(asset.productList)));
                    File.WriteAllBytes(path + "/UniPay_Currencies.json", System.Text.Encoding.UTF8.GetBytes(IAPSettingsExporter.ToJSON(asset.currencyList)));                    
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Local Storage", EditorStyles.boldLabel);

            GUI.enabled = Application.isPlaying;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh"))
            {
                GetDatabaseContent();
            }

            if (GUILayout.Button("Clear"))
            {
                if (EditorUtility.DisplayDialog("Clear Local Database Entries",
                                                "Are you sure you want to clear the PlayerPref and PersistentStorage data for this project? (This includes UniPay data, but also all other PlayerPrefs)", "Clear", "Cancel"))
                {
                    string unityPurchasingPath = Path.Combine(Path.Combine(Application.persistentDataPath, "Unity"), "UnityPurchasing");
                    
                    if (Directory.Exists(unityPurchasingPath))
                        UnityEngine.Purchasing.UnityPurchasing.ClearTransactionLog();

                    if (Directory.Exists(unityPurchasingPath))
                        Directory.Delete(unityPurchasingPath, true);

                    #if ACTK_IS_HERE
                    if(!ObscuredFilePrefs.IsInited) ObscuredFilePrefs.Init();
                    ObscuredPrefs.DeleteAll();
                    ObscuredFilePrefs.DeleteAll();
                    #endif

                    PlayerPrefs.DeleteAll(); //PlayerPrefs
                    File.Delete(Application.persistentDataPath + "/" + DBManager.prefsKey + DBManager.persistentFileExt); //PersistentStorage

                    if (DBManager.GetInstance() != null)
                        DBManager.GetInstance().Init();

                    GetDatabaseContent();
                }
            }
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            EditorGUILayout.SelectableLabel(string.IsNullOrEmpty(databaseContent) ? "no data / only visible at runtime" : databaseContent, GUI.skin.GetStyle("HelpBox"), GUILayout.MaxHeight(150));

            EditorGUILayout.Space();
            DrawSourceCustomization();

            EditorGUILayout.Space(15);
            GUILayout.Label("Server-Side Receipt Validation", EditorStyles.boldLabel);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("The integration with IAPGUARD allows for the most secure receipt validation method available, without having to manage your own servers." +
                                "\n\nIAPGUARD offers receipt validation for all product types to detect fake receipts, active or expired subscriptions and billing issues with user's payments. " +
                                "Based on this information, you can tell users to take action in order to finish their checkout. " +
                                "Security measures ensure that a transaction is valid and only redeemed once across your app, protecting your revenue and preventing duplicate purchase attempts.",
                                new GUIStyle(EditorStyles.label) { wordWrap = true });
                GUILayout.Space(5);

                GUI.color = Color.yellow;
                if (GUILayout.Button("Check it out!"))
                {
                    Help.BrowseURL("https://iapguard.com");
                }
                GUI.color = Color.white;
            }
            GUILayout.EndVertical();
        }


        void DrawSourceCustomization()
        {
            EditorGUILayout.LabelField("Source Customization", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("UI", GUILayout.Width(IAPSettingsStyles.buttonWidth));

            uiPlugin = (UIAssetPlugin)EditorGUILayout.EnumPopup(uiPlugin);

            if (GUILayout.Button("Convert", GUILayout.Width(IAPSettingsStyles.buttonWidth)))
            {
                if (EditorUtility.DisplayDialog("Convert UI Source Code",
                                                "Are you sure you want to convert the UI references in code? If choosing a UI solution other than Unity UI, " +
                                                "all sample shop prefabs and demo scenes will break. You should not do this without a backup.", "Continue", "Cancel"))
                {
                    UISourceConverterData.Convert(uiPlugin);
                }
            }
            EditorGUILayout.EndHorizontal();
        }


        void DrawToolBar2()
        {
            EditorGUILayout.Space();
            GUILayout.Label("Info", EditorStyles.boldLabel);
            if (GUILayout.Button("Homepage", GUILayout.Width(IAPSettingsStyles.buttonWidth)))
            {
                Help.BrowseURL("https://flobuk.com");
            }

            EditorGUILayout.Space();
            GUILayout.Label("Support", EditorStyles.boldLabel);
            if (GUILayout.Button("Online Documentation", GUILayout.Width(IAPSettingsStyles.buttonWidth)))
            {
                Help.BrowseURL("https://flobuk.gitlab.io/assets/docs/pay");
            }

            if (GUILayout.Button("Scripting Reference", GUILayout.Width(IAPSettingsStyles.buttonWidth)))
            {
                Help.BrowseURL("https://flobuk.gitlab.io/assets/docs/pay/api/");
            }

            if (GUILayout.Button("Unity Forum", GUILayout.Width(IAPSettingsStyles.buttonWidth)))
            {
                Help.BrowseURL("https://forum.unity3d.com/threads/194975");
            }

            if (GUILayout.Button("Discord", GUILayout.Width(IAPSettingsStyles.buttonWidth)))
            {
                Help.BrowseURL("https://discord.gg/E8DPRe25MS");
            }

            EditorGUILayout.Space();
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("Support me! :-)", EditorStyles.boldLabel);
                GUILayout.Label("Please consider leaving a small donation or positive rating on the Unity Asset Store. As a solo developer, each review counts! " +
                                "Your support helps me stay motivated, improving this asset and making it more popular. \n\nIf you are looking for support, please head to the Unity Forum or Discord (see Support buttons above).", new GUIStyle(EditorStyles.label) { wordWrap = true });
                GUILayout.Space(15f);

                GUILayout.BeginHorizontal();
                GUI.color = Color.yellow;
                if (GUILayout.Button("Donation", GUILayout.Width(IAPSettingsStyles.buttonWidth)))
                {
                    Help.BrowseURL("https://flobuk.com");
                }
                if (GUILayout.Button("Review Asset", GUILayout.Width(IAPSettingsStyles.buttonWidth)))
                {
                    Help.BrowseURL("https://assetstore.unity.com/packages/slug/192362?aid=1011lGiF&pubref=editor_sis");
                }
                GUI.color = Color.white;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }


        void GetScriptingDefines()
        {
            desktopPlugin = (DesktopPlugin)FindScriptingDefineIndex(NamedBuildTarget.Standalone);
            webPlugin = (WebPlugin)FindScriptingDefineIndex(NamedBuildTarget.WebGL);
            androidPlugin = (AndroidPlugin)FindScriptingDefineIndex(NamedBuildTarget.Android);

            //check Unity IAP
            isPackageImported = false;

            //download PackageManager list, retrieved later
            pckList = Client.List(true);
        }


        int FindScriptingDefineIndex(NamedBuildTarget namedTarget)
        {
            BuildTargetGroup groupTarget = namedTarget.ToBuildTargetGroup();
            string str = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
            string[] defines = null;

            switch (groupTarget)
            {
                case BuildTargetGroup.Standalone:
                    defines = EnumHelper.GetEnumDescriptions(desktopPlugin);
                    break;
                case BuildTargetGroup.WebGL:
                    defines = EnumHelper.GetEnumDescriptions(webPlugin);
                    break;
                case BuildTargetGroup.Android:
                    defines = EnumHelper.GetEnumDescriptions(androidPlugin);
                    break;
                case BuildTargetGroup.Unknown:
                    BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;
                    BuildTargetGroup activeGroup = BuildPipeline.GetBuildTargetGroup(activeTarget);
                    str = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(activeGroup));
                    break;
            }

            for (int i = 1; i < defines.Length; i++)
            {
                if (str.Contains(defines[i]))
                {
                    return i;
                }
            }

            return 0;
        }


        void ApplyScriptingDefines()
        {
            SetScriptingDefine(NamedBuildTarget.Standalone, EnumHelper.GetEnumDescriptions(desktopPlugin), (int)desktopPlugin);
            SetScriptingDefine(NamedBuildTarget.WebGL, EnumHelper.GetEnumDescriptions(webPlugin), (int)webPlugin);
            SetScriptingDefine(NamedBuildTarget.Android, EnumHelper.GetEnumDescriptions(androidPlugin), (int)androidPlugin);
        }


        void SetScriptingDefine(NamedBuildTarget namedTarget, string[] allDefines, int newDefine)
        {
            string[] defines;
            PlayerSettings.GetScriptingDefineSymbols(namedTarget, out defines);
            List<string> list = defines.ToList();

            for (int i = 0; i < allDefines.Length; i++)
            {
                if (string.IsNullOrEmpty(allDefines[i])) continue;
                list.Remove(allDefines[i]);
            }

            if (newDefine > 0)
                list.Add(allDefines[newDefine]);

            defines = list.ToArray();
            PlayerSettings.SetScriptingDefineSymbols(namedTarget, defines);
        }


        void GetDatabaseContent()
        {
            if (Application.isPlaying && IAPManager.GetInstance() != null && DBManager.GetInstance() != null)
                databaseContent = DBManager.Read();

            GUIUtility.keyboardControl = 0;
        }


        // Register the SettingsProvider
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new IAPSettingsProvider("Project/UniPay In-App Purchasing", SettingsScope.Project);

            // Automatically extract all keywords from the Styles.
            provider.keywords = GetSearchKeywordsFromGUIContentProperties<Styles>();
            return provider;
        }
    }
}