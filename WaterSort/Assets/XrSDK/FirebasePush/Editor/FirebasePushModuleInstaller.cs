using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace XrSDK
{
    /// <summary>
    /// 通过 Unity API 把 FirebasePush 正确挂到 Project Init Settings。
    /// 不要手改 YAML 子资源，否则会出现 Object reference is null。
    /// </summary>
    public static class FirebasePushModuleInstaller
    {
        private const string SettingsPath = "Assets/XrSDK/Resources/Settings/Project Init Settings.asset";
        private const string SessionKey = "FirebasePush.ModuleRepaired.v2";
        private const string CloudDefine = "FIREBASE_USE_CLOUD_BACKEND";

        [InitializeOnLoadMethod]
        private static void AutoRepairOnLoad()
        {
            if (SessionState.GetBool(SessionKey, false))
                return;

            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += () =>
            {
                RepairFirebaseModule(forceLog: false);
                EnsureCloudDefineForCurrentSettings();
            };
        }

        [MenuItem("Tools/Firebase Push/Repair Module In Project Init Settings")]
        private static void MenuRepair()
        {
            SessionState.EraseBool(SessionKey);
            RepairFirebaseModule(forceLog: true);
            EnsureCloudDefineForCurrentSettings();
        }

        [MenuItem("Tools/Firebase Push/Enable Cloud Backend Define")]
        private static void MenuEnableCloudDefine()
        {
            SetDefine(CloudDefine, true);
        }

        [MenuItem("Tools/Firebase Push/Disable Cloud Backend Define")]
        private static void MenuDisableCloudDefine()
        {
            SetDefine(CloudDefine, false);
        }

        public static void RepairFirebaseModule(bool forceLog)
        {
            ProjectInitSettings settings = AssetDatabase.LoadAssetAtPath<ProjectInitSettings>(SettingsPath);
            if (settings == null)
            {
                if (forceLog)
                    Debug.LogError("[FirebasePush] Project Init Settings not found: " + SettingsPath);
                return;
            }

            SerializedObject so = new SerializedObject(settings);
            SerializedProperty modulesProp = so.FindProperty("modules");
            if (modulesProp == null)
            {
                Debug.LogError("[FirebasePush] modules property not found.");
                return;
            }

            // 1) 清掉 null / missing
            for (int i = modulesProp.arraySize - 1; i >= 0; i--)
            {
                Object refObj = modulesProp.GetArrayElementAtIndex(i).objectReferenceValue;
                if (refObj == null)
                {
                    modulesProp.DeleteArrayElementAtIndex(i);
                    continue;
                }

                // 2) 清掉旧的坏 Firebase 子资源（手改 YAML 残留）
                if (refObj is FirebasePushModConfig ||
                    refObj.name.Contains("FirebasePush"))
                {
                    modulesProp.GetArrayElementAtIndex(i).objectReferenceValue = null;
                    modulesProp.DeleteArrayElementAtIndex(i);
                    Object.DestroyImmediate(refObj, true);
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            // 3) 顺带清掉 asset 内孤儿 Firebase 子对象
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(SettingsPath);
            foreach (Object obj in all)
            {
                if (obj == null || obj == settings)
                    continue;
                if (obj is FirebasePushModConfig || obj.name.Contains("FirebasePush"))
                    Object.DestroyImmediate(obj, true);
            }

            // 4) 用 Unity API 重新挂接
            FirebasePushModConfig module = ScriptableObject.CreateInstance<FirebasePushModConfig>();
            module.name = typeof(FirebasePushModConfig).ToString();
            module.hideFlags = HideFlags.None;
            module.BackendMode = FirebasePushBackendMode.FirebaseCloud;
            module.RequestPermissionOnStart = false;
            module.EnableAnonymousAuth = true;
            module.SaveTokenToFirestore = true;
            module.UsersCollection = "users";
            module.SubscribeDefaultTopic = true;
            module.DefaultTopic = "all_users";
            module.LocalTokenRegisterUrl = "";
            module.ReportTokenToLocalServer = true;

            AssetDatabase.AddObjectToAsset(module, settings);

            so.Update();
            modulesProp = so.FindProperty("modules");
            modulesProp.arraySize++;
            modulesProp.GetArrayElementAtIndex(modulesProp.arraySize - 1).objectReferenceValue = module;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(module);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(SettingsPath, ImportAssetOptions.ForceUpdate);

            Debug.Log("[FirebasePush] Firebase Push Module repaired and linked to Project Init Settings.");
        }

        private static void EnsureCloudDefineForCurrentSettings()
        {
            ProjectInitSettings settings = AssetDatabase.LoadAssetAtPath<ProjectInitSettings>(SettingsPath);
            if (settings == null || settings.Modules == null)
                return;

            foreach (BaseModulePendant m in settings.Modules)
            {
                if (m is FirebasePushModConfig cfg &&
                    cfg.BackendMode == FirebasePushBackendMode.FirebaseCloud &&
                    !HasDefine(CloudDefine))
                {
                    SetDefine(CloudDefine, true);
                    return;
                }
            }
        }

        private static bool HasDefine(string define)
        {
            BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            return (";" + defines + ";").Contains(";" + define + ";");
        }

        private static void SetDefine(string define, bool enable)
        {
            BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            var list = new List<string>(
                defines.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries));

            if (enable)
            {
                if (!list.Contains(define))
                    list.Add(define);
            }
            else
            {
                list.Remove(define);
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", list.ToArray()));
            Debug.Log("[FirebasePush] Scripting Define updated: " + string.Join(";", list.ToArray()));
        }
    }
}
