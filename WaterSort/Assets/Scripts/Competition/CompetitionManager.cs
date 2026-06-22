using System;
using System.Collections.Generic;
using UnityEngine;

namespace XrCode
{
    public class CompetitionManager : Singleton<CompetitionManager>, ILoad, IDispose
    {
        public Action OnFinished;

        private Dictionary<CompetitionKey, CompetitionItem> competitionValues;

        #region 其他

        public Dictionary<string, object> attributionData;//归因数据

        #endregion

        public void Load()
        {
            competitionValues = new Dictionary<CompetitionKey, CompetitionItem>();
            CompetitionItem IAACM  = new CompetitionItem();
            IAACM.ReSet();
            IAACM.SetInfo(3, OnIAAFinal);
            competitionValues.Add(CompetitionKey.IfIAA, IAACM);
            //SkipIAAFromAF();

            CompetitionItem AFCM = new CompetitionItem();
            AFCM.ReSet();
            AFCM.SetInfo(2, OnAFFinal);
            competitionValues.Add(CompetitionKey.IFAF, AFCM);
#if UNITY_EDITOR
            SkipCompetition(CompetitionKey.IFAF);
#endif
        }

        /// <summary>
        /// 竞争值设置
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="value">竞争值</param>
        /// <param name="level">竞争等级</param>
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
        /// 跳过某次竞争（非结束）
        /// </summary>
        /// <param name="key">键名</param>
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
        }

        private void OnAFFinal(object obj)
        {
            GameDefines.AFJustState = (bool)obj;
            if (attributionData == null) attributionData = new Dictionary<string, object>();
            TDAnalyticsManager.Instance.SetAttributionData(attributionData);
        }

        #region 其他内容

        /// <summary>
        /// 如果af一直没有值，就直接跳过那次竞争
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