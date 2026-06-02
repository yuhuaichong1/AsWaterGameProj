#if UNITY_EDITOR
namespace AsGame.Editor.CocosImport
{
    public sealed class CocosImportResult
    {
        public string PrefabName;
        public string Category;
        public string CocosSource;
        public string UnityPrefabPath;
        public string UnityScript;
        public int LabelCount;
        public int SpriteCount;
        public bool Success;
        public string Error;
    }
}
#endif
