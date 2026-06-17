using System.IO;
using UnityEngine;

# if UNITY_EDITOR || UNITY_ANDRIOD 
using UnityEditor;
#endif

namespace XrSDK
{
    public static class RuntimeEditorUtils
    {
        public static T GetAssetByName<T>(string name = "") where T : Object
        {
# if UNITY_EDITOR || UNITY_ANDRIOD 
            string[] assets = AssetDatabase.FindAssets((string.IsNullOrEmpty(name) ? "" : name + " ") + "t:" + typeof(T).Name);
            if (assets.Length > 0)
            {
                if (string.IsNullOrEmpty(name))
                {
                    return (T)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assets[0]), typeof(T));
                }
                else
                {
                    string assetPath;
                    for (int i = 0; i < assets.Length; i++)
                    {
                        assetPath = AssetDatabase.GUIDToAssetPath(assets[i]);
                        if (Path.GetFileNameWithoutExtension(assetPath) == name)
                        {
                            return (T)AssetDatabase.LoadAssetAtPath(assetPath, typeof(T));
                        }
                    }
                }
            }
#endif

            return null;
        }

        public static void SetDirty(Object obj)
        {
# if UNITY_EDITOR || UNITY_ANDRIOD 
            EditorUtility.SetDirty(obj);
#endif
        }

        public static void ToggleVisibility(this GameObject gameObject, bool state)
        {
# if UNITY_EDITOR || UNITY_ANDRIOD 
            SceneVisibilityManager.instance.ToggleVisibility(gameObject, state);
#endif
        }

        public static void TogglePicking(this GameObject gameObject, bool state)
        {
# if UNITY_EDITOR || UNITY_ANDRIOD 
            
            SceneVisibilityManager.instance.TogglePicking(gameObject, state);
#endif
        }
    }
}
