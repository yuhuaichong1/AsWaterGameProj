using cfg;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine.UI;

namespace XrCode
{
    public class WithdrawModule : BaseModule
    {
        private Dictionary<int, string> payTypeInfo;//不同兑现渠道对应的信息

        private string wName;//兑现姓名
        private string wPhoneOrEmail;//兑现信息
        private EPayType poeType;//兑现信息类型

        private Dictionary<int, WithdrawalRecordItem> withdrawalRecordItems;//兑现记录数据

        private Dictionary<int, ConfMoneyInterval> MIData;
        private List<double> TargetInterval;


        private int trWaitTime;//累计奖励的累计等待时间
        private TrStatus trStatus;//累计奖励当前状态

        private float curMaxTargetMoney;//当前最大目标金钱
        private EPayType curMaxTargetType;//当前最大目标金钱的

        protected override void OnLoad()
        {
            FacadeAdd();

            LoadData();
        }

        #region Facade

        private void FacadeAdd()
        {
            FacadeWithdraw.GetWName += GetWName;
            FacadeWithdraw.SetWName += SetWName;
            FacadeWithdraw.GetWPhoneOrEmail += GetWPhoneOrEmail;
            FacadeWithdraw.SetWPhoneOrEmail += SetWPhoneOrEmail;
            FacadeWithdraw.GetPayType += GetPOEType;
            FacadeWithdraw.SetPayType += SetPOEType;
            FacadeWithdraw.CreateOrder += CreateOrder;
            FacadeWithdraw.SaveCurWithdrawalRecordItems += SaveCurWithdrawalRecordItems;
            FacadeWithdraw.GetTotalRecordMoney += GetTotalRecordMoney;
            FacadeWithdraw.GetWithdrawalRecordItems += GetWithdrawalRecordItems;
            FacadeWithdraw.GetWithdrawalRecordItemById += GetWithdrawalRecordItemById;
            FacadeWithdraw.CheckOpenUI += CheckOpenUI;
            FacadeWithdraw.GetLuckySpinReward += GetLuckySpinReward;
            FacadeWithdraw.GetLuckyReward += GetLuckyReward;
            FacadeWithdraw.GetLevelComplateReward += GetLevelComplateReward;
            FacadeWithdraw.GetWithdrawHighValueStr += GetWithdrawHighValueStr;
            FacadeWithdraw.OpenTotalRewardUI += OpenTotalRewardUI;
            FacadeWithdraw.GetCurTRDay += GetCurTRDay;
            FacadeWithdraw.GetRemainTRDay += GetRemainTRDay;
            FacadeWithdraw.SetCurTRDay += SetCurTRDay;
            FacadeWithdraw.SetTrStatus += SetTrStatus;
            FacadeWithdraw.RefushWaitDay += RefushWaitDay;
            FacadeWithdraw.SetWPhoneOrEmail2 += SetWPhoneOrEmail2;
            FacadeWithdraw.GetWPhoneOrEmail2 += GetWPhoneOrEmail2;
            FacadeWithdraw.GetEliminationReward += GetEliminationReward;
        }

        private void FacadeRemove()
        {
            FacadeWithdraw.GetWName -= GetWName;
            FacadeWithdraw.SetWName -= SetWName;
            FacadeWithdraw.GetWPhoneOrEmail -= GetWPhoneOrEmail;
            FacadeWithdraw.SetWPhoneOrEmail -= SetWPhoneOrEmail;
            FacadeWithdraw.GetPayType -= GetPOEType;
            FacadeWithdraw.SetPayType -= SetPOEType;
            FacadeWithdraw.CreateOrder -= CreateOrder;
            FacadeWithdraw.SaveCurWithdrawalRecordItems -= SaveCurWithdrawalRecordItems;
            FacadeWithdraw.GetTotalRecordMoney += GetTotalRecordMoney;
            FacadeWithdraw.GetWithdrawalRecordItems -= GetWithdrawalRecordItems;
            FacadeWithdraw.GetWithdrawalRecordItemById -= GetWithdrawalRecordItemById;
            FacadeWithdraw.CheckOpenUI -= CheckOpenUI;
            FacadeWithdraw.GetLuckyReward -= GetLuckyReward;
            FacadeWithdraw.GetLevelComplateReward -= GetLevelComplateReward;
            FacadeWithdraw.GetWithdrawHighValueStr -= GetWithdrawHighValueStr;
            FacadeWithdraw.OpenTotalRewardUI -= OpenTotalRewardUI;
            FacadeWithdraw.GetCurTRDay -= GetCurTRDay;
            FacadeWithdraw.GetRemainTRDay -= GetRemainTRDay;
            FacadeWithdraw.SetCurTRDay -= SetCurTRDay;
            FacadeWithdraw.SetTrStatus -= SetTrStatus;
            FacadeWithdraw.RefushWaitDay -= RefushWaitDay;
            FacadeWithdraw.SetWPhoneOrEmail2 -= SetWPhoneOrEmail2;
            FacadeWithdraw.GetWPhoneOrEmail2 -= GetWPhoneOrEmail2;
            FacadeWithdraw.GetEliminationReward -= GetEliminationReward;
        }

        #endregion

        #region Get/Set

        #region wName
        private string GetWName()
        {
            return wName;
        }
        private void SetWName(string value)
        {
            wName = value;
            SPlayerPrefs.SetString(PlayerPrefDefines.wName, wName);
            SPlayerPrefs.Save();
        }
        #endregion

        #region wPhoneOrEmail
        private string GetWPhoneOrEmail()
        {
            return wPhoneOrEmail;
        }

        private void SetWPhoneOrEmail(string value)
        {
            wPhoneOrEmail = value;
            SPlayerPrefs.SetString(PlayerPrefDefines.wPhoneOrEmail, wPhoneOrEmail);
            SPlayerPrefs.Save();
        }
        #endregion

        #region poeType
        private EPayType GetPOEType()
        {
            return poeType;
        }
        private void SetPOEType(EPayType value)
        {
            poeType = value;
            SPlayerPrefs.SetInt(PlayerPrefDefines.poeType, (int)poeType);
            SPlayerPrefs.Save();
        }
        #endregion

        #region withdrawalRecordItems
        private List<WithdrawalRecordItem> GetWithdrawalRecordItems()
        {
            return withdrawalRecordItems.Values.ToList();
        }

        private WithdrawalRecordItem GetWithdrawalRecordItemById(int orderId)
        {
            if (withdrawalRecordItems.ContainsKey(orderId))
            {
                return withdrawalRecordItems[orderId];
            }
            else
            {
                D.Error($"'{orderId}' is not exist in withdraw orders");
                return null;
            }
        }

        #endregion

        #endregion

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void LoadData()
        {
            withdrawalRecordItems = new Dictionary<int, WithdrawalRecordItem>();

            MIData = ConfigModule.Instance.Tables.TBMoneyInterval.DataMap;
            TargetInterval = new List<double>();
            foreach (ConfMoneyInterval item in MIData.Values)
            {
                TargetInterval.Add(item.MoneyMax);
            }
            TargetInterval.Add(0);
            TargetInterval.Sort();

            wName = SPlayerPrefs.GetString(PlayerPrefDefines.wName, "");
            wPhoneOrEmail = SPlayerPrefs.GetString(PlayerPrefDefines.wPhoneOrEmail, "");
            poeType = (EPayType)SPlayerPrefs.GetInt(PlayerPrefDefines.poeType, (int)EPayType.None);
            List<string> wrisTemp = SPlayerPrefs.GetList<string>(PlayerPrefDefines.wrisTemp, new List<string>());
            foreach (string wri in wrisTemp)
            {
                string[] values = wri.Split("_");
                WithdrawalRecordItem item = new WithdrawalRecordItem()
                {
                    OrderId = int.Parse(values[0]),
                    LevelId = int.Parse(values[1]),
                    CreatedDate = values[2],
                    WRState = (EWithRecordState)int.Parse(values[3]),
                    WRMoney = float.Parse(values[4]),
                };
                withdrawalRecordItems.Add(item.OrderId, item);
            }



            trStatus = (TrStatus)SPlayerPrefs.GetInt(PlayerPrefDefines.trStatus, 1);
            if(trStatus == TrStatus.WaitResults || trStatus == TrStatus.WaitNext)
                RefushWaitDay(false);

            payTypeInfo = SPlayerPrefs.GetDictionary<int, string>(PlayerPrefDefines.payTypeInfo, new Dictionary<int, string>());
            List<PayNode> payNodes = FacadePayType.GetPayItems();
            bool ifSave = false;
            foreach(PayNode payNode in payNodes)
            {
                int typeInt = (int)(payNode.payType);
                if (!payTypeInfo.ContainsKey(typeInt))
                {
                    ifSave = true;
                    payTypeInfo.Add(typeInt, "");
                }
            }
            if(ifSave)
            {
                SPlayerPrefs.SetDictionary<int, string>(PlayerPrefDefines.payTypeInfo, payTypeInfo);
                SPlayerPrefs.Save();
            }

            //curMaxTargetMoney = SPlayerPrefs.GetFloat(PlayerPrefDefines.curMaxTargetMoney, 1000);
            //curMaxTargetType = (EPayType)SPlayerPrefs.GetInt(PlayerPrefDefines.curMaxTargetType, (int)payNodes[0].payType);
        }

        /// <summary>
        /// 创建订单
        /// </summary>
        private WithdrawalRecordItem CreateOrder(int level, double money)
        {
            WithdrawalRecordItem recordItem = new WithdrawalRecordItem()
            {
                OrderId = withdrawalRecordItems.Count,
                LevelId = level,
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd"),
                WRState = EWithRecordState.State1,
                WRMoney = money,
            };

            withdrawalRecordItems.Add(recordItem.OrderId, recordItem);

            SaveCurWithdrawalRecordItems();

            return recordItem;
        }

        /// <summary>
        /// 保存单当前订单数据
        /// </summary>
        private void SaveCurWithdrawalRecordItems()
        {
            List<string> wrisTemp = new List<string>();

            foreach (WithdrawalRecordItem item in withdrawalRecordItems.Values)
            {
                string str = $"{item.OrderId}_{item.LevelId}_{item.CreatedDate}_{(int)item.WRState}_{item.WRMoney}";
                wrisTemp.Add(str);
            }

            SPlayerPrefs.SetList<string>(PlayerPrefDefines.wrisTemp, wrisTemp);
            SPlayerPrefs.Save();
        }

        /// <summary>
        /// 获得总可兑现金额
        /// </summary>
        /// <returns>总可兑现金额</returns>
        private double GetTotalRecordMoney()
        {
            double totalValue = 0;
            foreach(WithdrawalRecordItem item in withdrawalRecordItems.Values)
            {
                totalValue += item.WRMoney;
            }

            return totalValue;
        }

        /// <summary>
        /// 检测应该打开UIEnterInfo还是UIConfirm
        /// </summary>
        private void CheckOpenUI(bool b, WithdrawalRecordItem item)
        {
            if (string.IsNullOrEmpty(wPhoneOrEmail))
            {
                //UIManager.Instance.OpenAsync<UIWithdrawEnterInfo>(EUIType.EUIWithdrawEnterInfo);
            }
            else
            {
                //UIManager.Instance.OpenAsync<UIWithdrawConfirm>(EUIType.EUIWithdrawConfirm, UIOpenType.None, null, b, item);
            }
        }

        /// <summary>
        /// 根据剩余目标金额查找 MoneyInterval 配置的 sn。
        /// TargetInterval 升序断点对应 sn 从大到小，不能用 TargetInterval.Count 做反转。
        /// </summary>
        private int GetMoneyIntervalSn(float remainTarget)
        {
            int intervalId = TargetInterval.GetRangeIndex(remainTarget);
            if (intervalId < 0)
                return -1;
            return MIData.Count - 1 - intervalId;
        }

        /// <summary>
        /// 获得幸运转盘金额奖励的奖励值
        /// </summary>
        /// <returns>幸运转盘金额奖励的奖励值</returns>
        private float GetLuckySpinReward(float count)
        {
            float reward = 1;

            if(GameDefines.ifIAA)
            {
                reward = count;
            }
            else
            {
                //reward = GameDefines.RewardCoe;
                reward = FacadePayout.GetLuckySpinAmount();
            }

            return reward;
        }

        /// <summary>
        /// 获取幸运奖励金额奖励的奖励值
        /// </summary>
        /// <returns>幸运奖励金额奖励的奖励值</returns>
        private float GetLuckyReward()
        {
            float reward = 10;

            if (GameDefines.ifIAA)
            {
                reward = 10;
            }
            else
            {
                //reward = UnityEngine.Random.Range(GameDefines.LuckyReward_RandomRange.x, GameDefines.LuckyReward_RandomRange.y);
                reward = FacadePayout.GetLuckyRewardAmount();
            }

            return reward;
        }

        /// <summary>
        /// 获取关卡完成金额奖励的奖励值
        /// </summary>
        /// <returns>关卡完成金额奖励的奖励值</returns>
        private float GetLevelComplateReward()
        {
            float reward = 20;

            if (GameDefines.ifIAA)
            {
                reward = 20;
            }
            else
            {
                //reward = UnityEngine.Random.Range(GameDefines.LuckyReward_RandomRange.x, GameDefines.LuckyReward_RandomRange.y);
                reward = FacadePayout.GetLevelComplatedAmount();
            }
            return reward;
        }

        private float GetEliminationReward()
        {
            float reward = 1;

            if (GameDefines.ifIAA)
            {
                reward = GameDefines.IAA_Elimination_Money;
            }
            else
            {
                //reward = UnityEngine.Random.Range(GameDefines.LuckyReward_RandomRange.x, GameDefines.LuckyReward_RandomRange.y);
                reward = FacadePayout.GetEliminationAmount();
            }
            return reward;
        }

        /// <summary>
        /// 获得兑现提示区间文本
        /// </summary>
        /// <returns>兑现提示区间文本</returns>
        private string GetWithdrawHighValueStr()
        {
            string v1 = FacadePayType.RegionalChange(GameDefines.HighValue.x).Split('.')[0];
            string v2 = FacadePayType.RegionalChange(GameDefines.HighValue.y).Split('.')[0];
            return $"{v1}-{v2}";
        }


        #region TotalReward相关

        private void OpenTotalRewardUI()
        {
            switch (trStatus)
            {
                case TrStatus.PassLevel:
                    UIManager.Instance.OpenAsync<UITotalReward>(EUIType.EUITotalReward);
                    break;
                case TrStatus.WaitResults:
                    RefushWaitDay(false);

                    if (GetRemainTRDay() != 0)
                        UIManager.Instance.OpenAsync<UITotalReward2>(EUIType.EUITotalReward2);
                    else
                    {
                        SetTrStatus(TrStatus.ViewResults);
                        UIManager.Instance.OpenAsync<UITotalReward3>(EUIType.EUITotalReward3);
                    }
                    break;
                case TrStatus.ViewResults:
                    UIManager.Instance.OpenAsync<UITotalReward3>(EUIType.EUITotalReward3);
                    break;
                case TrStatus.WaitNext:
                    RefushWaitDay(false);

                    if (GetRemainTRDay() != 0)
                        UIManager.Instance.OpenAsync<UITotalReward2>(EUIType.EUITotalReward2);
                    else
                    {
                        SetTrStatus(TrStatus.ViewResults);
                        UIManager.Instance.OpenAsync<UITotalReward3>(EUIType.EUITotalReward3);
                    }
                    break;
            }
        }

        private void RefushWaitDay(bool ifUpdate)
        {
            double totalS = SCheckDateTime.Instance.SinceLastTime(GameDefines.totalRewardKey, ifUpdate);
            trWaitTime = (int)(totalS / 86400);
            SetCurTRDay(trWaitTime);
        }

        private int GetCurTRDay()
        {
            return trWaitTime;
        }

        private void SetCurTRDay(int value)
        {
            trWaitTime = value;
            SPlayerPrefs.SetInt(PlayerPrefDefines.trWaitTime, trWaitTime);
            SPlayerPrefs.Save();
        }

        private int GetRemainTRDay()
        {
            switch(trStatus)
            {
                case TrStatus.WaitResults:
                    return GameDefines.totalRewardTime - trWaitTime;
                case TrStatus.WaitNext:
                    return GameDefines.totalRewardNextTime - trWaitTime;
                default: 
                    return 9;
            }
        }

        private void SetTrStatus(TrStatus value)
        {
            trStatus = value;
            SPlayerPrefs.SetInt(PlayerPrefDefines.trStatus, (int)trStatus);
            SPlayerPrefs.Save();
        }

        #endregion

        #region Account相关

        private string GetWPhoneOrEmail2(EPayType type)
        {
            return payTypeInfo[(int)type];
        }

        private void SetWPhoneOrEmail2(EPayType type, string value)
        {
            payTypeInfo[(int)type] = value;
            SPlayerPrefs.SetDictionary<int, string>(PlayerPrefDefines.payTypeInfo, payTypeInfo);
            SPlayerPrefs.Save();
        }

        #endregion

        protected override void OnDispose()
        {
            FacadeRemove();

            withdrawalRecordItems.Clear();
            withdrawalRecordItems = null;
        }
    }
}


