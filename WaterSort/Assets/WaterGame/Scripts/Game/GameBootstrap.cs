using UnityEngine;
using UnityEngine.EventSystems;
using AsGame.Core;
using AsGame.Scenes;
using XrCode;

namespace AsGame.Water
{
    /// <summary>Legacy debug bootstrap. Use SceneFlowManager + UI prefabs instead.</summary>
    public static class GameBootstrap
    {
        [System.Obsolete("Use SceneFlowManager and Generate All UI Prefabs instead.")]
        public static void BuildGame()
        {
            if (SceneFlowManager.Instance == null)
            {
                var flow = new GameObject("SceneFlowManager", typeof(SceneFlowManager));
                Object.DontDestroyOnLoad(flow);
            }

            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Object.DontDestroyOnLoad(es);
            }

            SceneFlowManager.LoadScene(SceneId.Game);
        }
    }
}
