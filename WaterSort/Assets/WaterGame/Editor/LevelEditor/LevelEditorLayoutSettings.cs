using AsGame.Core;
using UnityEditor;
using UnityEngine;

namespace AsGame.Editor.LevelEditor
{
    public class LevelEditorLayoutSettings : ScriptableObject
    {
        public const string AssetPath = "Assets/WaterGame/Editor/LevelEditor/LevelEditorLayoutSettings.asset";

        public GameplayScreenLayoutData layout = new();

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
