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
        private string wName;//兑现姓名
        private string wPhoneOrEmail;//兑现信息
        private EPayType poeType;//兑现信息类型

        private Dictionary<int, WithdrawalRecordItem> withdrawalRecordItems;//兑现记录数据

        private WithdrawTarget curWithdrawTarget;//当前兑现目标
        private float wTarget;//目标兑现金额
        private int curCheckInDay;//当前累计兑现签到天数
        private int curCheckInLevel;//当前累计兑现签到关卡
        private bool canWithdraw;//是否能够兑现

        private Dictionary<int, ConfMoneyInterval> MIData;
        private List<float> TargetInterval;

        private bool ifAfterCreate;//是否在关闭界面后走下一步

        private bool ifOpenUIDateShow;

        private bool ifDailyChecked;//今日的每日签到是否完成

        private int curCehckInBankDay;//当前银行累计审核天数

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
            FacadeWithdraw.GetCurWithdrawTarget += GetCurWithdrawTarget;
            FacadeWithdraw.SetCurWithdrawTarget += SetCurWithdrawTarget;
            FacadeWithdraw.GetCurCheckInDay += GetCurCheckInDay;
            FacadeWithdraw.SetCurCheckInDay += SetCurCheckInDay;
            FacadeWithdraw.AddCurCheckInDay += AddCurCheckInDay;
            FacadeWithdraw.GetCurCheckLevel += GetCurCheckLevel;
            FacadeWithdraw.SetCurCheckLevel += SetCurCheckLevel;
            FacadeWithdraw.AddCurCheckInLevel += AddCurCheckInLevel;
            FacadeWithdraw.GetWTarget += GetWTarget;
            FacadeWithdraw.SetWTarget += SetWTarget;
            FacadeWithdraw.CreateOrder += CreateOrder;
            FacadeWithdraw.SaveCurWithdrawalRecordItems += SaveCurWithdrawalRecordItems;
            FacadeWithdraw.GetTotalRecordMoney += GetTotalRecordMoney;
            FacadeWithdraw.GetWithdrawalRecordItems += GetWithdrawalRecordItems;
            FacadeWithdraw.GetWithdrawalRecordItemById += GetWithdrawalRecordItemById;
            FacadeWithdraw.CheckOpenUI += CheckOpenUI;
            FacadeWithdraw.ActionByCurWTarget += ActionByCurWTarget;
            FacadeWithdraw.GetRemainTarget += GetRemainTarget;
            FacadeWithdraw.GetCanWithdraw += GetCanWithdraw;
            FacadeWithdraw.SetCanWithdraw += SetCanWithdraw;
            FacadeWithdraw.GetLuckySpinReward += GetLuckySpinReward;
            FacadeWithdraw.GetLuckyReward += GetLuckyReward;
            FacadeWithdraw.GetLevelComplateReward += GetLevelComplateReward;
            FacadeWithdraw.AfterCloseWUI += AfterCloseWUI;
            FacadeWithdraw.SetIfAfterCreate += SetIfAfterCreate;
            FacadeWithdraw.GetWithdrawHighValueStr += GetWithdrawHighValueStr;
            FacadeWithdraw.GetWCheckInLevelText += GetWCheckInLevelText;
            FacadeWithdraw.GetWCheckInDayText += GetWCheckInDayText;
            FacadeWithdraw.ReSetCheckInData += ReSetCheckInData;
            FacadeWithdraw.GetCurCheckInBankDay += GetCurCheckInBankDay;
            FacadeWithdraw.SetCurCheckInBankDay += SetCurCheckInBankDay;
        }

        private void FacadeRemove()
        {
            FacadeWithdraw.GetWName -= GetWName;
            FacadeWithdraw.SetWName -= SetWName;
            FacadeWithdraw.GetWPhoneOrEmail -= GetWPhoneOrEmail;
            FacadeWithdraw.SetWPhoneOrEmail -= SetWPhoneOrEmail;
            FacadeWithdraw.GetPayType -= GetPOEType;
            FacadeWithdraw.SetPayType -= SetPOEType;
            FacadeWithdraw.GetCurWithdrawTarget -= GetCurWithdrawTarget;
            FacadeWithdraw.SetCurWithdrawTarget -= SetCurWithdrawTarget;
            FacadeWithdraw.GetCurCheckInDay -= GetCurCheckInDay;
            FacadeWithdraw.SetCurCheckInDay -= SetCurCheckInDay;
            FacadeWithdraw.AddCurCheckInDay -= AddCurCheckInDay;
            FacadeWithdraw.GetCurCheckLevel -= GetCurCheckLevel;
            FacadeWithdraw.SetCurCheckLevel -= SetCurCheckLevel;
            FacadeWithdraw.AddCurCheckInLevel -= AddCurCheckInLevel;
            FacadeWithdraw.GetWTarget -= GetWTarget;
            FacadeWithdraw.SetWTarget -= SetWTarget;
            FacadeWithdraw.CreateOrder -= CreateOrder;
            FacadeWithdraw.SaveCurWithdrawalRecordItems -= SaveCurWithdrawalRecordItems;
            FacadeWithdraw.GetTotalRecordMoney += GetTotalRecordMoney;
            FacadeWithdraw.GetWithdrawalRecordItems -= GetWithdrawalRecordItems;
            FacadeWithdraw.GetWithdrawalRecordItemById -= GetWithdrawalRecordItemById;
            FacadeWithdraw.CheckOpenUI -= CheckOpenUI;
            FacadeWithdraw.ActionByCurWTarget -= ActionByCurWTarget;
            FacadeWithdraw.GetRemainTarget -= GetRemainTarget;
            FacadeWithdraw.GetCanWithdraw -= GetCanWithdraw;
            FacadeWithdraw.SetCanWithdraw -= SetCanWithdraw;
            FacadeWithdraw.GetLuckyReward -= GetLuckyReward;
            FacadeWithdraw.GetLevelComplateReward -= GetLevelComplateReward;
            FacadeWithdraw.AfterCloseWUI -= AfterCloseWUI;
            FacadeWithdraw.SetIfAfterCreate -= SetIfAfterCreate;
            FacadeWithdraw.GetWithdrawHighValueStr -= GetWithdrawHighValueStr;
            FacadeWithdraw.GetWCheckInLevelText -= GetWCheckInLevelText;
            FacadeWithdraw.GetWCheckInDayText -= GetWCheckInDayText;
            FacadeWithdraw.ReSetCheckInData -= ReSetCheckInData;
            FacadeWithdraw.GetCurCheckInBankDay -= GetCurCheckInBankDay;
            FacadeWithdraw.SetCurCheckInBankDay -= SetCurCheckInBankDay;
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

        #region curWithdrawTarget

        private WithdrawTarget GetCurWithdrawTarget()
        {
            return curWithdrawTarget;
        }

        private void SetCurWithdrawTarget(WithdrawTarget value)
        {
            curWithdrawTarget = value;
            SPlayerPrefs.SetInt(PlayerPrefDefines.curWithdrawTarget, (int)curWithdrawTarget);
            SPlayerPrefs.Save();
        }

        #endregion

        #region curCheckInDay

        private int GetCurCheckInDay()
        {
            return curCheckInDay;
        }

        private void SetCurCheckInDay(int value)
        {
            curCheckInDay = value;
            if(curCheckInDay > GameDefines.CheckInDay)
            {
                curCheckInDay = GameDefines.CheckInDay;
            }
            SPlayerPrefs.SetInt(PlayerPrefDefines.curCheckInDay, curCheckInDay);
            SPlayerPrefs.Save();
        }

        private void AddCurCheckInDay(int value)
        {
            curCheckInDay += value;
            if (curCheckInDay > GameDefines.CheckInDay)
            {
                curCheckInDay = GameDefines.CheckInDay;
            }
            if(curCheckInDay == GameDefines.CheckInDay)
            {
                FacadeGuide.SetIfTutorial(true);
            }
            SPlayerPrefs.SetInt(PlayerPrefDefines.curCheckInDay, curCheckInDay);
            SPlayerPrefs.Save();
        }

        #endregion

        #region curCheckInLevel

        private int GetCurCheckLevel()
        {
            return curCheckInLevel;
        }

        private void SetCurCheckLevel(int value)
        {
            curCheckInLevel = value;
            if (curCheckInLevel > GameDefines.CheckInLevel)
            {
                curCheckInLevel = GameDefines.CheckInLevel;
            }
            SPlayerPrefs.SetInt(PlayerPrefDefines.curCheckInLevel, curCheckInLevel);
            SPlayerPrefs.Save();
        }

        private void AddCurCheckInLevel(int value)
        {
            curCheckInLevel += value;
            if(curCheckInLevel > GameDefines.CheckInLevel)
            {
                curCheckInLevel = GameDefines.CheckInLevel;
            }
            if(curCheckInLevel == GameDefines.CheckInLevel && ifDailyChecked)
            {
                SetIfDailyChecked(false);
                AddCurCheckInDay(1);
            }
            SPlayerPrefs.SetInt(PlayerPrefDefines.curCheckInLevel, curCheckInLevel);
            SPlayerPrefs.Save();
        }

        #endregion

        #region wTarget

        private float GetWTarget()
        {
            return wTarget;
        }

        private void SetWTarget()
        {
            float target;
            double curMoney = FacadePlayer.GetMoney();

            int n = (int)(Math.Floor(curMoney) / 1000);

            target = (n + 2) * 1000;
            if (target < GameDefines.MinWithdrawalAmount) target = GameDefines.MinWithdrawalAmount;

            wTarget = target;

            SPlayerPrefs.SetFloat(PlayerPrefDefines.wTarget, wTarget);
            SPlayerPrefs.Save();

            //ModuleMgr.Instance.TDAnalyticsManager.CurTargetsCoins(wTarget);
        }

        #endregion

        #region canWithdraw

        private bool GetCanWithdraw()
        {
            return canWithdraw;
        }

        private void SetCanWithdraw(bool b)
        {
            canWithdraw = b;
            SPlayerPrefs.SetBool(PlayerPrefDefines.canWithdraw, canWithdraw);
            SPlayerPrefs.Save();
        }

        #endregion

        #region ifAfterCreate

        private void SetIfAfterCreate(bool b)
        {
            ifAfterCreate = b;
        }

        #endregion

        #region ifDailyChecked

        private void SetIfDailyChecked(bool value)
        {
            ifDailyChecked = value;
            SPlayerPrefs.SetBool(PlayerPrefDefines.ifDailyChecked, ifDailyChecked);
            SPlayerPrefs.Save();
        }

        #endregion

        #region curCheckInBankDay

        private int GetCurCheckInBankDay()
        {
            return curCehckInBankDay;
        }

        private void SetCurCheckInBankDay(int value)
        {
            curCehckInBankDay = value;
            SPlayerPrefs.SetInt(PlayerPrefDefines.curCehckInBankDay, curCehckInBankDay);
            SPlayerPrefs.Save();
        }

        #endregion

        #endregion

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void LoadData()
        {
            withdrawalRecordItems = new Dictionary<int, WithdrawalRecordItem>();

            wTarget = SPlayerPrefs.GetFloat(PlayerPrefDefines.wTarget, 0);

            MIData = ConfigModule.Instance.Tables.TBMoneyInterval.DataMap;
            TargetInterval = new List<float>();
            foreach (ConfMoneyInterval item in MIData.Values)
            {
                TargetInterval.Add(item.MoneyMax);
            }
            TargetInterval.Add(0);
            TargetInterval.Sort();

            wName = SPlayerPrefs.GetString(PlayerPrefDefines.wName, "");
            wPhoneOrEmail = SPlayerPrefs.GetString(PlayerPrefDefines.wPhoneOrEmail, "");
            poeType = (EPayType)SPlayerPrefs.GetInt(PlayerPrefDefines.poeType, (int)EPayType.None);
            curWithdrawTarget = (WithdrawTarget)SPlayerPrefs.GetInt(PlayerPrefDefines.curWithdrawTarget, (int)WithdrawTarget.PassLevel);
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
                    TargetType = (WithdrawTarget)int.Parse(values[5]),
                };
                withdrawalRecordItems.Add(item.OrderId, item);
            }

            //ifAfterCreate = true;
            ifOpenUIDateShow = false;

            curCheckInDay = SPlayerPrefs.GetInt(PlayerPrefDefines.curCheckInDay, 0);
            curCheckInLevel = SPlayerPrefs.GetInt(PlayerPrefDefines.curCheckInLevel, 0);
            ifDailyChecked = SPlayerPrefs.GetBool(PlayerPrefDefines.ifDailyChecked, true);
            if (curWithdrawTarget == WithdrawTarget.CheckIn && SCheckDateTime.Instance.IfNextDay(GameDefines.CheckInDayKey))
            {
                SetIfDailyChecked(true);
                SetCurCheckLevel(0);
            }

            curCehckInBankDay = SPlayerPrefs.GetInt(PlayerPrefDefines.curCehckInBankDay, 0);
            if(curCheckInDay == GameDefines.CheckInDay && SCheckDateTime.Instance.IfNextDay(GameDefines.CheckInBankKey))
            {
                curCehckInBankDay += 1;
                SetCurCheckInBankDay(curCehckInBankDay);
            }
        }

        /// <summary>
        /// 创建订单
        /// </summary>
        private WithdrawalRecordItem CreateOrder(int level, float money)
        {
            WithdrawalRecordItem recordItem = new WithdrawalRecordItem()
            {
                OrderId = withdrawalRecordItems.Count,
                LevelId = level,
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd"),
                WRState = EWithRecordState.GoWithdrawal,
                WRMoney = money,
                TargetType = curWithdrawTarget,
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
                string str = $"{item.OrderId}_{item.LevelId}_{item.CreatedDate}_{(int)item.WRState}_{item.WRMoney}_{(int)item.TargetType}";
                wrisTemp.Add(str);
            }

            SPlayerPrefs.SetList<string>(PlayerPrefDefines.wrisTemp, wrisTemp);
            SPlayerPrefs.Save();
        }

        /// <summary>
        /// 获得总可兑现金额
        /// </summary>
        /// <returns>总可兑现金额</returns>
        private float GetTotalRecordMoney()
        {
            double totalValue = 0;
            foreach(WithdrawalRecordItem item in withdrawalRecordItems.Values)
            {
                if (item.TargetType != WithdrawTarget.PassLevel) continue;
                totalValue += item.WRMoney;
            }

            return (float)totalValue;
        }

        /// <summary>
        /// 检测应该打开UIEnterInfo还是UIConfirm
        /// </summary>
        private void CheckOpenUI(bool b, WithdrawalRecordItem item)
        {
            if (string.IsNullOrEmpty(wPhoneOrEmail))
            {
                UIManager.Instance.OpenAsync<UIWithdrawEnterInfo>(EUIType.EUIWithdrawEnterInfo);
            }
            else
            {
                UIManager.Instance.OpenAsync<UIWithdrawConfirm>(EUIType.EUIWithdrawConfirm, UIOpenType.None, null, b, item);
            }
        }

        /// <summary>
        /// 根据当前兑现目标执行不同的方法
        /// </summary>
        /// <param name="PL_Action">当目标为“指定关卡”时的方法</param>
        /// <param name="AOM_Action">当目标为“指定金额”时的方法</param>
        /// <param name="CI_Action">当目标为“签到”时的方法</param>
        private void ActionByCurWTarget(Action<int> PL_Action, Action<double> AOM_Action, Action<int> CI_Action)
        {
            switch(curWithdrawTarget)
            {
                case WithdrawTarget.PassLevel:
                    int curLevel = FacadePlayer.GetLevel();
                    PL_Action?.Invoke(curLevel);
                    break;
                case WithdrawTarget.AmountOfMoney:
                    double curMoney = FacadePlayer.GetMoney();
                    AOM_Action?.Invoke(curMoney);
                    break;
                case WithdrawTarget.CheckIn:
                    CI_Action?.Invoke(curCheckInDay);
                    break;
            }
        }

        /// <summary>
        /// 得到距离目标的剩余金额
        /// </summary>
        /// <returns>剩余目标金额</returns>
        private float GetRemainTarget()
        {
            float remain = wTarget - (float)FacadePlayer.GetMoney();
            if (remain < 0)
                remain = 0;
            return remain;
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
        private float GetLuckySpinReward()
        {
            float reward = 1;

            if(GameDefines.ifIAA)
            {
                reward = 1;
            }
            else
            {
                ActionByCurWTarget((v) =>
                {
                    reward = GameDefines.RewardCoe;
                }, (v) =>
                {
                    int id = GetMoneyIntervalSn(GetRemainTarget());
                    reward = MIData[id].LSReward;
                }, (v) =>
                {
                    reward = MIData[MIData.Count - 1].LSReward;
                });
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
                ActionByCurWTarget((v) =>
                {
                    reward = UnityEngine.Random.Range(GameDefines.LuckyReward_RandomRange.x, GameDefines.LuckyReward_RandomRange.y);
                }, (v) =>
                {
                    int id = GetMoneyIntervalSn(GetRemainTarget());
                    reward = UnityEngine.Random.Range(MIData[id].LRMin, MIData[id].LRMax);
                }, (v) =>
                {
                    int id = MIData.Count - 1;
                    reward = UnityEngine.Random.Range(MIData[id].LRMin, MIData[id].LRMax);
                });
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
                ActionByCurWTarget((v) =>
                {
                    reward = UnityEngine.Random.Range(GameDefines.LuckyReward_RandomRange.x, GameDefines.LuckyReward_RandomRange.y);
                }, (v) =>
                {
                    int id = GetMoneyIntervalSn(GetRemainTarget());

                    if (id < 0)
                    {
                        reward = UnityEngine.Random.Range(GameDefines.LuckyReward_RandomRange.x, GameDefines.LuckyReward_RandomRange.y);
                    }
                    else
                    {
                        reward = UnityEngine.Random.Range(MIData[id].LSMin, MIData[id].LSMax);
                    }
                }, (v) =>
                {
                    int id = MIData.Count - 1;
                    reward = UnityEngine.Random.Range(MIData[id].LSMin, MIData[id].LSMax);
                });
            }
            return reward;
        }

        /// <summary>
        /// 在关闭兑现步骤的某一界面时，应该继续往下走的步骤
        /// </summary>
        private void AfterCloseWUI()
        {
            if(ifAfterCreate)
            {
                ifAfterCreate = false;

                ActionByCurWTarget((level) =>
                {
                    switch (level)
                    {
                        case 2:
                            UIManager.Instance.OpenAsync<UIWithdrawKeepEarn>(EUIType.EUIWithdrawKeepEarn);
                            break;
                        case 3:
                            UIManager.Instance.OpenAsync<UIWithdrawKeepEarn>(EUIType.EUIWithdrawKeepEarn);
                            break;
                        case 4:
                            FacadeGamePlay.CreateLevel();
                            break;
                    }
                }, (money) =>
                {
                    if(!ifOpenUIDateShow && FacadePlayer.GetMoney() + GameDefines.DiffVal <= wTarget - 0.01f)
                    {
                        UIManager.Instance.OpenAsync<UIWithdrawLuckyPlayer>(EUIType.EUIWithdrawLuckyPlayer);
                        ifOpenUIDateShow = true;
                        ifAfterCreate = true;
                    }
                    else
                    {
                        UIManager.Instance.OpenAsync<UIDateShow>(EUIType.EUIDateShow);
                        ifOpenUIDateShow = false;
                    }
                    //FacadeGamePlay.CreateLevel();
                }, (day) =>
                {
                    FacadeGamePlay.CreateLevel();
                });
            }       
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

        /// <summary>
        /// 获取当前签到目标关卡文本
        /// </summary>
        /// <returns>签到目标关卡文本</returns>
        private string GetWCheckInLevelText()
        {
            if(curCheckInLevel >= GameDefines.CheckInLevel)
            {
                return FacadeLanguage.GetText("10008");
            }
            else
            {
                return string.Format(FacadeLanguage.GetText("10007"), GameDefines.CheckInLevel - curCheckInLevel);
            }
        }

        /// <summary>
        /// 获取当前签到目标日文本
        /// </summary>
        /// <returns>签到目标日文本</returns>
        private string GetWCheckInDayText()
        {
            if(curCheckInDay >= GameDefines.CheckInDay)
            {
                return string.Format(FacadeLanguage.GetText("10128"));
            }
            else
            {
                return string.Format(FacadeLanguage.GetText("10083"), GameDefines.CheckInDay, GameDefines.CheckInDay - curCheckInDay);
            }
        }

        /// <summary>
        /// 重置签到数据
        /// </summary>
        private void ReSetCheckInData()
        {
            FacadePlayer.SetMoney(0);
            FacadeGamePlay.SetCurMoneyShow();

            FacadeWithdraw.SetCurCheckInDay(0);
            if (FacadeWithdraw.GetCurCheckLevel() >= GameDefines.CheckInLevel)
            {
                FacadeWithdraw.AddCurCheckInDay(1);
            }

            FacadeGamePlay.SetLevelShow();
        }

        protected override void OnDispose()
        {
            FacadeRemove();

            withdrawalRecordItems.Clear();
            withdrawalRecordItems = null;
        }
    }
}


