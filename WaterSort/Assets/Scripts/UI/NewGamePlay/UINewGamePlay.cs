
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UINewGamePlay : BaseUI
    {
        protected override void OnAwake()
        {
            
        }
        protected override void OnEnable()
        {
            int curLevel = FacadePlayer.GetLevel();
            bool b1 = curLevel == GameDefines.NGPLevel1;
            bool b2 = curLevel == GameDefines.NGPLevel2;
            bool b3 = curLevel == GameDefines.NGPLevel3;

            mGPIcon1.gameObject.SetActive(b1);
            mGPIcon2.gameObject.SetActive(b2);
            mGPIcon3.gameObject.SetActive(b3);
            mContentText1.gameObject.SetActive(b1);
            mContentText2.gameObject.SetActive(b2);
            mContentText3.gameObject.SetActive(b3);

            ShowAnim(mPlane);
        }
        	    private void OnContinueBtnClickHandle()
        {
            HideAnim(mPlane, () => 
            {
                //UIManager.Instance.OpenAsync<UIWithdrawTarget>(EUIType.EUIWithdrawTarget, UIOpenType.None, null, FacadeGamePlay.CreateLevel);
                UIManager.Instance.CloseUI(EUIType.EUINewGamePlay);
            });
        }

        protected override void OnDisable() { }
        protected override void OnDispose() { }
    }
}