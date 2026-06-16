using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AsGame.Data
{
    /// <summary>关卡编辑器「试玩」：进入 Play 模式时指定要运行的关卡号。</summary>
    public static class LevelEditorPlaySession
    {
        public const string EditorPrefKey = "WaterGame.LevelEditor.PlayTestLevel";

        /// <summary>试玩前调用：写入当前关卡号（与运行时 PlayerModule 读取通道一致）并登记 EditorPrefs。</summary>
        public static void SchedulePlayTest(int levelIndex)
        {
            levelIndex = Mathf.Max(1, levelIndex);

            // 运行时 PlayerModule 通过 SPlayerPrefs.GetInt(PlayerPrefDefines.level) 读取当前关卡。
            SPlayerPrefs.SetInt(PlayerPrefDefines.level, levelIndex);

#if UNITY_EDITOR
            EditorPrefs.SetInt(EditorPrefKey, levelIndex);
#endif
        }

        /// <summary>运行时（仅 Editor Play）：是否应使用试玩关卡号。</summary>
        public static bool TryGetPlayTestLevel(out int levelIndex)
        {
#if UNITY_EDITOR
            if (Application.isPlaying && EditorPrefs.HasKey(EditorPrefKey))
            {
                levelIndex = EditorPrefs.GetInt(EditorPrefKey, 1);
                return true;
            }
#endif
            levelIndex = 0;
            return false;
        }

#if UNITY_EDITOR
        public static void ClearScheduledPlayTest()
        {
            if (EditorPrefs.HasKey(EditorPrefKey))
                EditorPrefs.DeleteKey(EditorPrefKey);
        }
#endif
    }
}
