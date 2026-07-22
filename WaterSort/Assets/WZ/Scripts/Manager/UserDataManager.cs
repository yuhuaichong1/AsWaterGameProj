using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;


namespace WZSDK
{
    [Serializable]
    public class UserLevelData
    {
        public int sn;
        public int level;
        public int needExp;
    }


    [Serializable]
    public class PlayerLevelData
    {
        public string userName;
        public string userID;
        public int userLevel;
        public int userExp;
    }


    public class UserDataManager : MonoSingleton<UserDataManager>, ILoad, IDispose
    {
        private Sprite singleCoin;
        private Sprite MultCoin;
        private Sprite singlGem;
        private Sprite GetMulteGem;
        private List<UserLevelData> levelDataList = new List<UserLevelData>();
        private List<Dictionary<string, object>> stageData = new List<Dictionary<string, object>>();
        private List<Dictionary<string, object>> gearData = new List<Dictionary<string, object>>();
        private List<Dictionary<string, object>> gemData = new List<Dictionary<string, object>>();
        private List<Dictionary<string, object>> barrageData = new List<Dictionary<string, object>>();
        private List<Dictionary<string, object>> PayRegionData = new List<Dictionary<string, object>>();
        private Dictionary<int, List<LevelRewardStatic.RewardItem>> levelRewardData = new Dictionary<int, List<LevelRewardStatic.RewardItem>>();
        private Dictionary<int, ConfLuckySpin> luckySpinData = new Dictionary<int, ConfLuckySpin>();
        private HashSet<int> luckyRewardLevels = new HashSet<int>();
        private HashSet<int> luckyRewardIconLevels = new HashSet<int>();
        private PlayerLevelData playerData = new PlayerLevelData();
        private static readonly Dictionary<string, List<string[]>> csvRowsCache = new Dictionary<string, List<string[]>>();

        public LanguageType initialLanguage = LanguageType.English;
        public string targetLanguage = null;
        private string countryCode = "";
        private void Initialize()
        {
            LoadPayRegionFromCSV();
            GetLanguageData();
            ApplyLanguage();
            LoadLevelConfigFromCSV();
            LoadStageFromCSV();
            LoadGearFromCSV();
            LoadPlayerData();
            LoadExtractDataFromCSV();
            LoadGemFromCSV();
            //LoadLevelRewardFromCSV();
        
            LoadLuckySpinFromCSV();
            LoadLuckyRewardFromCSV();





            FacadePayTypeExtend.GetSingleCoin += GetSingleCoin;
            FacadePayTypeExtend.GetMultCoinCoinAct += GetMultCoinCoinAct;

            FacadePayTypeExtend.GetSingleGem += GetSingleGem;
            FacadePayTypeExtend.GetMulteGem += GetMulteGemAct;


            MultCoin = ImageLocalizationManager.Instance != null
                ? ImageLocalizationManager.Instance.GetSprite("1001")
                : null;//堆叠钱（成功页 / EffectRewardItem）
            singleCoin = ImageLocalizationManager.Instance != null
                ? ImageLocalizationManager.Instance.GetSprite("1002")
                : null;//单个（飞币等）

            if (MultCoin == null)
                MultCoin = Resources.Load<Sprite>(GameDefines.ERMultCoinIconPath);
            if (singleCoin == null)
                singleCoin = Resources.Load<Sprite>(GameDefines.ERFlyMoneyIconPath);

            singlGem = Resources.Load<Sprite>(GameDefines.Default_SingleGem);//单个钻石
            GetMulteGem = Resources.Load<Sprite>(GameDefines.Default_SingDuileGem);//堆叠
        }

        private Sprite GetSingleCoin()
        {
            if (singleCoin == null)
                singleCoin = Resources.Load<Sprite>(GameDefines.ERFlyMoneyIconPath);
            return singleCoin;
        }

        private Sprite GetMultCoinCoinAct()
        {
            if (MultCoin == null)
                MultCoin = Resources.Load<Sprite>(GameDefines.ERMultCoinIconPath);
            return MultCoin;
        }
        private Sprite GetSingleGem() => singlGem;



        private Sprite GetMulteGemAct() => GetMulteGem;
        public void Load() => Initialize();

        public static List<string[]> GetCsvRows(string csvName)
        {
            string resourcePath = NormalizeCsvResourcePath(csvName);
            if (csvRowsCache.TryGetValue(resourcePath, out List<string[]> cachedRows))
            {
                return cachedRows;
            }

            List<string[]> rows = new List<string[]>();
            TextAsset csvFile = Resources.Load<TextAsset>(resourcePath);
            if (csvFile == null)
            {
                Debug.LogError($"找不到CSV文件: {resourcePath}");
                csvRowsCache[resourcePath] = rows;
                return rows;
            }

            rows = ParseCsvRows(csvFile.text);

            csvRowsCache[resourcePath] = rows;
            return rows;
        }

        public static List<string[]> ParseCsvRows(string csvText)
        {
            List<string[]> rows = new List<string[]>();
            if (string.IsNullOrEmpty(csvText))
            {
                return rows;
            }

            List<string> currentRow = new List<string>();
            StringBuilder currentField = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < csvText.Length; i++)
            {
                char c = csvText[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < csvText.Length && csvText[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    currentRow.Add(currentField.ToString());
                    currentField.Length = 0;
                    continue;
                }

                if ((c == '\r' || c == '\n') && !inQuotes)
                {
                    if (c == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n')
                    {
                        i++;
                    }

                    currentRow.Add(currentField.ToString());
                    currentField.Length = 0;

                    if (!IsCsvRowEmpty(currentRow))
                    {
                        rows.Add(currentRow.ToArray());
                    }

                    currentRow = new List<string>();
                    continue;
                }

                currentField.Append(c);
            }

            if (currentField.Length > 0 || currentRow.Count > 0)
            {
                currentRow.Add(currentField.ToString());
                if (!IsCsvRowEmpty(currentRow))
                {
                    rows.Add(currentRow.ToArray());
                }
            }

            return rows;
        }

        public static List<string> ParseCsvLineValues(string line)
        {
            List<string> values = new List<string>();
            bool inQuotes = false;
            string currentField = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField += '"';
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentField);
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }

            values.Add(currentField);
            return values;
        }

        private static bool IsCsvRowEmpty(List<string> row)
        {
            if (row == null || row.Count == 0)
            {
                return true;
            }

            return row.All(string.IsNullOrWhiteSpace);
        }

        private static string NormalizeCsvResourcePath(string csvName)
        {
            string resourcePath = csvName.Replace("\\", "/").Trim();
            if (resourcePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                resourcePath = resourcePath.Substring(0, resourcePath.Length - 4);
            }

            if (!resourcePath.StartsWith("CSV/", StringComparison.OrdinalIgnoreCase))
            {
                resourcePath = $"CSV/{resourcePath}";
            }

            return resourcePath;
        }

        private void LoadLevelConfigFromCSV()
        {
            levelDataList.Clear();
            try
            {
                List<string[]> rows = GetCsvRows("UserLevel");
                if (rows.Count <= 1)
                {
                    return;
                }

                for (int i = 1; i < rows.Count; i++)
                {
                    string[] values = rows[i];
                    if (values.Length < 3) continue;

                    if (values.Length >= 3)
                    {
                        try
                        {
                            levelDataList.Add(new UserLevelData
                            {
                                sn = int.Parse(values[0].Trim()),
                                level = int.Parse(values[1].Trim()),
                                needExp = int.Parse(values[2].Trim())
                            });
                        }
                        catch
                        {
                            ;
                        }
                    }
                }


            }
            catch (Exception ex)
            {

            }
        }

        private void LoadStageFromCSV()
        {
            stageData.Clear();
            try
            {
                List<string[]> rows = GetCsvRows("Stage");
                if (rows.Count <= 2)
                {
                    return;
                }

                string[] headers = rows[0];
                for (int i = 2; i < rows.Count; i++)
                {
                    string[] values = rows[i];
                    try
                    {
                        var dictionary = new Dictionary<string, object>();
                        int fieldCount = Mathf.Min(headers.Length, values.Length);
                        for (int i1 = 0; i1 < fieldCount; i1++)
                        {
                            dictionary[headers[i1].Trim()] = values[i1].Trim();
                        }
                        if (dictionary.Count > 0)
                            stageData.Add(dictionary);
                    }
                    catch
                    {

                    }
                }


            }
            catch (Exception ex)
            {

            }
        }

        private void LoadGearFromCSV()
        {
            gearData.Clear();
            try
            {
                List<string[]> rows = GetCsvRows("Gear");
                if (rows.Count <= 2)
                {
                    return;
                }

                string[] headers = rows[0];
                for (int i = 2; i < rows.Count; i++)
                {
                    string[] values = rows[i];
                    try
                    {
                        var dictionary = new Dictionary<string, object>();
                        int fieldCount = Mathf.Min(headers.Length, values.Length);
                        for (int i1 = 0; i1 < fieldCount; i1++)
                        {
                            dictionary[headers[i1].Trim()] = values[i1].Trim();
                        }
                        if (dictionary.Count > 0)
                            gearData.Add(dictionary);
                    }
                    catch
                    {

                    }
                }


            }
            catch (Exception ex)
            {

            }
        }

        private void LoadGemFromCSV()
        {
            gemData.Clear();
            try
            {
                List<string[]> rows = GetCsvRows("Gem");
                if (rows.Count <= 2)
                {
                    return;
                }

                string[] headers = rows[0];
                for (int i = 2; i < rows.Count; i++)
                {
                    string[] values = rows[i];
                    try
                    {
                        var dictionary = new Dictionary<string, object>();
                        int fieldCount = Mathf.Min(headers.Length, values.Length);
                        for (int i1 = 0; i1 < fieldCount; i1++)
                        {
                            dictionary[headers[i1].Trim()] = values[i1].Trim();
                        }
                        if (dictionary.Count > 0)
                            gemData.Add(dictionary);
                    }
                    catch
                    {

                    }
                }


            }
            catch (Exception ex)
            {

            }
        }


        private void LoadPayRegionFromCSV()
        {
            PayRegionData.Clear();
            try
            {
                List<string[]> rows = GetCsvRows("PayRegion");
                if (rows.Count == 0)
                {
                    return;
                }

                if (rows.Count < 2)
                {
                    Debug.LogError("CSV文件行数不足");
                    return;
                }

                string[] headers = rows[0];
                for (int i = 1; i < rows.Count; i++)
                {
                    string[] values = rows[i];
                    if (values.Length >= headers.Length)
                    {
                        var dictionary = new Dictionary<string, object>();
                        for (int j = 0; j < headers.Length; j++)
                        {
                            string key = headers[j].Trim();
                            string value = j < values.Length ? values[j].Trim() : "";
                            dictionary[key] = value;
                        }
                        PayRegionData.Add(dictionary);
                    }
                    else
                    {
                        Debug.LogWarning($"第{i + 1}行字段数不匹配: 期望{headers.Length}个, 实际{values.Length}个");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"加载CSV失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void LoadExtractDataFromCSV()
        {
            barrageData.Clear();
            try
            {
                List<string[]> rows = GetCsvRows("Extractdata");
                if (rows.Count <= 2)
                {
                    return;
                }

                string[] headers = rows[0];
                for (int i = 2; i < rows.Count; i++)
                {
                    string[] values = rows[i];
                    try
                    {
                        var dictionary = new Dictionary<string, object>();
                        int fieldCount = Mathf.Min(headers.Length, values.Length);
                        for (int i1 = 0; i1 < fieldCount; i1++)
                        {
                            dictionary[headers[i1].Trim()] = values[i1].Trim();
                        }
                        if (dictionary.Count > 0)
                            barrageData.Add(dictionary);
                    }
                    catch
                    {

                    }
                }


            }
            catch (Exception ex)
            {

            }
        }



        private void LoadLuckySpinFromCSV()
        {
            luckySpinData.Clear();

            try
            {
                List<string[]> rows = GetCsvRows("LuckySpin");
                if (rows.Count <= 1)
                    return;

                for (int i = 1; i < rows.Count; i++)
                {
                    string[] values = rows[i];
                    if (values.Length < 4)
                        continue;

                    if (!int.TryParse(values[0].Trim(), out int sn)
                        || !int.TryParse(values[1].Trim(), out int type)
                        || !float.TryParse(values[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float count)
                        || !double.TryParse(values[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double probability))
                    {
                        continue;
                    }

                    luckySpinData[sn] = new ConfLuckySpin
                    {
                        Sn = sn,
                        Type = type,
                        Count = count,
                        Probability = probability
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"加载LuckySpin失败: {ex.Message}");
            }
        }

        private void LoadLevelRewardFromCSV()
        {
            levelRewardData.Clear();

            try
            {
                List<string[]> rows = GetCsvRows("LevelReward");
                if (rows.Count <= 1)
                    return;

                string[] headers = rows[0];
                int levelIndex = Array.FindIndex(headers, header => string.Equals(header.Trim(), "level", StringComparison.OrdinalIgnoreCase));
                int rewardIndex = Array.FindIndex(headers, header => string.Equals(header.Trim(), "levelReward", StringComparison.OrdinalIgnoreCase));

                if (levelIndex < 0 || rewardIndex < 0)
                    return;

                for (int i = 1; i < rows.Count; i++)
                {
                    string[] values = rows[i];
                    if (levelIndex >= values.Length || rewardIndex >= values.Length)
                        continue;

                    if (!int.TryParse(values[levelIndex].Trim(), out int level) || level <= 0)
                        continue;

                    List<LevelRewardStatic.RewardItem> rewards = ParseLevelRewardItems(values[rewardIndex]);
                    if (rewards.Count > 0)
                        levelRewardData[level] = rewards;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"加载LevelReward失败: {ex.Message}");
            }
        }

        private List<LevelRewardStatic.RewardItem> ParseLevelRewardItems(string rewardValue)
        {
            List<LevelRewardStatic.RewardItem> rewards = new List<LevelRewardStatic.RewardItem>();
            if (string.IsNullOrWhiteSpace(rewardValue))
                return rewards;

            string normalized = rewardValue
                .Replace(";", ",")
                .Replace("|", ",");

            string[] tokens = normalized
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(token => token.Trim())
                .Where(token => !string.IsNullOrEmpty(token))
                .ToArray();

            for (int i = 0; i < tokens.Length;)
            {
                if (TryParseLevelRewardToken(tokens[i], out LevelRewardStatic.RewardItem rewardItem))
                {
                    rewards.Add(rewardItem);
                    i++;
                    continue;
                }

                if (i + 1 < tokens.Length
                    && int.TryParse(tokens[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int type)
                    && float.TryParse(tokens[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out float amount))
                {
                    rewards.Add(new LevelRewardStatic.RewardItem
                    {
                        type = type,
                        amount = amount
                    });
                    i += 2;
                    continue;
                }

                i++;
            }

            return rewards;
        }

        private bool TryParseLevelRewardToken(string token, out LevelRewardStatic.RewardItem rewardItem)
        {
            rewardItem = null;
            if (string.IsNullOrWhiteSpace(token))
                return false;

            string[] parts = token.Split(':');
            if (parts.Length != 2)
                return false;

            if (!int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int type)
                || !float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float amount))
            {
                return false;
            }

            rewardItem = new LevelRewardStatic.RewardItem
            {
                type = type,
                amount = amount
            };
            return true;
        }

        private void LoadLuckyRewardFromCSV()
        {
            luckyRewardLevels.Clear();
            luckyRewardIconLevels.Clear();

            if (TryApplyLuckyRewardFromServerConfig())
                return;

            try
            {
                List<string[]> rows = GetCsvRows("LuckyReward");
                if (rows.Count <= 1)
                    return;

                string[] headers = rows[0];
                int levelIndex = Array.FindIndex(headers, header => string.Equals(header.Trim(), "Level", StringComparison.OrdinalIgnoreCase));
                int showIconIndex = Array.FindIndex(headers, header => string.Equals(header.Trim(), "ShowIcon", StringComparison.OrdinalIgnoreCase));

                if (levelIndex < 0)
                    return;

                for (int i = 1; i < rows.Count; i++)
                {
                    string[] values = rows[i];
                    if (levelIndex >= values.Length || !int.TryParse(values[levelIndex].Trim(), out int level))
                        continue;

                    luckyRewardLevels.Add(level);

                    bool shouldShow = true;
                    if (showIconIndex >= 0 && showIconIndex < values.Length)
                    {
                        shouldShow = !string.Equals(values[showIconIndex].Trim(), "0", StringComparison.Ordinal);
                    }

                    if (shouldShow)
                        luckyRewardIconLevels.Add(level);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"加载LuckyReward失败: {ex.Message}");
            }
        }

        private bool TryApplyLuckyRewardFromServerConfig()
        {
            if (GameDefines.ServerLuckyRewardLevels == null || GameDefines.ServerLuckyRewardLevels.Count <= 0)
                return false;

            for (int i = 0; i < GameDefines.ServerLuckyRewardLevels.Count; i++)
            {
                int level = GameDefines.ServerLuckyRewardLevels[i];
                if (level <= 0)
                    continue;

                luckyRewardLevels.Add(level);
                luckyRewardIconLevels.Add(level);
            }

            Debug.Log($"LuckyReward 使用服务器配置，共 {luckyRewardLevels.Count} 个关卡");
            return luckyRewardLevels.Count > 0;
        }

        public void RefreshLuckyRewardConfig()
        {
            LoadLuckyRewardFromCSV();
        }

        public Dictionary<string, object> getStageData(int id)
        {
            foreach (var stage in stageData)
            {
                if (int.Parse((string)stage["Id"]) == id)
                {
                    return stage;
                }
            }
            return null;
        }

        public Dictionary<string, object> getGearData(int id)
        {
            foreach (var gear in gearData)
            {
                if (int.Parse((string)gear["id"]) == id)
                {
                    return gear;
                }
            }
            return null;
        }

        public List<Dictionary<string, object>> getAllGearData()
        {
            return gearData;
        }

        public List<Dictionary<string, object>> getAllBarrageData()
        {
            return barrageData;
        }

        public List<Dictionary<string, object>> getAllGemData()
        {
            return gemData;
        }
        public List<Dictionary<string, object>> getAllPayRegionData()
        {
            return PayRegionData;
        }

        public Dictionary<string, object> getGemData(int id)
        {
            foreach (var gem in gemData)
            {
                if (int.Parse((string)gem["id"]) == id)
                {
                    return gem;
                }
            }
            return null;
        }

        public List<LevelRewardStatic.RewardItem> GetLevelRewards(int level)
        {
            if (!levelRewardData.TryGetValue(level, out List<LevelRewardStatic.RewardItem> rewards))
            {
                return null;
            }

            return rewards
                .Select(reward => new LevelRewardStatic.RewardItem
                {
                    type = reward.type,
                    amount = reward.amount
                })
                .ToList();
        }

        public Dictionary<int, ConfLuckySpin> GetLuckySpinData()
        {
            return luckySpinData.ToDictionary(
                pair => pair.Key,
                pair => new ConfLuckySpin
                {
                    Sn = pair.Value.Sn,
                    Type = pair.Value.Type,
                    Count = pair.Value.Count,
                    Probability = pair.Value.Probability
                });
        }

        public bool IsLuckyRewardLevel(int level)
        {
            return luckyRewardLevels.Contains(level);
        }

        public bool ShouldShowLuckyRewardIcon(int level)
        {
            return luckyRewardIconLevels.Contains(level);
        }

        private void LoadPlayerData()
        {
            playerData.userName = SPlayerPrefs.HasKey(FacadePlayerPrefExtend.userName)
                ? SPlayerPrefs.GetString(FacadePlayerPrefExtend.userName)
                : GetRandomName();

            playerData.userID = SPlayerPrefs.HasKey(FacadePlayerPrefExtend.userID)
                ? SPlayerPrefs.GetString(FacadePlayerPrefExtend.userID)
                : GetRandomID();

            playerData.userLevel = SPlayerPrefs.GetInt(FacadePlayerPrefExtend.userLevel, 0);
            playerData.userExp = SPlayerPrefs.GetInt(FacadePlayerPrefExtend.userExp, 0);

        }

        private string GetRandomName()
        {
            char[] nameChars = GameDefines.nameString.ToCharArray();
            char c1 = nameChars[UnityEngine.Random.Range(0, nameChars.Length)];
            char c2 = nameChars[UnityEngine.Random.Range(0, nameChars.Length)];
            string name = $"Player_{c1}{c2}";

            SPlayerPrefs.SetString(FacadePlayerPrefExtend.userName, name);
            SPlayerPrefs.Save();
            return name;
        }

        private string GetRandomID()
        {
            string id = Guid.NewGuid().ToString();
            SPlayerPrefs.SetString(FacadePlayerPrefExtend.userID, id);
            SPlayerPrefs.Save();
            return id;
        }


        public void AddExp(int value)
        {
            if (playerData.userLevel >= levelDataList.Count - 1)
            {

                return;
            }

            playerData.userExp += value;
            CheckNextLevel();
            SPlayerPrefs.SetInt(FacadePlayerPrefExtend.userLevel, playerData.userLevel);
            SPlayerPrefs.SetInt(FacadePlayerPrefExtend.userExp, playerData.userExp);
            SPlayerPrefs.Save();
        }


        private void CheckNextLevel()
        {
            if (playerData.userLevel >= levelDataList.Count - 1) return;

            while (true)
            {
                int needExp = GetNextLevelNeedExp();
                if (needExp <= 0 || playerData.userExp < needExp) break;

                playerData.userLevel++;
                playerData.userExp -= needExp;

                if (playerData.userLevel >= levelDataList.Count - 1) break;
            }
        }


        public int GetNextLevelNeedExp()
        {
            if (playerData.userLevel >= levelDataList.Count - 1) return 0;
            return playerData.userLevel < levelDataList.Count
                ? levelDataList[playerData.userLevel].needExp
                : levelDataList.FirstOrDefault(d => d.level == playerData.userLevel + 1)?.needExp ?? 100;
        }


        /// <summary>
        /// 从barrageData中随机获取play和stage的值
        /// </summary>
        public (string play, int stage) GetRandomBarragePlayAndStage()
        {
            var randomData = GetRandomBarrageData();

            if (randomData == null)
            {
                return ("player_AX", 300);
            }
            string play = randomData.ContainsKey("play") ? randomData["play"].ToString() : "player_AX";
            int stage = 300;
            if (randomData.ContainsKey("Stage"))
            {
                int.TryParse(randomData["Stage"].ToString(), out stage);
            }

            return (play, stage);
        }

        public Dictionary<string, object> GetRandomBarrageData()
        {
            if (barrageData == null || barrageData.Count == 0)
            {
                Debug.LogWarning("barrageData 为空，无法获取随机数据");
                return null;
            }

            int randomIndex = UnityEngine.Random.Range(0, barrageData.Count);
            return barrageData[randomIndex];
        }

        public int GetExp() => playerData.userExp;
        public int GetUserLevel() => playerData.userLevel;
        public string GetUserName() => playerData.userName;
        public string GetUserID() => playerData.userID;

        private void GetLanguageData()
        {

            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            string[] cultureParts = currentCulture.Name.Split('-');
            countryCode = cultureParts[1];// "BR";//; // 测试用，实际应该用 cultureParts[1]

            var payRegionList = UserDataManager.Instance.getAllPayRegionData();

            foreach (var region in payRegionList)
            {
                var code = region["code"].ToString();
                if (code == countryCode)
                {
                    targetLanguage = region["lang"].ToString();
                    GameManagerWZ.instance.mark = region["mark"].ToString();
                    Debug.Log($"获取到国家 {countryCode}，语言: {targetLanguage}，货币: {GameManagerWZ.instance.mark}");
                    break;
                }
            }

            if (string.IsNullOrEmpty(targetLanguage))
            {
                Debug.Log($"未匹配到国家 {countryCode}，将使用初始语言: {initialLanguage}");
            }


        }

        private void ApplyLanguage()
        {
            if (string.IsNullOrEmpty(targetLanguage))
            {
                SetLanguage(initialLanguage);
                return;
            }
            LanguageType languageType = ConvertStringToLanguageType(targetLanguage);
            SetLanguage(languageType);
            Debug.Log($"应用语言: {languageType}");
        }

        private LanguageType ConvertStringToLanguageType(string lang)
        {
            switch (lang)
            {
                case "zh-CN":
                    return LanguageType.Chinese;
                case "en-US":
                    return LanguageType.English;
                case "ja-JP":
                    return LanguageType.Japanese;
                case "pt-BR":
                    return LanguageType.Brazilian;
                case "id-ID":
                    return LanguageType.Indonesian;
                default:
                    return LanguageType.English;
            }
        }

        private void SetLanguage(LanguageType language)
        {

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.SetLanguage(language);
            }
            else
            {
                Debug.LogError("LocalizationManager.Instance 为空！");
            }

            if (ImageLocalizationManager.Instance != null)
            {
                ImageLocalizationManager.Instance.SetLanguage(language);
            }
            else
            {
                Debug.LogError("ImageLocalizationManager.Instance 为空！");
            }
        }
        public void Dispose()
        {
        }

        public void OnDestroy()
        {
            FacadePayTypeExtend.GetSingleCoin -= GetSingleCoin;
            FacadePayTypeExtend.GetMultCoinCoinAct -= GetMultCoinCoinAct;
            FacadePayTypeExtend.GetSingleGem -= GetSingleGem;
            FacadePayTypeExtend.GetMulteGem -= GetMulteGemAct;
        }
    }
}
