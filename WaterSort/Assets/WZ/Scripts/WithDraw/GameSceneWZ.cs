using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace WZSDK
{
    //继承mono脚本，需要挂载游戏物体
    public class GameSceneWZ : MonoBehaviour
    {
        private float dt;
        private void Awake()
        {
            GameApp.Instance.Init();
        }

        void Start()
        {
            // var stage = GameManager.instance.getPlayerStage();
            // if (stage == 0)
            // {
            //     stage = 1;
            // }
            // var dataModel = new DataModel();
            // dataModel.Level = 1;
            // dataModel.Money = 0.04;
            // dataModel.Num = stage;
            // dataModel.CloseType = 2;
            // dataModel.Type = 2;
            // GameApp.viewManager.Open(ViewType.WithDraw, dataModel);



        }
        void Update()
        {
            dt = Time.deltaTime;
            GameApp.Instance.Update(dt);
        }

        public void withDraw(int level, double money, int closeType, ViewType viewType, string historyId, int stageId = 0)
        {
            int stage = stageId > 0 ? stageId : GameManagerWZ.instance.getPlayerStage();
            if (stage <= 0)
                stage = 1;

            var dataModel = new DataModel();
            dataModel.Level = level;
            dataModel.Money = money;
            dataModel.Num = stage;
            dataModel.CloseType = closeType;
            dataModel.HistoryId = historyId;
            GameApp.viewManager.Open(viewType, dataModel);
        }

        public void withDrawTem(int level, double money, int closeType, int type)
        {
            var stage = GameManagerWZ.instance.getPlayerStage();
            if (stage <= 0)
            {
                stage = 1;
            }
            var dataModel = new DataModel();
            dataModel.Level = level;
            dataModel.Money = money;
            dataModel.Num = stage;
            dataModel.CloseType = closeType;
            dataModel.Type = type;
            GameApp.viewManager.Open(ViewType.WithDraw, dataModel);
        }

        public string setHistory(double money, int stage)
        {
            List<HistoryModel> historyModels = SPlayerPrefs.GetListTem<HistoryModel>(FacadePlayerPrefExtend.History);
            if (historyModels == null)
            {
                historyModels = new List<HistoryModel>();
            }
            var historyModel = new HistoryModel();
            historyModel.DateTime = DateTime.Today.Month + "/" + DateTime.Today.Day + "/" + DateTime.Today.Year;
            historyModel.Money = money;
            historyModel.ID = Guid.NewGuid().ToString();
            historyModel.Status = 2;
            historyModel.Stage = stage;
            historyModels.Add(historyModel);
            SPlayerPrefs.SetListTem(FacadePlayerPrefExtend.History, historyModels);
            return historyModel.ID;
        }
    }
}
