using UnityEngine;

namespace AsGame.Scenes
{
    /// <summary>
    /// Entry point in the Water scene. Canvases (Loading/Home/Gameplay) are built at runtime by C# — not saved as prefabs.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameEntry : MonoBehaviour
    {
        void Awake()
        {
            if (SceneFlowManager.Instance != null) return;
            var go = new GameObject("SceneFlowManager", typeof(SceneFlowManager));
            DontDestroyOnLoad(go);
        }
    }
}
