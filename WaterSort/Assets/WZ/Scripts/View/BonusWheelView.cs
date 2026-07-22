using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    public class BonusWheelView : BaseView
    {
        private const int BonusRewardCount = 8;

        protected Button mExitBtn;
        protected Button mLotteryBtn;
        protected RectTransform mTable;
        protected GameObject mSpin;

        private List<float> mRewardAngles;
        private bool mIsEventsBound;
        private bool mIsInitialized;
        private bool mHasAutoStartedSpin;
        private LuckySpinOpenData mOpenData;

        public void Awake()
        {
            InitUIReferences();
            SetSpinVisible(true);
        }

        protected override void HandleViewArgs(object[] args)
        {
            mOpenData = null;
            mHasAutoStartedSpin = false;

            if (args == null)
            {
                return;
            }

            foreach (object arg in args)
            {
                if (arg is LuckySpinOpenData openData)
                {
                    mOpenData = openData;
                    break;
                }
            }
        }

        public override void InitView()
        {
            if (!mIsInitialized)
            {
                InitializeBonusWheel();
                mIsInitialized = true;
            }

            OnEnableView();
            BindButtonEvents();
            StartAutoSpinIfNeeded();
        }

        private void OnDisable()
        {
            SetSpinVisible(false);
        }

        public override void Start()
        {
        }

        public override void Update()
        {
        }

        private void InitUIReferences()
        {
            mExitBtn = transform.Find("ExitBtn")?.GetComponent<Button>();
            mLotteryBtn = transform.Find("Plane/LotteryBtn")?.GetComponent<Button>();
            mTable = transform.Find("Plane/Turntable/Table")?.GetComponent<RectTransform>();
            mSpin = transform.Find("Plane/Turntable/spin")?.gameObject;
        }

        private void InitializeBonusWheel()
        {
            mRewardAngles = GameDefines.BonusWheel_Angles;
        }

        private int GetConfiguredRewardCount()
        {
            if (mRewardAngles == null)
            {
                return 0;
            }

            return Math.Min(mRewardAngles.Count, BonusRewardCount);
        }

        private int GetTargetIndex(int rewardCount)
        {
            return rewardCount > 0 ? 0 : -1;
        }

        private void BindButtonEvents()
        {
            if (mIsEventsBound)
            {
                RemoveButtonEvents();
            }

            if (mExitBtn != null)
            {
                mExitBtn.onClick.AddListener(OnExitBtnClickHandle);
            }

            if (mLotteryBtn != null)
            {
                mLotteryBtn.onClick.AddListener(OnLotteryBtnClickHandle);
            }

            mIsEventsBound = true;
        }

        private void RemoveButtonEvents()
        {
            if (mExitBtn != null)
            {
                mExitBtn.onClick.RemoveAllListeners();
            }

            if (mLotteryBtn != null)
            {
                mLotteryBtn.onClick.RemoveAllListeners();
            }

            mIsEventsBound = false;
        }

        public void OnEnableView()
        {
            if (mExitBtn != null)
            {
                mExitBtn.gameObject.SetActive(!(mOpenData?.IsMandatory ?? false));
            }

            if (mLotteryBtn != null)
            {
                mLotteryBtn.gameObject.SetActive(true);
            }

            SetSpinVisible(false);

            if (mTable != null)
            {
                mTable.rotation = Quaternion.identity;
            }
        }

        private void StartAutoSpinIfNeeded()
        {
            if (mHasAutoStartedSpin
                || !(mOpenData?.AutoStartSpin ?? false)
                || mLotteryBtn == null
                || !mLotteryBtn.gameObject.activeInHierarchy)
            {
                return;
            }

            mHasAutoStartedSpin = true;
            StartCoroutine(AutoStartSpinCoroutine());
        }

        private IEnumerator AutoStartSpinCoroutine()
        {
            yield return null;

            if (mLotteryBtn != null && mLotteryBtn.gameObject.activeInHierarchy)
            {
                OnLotteryBtnClickHandle();
            }
        }

        private void OnExitBtnClickHandle()
        {
            SetSpinVisible(false);
            UIManager.instance.CloseView(EUIType.BonusWheelView);
        }

        private void OnLotteryBtnClickHandle()
        {
            if (mExitBtn != null)
            {
                mExitBtn.gameObject.SetActive(false);
            }

            if (mLotteryBtn != null)
            {
                mLotteryBtn.gameObject.SetActive(false);
            }

            SetSpinVisible(true);
            RotateTable();
        }

        private void RotateTable()
        {
            int rewardCount = GetConfiguredRewardCount();
            if (rewardCount <= 0 || mTable == null)
            {
                Debug.LogError("BonusWheel reward data is empty.");
                SetSpinVisible(false);
                OnEnableView();
                return;
            }

            int targetIndex = GetTargetIndex(rewardCount);
            float rewardAmount = GetBaseRewardAmount();
            float finalAngle = -(GameDefines.LS_rotateCount * 360f - mRewardAngles[targetIndex]);

            mTable.transform.DOLocalRotate(Vector3.forward * finalAngle, GameDefines.LS_rotateTime, RotateMode.FastBeyond360)
                .SetEase(Ease.OutQuint)
                .OnComplete(() =>
                {
                    SetSpinVisible(false);

                    StartCoroutine(DelayedAction(0.5f, () =>
                    {
                        UIManager.instance.CloseView(EUIType.BonusWheelView);
                    }));
                    PlayMoneyRewardEffect(rewardAmount, mOpenData?.OnRewardComplete);
                });

            StartCoroutine(PlaySpinSound());
        }

        private float GetBaseRewardAmount()
        {
            if (mOpenData != null && mOpenData.ForcedMoneyAmount > 0f)
            {
                return GameManagerWZ.instance != null && GameManagerWZ.instance.CanGrantCoinRewardForCurrentLevel()
                    ? mOpenData.ForcedMoneyAmount
                    : 0f;
            }

            if (GameManagerWZ.instance == null)
            {
                return 0f;
            }

            return GameManagerWZ.instance.GetCoinRewardAmount(3);
        }

        private IEnumerator PlaySpinSound()
        {
            for (int i = 0; i < 7; i++)
            {
                yield return new WaitForSeconds(GameDefines.LS_rotateTime / 20f);
                PlaySoundEffect(AudioStCategory.ELuckySpin);
            }

            yield return new WaitForSeconds(GameDefines.LS_rotateTime - 1f);
            PlaySoundEffect(AudioStCategory.ELuckySpin2);
        }

        private IEnumerator DelayedAction(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        private void SetSpinVisible(bool isVisible)
        {
            if (mSpin != null)
            {
                mSpin.SetActive(isVisible);
            }
        }

        private void PlayMoneyRewardEffect(float count, Action finishAction = null)
        {
            if (count <= 0f)
            {
                finishAction?.Invoke();
                return;
            }

            if (GetMoneyRewardTarget() == LuckySpinMoneyRewardTarget.LuckyWallet)
            {
                WithdrawApiService.AddWalletIncome(count);
                finishAction?.Invoke();
                return;
            }

            if (FacadeEffectExtend.PlayGetRewardEffectHandle == null)
            {
                GameManagerWZ.instance.AddCoin(count);
                finishAction?.Invoke();
                return;
            }

            FacadeEffectExtend.PlayGetRewardEffectHandle(new ERewardItemStruct[]
            {
                new ERewardItemStruct()
                {
                    Type = ERewardType.Money,
                    Count = count,
                }
            }, finishAction);
        }

        private LuckySpinMoneyRewardTarget GetMoneyRewardTarget()
        {
            return mOpenData?.MoneyRewardTarget ?? LuckySpinMoneyRewardTarget.CurrentCoin;
        }

        private void CleanupResources()
        {
            mRewardAngles = null;
            mOpenData = null;
        }

        private void OnDestroy()
        {
            if (mIsEventsBound)
            {
                RemoveButtonEvents();
            }

            CleanupResources();
            mIsInitialized = false;
        }

        private void PlaySoundEffect(AudioStCategory category)
        {
            FacadeAudioExtend.PlayEffectHandle?.Invoke(category);
        }
    }
}
