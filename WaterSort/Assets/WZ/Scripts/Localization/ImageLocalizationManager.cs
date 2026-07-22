namespace WZSDK
{
    using System.Collections.Generic;
    using UnityEngine;
    
    public class ImageLocalizationManager : MonoBehaviour
    {
        public static ImageLocalizationManager Instance { get; private set; }
    
        [Header("图片多语言配置")]
        LanguageType defaultLanguage = LanguageType.Chinese;
        private const string ImageCsvName = "ImageLocalization";
        string imageFolderPath = "";
    
        // 修改数据结构：存储图片路径而不是直接加载Sprite
        private Dictionary<LanguageType, Dictionary<string, string>> imagePathData;
        private Dictionary<string, Sprite> loadedSprites; // 缓存已加载的图片
        private LanguageType currentLanguage;
    
        public System.Action OnImageLanguageChanged;
    
        private Dictionary<LanguageType, string> languageCodeMap = new Dictionary<LanguageType, string>()
        {
            { LanguageType.Chinese, "zh-CN" },
            { LanguageType.English, "en-US" },
            { LanguageType.Japanese, "ja-JP" },
            { LanguageType.Brazilian, "pt-BR" },
            { LanguageType.Indonesian, "id-ID" }
        };
    
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    
        void Initialize()
        {
            imagePathData = new Dictionary<LanguageType, Dictionary<string, string>>();
            loadedSprites = new Dictionary<string, Sprite>();
            LoadImagePathData();
        }
    
        void LoadImagePathData()
        {
            List<string[]> rows = UserDataManager.GetCsvRows(ImageCsvName);
            if (rows.Count == 0)
            {
                return;
            }
    
            if (rows.Count < 2)
            {
                Debug.LogError("图片CSV文件格式错误！");
                return;
            }
    
            // 解析表头获取语言列
            string[] headers = rows[0];
    
            // 初始化语言字典
            for (int i = 1; i < headers.Length; i++)
            {
                LanguageType langType = GetLanguageTypeFromCode(headers[i]);
                imagePathData[langType] = new Dictionary<string, string>();
            }
    
            // 填充路径数据
            for (int i = 1; i < rows.Count; i++)
            {
                string[] values = rows[i];
                if (values.Length < 2) continue;
    
                string imageId = values[0];
                for (int j = 1; j < values.Length && j < headers.Length; j++)
                {
                    LanguageType langType = GetLanguageTypeFromCode(headers[j]);
                    if (imagePathData.ContainsKey(langType) && !string.IsNullOrEmpty(values[j]))
                    {
                        imagePathData[langType][imageId] = values[j];
                    }
                }
            }
    
    
        }
    
        LanguageType GetLanguageTypeFromCode(string languageCode)
        {
            foreach (var pair in languageCodeMap)
            {
                if (pair.Value == languageCode)
                    return pair.Key;
            }
            return LanguageType.Chinese;
        }
    
        // 按需加载图片
        Sprite LoadSpriteOnDemand(string spritePath)
        {
            if (string.IsNullOrEmpty(spritePath))
                return null;
    
            // 检查是否已加载
            if (loadedSprites.ContainsKey(spritePath))
            {
                return loadedSprites[spritePath];
            }
    
            // 加载新图片
            string fullPath = System.IO.Path.Combine(imageFolderPath, spritePath).Replace("\\", "/");
            Sprite sprite = Resources.Load<Sprite>(fullPath);
    
            if (sprite != null)
            {
                loadedSprites[spritePath] = sprite;
            }
            else
            {
                Debug.LogWarning($"无法加载图片: {fullPath}");
            }
    
            return sprite;
        }
    
        // 清理当前语言的图片缓存（切换语言时调用）
        void ClearCurrentLanguageCache()
        {
            loadedSprites.Clear();
        }
    
        // 预加载特定图片（可选）
        public void PreloadImage(string imageId)
        {
            if (imagePathData.ContainsKey(currentLanguage) &&
                imagePathData[currentLanguage].ContainsKey(imageId))
            {
                string spritePath = imagePathData[currentLanguage][imageId];
                LoadSpriteOnDemand(spritePath);
            }
        }
    
        public void SetLanguage(LanguageType language)
        {
            if (imagePathData.ContainsKey(language))
            {
                // 切换语言前清理缓存
                ClearCurrentLanguageCache();
    
                currentLanguage = language;
                OnImageLanguageChanged?.Invoke();
           
            }
            else
            {
                Debug.LogWarning($"图片语言 {language} 不存在，使用默认语言 {defaultLanguage}");
                currentLanguage = defaultLanguage;
            }
        }
    
        public Sprite GetSprite(string imageId)
        {
            if (imagePathData.ContainsKey(currentLanguage) &&
                imagePathData[currentLanguage].ContainsKey(imageId))
            {
                string spritePath = imagePathData[currentLanguage][imageId];
                return LoadSpriteOnDemand(spritePath);
            }
    
            Debug.LogWarning($"未找到图片ID: {imageId} 对于语言: {currentLanguage}");
            return null;
        }
    
        // 获取图片路径而不加载（用于检查是否存在）
        public string GetSpritePath(string imageId)
        {
            if (imagePathData.ContainsKey(currentLanguage) &&
                imagePathData[currentLanguage].ContainsKey(imageId))
            {
                return imagePathData[currentLanguage][imageId];
            }
            return null;
        }
    
        // 手动释放图片资源
        public void UnloadSprite(string imageId)
        {
            if (imagePathData.ContainsKey(currentLanguage) &&
                imagePathData[currentLanguage].ContainsKey(imageId))
            {
                string spritePath = imagePathData[currentLanguage][imageId];
                if (loadedSprites.ContainsKey(spritePath))
                {
                    // 如果不再需要，可以从Resources中卸载
                    Resources.UnloadAsset(loadedSprites[spritePath]);
                    loadedSprites.Remove(spritePath);
                }
            }
        }
    
        // 强制清理所有缓存
        public void ClearAllCache()
        {
            foreach (var sprite in loadedSprites.Values)
            {
                Resources.UnloadAsset(sprite);
            }
            loadedSprites.Clear();
            Resources.UnloadUnusedAssets();
        }
    
        public LanguageType GetCurrentLanguage()
        {
            return currentLanguage;
        }
    }
}
