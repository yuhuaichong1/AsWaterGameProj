
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIWithdrawTarget : BaseUI
    {
        private UIWTOpenType curOpenType;
        private Action clickAction;

        protected override void OnSetParam(params object[] args)
        {
            curOpenType = (UIWTOpenType)args[0];
            clickAction = (Action)args[1];
        }

        protected override void OnAwake()
        {}
        protected override void OnEnable()
        {
            switch (curOpenType)
            {
                case UIWTOpenType.FirstTarget:
                    ShowFirstTarget();
                    break;
                case UIWTOpenType.AVPTarget:
                    ShowAVPTarget();
                    break;
                case UIWTOpenType.VPTarget:
                    ShowVPTarget();
                    break;
                case UIWTOpenType.SVPTarget:
                    ShowSVPTarget();
                    break;
                case UIWTOpenType.SVPMoneyTarget:
                    ShowSVPMoneyTarget();
                    break;
                case UIWTOpenType.FinishTarget1:
                    ShowFinishTarget();
                    break;
                case UIWTOpenType.FinishTarget2:
                    ShowFinishTarget2();
                    break;
            }

            ShowAnim(mPlane);
        }

        /// <summary>
        /// 首次进入游戏时的目标
        /// </summary>
        private void ShowFirstTarget()
        {
            curOpenType = UIWTOpenType.FirstTarget;

            mTitle.gameObject.SetActive(false);
            mLevelTargetTip.gameObject.SetActive(false);

            ShowWaiterAndName(0);
            ShowContent(0);
            mWTS1Text.text = string.Format(FacadeLanguage.GetText("10118"), FacadeWithdraw.GetWithdrawHighValueStr());
        }

        /// <summary>
        /// 第1关的目标
        /// </summary>
        private void ShowAVPTarget()
        {
            curOpenType = UIWTOpenType.AVPTarget;

            mTitleContent.text = string.Format(FacadeLanguage.GetText("10010"), 1);
            mLevelTargetTip.gameObject.SetActive(false);

            ShowWaiterAndName(0);
            ShowContent(1);

            mWTS1Text.text = string.Format(FacadeLanguage.GetText("10120"), 1);
        }

        /// <summary>
        /// 第2关的目标
        /// </summary>
        private void ShowVPTarget()
        {
            curOpenType = UIWTOpenType.VPTarget;

            mTitleContent.text = string.Format(FacadeLanguage.GetText("10010"), 2);
            mLevelTargetTip.gameObject.SetActive(false);

            ShowWaiterAndName(1);
            ShowContent(1);

            mWTS1Text.text = string.Format(FacadeLanguage.GetText("10120"), 2);
        }

        /// <summary>
        /// 第3~17关的目标
        /// </summary>
        private void ShowSVPTarget()
        {
            curOpenType = UIWTOpenType.SVPTarget;

            mTitleContent.text = string.Format(FacadeLanguage.GetText("10010"), 8);
            bool showTip = FacadePlayer.GetLevel() >= GameDefines.miniLevel_Start;
            mLevelTargetTip.gameObject.SetActive(showTip);
            if (showTip)
            {
                int curRound = FacadePlayer.GetLevel() - (GameDefines.miniLevel_Start - 1) + 1;
                int totalRound = GameDefines.miniLevel_End - GameDefines.miniLevel_Start + 2;
                mLTText.text = string.Format(FacadeLanguage.GetText("10115"), GameDefines.miniLevel_Start - 1, $"{curRound}/{totalRound}");
            }

            ShowWaiterAndName(2);
            ShowContent(1);

            mWTS1Text.text = string.Format(FacadeLanguage.GetText("10120"), GameDefines.miniLevel_Start - 1);

        }

        /// <summary>
        /// 目标兑现数值的目标
        /// </summary>
        private void ShowSVPMoneyTarget()
        {
            curOpenType = UIWTOpenType.SVPMoneyTarget;

            mTitle.gameObject.SetActive(false);
            mLevelTargetTip.gameObject.SetActive(false);

            ShowWaiterAndName(2);
            ShowContent(2);

            mContentText2.text = string.Format(FacadeLanguage.GetText("10005"), FacadeWithdraw.GetRemainTarget(), FacadeWithdraw.GetWTarget());
        }

        private void ShowFinishTarget()
        {
            curOpenType = UIWTOpenType.FinishTarget1;

            mWTS2Text.text = string.Format(FacadeLanguage.GetText("10120"), FacadePlayer.GetLevel() - 1);

            ShowWaiterAndName(0);
            ShowContent(3);
        }

        private void ShowFinishTarget2()
        {
            curOpenType = UIWTOpenType.FinishTarget2;

            ShowWaiterAndName(0);
            ShowContent(4);
        }

        /// <summary>
        /// 展示哪个人物
        /// </summary>
        /// <param name="id"></param>
        private void ShowWaiterAndName(int id)
        {
            mWaiter1.gameObject.SetActive(id == 0);
            mName1.gameObject.SetActive(id == 0);
            mWaiter2.gameObject.SetActive(id == 1);
            mName2.gameObject.SetActive(id == 1);
            mWaiter3.gameObject.SetActive(id == 2);
            mName3.gameObject.SetActive(id == 2);
        }

        /// <summary>
        /// 展示哪个内容
        /// </summary>
        /// <param name="id"></param>
        private void ShowContent(int id)
        {
            mContentText1.gameObject.SetActive(id == 0);
            mWTargetSign1.gameObject.SetActive(id == 1);
            mContentText2.gameObject.SetActive(id == 2);
            mWTargetSign2.gameObject.SetActive(id == 3);
            mWTargetSign3.gameObject.SetActive(id == 4);
        }

        private void OnContinueBtnClickHandle()
        {
            if (curOpenType == UIWTOpenType.FirstTarget)
            {
                ShowAVPTarget();
                //clickAction?.Invoke();
            }
            else if (curOpenType == UIWTOpenType.FinishTarget1)
            {
                ShowFinishTarget2();
            }
            else
            {
                HideAnim(mPlane, () =>
                {
                    UIManager.Instance.CloseUI(EUIType.EUIWithdrawTarget);
                    //FacadeGamePlay.CreateLevel();
                    clickAction?.Invoke();
                });
            }
        }
        protected override void OnDisable() { }
        protected override void OnDispose() { }
    }
}