using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    /// <summary>
    /// 转盘配置数据类
    /// </summary>
    public class ConfLuckySpin
    {
        public int Sn { get; set; }
        public int Type { get; set; }
        public float Count { get; set; }
        public double Probability { get; set; }
    }

    public class LuckySpinOpenData
    {
        public bool ForceMoneyReward { get; set; }
        public float ForcedMoneyAmount { get; set; }
        public LuckySpinMoneyRewardTarget MoneyRewardTarget { get; set; } = LuckySpinMoneyRewardTarget.CurrentCoin;
        public bool IsMandatory { get; set; }
        public bool AutoStartSpin { get; set; }
        public int FixedBonusWheelAngleIndex { get; set; } = -1;
        public Action OnRewardComplete { get; set; }
    }

    public enum LuckySpinMoneyRewardTarget
    {
        LuckyWallet = 0,
        CurrentCoin = 1,
    }

    public class LuckySpinView : BaseView
    {
        private const double ProbabilityPercentTotal = 100d;
        private const double ProbabilityTolerance = 0.0001d;
        private const float DefaultLuckyWalletRewardAmount = 0.1f;
        private const float FirstLuckyWalletRewardDisplayDelay = 1f;

        protected RectTransform mPlane;
        protected Button mExitBtn;
        protected Button mLotteryBtn;
        protected RectTransform mTable;
        protected GameObject mSpin;
        protected Text mTTItem1name;
        protected Image mTTItem1Icon;
        protected Text mTTItem2name;
        protected Image mTTItem2Icon;
        protected Text mTTItem3name;
        protected Image mTTItem3Icon;
        protected Text mTTItem4name;
        protected Image mTTItem4Icon;
        protected Text mTTItem5name;
        protected Image mTTItem5Icon;
        protected Text mTTItem6name;
        protected Image mTTItem6Icon;


        private static Dictionary<int, ConfLuckySpin> s_SpinData;
        private static Dictionary<int, float> s_Angles;
        private static Dictionary<ELuckySpinRewardType, (string name, Sprite sprite)> s_SpriteCache;
        private static bool s_IsDataLoaded = false;


        private Dictionary<int, ConfLuckySpin> spinData;
        private Dictionary<int, float> angles;

        private bool mIsEventsBound = false;
        private bool mIsInitialized = false;
        private LuckySpinOpenData mOpenData;
        private bool mHasAutoStartedSpin;

        public void Awake()
        {
            InitUIReferences();
            SetSpinVisible(false);
        }

        protected override void HandleViewArgs(object[] args)
        {
            mOpenData = null;
            mHasAutoStartedSpin = false;

            if (args == null)
                return;

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
                EnsureSpinDataLoaded();
                spinData = s_SpinData;
                angles = s_Angles;
                UpdateSpinUI();
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

        /// <summary>
        /// 初始化UI引用
        /// </summary>
        private void InitUIReferences()
        {
            mPlane = transform.Find("Plane").GetComponent<RectTransform>();
            mExitBtn = transform.Find("ExitBtn").GetComponent<Button>();
            mLotteryBtn = transform.Find("Plane/LotteryBtn").GetComponent<Button>();
            mTable = transform.Find("Plane/Turntable/Table").GetComponent<RectTransform>();
            mSpin = transform.Find("Plane/spin")?.gameObject;


            mTTItem1name = transform.Find("Plane/Turntable/Table/TTItem1/TTItem1name")?.GetComponent<Text>();
            mTTItem1Icon = transform.Find("Plane/Turntable/Table/TTItem1/TTItem1Icon")?.GetComponent<Image>();
            mTTItem2name = transform.Find("Plane/Turntable/Table/TTItem2/TTItem2name")?.GetComponent<Text>();
            mTTItem2Icon = transform.Find("Plane/Turntable/Table/TTItem2/TTItem2Icon")?.GetComponent<Image>();
            mTTItem3name = transform.Find("Plane/Turntable/Table/TTItem3/TTItem3name")?.GetComponent<Text>();
            mTTItem3Icon = transform.Find("Plane/Turntable/Table/TTItem3/TTItem3Icon")?.GetComponent<Image>();
            mTTItem4name = transform.Find("Plane/Turntable/Table/TTItem4/TTItem4name")?.GetComponent<Text>();
            mTTItem4Icon = transform.Find("Plane/Turntable/Table/TTItem4/TTItem4Icon")?.GetComponent<Image>();
            mTTItem5name = transform.Find("Plane/Turntable/Table/TTItem5/TTItem5name")?.GetComponent<Text>();
            mTTItem5Icon = transform.Find("Plane/Turntable/Table/TTItem5/TTItem5Icon")?.GetComponent<Image>();
            mTTItem6name = transform.Find("Plane/Turntable/Table/TTItem6/TTItem6name")?.GetComponent<Text>();
            mTTItem6Icon = transform.Find("Plane/Turntable/Table/TTItem6/TTItem6Icon")?.GetComponent<Image>();
        }

        private void EnsureSpinDataLoaded()
        {
            if (s_IsDataLoaded && s_SpinData != null && s_SpinData.Count > 0)
            {
                return;
            }


            LoadSpinDataFromCSVStatic();

            InitSpinDataStatic();
            PreloadSprites();
            s_IsDataLoaded = true;
            Debug.Log($"转盘数据初始化完成，共 {s_SpinData?.Count ?? 0} 项");
        }

        /// <summary>
        /// 从CSV文件加载转盘数据
        /// </summary>
        private void LoadSpinDataFromCSVStatic()
        {
            if (UserDataManager.Instance == null)
            {
                Debug.LogError("UserDataManager is null, cannot load LuckySpin data.");
                return;
            }

            s_SpinData = UserDataManager.Instance.GetLuckySpinData();
            Debug.Log($"成功加载 {s_SpinData.Count} 条转盘配置数据");
        }

        /// <summary>
        /// 初始化转盘数据
        /// </summary>
        private static void InitSpinDataStatic()
        {
            if (s_SpinData == null || s_SpinData.Count == 0)
            {
                return;
            }

            s_Angles = new Dictionary<int, float>();

            int i = 0;
            foreach (var kvp in s_SpinData.OrderBy(k => k.Key))
            {
                int sn = kvp.Key;
                if (i < GameDefines.LS_Angles.Count)
                {
                    s_Angles.Add(sn, GameDefines.LS_Angles[i]);
                }
                i++;
            }
        }

        /// <summary>
        /// 预加载所有转盘相关的Sprite
        /// </summary>
        private void PreloadSprites()
        {
            s_SpriteCache = new Dictionary<ELuckySpinRewardType, (string, Sprite)>();

            foreach (ELuckySpinRewardType type in Enum.GetValues(typeof(ELuckySpinRewardType)))
            {
                string name = GetLocalizedTextStatic(GetLocalizationKey(type));
                Sprite sprite = null;

                switch (type)
                {
                    case ELuckySpinRewardType.Money:
                        sprite = GameDefines.ifIAA ? Resources.Load<Sprite>(GameDefines.LSIAAMoneyIconPath) : FacadePayTypeExtend.GetSingleCoin();
                        break;
                    case ELuckySpinRewardType.Gem:
                        sprite = FacadePayTypeExtend.GetSingleGem();
                        break;
                    case ELuckySpinRewardType.AddBottle:
                        sprite = Resources.Load<Sprite>(GameDefines.ERAddSpaceIconPath);
                        break;
                    case ELuckySpinRewardType.Clear:
                        sprite = Resources.Load<Sprite>(GameDefines.ERClearIconPath);
                        break;
                    case ELuckySpinRewardType.Prompt:
                        sprite = Resources.Load<Sprite>(GameDefines.ERAddPromptIconPath);
                        break;

                    case ELuckySpinRewardType.Guid:
                        sprite = Resources.Load<Sprite>(GameDefines.ERAddGuidIconPath);
                        break;
                    case ELuckySpinRewardType.Undo:
                        sprite = Resources.Load<Sprite>(GameDefines.ERHammerIconPath);
                        break;
                    case ELuckySpinRewardType.LuckyWalletMoney:
                        sprite = Resources.Load<Sprite>(GameDefines.LSLuckyWalletIconPath);
                        break;
                }

                s_SpriteCache[type] = (name, sprite);
            }
        }



        /// <summary>
        /// 更新UI显示
        /// </summary>
        private void UpdateSpinUI()
        {
            if (s_SpinData == null) return;

            int i = 0;
            foreach (var kvp in s_SpinData.OrderBy(k => k.Key))
            {
                var conf = kvp.Value;
                if (i < GameDefines.LS_Angles.Count)
                {
                    var (name, sprite) = GetItemInfoFromCache((ELuckySpinRewardType)conf.Type, GetResolvedRewardCount(conf));
                    SetItemShow(i, name, sprite);
                }
                i++;
            }
        }

        /// <summary>
        /// 从缓存获取名称和图片
        /// </summary>
        private (string, Sprite) GetItemInfoFromCache(ELuckySpinRewardType type, float count)
        {
            if (s_SpriteCache != null && s_SpriteCache.ContainsKey(type))
            {
                var info = s_SpriteCache[type];
                string displayName = $"{info.name}";// x{count}";
                return (displayName, info.sprite);
            }

            return GetItemInfo(type);
        }

        /// <summary>
        /// 获取名称和图片
        /// </summary>
        private (string, Sprite) GetItemInfo(ELuckySpinRewardType type)
        {
            string name = GetLocalizedText(GetLocalizationKey(type));
            Sprite sprite = null;

            switch (type)
            {
                case ELuckySpinRewardType.Money:
                    sprite = GameDefines.ifIAA ? Resources.Load<Sprite>(GameDefines.LSIAAMoneyIconPath) : GetSingleCoinSprite();
                    break;
                case ELuckySpinRewardType.Gem:
                    sprite = GameDefines.ifIAA ? GetSingleGemSprite() : GetSingleGemSprite();
                    break;
                case ELuckySpinRewardType.AddBottle:
                    sprite = Resources.Load<Sprite>(GameDefines.ERAddSpaceIconPath);
                    break;
                case ELuckySpinRewardType.Clear:
                    sprite = Resources.Load<Sprite>(GameDefines.ERClearIconPath);
                    break;
                case ELuckySpinRewardType.Undo:
                    sprite = Resources.Load<Sprite>(GameDefines.ERHammerIconPath);
                    break;
                case ELuckySpinRewardType.LuckyWalletMoney:
                    sprite = Resources.Load<Sprite>(GameDefines.LSLuckyWalletIconPath);
                    break;
            }

            return (name, sprite);
        }

        /// <summary>
        /// 获取本地化键值
        /// </summary>
        private static string GetLocalizationKey(ELuckySpinRewardType type)
        {
            switch (type)
            {
                case ELuckySpinRewardType.Money: return "1006";
                case ELuckySpinRewardType.Gem: return "1007";
                case ELuckySpinRewardType.AddBottle: return "1008";
                case ELuckySpinRewardType.Clear: return "1009";
                case ELuckySpinRewardType.Undo: return "1010";
                case ELuckySpinRewardType.LuckyWalletMoney: return "3013";
                default: return "1006";
            }
        }

        /// <summary>
        /// 本地化文本获取
        /// </summary>
        private string GetLocalizedTextStatic(string key)
        {
            if (LocalizationManager.Instance != null)
            {
                return LocalizationManager.Instance.GetText(key);
            }
            return key;
        }

        /// <summary>
        /// 设置某项中的图片和名称
        /// </summary>
        private void SetItemShow(int pos, string name, Sprite icon)
        {
            switch (pos)
            {
                case 0:
                    if (mTTItem1name != null) mTTItem1name.text = name;
                    if (mTTItem1Icon != null) mTTItem1Icon.sprite = icon;
                    break;
                case 1:
                    if (mTTItem2name != null) mTTItem2name.text = name;
                    if (mTTItem2Icon != null) mTTItem2Icon.sprite = icon;
                    break;
                case 2:
                    if (mTTItem3name != null) mTTItem3name.text = name;
                    if (mTTItem3Icon != null) mTTItem3Icon.sprite = icon;
                    break;
                case 3:
                    if (mTTItem4name != null) mTTItem4name.text = name;
                    if (mTTItem4Icon != null) mTTItem4Icon.sprite = icon;
                    break;
                case 4:
                    if (mTTItem5name != null) mTTItem5name.text = name;
                    if (mTTItem5Icon != null) mTTItem5Icon.sprite = icon;
                    break;
                case 5:
                    if (mTTItem6name != null) mTTItem6name.text = name;
                    if (mTTItem6Icon != null) mTTItem6Icon.sprite = icon;
                    break;
            }
        }

        /// <summary>
        /// 绑定按钮事件
        /// </summary>
        private void BindButtonEvents()
        {
            if (mIsEventsBound)
            {
                RemoveButtonEvents();
            }

            if (mExitBtn != null)
                mExitBtn.onClick.AddListener(OnExitBtnClickHandle);

            if (mLotteryBtn != null)
                mLotteryBtn.onClick.AddListener(OnLotteryBtnClickHandle);

            mIsEventsBound = true;
        }

        /// <summary>
        /// 移除按钮事件
        /// </summary>
        private void RemoveButtonEvents()
        {
            if (mExitBtn != null)
                mExitBtn.onClick.RemoveAllListeners();

            if (mLotteryBtn != null)
                mLotteryBtn.onClick.RemoveAllListeners();
            mIsEventsBound = false;
        }

        /// <summary>
        /// 界面启用时的处理
        /// </summary>
        public void OnEnableView()
        {
            if (mExitBtn != null)
                mExitBtn.gameObject.SetActive(!(mOpenData?.IsMandatory ?? false));

            if (mLotteryBtn != null)
                mLotteryBtn.gameObject.SetActive(true);

            SetSpinVisible(false);

            if (mTable != null)
                mTable.rotation = Quaternion.identity;
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
            StartManagedCoroutine(AutoStartSpinCoroutine());
        }

        private IEnumerator AutoStartSpinCoroutine()
        {
            yield return null;

            if (mLotteryBtn != null && mLotteryBtn.gameObject.activeInHierarchy)
                OnLotteryBtnClickHandle();
        }

        /// <summary>
        /// 退出按钮点击处理
        /// </summary>
        private void OnExitBtnClickHandle()
        {
            SetSpinVisible(false);
            UIManager.instance.CloseView(EUIType.LuckySpinView);
        }

        /// <summary>
        /// 抽奖按钮点击处理
        /// </summary>
        private void OnLotteryBtnClickHandle()
        {
            if (mExitBtn != null)
                mExitBtn.gameObject.SetActive(false);

            if (mLotteryBtn != null)
                mLotteryBtn.gameObject.SetActive(false);

            SetSpinVisible(true);
            RotateTable();
        }

        /// <summary>
        /// 开转！
        /// </summary>
        private void RotateTable()
        {
            if (spinData == null || spinData.Count == 0)
            {
                Debug.LogError("转盘数据未初始化，无法开始抽奖");
                SetSpinVisible(false);
                OnEnableView();
                return;
            }

            bool useForcedMoneyReward = TryGetForcedMoneyReward(out int target, out float forcedMoneyReward);
            if (!useForcedMoneyReward)
                target = GetRewardIndexByProbability();

            if (!spinData.ContainsKey(target))
            {
                Debug.LogError($"目标奖励不存在: {target}，使用默认值0");
                target = 0;
            }

            ELuckySpinRewardType type = (ELuckySpinRewardType)spinData[target].Type;
            float count = useForcedMoneyReward ? forcedMoneyReward : GetResolvedRewardCount(spinData[target]);

            if (!angles.ContainsKey(target))
            {
                Debug.LogError($"目标角度不存在: {target}，使用默认角度0");
                angles[target] = 0;
            }

            float finalAngle = -(GameDefines.LS_rotateCount * 360 - angles[target]);

            mTable.transform.DOLocalRotate(Vector3.forward * finalAngle, GameDefines.LS_rotateTime, RotateMode.FastBeyond360)
                .SetEase(Ease.OutQuint)
                .OnComplete(() =>
                {
                    SetSpinVisible(false);

                    StartManagedCoroutine(DelayedAction(0.5f, () =>
                    {
                        UIManager.instance.CloseView(EUIType.LuckySpinView);
                    }));
                    PlayRewardEffect(type, count, mOpenData?.OnRewardComplete, useForcedMoneyReward);
                });

            StartManagedCoroutine(PlaySpinSound());
        }

        private bool TryGetForcedMoneyReward(out int target, out float rewardAmount)
        {
            target = -1;
            rewardAmount = 0f;

            if (mOpenData == null || !mOpenData.ForceMoneyReward || mOpenData.ForcedMoneyAmount <= 0f)
                return false;

            var moneyRewardIndexes = spinData
                .Where(kvp => (ELuckySpinRewardType)kvp.Value.Type == ELuckySpinRewardType.Money)
                .Select(kvp => kvp.Key)
                .ToList();

            if (moneyRewardIndexes.Count <= 0)
                return false;

            target = moneyRewardIndexes[UnityEngine.Random.Range(0, moneyRewardIndexes.Count)];
            rewardAmount = mOpenData.ForcedMoneyAmount;
            return true;
        }

        /// <summary>
        /// 根据概率权重获取奖励索引
        /// </summary>
        private int GetRewardIndexByProbability()
        {
            if (TryGetGuaranteedLuckyWalletReward(out int guaranteedTarget))
                return guaranteedTarget;

            if (spinData == null || spinData.Count == 0)
            {
                Debug.LogError("转盘奖励数据为空");
                return 0;
            }

            double totalWeight = spinData
                .OrderBy(kvp => kvp.Key)
                .Sum(kvp => Math.Max(kvp.Value.Probability, 0d));
            if (totalWeight <= 0d)
            {
                Debug.LogError("总权重为0，使用随机索引");
                int fallbackIndex = UnityEngine.Random.Range(0, spinData.Count);
                return spinData.OrderBy(kvp => kvp.Key).ElementAt(fallbackIndex).Key;
            }

            bool useDirectPercentMode = Math.Abs(totalWeight - ProbabilityPercentTotal) <= ProbabilityTolerance;
            if (!useDirectPercentMode)
            {
                Debug.LogWarning($"LuckySpin probability total is {totalWeight:F4}, expected 100. Falling back to weighted mode.");
            }

            double randomValue = UnityEngine.Random.value * (useDirectPercentMode ? ProbabilityPercentTotal : totalWeight);
            double currentSum = 0d;

            foreach (var kvp in spinData.OrderBy(k => k.Key))
            {
                currentSum += Math.Max(kvp.Value.Probability, 0d);
                if (randomValue < currentSum)
                {
                    return kvp.Key;
                }
            }

            return spinData.OrderBy(kvp => kvp.Key).FirstOrDefault().Key;
        }

        private bool TryGetGuaranteedLuckyWalletReward(out int target)
        {
            target = -1;
            if (spinData == null || spinData.Count == 0)
                return false;

            if (HasClaimedLuckyWalletReward() || !CanUseLuckyWalletReward())
                return false;

            foreach (var kvp in spinData.OrderBy(k => k.Key))
            {
                if ((ELuckySpinRewardType)kvp.Value.Type != ELuckySpinRewardType.LuckyWalletMoney)
                    continue;

                target = kvp.Key;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 播放音效
        /// </summary>
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

        /// <summary>
        /// 延迟执行
        /// </summary>
        private IEnumerator DelayedAction(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        private Coroutine StartManagedCoroutine(IEnumerator routine)
        {
            if (routine == null)
                return null;

            if (GameManagerWZ.instance != null)
                return GameManagerWZ.instance.StartCoroutine(routine);

            return StartCoroutine(routine);
        }

        private void SetSpinVisible(bool isVisible)
        {
            if (mSpin != null)
                mSpin.SetActive(isVisible);
        }

        /// <summary>
        /// 播放奖励特效
        /// </summary>
        private void PlayRewardEffect(ELuckySpinRewardType type, float count, Action finishAction = null, bool useExactCount = false)
        {
            ERewardType rewardType = ERewardType.Money;

            switch (type)
            {
                case ELuckySpinRewardType.Money:
                    rewardType = ERewardType.Money;
                    if (!useExactCount)
                    {
                        count = GameManagerWZ.instance.GetCoinRewardAmount(3);
                    }
                    else if (GetMoneyRewardTarget() == LuckySpinMoneyRewardTarget.CurrentCoin
                             && GameManagerWZ.instance != null
                             && !GameManagerWZ.instance.CanGrantCoinRewardForCurrentLevel())
                    {
                        count = 0f;
                    }
                    break;
                case ELuckySpinRewardType.Gem:
                    rewardType = ERewardType.Gem;
                    count = GameManagerWZ.instance.getResourceNum(2, 0, 2);
                    break;
                case ELuckySpinRewardType.AddBottle:
                    rewardType = ERewardType.AddBottle;
                    break;
                case ELuckySpinRewardType.Clear:
                    rewardType = ERewardType.Clear;
                    break;
                case ELuckySpinRewardType.Undo:
                    rewardType = ERewardType.Undo;
                    break;

                case ELuckySpinRewardType.Prompt:
                    rewardType = ERewardType.Prompt;
                    break;

                case ELuckySpinRewardType.Guid:
                    rewardType = ERewardType.Guid;
                    break;

                case ELuckySpinRewardType.LuckyWalletMoney:
                    rewardType = ERewardType.LuckyWalletMoney;
                    break;
            }

            if (count <= 0f)
            {
                finishAction?.Invoke();
                return;
            }

            if (type == ELuckySpinRewardType.LuckyWalletMoney)
            {
                PlayLuckyWalletRewardEffect(count, () => ApplyServerLuckyWalletReward(count, finishAction), FirstLuckyWalletRewardDisplayDelay);
                return;
            }

            if (type == ELuckySpinRewardType.Money && GetMoneyRewardTarget() == LuckySpinMoneyRewardTarget.LuckyWallet)
            {
                PlayLuckyWalletRewardEffect(count, () => ApplyLuckyWalletMoneyReward(count, finishAction));
                return;
            }

            if (FacadeEffectExtend.PlayGetRewardEffectHandle == null)
            {
                ApplyRewardDirectly(rewardType, count);
                finishAction?.Invoke();
                return;
            }

            FacadeEffectExtend.PlayGetRewardEffectHandle(new ERewardItemStruct[]
            {
            new ERewardItemStruct()
            {
                Type = rewardType,
                Count = count,
            }
            }, finishAction);
        }

        private void PlayLuckyWalletRewardEffect(float count, Action applyRewardAction, float delayBeforeOpen = 0f)
        {
            if (count <= 0f)
            {
                applyRewardAction?.Invoke();
                return;
            }

            if (FacadeEffectExtend.PlayGetRewardEffectHandle == null)
            {
                applyRewardAction?.Invoke();
                return;
            }

            Action showRewardEffect = () => FacadeEffectExtend.PlayGetRewardEffectHandle(new ERewardItemStruct[]
            {
                new ERewardItemStruct()
                {
                    Type = ERewardType.LuckyWalletMoney,
                    Count = count,
                }
            }, applyRewardAction);

            if (delayBeforeOpen > 0f)
            {
                StartManagedCoroutine(DelayedAction(delayBeforeOpen, showRewardEffect));
                return;
            }

            showRewardEffect();
        }

        private void ApplyRewardDirectly(ERewardType rewardType, float count)
        {
            switch (rewardType)
            {
                case ERewardType.Money:
                    GameManagerWZ.instance.AddCoin(count);
                    break;
                case ERewardType.Gem:
                    GameManagerWZ.instance.AddGems(count);
                    break;
                case ERewardType.AddBottle:
                    GameManagerWZ.instance.RewardHintBottle(Mathf.RoundToInt(count));
                    break;
                case ERewardType.Clear:
                    GameManagerWZ.instance.RewardClear(Mathf.RoundToInt(count));
                    break;
                case ERewardType.Undo:
                    GameManagerWZ.instance.RewardUndo(Mathf.RoundToInt(count));
                    break;
                case ERewardType.LuckyWalletMoney:
                    MarkLuckyWalletRewardClaimed();
                    WithdrawApiService.AddWalletIncome(ResolveLuckyWalletRewardAmount(count), WithdrawConstants.WalletIncomeTypeNormal);
                    break;
            }
        }

        private LuckySpinMoneyRewardTarget GetMoneyRewardTarget()
        {
            return mOpenData?.MoneyRewardTarget ?? LuckySpinMoneyRewardTarget.CurrentCoin;
        }

        private void ApplyLuckyWalletMoneyReward(float fallbackAmount, Action finishAction)
        {
            float resolvedFallbackAmount = WithdrawApiService.GetCachedLuckySpinRewardAmount(fallbackAmount);
            if (GameManagerWZ.instance == null)
            {
                WithdrawApiService.AddWalletIncome(resolvedFallbackAmount);
                finishAction?.Invoke();
                return;
            }

            GameManagerWZ.instance.StartCoroutine(WithdrawApiService.QueryLuckySpinRewardAmount(resolvedFallbackAmount, amount =>
            {
                WithdrawApiService.AddWalletIncome(amount);
                finishAction?.Invoke();
            }, error =>
            {
                Debug.LogWarning($"LuckySpin lucky wallet reward query failed: {error}");
            }));
        }

        private void ApplyServerLuckyWalletReward(float fallbackAmount, Action finishAction)
        {
            MarkLuckyWalletRewardClaimed();
            WithdrawApiService.AddWalletIncome(ResolveLuckyWalletRewardAmount(fallbackAmount), WithdrawConstants.WalletIncomeTypeNormal);
            finishAction?.Invoke();
        }

        private float GetResolvedRewardCount(ConfLuckySpin conf)
        {
            if (conf == null)
                return 0f;

            if ((ELuckySpinRewardType)conf.Type == ELuckySpinRewardType.LuckyWalletMoney)
                return ResolveLuckyWalletRewardAmount(conf.Count);

            return conf.Count;
        }

        private float ResolveLuckyWalletRewardAmount(float fallbackAmount)
        {
            if (GameDefines.WheelReward > 0f)
                return GameDefines.WheelReward;

            if (fallbackAmount > 0f)
                return fallbackAmount;

            return DefaultLuckyWalletRewardAmount;
        }

        private bool CanUseLuckyWalletReward()
        {
            GameManagerWZ gm = GameManagerWZ.instance;
            if (gm != null)
                return gm.currentLv >= GameDefines.LuckyWalletUnlockLevel;

            return PlayerPrefs.GetInt(FacadePlayerPrefExtend.currentLevel, 1) >= GameDefines.LuckyWalletUnlockLevel;
        }

        private bool HasClaimedLuckyWalletReward()
        {
            return PlayerPrefs.GetInt(FacadePlayerPrefExtend.LuckySpinWalletRewardClaimed, 0) != 0;
        }

        private void MarkLuckyWalletRewardClaimed()
        {
            if (HasClaimedLuckyWalletReward())
                return;

            PlayerPrefs.SetInt(FacadePlayerPrefExtend.LuckySpinWalletRewardClaimed, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private void CleanupResources()
        {
            spinData = null;
            angles = null;
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

        #region 辅助方法

        private string GetLocalizedText(string key)
        {
            if (LocalizationManager.Instance != null)
            {
                return LocalizationManager.Instance.GetText(key);
            }
            return key;
        }

        private Sprite GetSingleCoinSprite()
        {
            return FacadePayTypeExtend.GetSingleCoin();
        }

        private Sprite GetSingleGemSprite()
        {
            return FacadePayTypeExtend.GetSingleGem();
        }

        private void PlaySoundEffect(AudioStCategory category)
        {
            FacadeAudioExtend.PlayEffectHandle?.Invoke(category);
        }

        #endregion


    }

}
