using System;
using System.Collections.Generic;
using UnityEngine;
using XrCode;
using YRTT;

namespace XrSDK
{
    public class YRTModule : BaseModule
    {
        private Dictionary<string, object> AttributionInfo;
        private bool initSuccess;

        public YRTModule(YRTData data)
        {
            
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            AddFacade();

            Debug.Log("SDK 开始初始化");
            YRTTSDK.Instance.Init((success, info) =>
            {
                Debug.Log(success ? "SDK 初始化成功" : "SDK 初始化失败");
                initSuccess = success;
                if (success)
                {
                    AttributionInfo = new Dictionary<string, object>() 
                    {
                        {"ServerTimeStamp", info.ServerTimeStamp},
                        {"FromInviteCode", info.FromInviteCode},
                        {"InviteCode", info.InviteCode},
                        {"Home", info.Home},
                        {"Uk", info.Uk},
                        {"ActiveDays", info.ActiveDays},
                        {"RegisterDays", info.RegisterDays},
                        {"channel", info.Channel},
                        {"LastLoginTimeStamp", info.LastLoginTimeStamp},
                        {"Country", info.Country},
                        {"CreateTimeStamp", info.CreateTimeStamp}
                    };

                    //SCompetitionManager.Instance.CompetitionVariable("Attribution", "", 1);
                    CompetitionManager.Instance.CompetitionVariable(CompetitionKey.YRTT, true, 0);
                    TDAnalyticsManager.Instance.SetAttributionData(AttributionInfo);

                    if (!YRTTSDK.Instance.HadNotifyPermission())
                    {
                        YRTTSDK.Instance.RequestNotifyPermission();
                    }
                }
                else 
                {
                    CompetitionManager.Instance.CompetitionVariable(CompetitionKey.YRTT, false, 0);
                }
            });

            
        }

        #region Facade

        private void AddFacade()
        {
            YRTDefines.GetAttributionInfo += GetAttributionInfo;
            YRTDefines.GetInitSuccess += GetInitSuccess;

            YRTDefines.EnterMainUI += EnterMainUI;
            YRTDefines.LoginSuccess += LoginSuccess;
            YRTDefines.EnterMainUI += EnterMainUI;
            YRTDefines.GuideStepFinish += GuideStep;
        }

        private void RemoveFacade()
        {
            YRTDefines.GetAttributionInfo -= GetAttributionInfo;
            YRTDefines.GetInitSuccess -= GetInitSuccess;

            YRTDefines.EnterMainUI -= EnterMainUI;
            YRTDefines.LoginSuccess -= LoginSuccess;
            YRTDefines.EnterMainUI -= EnterMainUI;
            YRTDefines.GuideStepFinish -= GuideStep;
        }

        #endregion

        private Dictionary<string, object> GetAttributionInfo()
        {
            return AttributionInfo;
        }

        private bool GetInitSuccess()
        {
            return initSuccess;
        }

        protected override void OnDispose()
        {
            base.OnDispose();

            RemoveFacade();
        }

        #region 事件


        /// <summary>
        /// 引导步骤
        /// </summary>
        /// <param name="curStep">当前引导步骤</param>
        public void GuideStep(int curStep)
        {
            if (!initSuccess) return;
            YRTTSDK.Instance.Track("GuideStep", "step", curStep);

        }

        /// <summary>
        /// 游戏进入了主界面
        /// </summary>
        public void EnterMainUI()
        {
            if (!initSuccess) return;
            YRTTSDK.Instance.EnterHomePage();
            YRTTSDK.Instance.Track("Enter_MainUI");

        }

        /// <summary>
        /// 玩家登录成功
        /// </summary>
        public void LoginSuccess()
        {
            if (!initSuccess) return;
            YRTTSDK.Instance.Track("Login_Success");
        }

        /// <summary>
        /// 玩家注册成功
        /// </summary>
        /// <param name="userId">玩家Id</param>
        public void RegisterFinish()
        {
            if (!initSuccess) return;
            YRTTSDK.Instance.Track("Register_Finish");
        }

        #endregion
    }
}

