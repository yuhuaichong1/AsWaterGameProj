using UnityEditor;
using UnityEngine;

namespace XrSDK
{
    [CustomEditor(typeof(FirebasePushModConfig))]
    public class FirebasePushModConfigEditor : UnityEditor.Editor
    {
        private const string CloudDefine = "FIREBASE_USE_CLOUD_BACKEND";
        private SerializedProperty _backendModeProp;
        private FirebasePushBackendMode _lastBackendMode;

        private void OnEnable()
        {
            _backendModeProp = serializedObject.FindProperty("BackendMode");
            if (_backendModeProp != null)
                _lastBackendMode = (FirebasePushBackendMode)_backendModeProp.enumValueIndex;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            FirebasePushModConfig config = (FirebasePushModConfig)target;
            bool cloudDefineOn = HasDefine(CloudDefine);

            EditorGUILayout.HelpBox(
                "两种模式共用 FirebaseMessaging。\n" +
                "• FirebaseCloud：需导入 Auth+Firestore，并开启宏 FIREBASE_USE_CLOUD_BACKEND（切换到此模式会自动添加）\n" +
                "• LocalServer：可不导入 Auth/Firestore；填写 LocalTokenRegisterUrl 或接管 FacadeFirebasePush.UploadTokenToLocalServer",
                MessageType.Info);

            // 先画默认 Inspector，才能立刻读到用户改后的 BackendMode
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            FirebasePushBackendMode currentMode = config.BackendMode;
            if (currentMode != _lastBackendMode)
            {
                if (currentMode == FirebasePushBackendMode.FirebaseCloud && !HasDefine(CloudDefine))
                {
                    SetDefine(CloudDefine, true);
                    cloudDefineOn = true;
                    Debug.Log("[FirebasePush] BackendMode 切换为 FirebaseCloud，已自动添加宏 FIREBASE_USE_CLOUD_BACKEND");
                }
                _lastBackendMode = currentMode;
            }

            cloudDefineOn = HasDefine(CloudDefine);

            if (config.BackendMode == FirebasePushBackendMode.FirebaseCloud)
            {
                if (cloudDefineOn)
                {
                    EditorGUILayout.LabelField("宏状态", CloudDefine + " = ON");
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "当前为 FirebaseCloud，但宏未开启，云端 Auth/Firestore 代码不会编译。",
                        MessageType.Warning);
                    if (GUILayout.Button("启用宏 FIREBASE_USE_CLOUD_BACKEND"))
                        SetDefine(CloudDefine, true);
                }
            }
            else if (config.BackendMode == FirebasePushBackendMode.LocalServer && cloudDefineOn)
            {
                EditorGUILayout.HelpBox(
                    "当前为 LocalServer。若已移除 Auth/Firestore 包，请关闭宏，避免误引用。切换模式不会自动删除宏。",
                    MessageType.Info);
                if (GUILayout.Button("关闭宏 FIREBASE_USE_CLOUD_BACKEND"))
                    SetDefine(CloudDefine, false);
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
            var list = new System.Collections.Generic.List<string>(
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
