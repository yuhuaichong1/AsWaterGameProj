using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine.UI;

using System.Collections;
using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
namespace WZSDK
{
    public class LevelGenerator : MonoBehaviour
    {
        private GameManagerWZ Gm => GameManagerWZ.instance;
        public bool NewGamePlayIsTrue;
        public void InitLvGen()
        {
            CheckAndShowTutorial();
            if (!NewGamePlayIsTrue)
            {
                ExecuteStageSettings();
            }
        }

        private void CheckAndShowTutorial()
        {
            /*  if (currentLevelData?.level == null) return;
              string tutorialKey = $"{FacadePlayerPrefExtend.LevelTutorialShown}_{GameManager.instance.currentLv}";
              if (PlayerPrefs.GetInt(tutorialKey, 0) == 1) return;

              foreach (var bottle in currentLevelData.level)
              {
                  if (bottle.hideCnt > 0)
                  {
                      UIManager.instance.ShowView(EUIType.NewGamePlay, 4);
                      NewGamePlayIsTrue = true;
                      PlayerPrefs.SetInt(tutorialKey, 1);
                      PlayerPrefs.Save();
                      return;
                  }
                  if (bottle.globalUnlockNeeded > 0)
                  {
                      UIManager.instance.ShowView(EUIType.NewGamePlay, 1);
                      NewGamePlayIsTrue = true;
                      PlayerPrefs.SetInt(tutorialKey, 1);
                      PlayerPrefs.Save();
                      return;
                  }
                  if (bottle.colorEliminateNeeded > 0)
                  {
                      UIManager.instance.ShowView(EUIType.NewGamePlay, 3);
                      NewGamePlayIsTrue = true;
                      PlayerPrefs.SetInt(tutorialKey, 1);
                      PlayerPrefs.Save();
                      return;
                  }
              }*/
            NewGamePlayIsTrue = false;
        }

        public void ExecuteStageSettings()
        {
            #region  阶段设置

            if (!GameDefines.ifIAA)
            {
                int stage = Gm.currentStage;
                Debug.LogError("当前阶段" + stage);

                var dataModel = new DataModel { Level = Gm.currentLv };
                var moneyModel = new DataModel { Money = Gm.GetCoin() };
                bool hasUnlockedLuckyWallet = Gm.currentLv >= GameDefines.LuckyWalletUnlockLevel;
                bool shouldShowLuckyWalletUnlockView = hasUnlockedLuckyWallet
                    && PlayerPrefs.GetInt(FacadePlayerPrefExtend.HasShownLuckyWalletAtLevel, 0) == 0;
                bool shouldShowLuckyTips = hasUnlockedLuckyWallet
                    && !shouldShowLuckyWalletUnlockView
                    && UserDataManager.Instance != null
                    && UserDataManager.Instance.IsLuckyRewardLevel(Gm.currentLv);

                if (shouldShowLuckyTips)
                {
                    UIManager.instance.ShowView(EUIType.LuckyTips);
                    return;
                }

                if (stage == 1)
                {
                    WaterSortWZBridge.NotifyLevel1IntroOpened();
                    GameApp.viewManager.Open(ViewType.kaiju_ui_1);
                }
               // if (stage == 2) GameApp.viewManager.Open(ViewType.kaiju_ui_2_1, dataModel);
                // Stage3 未达标时仍可展示 Mission 进度；达标后改由 LuckyWallet → Mission 串联，避免抢弹。
                if (Gm.currentStage == 3 && !Gm.Stage2CashGuidePending && !Gm.IsStageCompleted(3))
                    UIManager.instance.ShowView(EUIType.WithdrawMissionView);
                if (stage == 4 && !Gm.IsStage3CompletionFlowActive)
                    GameApp.viewManager.Open(ViewType.kaiju_ui_4, moneyModel);
                if (stage == 5) GameApp.viewManager.Open(ViewType.kaiju_ui_5, moneyModel);

                // Stage3 达标链路中的 LuckyWallet 优先；不要用关卡解锁 LuckyWallet 叠弹。
                if (shouldShowLuckyWalletUnlockView && !Gm.IsStage3CompletionFlowActive)
                {
                    PlayerPrefs.SetInt(FacadePlayerPrefExtend.HasShownLuckyWalletAtLevel, 1);
                    PlayerPrefs.Save();
                    UIManager.instance.ShowView(EUIType.LuckyWalletView);
                }
            }
            else
            {
                if (Gm.currentLv == 1)
                {

                }

            }
            #endregion
        }





    }
}
