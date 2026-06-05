
using cfg;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIWithdrawConfirm : BaseUI
    {
        private TBPayChannel PayChannelTable;

        private WithdrawalRecordItem UIProgressTarget;

        protected override void OnSetParam(params object[] args)
        {
            UIProgressTarget = (WithdrawalRecordItem)args[0];
        }

        protected override void OnAwake()
        {
            PayChannelTable = ConfigModule.Instance.Tables.TBPayChannel;
        }

        protected override void OnEnable()
        {
            string name = FacadeWithdraw.GetWName();
            string msg = FacadeWithdraw.GetWPhoneOrEmail();
            Sprite icon = ResourceMod.Instance.SyncLoad<Sprite>(PayChannelTable.Get((int)(FacadeWithdraw.GetPayType?.Invoke())).PicPath);

            mCurPayIcon.sprite = icon;
            mInfoText.text = name != "" ? $"{name}\n{msg}" : $"{msg}";

            ShowAnim(mPlane, () =>
            {
                
            });
        }

        private void OnExitBtnClickHandle()
        {
            HideAnim(mPlane, () => 
            {
                FacadeWithdraw.AfterCloseWUI();
                UIManager.Instance.CloseUI(EUIType.EUIWithdrawConfirm);
            });
        }

        private void OnReEnterBtnClickHandle()
        {
            HideAnim(mPlane, () =>
            {
                UIManager.Instance.CloseUI(EUIType.EUIWithdrawConfirm);
                UIManager.Instance.OpenAsync<UIWithdrawEnterInfo>(EUIType.EUIWithdrawEnterInfo, UIOpenType.None, null, UIProgressTarget);
            });
        }

        private void OnWNEnterBtnClickHandle()
        {
            HideAnim(mPlane, () =>
            {
                UIManager.Instance.CloseUI(EUIType.EUIWithdrawConfirm);

                UIManager.Instance.OpenAsync<UIWithdrawProgress>(EUIType.EUIWithdrawProgress, UIOpenType.None, null, UIProgressTarget);
            });
        }

        protected override void OnDisable() { }
        protected override void OnDispose() 
        {
            PayChannelTable = null;
        }
    }
}