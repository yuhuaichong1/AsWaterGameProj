using AsGame.Core;
using AsGame.Data;
using AsGame.Water;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Networking.UnityWebRequest;

namespace XrCode
{
    public partial class GamePlayModule : BaseModule
    {
        private int curLevelIndex;
        private List<CupData> curLevelData;
        private Dictionary<string, List<CupData>> allLevelData;
        private Dictionary<int, Bottle> cups = new();
        private Dictionary<int, Bottle> shadows = new();

        protected override void OnLoad()
        {
            curLevelData = new List<CupData>();
            allLevelData = new Dictionary<string, List<CupData>>();
            cups = new Dictionary<int, Bottle>();
            shadows = new Dictionary<int, Bottle>();

            FacadeAdd();

            LoadData();
            InitGame();
        }

        #region Facade

        /// <summary>
        /// 添加Facade接口
        /// </summary>
        private void FacadeAdd()
        {
            FacadeGamePlay.CreateLevel += CreateLevel;
            FacadeGamePlay.Func_Porp1 += Func_Porp1;
            FacadeGamePlay.Func_Porp2 += Func_Porp2;
            FacadeGamePlay.Func_Porp3 += Func_Porp3;
            FacadeGamePlay.RePlay += RePlay;
            FacadeGamePlay.GetCurLevelProgress += GetCurLevelProgress;
        }

        /// <summary>
        /// 去除Facade接口
        /// </summary>
        private void FacadeRemove()
        {
            FacadeGamePlay.CreateLevel += CreateLevel;
            FacadeGamePlay.Func_Porp1 += Func_Porp1;
            FacadeGamePlay.Func_Porp2 += Func_Porp2;
            FacadeGamePlay.Func_Porp3 += Func_Porp3;
            FacadeGamePlay.RePlay += RePlay;
            FacadeGamePlay.GetCurLevelProgress -= GetCurLevelProgress;
        }

        #endregion

        /// <summary>
        /// 加载持久化数据
        /// </summary>
        private void LoadData()
        {

        }

        /// <summary>
        /// 初始化游戏进程
        /// </summary>
        private void InitGame()
        {
            //CreateLevel();
        }

        /// <summary>
        /// 创建关卡
        /// </summary>
        private void CreateLevel()
        {
            curLevelIndex = FacadePlayer.GetLevel();
            GetLevelData(curLevelIndex);

            Debug.LogError("===>" + curLevelData.Count);
        }

        /// <summary>
        /// 获取当前关卡数据
        /// </summary>
        /// <param name="levelIndex">关卡id</param>
        private void GetLevelData(int levelIndex)
        {
            
            if (allLevelData.Count == 0)
            {
                GetAllLevelData();
            }

            if (allLevelData.TryGetValue($"level_{levelIndex}", out List<CupData> levelData))
            {
                curLevelData = levelData;
            }
            else
            {
                D.Error($"level_{levelIndex} is not exist");
            }
        }

        /// <summary>
        /// 获取所有关卡数据
        /// </summary>
        private void GetAllLevelData()
        {
            TextAsset ta = ResourceMod.Instance.SyncLoad<TextAsset>(GameDefines.LevelsDataPath);
            string jsonContent = ta.text;
            JObject root = JObject.Parse(jsonContent);

            foreach (var property in root.Properties())
            {
                string levelName = property.Name;  // "level_1", "level_2", ...
                JArray levelArray = (JArray)property.Value;

                var cupList = new List<CupData>();
                int id = 0;

                foreach (var item in levelArray)
                {
                    var cupData = new CupData();
                    cupData.id = id++;

                    // 解析位置 { "x": -120, "y": -194 }
                    JObject positionObj = (JObject)item[0];
                    float x = positionObj.Value<float>("x");
                    float y = positionObj.Value<float>("y");
                    cupData.position = new Vector2(x, y);

                    // 解析颜色数组
                    JArray colorsArray = (JArray)item[1];
                    cupData.colors = new List<int>();
                    foreach (var color in colorsArray)
                    {
                        cupData.colors.Add(color.Value<int>());
                    }

                    // 解析后续字段
                    cupData.whNums = item[2].Value<int>();
                    cupData.isVideo = item[3].Value<int>();
                    cupData.isLock = item[4].Value<int>();
                    cupData.lockColor = item[5].Value<int>();
                    cupData.lockNums = item[6].Value<int>();
                    cupData.isNull = 0;

                    cupList.Add(cupData);
                }

                allLevelData.Add(levelName, cupList);
            }
        }


        private void GenerateCups()
        {
            for (int i = 0; i < curLevelData.Count; i++)
            {
                CupData data = curLevelData[i];

                if (data.isNull != 0)
                {
                    cups[data.id] = null;
                    continue;
                }

                Vector3 pos = new Vector3(data.position.x, data.position.y + GameConstants.HalfBottleHeight, 0);
                //Bottle bottle = Bottle.Create(FacadeGamePlay.GetCupPart(), _bottleSprite);
            }
        }






        /// <summary>
        /// 重新开始
        /// </summary>
        private void RePlay()
        {

        }

        /// <summary>
        /// 获取当前关卡进度
        /// </summary>
        private float GetCurLevelProgress()
        {
            return 0;
        }

        #region 下三功能

        private void Func_Porp1()
        {

        }

        private void Func_Porp2()
        {

        }

        private void Func_Porp3()
        {

        }

        #endregion

        protected override void OnDispose()
        {
            FacadeRemove();
        }
    }
}
