using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIGamePlay : BaseUI
    {
        private Sequence scrollingTip;

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

            FacadeGamePlay.SetLevelShow += SetLevelShow;

            FacadeGamePlay.ScrollingTipAnim += ScrollingTipAnim;
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

            FacadeGamePlay.SetLevelShow -= SetLevelShow;

            FacadeGamePlay.ScrollingTipAnim -= ScrollingTipAnim;
        }

        #endregion

        private void InitShow()
        {
            mMoneyIcon.gameObject.SetActive(!GameDefines.ifIAA);
            mIAAMoneyIcon.gameObject.gameObject.SetActive(GameDefines.ifIAA);
            mCMBtn.gameObject.SetActive(!GameDefines.ifIAA);
            mCMDialog.gameObject.SetActive(!GameDefines.ifIAA);
            mWLProgress.gameObject.SetActive(false);
            mWPrompt.gameObject.SetActive(false);
            if (GameDefines.ifIAA) mReStartBtn.transform.position = mReStartBtnIAAPos.position;

            string levelText = string.Format(FacadeLanguage.GetText?.Invoke("10016"), FacadePlayer.GetLevel());
            mLTCurLevelText.text = levelText;
            mCurLevelText.text = levelText;

            SetCurMoneyShow();
            SetProp1CountShow();
            SetProp2CountShow();
            SetProp3CountShow();
        }

        protected override void OnEnable()
        {
            SetShuffleTipShow(false);

            FacadeGamePlay.StartLevel();
        }

        #region 设置部分UI的显示

        /// <summary>
        /// 设置当前金钱的显示
        /// </summary>
        private void SetCurMoneyShow()
        {
            double money = FacadePlayer.GetMoney?.Invoke() ?? 0;
            mCurMoneyText.text = FacadePayType.RegionalChange?.Invoke(money);
            //SetWPMsg();
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
        /// 设置提现目标显示
        /// </summary>
        private void SetWithdrawalTip(string value)
        {
            mCMDialogText.text = value;
        }

        /// <summary>
        /// 设置刷新功能的提示的显影
        /// </summary>
        /// <param name="b">显影</param>
        private void SetShuffleTipShow(bool b)
        {
            mShuffleTip.gameObject.SetActive(b);
        }

        /// <summary>
        /// 设置关卡显示
        /// </summary>
        private void SetLevelShow()
        {
            mCurLevelText.text = string.Format(FacadeLanguage.GetText("10016"), FacadePlayer.GetLevel());
            return;

            mCurLevel.gameObject.SetActive(true);

            int curLevel = FacadePlayer.GetLevel();

            string levelText;
            if (curLevel < GameDefines.miniLevel_Start)
                levelText = $"{curLevel}";
            else if (curLevel >= GameDefines.miniLevel_Start && curLevel <= GameDefines.miniLevel_Start)
                levelText = $"{GameDefines.miniLevel_Start - 1}-{curLevel - GameDefines.miniLevel_Start + 2}";
            else
                levelText = $"{curLevel - GameDefines.miniLevel_Start}";
            
            mLTCurLevelText.text = string.Format(FacadeLanguage.GetText("10016"), levelText);
            bool after8_10 = curLevel > GameDefines.miniLevel_End;

            mWLProgress.gameObject.SetActive(!after8_10 && !GameDefines.ifIAA);
            if (!after8_10)
            {
                UIGP_LP_Item uIGP_LP_Item;
                int startLevel;
                if (curLevel <= 4)
                {
                    startLevel = 1;
                }
                else
                {
                    startLevel = 4;
                }

                for (int i = startLevel; i <= startLevel + 3; i++)
                {
                    uIGP_LP_Item = mWLProgress.Items[i - startLevel];
                    uIGP_LP_Item.CurSign.SetActive(curLevel == i);
                    uIGP_LP_Item.FinishSign.SetActive(curLevel > i);
                    uIGP_LP_Item.levelText.text = i.ToString();
                    if (!GameDefines.ifIAA)
                        uIGP_LP_Item.WTip.gameObject.SetActive((i == 1 || i == 2) && curLevel <= i);
                    if (uIGP_LP_Item.Arrow != null)
                        uIGP_LP_Item.Arrow.gameObject.SetActive(curLevel >= i);
                }

                uIGP_LP_Item = mWLProgress.Items[mWLProgress.Items.Count - 1];
                uIGP_LP_Item.levelText.text = (GameDefines.miniLevel_Start - 1).ToString();
                uIGP_LP_Item.CurSign.SetActive(curLevel >= (GameDefines.miniLevel_Start - 1));
                uIGP_LP_Item.Arrow.gameObject.SetActive(curLevel >= (GameDefines.miniLevel_Start - 1));
                bool showMiniText = curLevel >= GameDefines.miniLevel_Start;
                mWLProgress.miniLevels.SetActive(showMiniText);
                if (showMiniText)
                    mWLProgress.miniLevelText.text = $"{curLevel - GameDefines.miniLevel_Start + 2}/{GameDefines.miniLevel_End - GameDefines.miniLevel_Start + 2}";


            }


            if (GameDefines.UsePayoutV2)
            {
                mWPrompt.gameObject.SetActive(false);
                mCMDialog.gameObject.SetActive(false);
            }
            else if (!GameDefines.ifIAA)
            {
                mCMDialog.gameObject.SetActive(!after8_10);
                if (!after8_10)
                {
                    int targetlevel = 0;
                    if (curLevel <= 2)
                        targetlevel = curLevel;
                    else
                        targetlevel = GameDefines.miniLevel_Start - 1;

                    mCMDialogText.text = string.Format(FacadeLanguage.GetText("10005"), targetlevel);
                }

                mWPrompt.gameObject.SetActive(after8_10);
                if (after8_10)
                {
                    SetWPMsg();
                }

                mCurLevelText.text = string.Format(FacadeLanguage.GetText("10016"), curLevel - GameDefines.miniLevel_Start);
            }
            else
            {
                mWPrompt.gameObject.SetActive(false);
                mCurLevelText.text = string.Format(FacadeLanguage.GetText("10016"), curLevel);
            }

        }

        /// <summary>
        /// 设置目标金额/签到提示文本
        /// </summary>
        private void SetWPMsg()
        {
            mWPText.text = "";
            mWPSText.text = $"{0}/{1}";
            mWPSlider.value = 0;
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
            UIManager.Instance.OpenAsync<UIWithdrawGoal2>(EUIType.EUIWithdrawGoal2);
        }

        private void OnTipExitBtnClickHandle()
        {
            FacadeGamePlay.EndPorp1();
            SetShuffleTipShow(false);
        }

        private void OnTRewardBtnClickHandle()
        {
            FacadeWithdraw.OpenTotalRewardUI();
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

        /// <summary>
        /// 滚动字幕显示
        /// </summary>
        private void ScrollingTipAnim()
        {
            if (FacadePlayer.GetLevel() <= 3 || GameDefines.ifIAA)
            {
                return;
            }

            if (scrollingTip == null)
            {
                scrollingTip = DOTween.Sequence();
                scrollingTip.AppendCallback(SetRandomScrollingTipShow);
                scrollingTip.AppendInterval(5);
                scrollingTip.Append(mMarque1.transform.DOMove(mM1EndPos.transform.position, GameDefines.ScollingTipAnimTime).SetEase(Ease.Linear));
                scrollingTip.Join(mMarque2.transform.DOMove(mM2EndPos.transform.position, GameDefines.ScollingTipAnimTime).SetEase(Ease.Linear));
                scrollingTip.AppendInterval(GameDefines.ScollingTipAnimInterval - 5);
                scrollingTip.SetLoops(-1);
                scrollingTip.SetAutoKill(false);
                scrollingTip.Play();
            }
            else
            {
                scrollingTip.Restart();
            }
        }

        /// <summary>
        /// 重置滚动字幕信息
        /// </summary>
        private void SetRandomScrollingTipShow()
        {
            mMarque1.transform.position = mM1StartPos.transform.position;
            mMarque2.transform.position = mM2StartPos.transform.position;

            string name1 = FacadePlayer.GetRandomName(false);
            string name2 = FacadePlayer.GetRandomName(false);
            string money1 = FacadePayType.RegionalChange(UnityEngine.Random.Range(GameDefines.ScollingTipAnimMoney.x, GameDefines.ScollingTipAnimMoney.y));
            string money2 = FacadePayType.RegionalChange(UnityEngine.Random.Range(GameDefines.ScollingTipAnimMoney.x, GameDefines.ScollingTipAnimMoney.y));
            List<PayNode> payNodes = FacadePayType.GetPayItems();
            Sprite icon1 = payNodes[UnityEngine.Random.Range(0, payNodes.Count)].icon;
            Sprite icon2 = payNodes[UnityEngine.Random.Range(0, payNodes.Count)].icon;

            mMar1Text.text = string.Format(FacadeLanguage.GetText("10072"), name1, money1);
            mMar1Icon.sprite = icon1;
            mMar2Text.text = string.Format(FacadeLanguage.GetText("10072"), name2, money2);
            mMar2Icon.sprite = icon2;
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