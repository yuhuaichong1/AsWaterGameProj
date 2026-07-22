namespace WZSDK
{
    using System.Collections.Generic;
    using UnityEngine;
    
    public enum LanguageType
    {
        Chinese,
        English,
        Japanese,
        Brazilian,  // 巴西葡萄牙语
        Indonesian  // 印尼语
    }
    
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }
    
        [Header("文本多语言配置")]
        LanguageType defaultLanguage = LanguageType.Chinese;
        private const string TextCsvName = "TextLocalization";
    
        // 修改数据结构：按语言分离数据，减少内存占用
        private Dictionary<LanguageType, Dictionary<string, string>> textData;
        private LanguageType currentLanguage;
        private Dictionary<string, string> currentLanguageData; // 当前语言的文本数据缓存
    
        public System.Action OnLanguageChanged;
    
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
            textData = new Dictionary<LanguageType, Dictionary<string, string>>();
            currentLanguageData = new Dictionary<string, string>();
            LoadTextData();
        }
    
        void LoadTextData()
        {
            List<string[]> rows = UserDataManager.GetCsvRows(TextCsvName);
            if (rows.Count == 0)
            {
                return;
            }
    
            if (rows.Count < 2)
            {
                Debug.LogError("文本CSV文件格式错误！");
                return;
            }
    
        
            string[] headers = rows[0];
    
         
            for (int i = 1; i < headers.Length; i++)
            {
                LanguageType langType = GetLanguageTypeFromCode(headers[i].Trim());
                textData[langType] = new Dictionary<string, string>();
            }
    
           
            for (int i = 1; i < rows.Count; i++)
            {
                string[] values = rows[i];
                if (values.Length < 2) continue;
    
                string key = values[0].Trim();
                for (int j = 1; j < values.Length && j < headers.Length; j++)
                {
                    LanguageType langType = GetLanguageTypeFromCode(headers[j].Trim());
                    if (textData.ContainsKey(langType))
                    {
                        textData[langType][key] = values[j];
                    }
                }
            }
    
    
    
     
            if (textData.ContainsKey(defaultLanguage))
            {
                currentLanguageData = textData[defaultLanguage];
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
    
        public void SetLanguage(LanguageType language)
        {
            if (textData.ContainsKey(language))
            {
                currentLanguage = language;
    
                currentLanguageData = textData[language];
                OnLanguageChanged?.Invoke();
          
            }
            else
            {
                Debug.LogWarning($"语言 {language} 不存在，使用默认语言 {defaultLanguage}");
                currentLanguage = defaultLanguage;
                currentLanguageData = textData[defaultLanguage];
            }
        }
    
        public string GetText(string key)
        {
       
            if (currentLanguageData.ContainsKey(key))
            {
                return currentLanguageData[key];
            }
    
            Debug.LogWarning($"未找到键为 {key} 的文本");
            return key;
        }
    
    
        public void PreloadTexts(params string[] keys)
        {
            foreach (string key in keys)
            {
                if (currentLanguageData.ContainsKey(key))
                {
                 
                }
            }
        }
    
    
        public List<string> GetAllTextKeys()
        {
            return new List<string>(currentLanguageData.Keys);
        }
    
    
        public bool HasText(string key)
        {
            return currentLanguageData.ContainsKey(key);
        }
    
     
        public Dictionary<string, string> GetTexts(params string[] keys)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (string key in keys)
            {
                result[key] = GetText(key);
            }
            return result;
        }
    
        public LanguageType GetCurrentLanguage()
        {
            return currentLanguage;
        }
    
        public List<LanguageType> GetAvailableLanguages()
        {
            return new List<LanguageType>(textData.Keys);
        }
    
    
        public void UnloadLanguageData(LanguageType language)
        {
            if (language != currentLanguage && textData.ContainsKey(language))
            {
                textData[language].Clear();
                Debug.Log($"已清理语言 {language} 的文本数据");
            }
        }
    
        // 重新加载所有数据（用于热重载）
        public void ReloadTextData()
        {
            textData.Clear();
            currentLanguageData.Clear();
            LoadTextData();
    
       
            if (textData.ContainsKey(currentLanguage))
            {
                currentLanguageData = textData[currentLanguage];
            }
            else if (textData.ContainsKey(defaultLanguage))
            {
                currentLanguageData = textData[defaultLanguage];
                currentLanguage = defaultLanguage;
            }
    
            Debug.Log("文本数据重新加载完成");
        }
    }
}
