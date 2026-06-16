using AsGame.Data;
using UnityEditor;

namespace AsGame.Editor.LevelEditor
{
    [InitializeOnLoad]
    static class LevelEditorPlaySessionHooks
    {
        static LevelEditorPlaySessionHooks()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                LevelEditorPlaySession.ClearScheduledPlayTest();
        }
    }
}
