using AsGame.Core;
using UnityEditor;
using UnityEngine;

namespace AsGame.Editor.LevelEditor
{
    public class LevelEditorLayoutSettings : ScriptableObject
    {
        public const string AssetPath = "Assets/WaterGame/Editor/LevelEditor/LevelEditorLayoutSettings.asset";

        public GameplayScreenLayoutData layout = new();
        public float bottleWidth = 91f;
        public float bottleHeight = 245f;
        public int waterColorCount = 3;
        public int waterTotalLayers = 12;
        public int waterDifficulty;
        /// <summary>关卡编辑器左侧面板宽度（右侧为局内预览）。</summary>
        public float leftPanelWidth = 420f;

        /// <summary>左侧「瓶子」组内容区高度（可拖拽调整）。</summary>
        public float sectionHeightCups = 360f;

        static LevelEditorLayoutSettings _instance;

        public static LevelEditorLayoutSettings Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = AssetDatabase.LoadAssetAtPath<LevelEditorLayoutSettings>(AssetPath);
                if (_instance == null)
                {
                    _instance = CreateInstance<LevelEditorLayoutSettings>();
                    _instance.layout = GameplayScreenLayout.Default.Clone();
                    var dir = System.IO.Path.GetDirectoryName(AssetPath);
                    if (!System.IO.Directory.Exists(dir))
                        System.IO.Directory.CreateDirectory(dir);
                    AssetDatabase.CreateAsset(_instance, AssetPath);
                    AssetDatabase.SaveAssets();
                }

                return _instance;
            }
        }

        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
}
