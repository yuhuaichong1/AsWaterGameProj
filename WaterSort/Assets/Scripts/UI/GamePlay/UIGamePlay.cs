
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIGamePlay : BaseUI
    {
        protected override void OnAwake()
        {
            FacadeAdd();

            InitShow();
        }

        #region Facade

        /// <summary>
        /// 添加Facade接口
        /// </summary>
        private void FacadeAdd()
        {
            FacadeGamePlay.SetCurMoneyShow += SetCurMoneyShow;
            FacadeGamePlay.SetProp1CountShow += SetProp1CountShow;
            FacadeGamePlay.SetProp2CountShow += SetProp2CountShow;
            FacadeGamePlay.SetProp3CountShow += SetProp3CountShow;
            FacadeGamePlay.SetCurLevelText += SetCurLevelText;
            FacadeGamePlay.SetWithdrawalTip += SetWithdrawalTip;
            FacadeGamePlay.SetShuffleTipShow += SetShuffleTipShow;

            FacadeGamePlay.GetCMDialogTextPos += GetCMDialogTextPos;
            FacadeGamePlay.GetFlyObjGoalPos += GetFlyObjGoalPos;

            FacadeGamePlay.GetCupPart += GetCupPart;
            FacadeGamePlay.GetCupPartShadow += GetCupPartShadow;
            FacadeGamePlay.GetPockets += GetPockets;

            FacadeGamePlay.AbleProp1Btn += AbleProp1Btn;
            FacadeGamePlay.AbleProp2Btn += AbleProp2Btn;
            FacadeGamePlay.AbleProp3Btn += AbleProp3Btn;
        }

        /// <summary>
        /// 去除Facade接口
        /// </summary>
        private void FacadeRemove()
        {
            FacadeGamePlay.SetCurMoneyShow -= SetCurMoneyShow;
            FacadeGamePlay.SetProp1CountShow -= SetProp1CountShow;
            FacadeGamePlay.SetProp2CountShow -= SetProp2CountShow;
            FacadeGamePlay.SetProp3CountShow -= SetProp3CountShow;
            FacadeGamePlay.SetCurLevelText -= SetCurLevelText;
            FacadeGamePlay.SetWithdrawalTip -= SetWithdrawalTip;
            FacadeGamePlay.SetShuffleTipShow -= SetShuffleTipShow;

            FacadeGamePlay.GetCMDialogTextPos -= GetCMDialogTextPos;
            FacadeGamePlay.GetFlyObjGoalPos -= GetFlyObjGoalPos;

            FacadeGamePlay.GetCupPart -= GetCupPart;
            FacadeGamePlay.GetCupPartShadow -= GetCupPartShadow;
            FacadeGamePlay.GetPockets -= GetPockets;

            FacadeGamePlay.AbleProp1Btn -= AbleProp1Btn;
            FacadeGamePlay.AbleProp2Btn -= AbleProp2Btn;
            FacadeGamePlay.AbleProp3Btn -= AbleProp3Btn;
        }

        #endregion

        private void InitShow()
        {
            mMoneyIcon.gameObject.SetActive(!GameDefines.ifIAA);
            mIAAMoneyIcon.gameObject.gameObject.SetActive(GameDefines.ifIAA);
            mCMBtn.gameObject.SetActive(!GameDefines.ifIAA);
            mCMDialog.gameObject.SetActive(!GameDefines.ifIAA);

            mCurLevelText.text = string.Format(FacadeLanguage.GetText?.Invoke("10016"), FacadePlayer.GetLevel());

            SetCurMoneyShow();
            SetProp1CountShow();
            SetProp2CountShow();
            SetProp3CountShow();
        }

        protected override void OnEnable()
        {
            SetShuffleTipShow(false);

            FacadeGamePlay.CreateLevel();
        }

        #region 设置部分UI的显示

        /// <summary>
        /// 设置当前金钱的显示
        /// </summary>
        private void SetCurMoneyShow()
        {
            double money = FacadePlayer.GetMoney?.Invoke() ?? 0;
            mCurMoneyText.text = FacadePayType.RegionalChange?.Invoke(money);
        }

        /// <summary>
        /// 设置当前道具1的数量的显示
        /// </summary>
        private void SetProp1CountShow()
        {
            int count = FacadePlayer.GetProp1Num();
            bool b = count > 0;

            mProp1_count_icon.gameObject.SetActive(b);
            mProp1_ad.gameObject.SetActive(!b);

            if (count > 0)
                mProp1_count_Text.text = count.ToString();
        }

        /// <summary>
        /// 设置当前道具2的数量的显示
        /// </summary>
        private void SetProp2CountShow()
        {
            int count = FacadePlayer.GetProp2Num?.Invoke() ?? 0;
            bool b = count > 0;

            mProp2_count_icon.gameObject.SetActive(b);
            mProp2_ad.gameObject.SetActive(!b);

            if (count > 0)
                mProp2_count_Text.text = count.ToString();
        }

        /// <summary>
        /// 设置当前道具3的数量的显示
        /// </summary>
        private void SetProp3CountShow()
        {
            int count = FacadePlayer.GetProp3Num();
            bool b = count > 0;

            mProp3_count_icon.gameObject.SetActive(b);
            mProp3_ad.gameObject.SetActive(!b);

            if (count > 0)
                mProp3_count_Text.text = count.ToString();
        }

        /// <summary>
        /// 设置当前关卡的显示
        /// </summary>
        private void SetCurLevelText()
        {
            int curLevel = FacadePlayer.GetLevel();
            mCurLevelText.text = string.Format(FacadeLanguage.GetText("10016"), curLevel);
        }

        /// <summary>
        /// 设置提现目标显示
        /// </summary>
        private void SetWithdrawalTip()
        {
            //mCMDialogText.text = string.Format(FacadeLanguage.GetText(), );
        }

        /// <summary>
        /// 设置刷新功能的提示的显影
        /// </summary>
        /// <param name="b">显影</param>
        private void SetShuffleTipShow(bool b)
        {
            mShuffleTip.gameObject.SetActive(b);
        }

        #endregion

        #region 获取部分UI

        /// <summary>
        /// 获取兑现提示框的位置
        /// </summary>
        /// <returns>兑现提示框的位置</returns>
        private Vector3 GetCMDialogTextPos()
        {
            return mCMDialogText.transform.position;
        }

        /// <summary>
        /// 获取飞行小特效物体的目标点
        /// </summary>
        /// <returns>飞行小特效物体的目标点</returns>
        private Dictionary<ERewardType, Vector3> GetFlyObjGoalPos() 
        { 
            Dictionary<ERewardType, Vector3> goals = new Dictionary<ERewardType, Vector3>() 
            {
                {ERewardType.Money, mCurMoneyText.transform.position},
                {ERewardType.Prop1, mBtn_Prop1.transform.position},
                {ERewardType.Prop2, mBtn_Prop2.transform.position},
                {ERewardType.Prop3, mBtn_Prop3.transform.position},
            };

            return goals;
        }

        private Transform GetCupPart()
        {
            return mCupPart;
        }

        private Transform GetCupPartShadow()
        {
            return mCupPartShadow;
        }

        private Transform GetPockets()
        {
            return mPockets;
        }

        #endregion

        #region 按钮事件

        private void OnSettingBtnClickHandle()
        {
            UIManager.Instance.OpenAsync<UISetting>(EUIType.EUISetting);
        }

	    private void OnReStartBtnClickHandle()
        {
            UIManager.Instance.OpenAsync<UIReStart>(EUIType.EUIReStart);
        }

	    private void OnBtn_Prop1ClickHandle()
        {
            if (FacadeGamePlay.GetStatus() != GameStatus.Gaming)
                return;

            if(FacadePlayer.GetProp1Num() > 0)
            {
                //FacadePlayer.AddProp1Num(-1);
                FacadeGamePlay.Func_Porp1();
                SetProp1CountShow();
            }
            else
                UIManager.Instance.OpenAsync<UIProp>(EUIType.EUIProp, UIOpenType.None, null, EFuncType.Prop1);
        }

	    private void OnBtn_Prop2ClickHandle()
        {
            if (FacadeGamePlay.GetStatus() != GameStatus.Gaming)
                return;

            if (FacadePlayer.GetProp2Num() > 0)
            {
                FacadePlayer.AddProp2Num(-1);
                FacadeGamePlay.Func_Porp2();
                SetProp2CountShow();
            }
            else
                UIManager.Instance.OpenAsync<UIProp>(EUIType.EUIProp, UIOpenType.None, null, EFuncType.Prop2);
        }

	    private void OnBtn_Prop3ClickHandle()
        {
            if (FacadeGamePlay.GetStatus() != GameStatus.Gaming)
                return;

            if (FacadePlayer.GetProp3Num() > 0)
            {
                FacadePlayer.AddProp3Num(-1);
                FacadeGamePlay.Func_Porp3();
                SetProp3CountShow();
            }
            else
                UIManager.Instance.OpenAsync<UIProp>(EUIType.EUIProp, UIOpenType.None, null, EFuncType.Prop3);
        }

	    private void OnCMBtnClickHandle()
        {
            if (!string.IsNullOrEmpty(FacadeWithdraw.GetWName?.Invoke()) && !string.IsNullOrEmpty(FacadeWithdraw.GetWPhoneOrEmail?.Invoke()))
                UIManager.Instance.OpenAsync<UIWithdrawConfirm>(EUIType.EUIConfirm);
            else
                UIManager.Instance.OpenAsync<UIWithdrawEnterInfo>(EUIType.EUIEnterInfomation);
        }

        private void OnTipExitBtnClickHandle()
        {
            FacadeGamePlay.EndPorp1();
            SetShuffleTipShow(false);
        }

        #endregion

        /// <summary>
        /// 设置功能1按钮是否可点击
        /// </summary>
        /// <param name="b">是否可点击</param>
        private void AbleProp1Btn(bool b)
        {
            mBtn_Prop1.interactable = b;
        }

        /// <summary>
        /// 设置功能2按钮是否可点击
        /// </summary>
        /// <param name="b">是否可点击</param>
        private void AbleProp2Btn(bool b)
        {
            mBtn_Prop2.interactable = b;
        }

        /// <summary>
        /// 设置功能3按钮是否可点击
        /// </summary>
        /// <param name="b">是否可点击</param>
        private void AbleProp3Btn(bool b)
        {
            mBtn_Prop3.interactable = b;
        }

        protected override void OnDisable()
        {
        
        }
        protected override void OnDispose()
        {
            FacadeRemove();
        }
    }
}