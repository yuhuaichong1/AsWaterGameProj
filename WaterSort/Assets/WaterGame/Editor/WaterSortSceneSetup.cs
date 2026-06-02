#if UNITY_EDITOR
using AsGame.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AsGame.Editor
{
    public static class WaterSortSceneSetup
    {
        const string GameScenePath = "Assets/WaterGame/Scenes/Game.unity";

        [MenuItem("AsGame/Setup Water Scene (Add Entry Object)", false, 50)]
        public static void SetupGameSceneEntry()
        {
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            if (UnityEngine.Object.FindObjectOfType<GameEntry>() != null)
            {
                EditorUtility.DisplayDialog("Water Scene", "Entry object already exists.", "OK");
                return;
            }

            var go = new GameObject("--- AsGame Entry (Press Play) ---");
            go.AddComponent<GameEntry>();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = go;
            EditorUtility.DisplayDialog("Done",
                "Entry object added.\n\n" +
                "1. Run AsGame → Generate All UI Prefabs (first time)\n" +
                "2. Open Assets/WaterGame/Scenes/Game.unity\n" +
                "3. Press Play",
                "OK");
        }
    }
}
#endif
