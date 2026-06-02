using UnityEngine;

namespace AsGame.UI
{
    public static class ResourcePrefabLoader
    {
        public static GameObject Instantiate(string resourcesPath, Transform parent = null)
        {
            var prefab = Resources.Load<GameObject>(resourcesPath);
            if (prefab == null)
            {
                Debug.LogError(
                    $"[ResourcePrefabLoader] Missing prefab at Resources/{resourcesPath}. " +
                    "Run AsGame → Generate All UI Prefabs in Unity Editor.");
                return null;
            }

            return parent != null
                ? UnityEngine.Object.Instantiate(prefab, parent)
                : UnityEngine.Object.Instantiate(prefab);
        }

        public static T Instantiate<T>(string resourcesPath, Transform parent = null) where T : Component
        {
            var go = Instantiate(resourcesPath, parent);
            return go != null ? go.GetComponent<T>() : null;
        }
    }
}
