using System;
using System.Collections.Generic;
using UnityEngine;

namespace XrCode
{
    public class CompetitionManager : Singleton<CompetitionManager>, ILoad, IDispose
    {
        public Action OnFinished;

        private Dictionary<CompetitionKey, CompetitionItem> competitionValues;

        #region ����

        public Dictionary<string, object> attributionData;//��������

        #endregion

        public void Load()
        {
            competitionValues = new Dictionary<CompetitionKey, CompetitionItem>();
            CompetitionItem IAACM  = new CompetitionItem();
            IAACM.ReSet();
            IAACM.SetInfo(2, OnIAAFinal);
            competitionValues.Add(CompetitionKey.IfIAA, IAACM);
            //SkipIAAFromAF();

            CompetitionItem AFCM = new CompetitionItem();
            AFCM.ReSet();
            AFCM.SetInfo(2, OnAFFinal);
            competitionValues.Add(CompetitionKey.IFAF, AFCM);
#if UNITY_EDITOR
            SkipCompetition(CompetitionKey.IFAF);
#endif

            CompetitionItem YRTTSDK = new CompetitionItem();
            YRTTSDK.ReSet();
            YRTTSDK.SetInfo(1, OnYRTTFinal);
            competitionValues.Add(CompetitionKey.YRTT, YRTTSDK);

            CompetitionItem START = new CompetitionItem();
            START.ReSet();
            START.SetInfo(2, OnSTARTFinal);
            competitionValues.Add(CompetitionKey.START, START);
        }

        /// <summary>
        /// ����ֵ����
        /// </summary>
        /// <param name="key">����</param>
        /// <param name="value">����ֵ</param>
        /// <param name="level">�����ȼ�</param>
        public void CompetitionVariable(CompetitionKey key, object value, int level)
        {
            if (competitionValues.ContainsKey(key))
            {
                competitionValues[key].SetFinalValue(value, level);
            }
            else
            {
                Debug.LogError($"\'{key}\' is not exist in competition values");
            }
        }

        /// <summary>
        /// ����ĳ�ξ������ǽ�����
        /// </summary>
        /// <param name="key">����</param>
        public void SkipCompetition(CompetitionKey key)
        {
            if (competitionValues.ContainsKey(key))
            {
                competitionValues[key].SkipCompetition();
            }
            else
            {
                Debug.LogError($"\'{key}\' is not exist in competition values");
            }
        }

        private void OnIAAFinal(object obj)
        {
            GameDefines.ifIAA = (bool)obj;
            OnFinished?.Invoke();
            Game.Instance.curPreLoadCount += 1;
            FacadeGamePlay.LoadingSilderMoveAnim?.Invoke();
            D.Error("[Final]:______________________________________________ iaa");
        }

        private void OnAFFinal(object obj)
        {
            GameDefines.AFJustState = (bool)obj;
            if (attributionData == null) attributionData = new Dictionary<string, object>();
            TDAnalyticsManager.Instance.SetAttributionData(attributionData);
            D.Error("[Final]:______________________________________________ af");
        }

        private void OnYRTTFinal(object obj)
        {
            bool result = (bool)obj;
            GameDefines.YRTTState = result;
            if (result)
            {
                Game.Instance.curPreLoadCount += 1;
                FacadeGamePlay.LoadingSilderMoveAnim?.Invoke();
                D.Error("[Final]:______________________________________________ YRTT succ");
            }
            else
            {
                D.Error("[Final]:______________________________________________ YRTT fail");
            }
        }

        private void OnSTARTFinal(object obj)
        {
            UIManager.Instance.OpenAsync<UILoading>(EUIType.EUILoading, UIOpenType.None, (BaseUI) => 
            {
                D.Error("[Final]:______________________________________________ start");
                Game.Instance.curPreLoadCount += 1;
                FacadeGamePlay.LoadingSilderMoveAnim();
            });
        }

        #region ��������

        /// <summary>
        /// ���afһֱû��ֵ����ֱ�������Ǵξ���
        /// </summary>
        private void SkipIAAFromAF()
        {
            STimerManager.Instance.CreateSTimer(GameDefines.AFWaitTime, 0 , false, true, () => 
            {
                SkipCompetition(CompetitionKey.IfIAA);
            });
        }

        #endregion


        public void Dispose()
        {
            
        }
    }
}