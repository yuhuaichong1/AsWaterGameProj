using System;
using System.Collections.Generic;
using System.Linq;
using static UnityEngine.GraphicsBuffer;

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

        protected override void OnLoad()
        {
            withdrawalRecordItems = new Dictionary<int, WithdrawalRecordItem>();

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
            if (curCheckInDay > GameDefines.CheckInDay)
            {
                curCheckInDay = GameDefines.CheckInDay;
            }
            SPlayerPrefs.SetInt(PlayerPrefDefines.curCheckInLevel, curCheckInLevel);
            SPlayerPrefs.Save();
        }

        private void AddCurCheckInLevel(int value)
        {
            curCheckInLevel += value;
            if(curCheckInDay > GameDefines.CheckInDay)
            {
                curCheckInDay = GameDefines.CheckInDay;
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

        #endregion

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void LoadData()
        {
            wName = SPlayerPrefs.GetString(PlayerPrefDefines.wName, "");
            wPhoneOrEmail = SPlayerPrefs.GetString(PlayerPrefDefines.wPhoneOrEmail, "");
            poeType = (EPayType)SPlayerPrefs.GetInt(PlayerPrefDefines.poeType, (int)EPayType.Other);
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

        }

        /// <summary>
        /// 创建订单
        /// </summary>
        private void CreateOrder(int level, float money)
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
        }

        /// <summary>
        /// 保存单当前订单数据
        /// </summary>
        private void SaveCurWithdrawalRecordItems()
        {
            List<string> wrisTemp = new List<string>();

            foreach (WithdrawalRecordItem item in withdrawalRecordItems.Values)
            {
                string str = $"{item.OrderId}_{item.LevelId}_{item.CreatedDate}_{(int)item.WRState}_{item.WRMoney}_{item.TargetType}";
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

        protected override void OnDispose()
        {
            FacadeRemove();

            withdrawalRecordItems.Clear();
            withdrawalRecordItems = null;
        }
    }
}


