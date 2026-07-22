using DG.Tweening;
using Spine;
using Spine.Unity;
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace WZSDK
{
    public class UIEffectView : BaseView
    {

        protected RectTransform mLTEMask;
        protected RectTransform mLTEPlane;
        protected RectTransform mLTEStartPoint;
        protected Button mDisappearEarlyBtn;
        protected RectTransform mLevelTargetEffect;
        protected Text mNextTargetText;
        protected RectTransform mMiniDialog;
        protected Text mMiniTargetText;
        protected Text mDialogText;
        protected RectTransform mGREMask;
        protected RectTransform mGetRewardEffect;
        protected RectTransform mRewardGroup;
        protected EffectRewardItem mEffectRewardItem;
        protected RectTransform mCongratulationEffect;
        protected Text mCEContent;
        protected ParticleSystem mFireworkEffectL;
        protected ParticleSystem mFireworkEffectR;
        protected RectTransform mFlyEffectParent;
        protected RectTransform mFlyProp;
        protected RectTransform mFlyMoney;
        protected RectTransform mFlyIAAMoney;
        protected RectTransform mFlyMoneyTip;
        protected RectTransform mFlyIAAMoneyTip;
        protected SkeletonGraphic mShaobaEffect;
        protected SkeletonGraphic mHammerEffect;
        protected RectTransform mClickEffectParent;
        protected SkeletonGraphic mClickEffect;
        protected SkeletonGraphic mDifficultyUpEffect;
        protected Text mDUEText;
        protected Text mNextTargetSubtitle;

        protected RectTransform mFlyGem;
        protected RectTransform mFlyGemTip;

        private Coroutine getRewardEffectCoroutine;
        private Queue<EffectRewardItem> ERItemPool;
        private List<EffectRewardItem> curERItem;
        private Transform LTEGoldTrans;
        private float CongratulationStartY;
        private float CongratulationEndY;

        private char[] nameChars;

        private Stack<GameObject> flyMoneyPool;
        private Stack<GameObject> flyMoneyTipPool;
        private Stack<GameObject> flyGemPool;
        private Stack<GameObject> flyGemTipPool;
        private Stack<GameObject> flyPropPool;
        private Stack<SkeletonGraphic> clickPool;
        private Dictionary<ERewardType, Vector3> flyObjGoldDic;

        private DG.Tweening.Sequence targetEffectSequence;
        private Action targetEffectFinishAction;

        private bool mIsEventsBound = false;

        private RectTransform streamer;
        public Transform startPoint;


        private Tween moneyImpactTween;
        private bool isMoneyImpactPlaying = false;
        private Queue<Action> pendingMoneyImpacts = new Queue<Action>();
        public void Awake()
        {
            FindUIComponents();
            InitializeObjectPools();
            InitializeEffectState();
        }

        public override void InitView()
        {
            RefreshEffectData();
            BindButtonEvent();
        }

        public override void Start() { }

        public override void Update() { }

        private void FindUIComponents()
        {
            mLTEMask = transform.Find("LevelTargetEffect/LTEMask").GetComponent<RectTransform>();
            mLTEPlane = transform.Find("LevelTargetEffect/LTEPlane").GetComponent<RectTransform>();
            mLTEStartPoint = transform.Find("LevelTargetEffect/LTEStartPoint").GetComponent<RectTransform>();
            mDisappearEarlyBtn = transform.Find("LevelTargetEffect/DisappearEarlyBtn").GetComponent<Button>();
            mLevelTargetEffect = transform.Find("LevelTargetEffect").GetComponent<RectTransform>();
            mNextTargetText = transform.Find("LevelTargetEffect/LTEPlane/NextTargetText").GetComponent<Text>();
            mMiniDialog = transform.Find("LevelTargetEffect/LTEPlane/MiniDialog").GetComponent<RectTransform>();
            mMiniTargetText = transform.Find("LevelTargetEffect/LTEPlane/MiniDialog/MiniTargetText").GetComponent<Text>();
            mDialogText = transform.Find("LevelTargetEffect/LTEPlane/Dialog/DialogText").GetComponent<Text>();
            mGREMask = transform.Find("GetRewardEffect/GREMask").GetComponent<RectTransform>();
            mGetRewardEffect = transform.Find("GetRewardEffect").GetComponent<RectTransform>();
            mRewardGroup = transform.Find("GetRewardEffect/Plane/RewardGroup").GetComponent<RectTransform>();
            mEffectRewardItem = transform.Find("GetRewardEffect/Plane/RewardGroup/EffectRewardItem").GetComponent<EffectRewardItem>();
            mCongratulationEffect = transform.Find("CongratulationEffect").GetComponent<RectTransform>();
            mCEContent = transform.Find("CongratulationEffect/CEContent").GetComponent<Text>();
            mFireworkEffectL = transform.Find("CongratulationEffect/FireworkEffectL").GetComponent<ParticleSystem>();
            mFireworkEffectR = transform.Find("CongratulationEffect/FireworkEffectR").GetComponent<ParticleSystem>();
            mFlyEffectParent = transform.Find("FlyEffectParent").GetComponent<RectTransform>();
            mFlyProp = transform.Find("FlyEffectParent/FlyProp").GetComponent<RectTransform>();
            mFlyMoney = transform.Find("FlyEffectParent/FlyMoney").GetComponent<RectTransform>();
            mFlyGem = transform.Find("FlyEffectParent/FlyGem").GetComponent<RectTransform>();
            mFlyIAAMoney = transform.Find("FlyEffectParent/FlyIAAMoney").GetComponent<RectTransform>();
            mFlyMoneyTip = transform.Find("FlyEffectParent/FlyMoneyTip").GetComponent<RectTransform>();
            mFlyGemTip = transform.Find("FlyEffectParent/FlyGemTip").GetComponent<RectTransform>();
            mFlyIAAMoneyTip = transform.Find("FlyEffectParent/FlyIAAMoneyTip").GetComponent<RectTransform>();
            mShaobaEffect = transform.Find("ShaobaEffect").GetComponent<SkeletonGraphic>();
            mHammerEffect = transform.Find("HammerEffect").GetComponent<SkeletonGraphic>();
            mClickEffectParent = transform.Find("ClickEffectParent").GetComponent<RectTransform>();
            mClickEffect = transform.Find("ClickEffectParent/ClickEffect").GetComponent<SkeletonGraphic>();
            mDifficultyUpEffect = transform.Find("DifficultyUpEffect").GetComponent<SkeletonGraphic>();
            mDUEText = transform.Find("DifficultyUpEffect/DUEText").GetComponent<Text>();
            mNextTargetSubtitle = transform.Find("LevelTargetEffect/LTEPlane/NextTargetSubtitle").GetComponent<Text>();
            startPoint = transform.Find("Point").GetComponent<Transform>();
            streamer = transform.Find("CongratulationEffect/streamer").GetComponent<RectTransform>();
        }

        private void InitializeObjectPools()
        {
            ERItemPool = new Queue<EffectRewardItem>();
            curERItem = new List<EffectRewardItem>();
            flyMoneyPool = new Stack<GameObject>();
            flyMoneyTipPool = new Stack<GameObject>();
            flyGemPool = new Stack<GameObject>();
            flyGemTipPool = new Stack<GameObject>();
            flyPropPool = new Stack<GameObject>();
            clickPool = new Stack<SkeletonGraphic>();
        }

        private void InitializeEffectState()
        {
            CongratulationStartY = mCongratulationEffect.localPosition.y;
            CongratulationEndY = mCongratulationEffect.localPosition.y - 650;


            mEffectRewardItem.gameObject.SetActive(false);
            mLevelTargetEffect.gameObject.SetActive(false);
            mGetRewardEffect.gameObject.SetActive(false);
            mFlyProp.gameObject.SetActive(false);
            mFlyMoney.gameObject.SetActive(false);
            mFlyIAAMoney.gameObject.SetActive(false);
            mFlyMoneyTip.gameObject.SetActive(false);
            mFlyIAAMoneyTip.gameObject.SetActive(false);
            if (mFlyGem != null) mFlyGem.gameObject.SetActive(false);
            if (mFlyGemTip != null) mFlyGemTip.gameObject.SetActive(false);
            mShaobaEffect.gameObject.SetActive(false);
            mHammerEffect.gameObject.SetActive(false);
            mClickEffect.gameObject.SetActive(false);
            mDifficultyUpEffect.gameObject.SetActive(false);


            if (mFlyMoney != null && mFlyMoneyTip != null)
            {
                // 切勿把预制体上已有的币图覆盖成 null（本地化图缺失时会整屏白块）。
                Sprite coinSprite = ResolveFlyMoneySprite(preserveTemplate: true);
                if (coinSprite != null)
                {
                    Image flyMoneyImage = mFlyMoney.GetComponent<Image>();
                    if (flyMoneyImage != null)
                        flyMoneyImage.sprite = coinSprite;

                    Transform tipIcon = mFlyMoneyTip.childCount > 0 && mFlyMoneyTip.GetChild(0).childCount > 0
                        ? mFlyMoneyTip.GetChild(0).GetChild(0)
                        : null;
                    Image tipImage = tipIcon != null ? tipIcon.GetComponent<Image>() : null;
                    if (tipImage != null)
                        tipImage.sprite = coinSprite;
                }
            }
        }

        private Sprite ResolveFlyMoneySprite(bool preserveTemplate = false)
        {
            // 飞币只用 icon_qianbi，不用成功页的钱堆图。
            Sprite sprite = Resources.Load<Sprite>(GameDefines.ERFlyMoneyIconPath);
            if (sprite != null)
                return sprite;

            if (preserveTemplate && mFlyMoney != null)
            {
                Image templateImage = mFlyMoney.GetComponent<Image>();
                if (templateImage != null && templateImage.sprite != null)
                    return templateImage.sprite;
            }

            return null;
        }

        public void RefreshEffectData()
        {
            StartCoroutine(DelayedRefreshEffectDataFixed());
        }

        private IEnumerator DelayedRefreshEffectDataFixed()
        {
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            RefreshFlyObjGoldDic();
        }

        private void RefreshFlyObjGoldDic()
        {
            Dictionary<ERewardType, Vector3> goals = null;
            if (FacadeGamePlayExtend.GetFlyObjGoldHandle != null)
                goals = FacadeGamePlayExtend.GetFlyObjGoldHandle.Invoke();

            flyObjGoldDic = goals ?? new Dictionary<ERewardType, Vector3>();
        }

        private Vector3 ResolveFlyTargetPos(ERewardType targetType)
        {
            if (flyObjGoldDic == null || !flyObjGoldDic.ContainsKey(targetType))
                RefreshFlyObjGoldDic();

            if (flyObjGoldDic != null && flyObjGoldDic.TryGetValue(targetType, out Vector3 cachedPos))
                return cachedPos;

            // HostDriven 下 WZ GamePlayerView 不存在时，回退到余额文本或起点，避免 KeyNotFoundException。
            if (targetType == ERewardType.Money || targetType == ERewardType.LuckyWalletMoney || targetType == ERewardType.Gem)
            {
                RectTransform moneyRect = ResolveCurMoneyRect();
                if (moneyRect != null)
                    return moneyRect.position;
            }

            if (startPoint != null)
                return startPoint.position;

            return transform.position;
        }

        protected void BindButtonEvent()
        {
            if (mIsEventsBound)
            {
                RemoveButtonEvents();
            }

            if (mDisappearEarlyBtn != null)
            {
                mDisappearEarlyBtn.onClick.AddListener(OnDisappearEarlyBtnClickHandle);
            }

            RegisterEventListeners();

            mIsEventsBound = true;
        }

        protected void RemoveButtonEvents()
        {
            if (mDisappearEarlyBtn != null)
            {
                mDisappearEarlyBtn.onClick.RemoveAllListeners();
            }

            UnregisterEventListeners();
        }

        private void RegisterEventListeners()
        {

            FacadeEffectExtend.PlayGetRewardEffectHandle += PlayGetRewardEffect;
            FacadeEffectExtend.PlayCongratulationEffectHandle += PlayCongratulationEffect;
            FacadeEffectExtend.PlayFlyMoneyHandle += PlayFlyMoney;
            FacadeEffectExtend.PlayFlyMoneyTipHandle += PlayFlyMoneyTip;

        }

        private void UnregisterEventListeners()
        {

            FacadeEffectExtend.PlayGetRewardEffectHandle -= PlayGetRewardEffect;
            FacadeEffectExtend.PlayCongratulationEffectHandle -= PlayCongratulationEffect;
            FacadeEffectExtend.PlayFlyMoneyHandle -= PlayFlyMoney;
            FacadeEffectExtend.PlayFlyMoneyTipHandle -= PlayFlyMoneyTip;

        }

        #region 播放提现目标特效
        private void PlayLevelTargetEffect(Action finishAction)
        {
            targetEffectFinishAction = finishAction;
            mLevelTargetEffect.gameObject.SetActive(true);
            mLTEPlane.transform.localScale = Vector3.one;
            SetLevelTargetText();

            mLTEPlane.transform.position = mLTEStartPoint.transform.position;
            targetEffectSequence = DOTween.Sequence();
            targetEffectSequence.Append(mLTEPlane.transform.DOLocalMoveY(0, GameDefines.LTE_MoveTime));
            targetEffectSequence.AppendInterval(GameDefines.LTE_StayTime);
            targetEffectSequence.Append(mLTEPlane.transform.DOMove(LTEGoldTrans.position, GameDefines.LTE_GoAwayTime));
            targetEffectSequence.Join(mLTEPlane.transform.DOScale(0, GameDefines.LTE_GoAwayTime));
            targetEffectSequence.OnComplete(() =>
            {
                PlayCongratulationEffect();
                mLevelTargetEffect.gameObject.SetActive(false);
                targetEffectFinishAction?.Invoke();
            });
        }

        private void OnDisappearEarlyBtnClickHandle()
        {
            if (targetEffectSequence != null)
                targetEffectSequence.Kill();

            PlayCongratulationEffect();
            mLevelTargetEffect.gameObject.SetActive(false);
            targetEffectFinishAction?.Invoke();
        }

        private void SetLevelTargetText()
        {
            int actualLevel = GameManagerWZ.instance.currentLv;
            mMiniTargetText.text = GetLevelDisplayText(actualLevel);
            mMiniDialog.gameObject.SetActive(IsInSmallLevelGroup(actualLevel));
            if (actualLevel == 2 || actualLevel >= GameDefines.SmallLevelGroupStart && actualLevel <= GameDefines.SmallLevelGroupEnd)
            {
                mNextTargetText.text = string.Format(FacadeLanguageExtend.GetTextHandle("10022"), GameDefines.SmallLevelGroupStart);
            }
            else if (actualLevel > 2 && actualLevel < GameDefines.SmallLevelGroupStart)
            {
                mNextTargetText.text = string.Format(FacadeLanguageExtend.GetTextHandle("10021"), GameDefines.SmallLevelGroupStart);
            }
            else
            {
                // mNextTargetText.text = FacadeWithdrawalExtend.GetTipMsgHandle();
            }

            mDialogText.text = string.Format(FacadeLanguageExtend.GetTextHandle("10020"), (int)UnityEngine.Random.Range(GameDefines.TargetArriveTime.x, GameDefines.TargetArriveTime.y + 1));
        }
        #endregion

        private string GetLevelDisplayText(int actualLevel)
        {
            int start = GameDefines.SmallLevelGroupStart;
            int end = GameDefines.SmallLevelGroupEnd;
            if (actualLevel <= start) return string.Format(FacadeLanguageExtend.GetTextHandle("10010"), actualLevel);
            if (IsInSmallLevelGroup(actualLevel)) return string.Format(FacadeLanguageExtend.GetTextHandle("1033"), start, actualLevel - start + 1, end - start + 1);
            return string.Format(FacadeLanguageExtend.GetTextHandle("10010"), actualLevel - (end - start));
        }

        private bool IsInSmallLevelGroup(int actualLevel)
        {
            return actualLevel > GameDefines.SmallLevelGroupStart && actualLevel <= GameDefines.SmallLevelGroupEnd;
        }

        #region 播放获取奖励特效
        private void PlayGetRewardEffect(ERewardItemStruct[] items, Action finishAction)
        {
            mGetRewardEffect.gameObject.SetActive(true);
            mGREMask.gameObject.SetActive(true);
            curERItem.Clear();


            var itemsWithIndex = items.Select((item, index) => new { Item = item, Index = index }).ToList();
            var sortedItems = itemsWithIndex
                .OrderBy(x => GetRewardTypeOrder(x.Item.Type))
                .ThenBy(x => x.Index)
                .Select(x => x.Item)
                .ToList();


            Dictionary<ERewardType, Queue<EffectRewardItem>> poolByType = new Dictionary<ERewardType, Queue<EffectRewardItem>>();


            while (ERItemPool.Count > 0)
            {
                EffectRewardItem item = ERItemPool.Dequeue();
                if (!poolByType.ContainsKey(item.ErType))
                {
                    poolByType[item.ErType] = new Queue<EffectRewardItem>();
                }
                poolByType[item.ErType].Enqueue(item);
            }

            foreach (ERewardItemStruct item in sortedItems)
            {
                EffectRewardItem newItem = null;

                if (poolByType.ContainsKey(item.Type) && poolByType[item.Type].Count > 0)
                {
                    newItem = poolByType[item.Type].Dequeue();
                }
                else
                {
                    newItem = GameObject.Instantiate(mEffectRewardItem, mRewardGroup).GetComponent<EffectRewardItem>();
                }

                newItem.gameObject.SetActive(true);
                newItem.Show(item.Type, item.Count);
                curERItem.Add(newItem);
            }
            foreach (var kv in poolByType)
            {
                while (kv.Value.Count > 0)
                {
                    ERItemPool.Enqueue(kv.Value.Dequeue());
                }
            }

            if (getRewardEffectCoroutine != null)
            {
                GameManagerWZ.instance.StopCoroutine(getRewardEffectCoroutine);
            }

            getRewardEffectCoroutine = GameManagerWZ.instance.StartCoroutine(GetRewardEffectCoroutine(finishAction));
        }

        private int GetRewardTypeOrder(ERewardType type)
        {
            switch (type)
            {
                case ERewardType.Money:
                    return 0;  // 金币最先
                case ERewardType.Gem:
                    return 1;  // 钻石第二
                case ERewardType.LuckyWalletMoney:
                    return 2;  // 钱包奖励紧随货币类展示
                case ERewardType.AddBottle:
                    return 3;  // 增加瓶子第三
                case ERewardType.Clear:
                    return 4;  // 清除第四
                case ERewardType.Undo:
                    return 5; 
                case ERewardType.Prompt:
                    return 6;  
                case ERewardType.Guid:
                    return 7; 
        
                default:
                    return 999;
            }
        }

        private System.Collections.IEnumerator GetRewardEffectCoroutine(Action finishAction)
        {
            yield return new WaitForSeconds(GameDefines.GRE_StayTime);

            mGetRewardEffect.gameObject.SetActive(false);
            mGREMask.gameObject.SetActive(false);
            foreach (EffectRewardItem item in curERItem)
            {
                ERItemPool.Enqueue(item);
                item.gameObject.SetActive(false);
                GetRewardEffectAfterFly(item);
            }
            curERItem.Clear();
            finishAction?.Invoke();
            getRewardEffectCoroutine = null;
        }


        private void GetRewardEffectAfterFly(EffectRewardItem ERItem)
        {
            switch (ERItem.ErType)
            {
                case ERewardType.Money:
                    GameManagerWZ.instance.AddCoin(ERItem.Count);
                    PlayFlyMoney(ERItem.transform, GameDefines.GRE_FlyMoneyCount, ERItem.Count, () =>
                    {
                    }, ERewardType.Money);
                    break;

                case ERewardType.Gem:
                    GameManagerWZ.instance.AddGems(ERItem.Count);
                    PlayFlyMoney(ERItem.transform, GameDefines.GRE_FlyMoneyCount, ERItem.Count, () =>
                    {

                    }, ERewardType.Gem);
                    break;

                case ERewardType.AddBottle:
                    GameManagerWZ.instance.RewardHintBottle(Mathf.RoundToInt(ERItem.Count));
                    PlayFlyProp(ERItem.transform, ERewardType.AddBottle, () =>
                    {

                    });
                    break;

                case ERewardType.Clear:
                    GameManagerWZ.instance.RewardClear(Mathf.RoundToInt(ERItem.Count));
                    PlayFlyProp(ERItem.transform, ERewardType.Clear, () =>
                    {

                    });
                    break;

                case ERewardType.Undo:
                    GameManagerWZ.instance.RewardUndo(Mathf.RoundToInt(ERItem.Count));
                    PlayFlyProp(ERItem.transform, ERewardType.Undo, () =>
                    {

                    });
                    break;
                /*case ERewardType.Prompt:
                    //GameManager.instance.RewardUndo(Mathf.RoundToInt(ERItem.Count));
                    GameData.Hint.Value++;
                    PlayFlyProp(ERItem.transform, ERewardType.Prompt, () =>
                    {

                    });
                    break;
                case ERewardType.Guid:
                    //GameManager.instance.RewardUndo(Mathf.RoundToInt(ERItem.Count));
                    GameData.GridLines.Value++;
                    PlayFlyProp(ERItem.transform, ERewardType.Guid, () =>
                    {

                    });
                    break;
                case ERewardType.LuckyWalletMoney:
                    break;*/
            }
        }
        #endregion

        #region 播放祝贺特效
        private void PlayCongratulationEffect()
        {
            if (GameDefines.ifIAA) return;

            int curLevel = GameManagerWZ.instance.currentLv;
            var (playerName, moneyAmount) = UserDataManager.Instance.GetRandomBarragePlayAndStage();
            int attemptCount = GetRandomAttempt();
            mCEContent.text = string.Format(
                LocalizationManager.Instance.GetText("1030"),
                playerName,          
                attemptCount,      
                FacadePayTypeExtend.RegionalChangeHandle(moneyAmount)
            );

            Debug.Log($"[祝贺特效] 玩家:{playerName}, 尝试:{attemptCount}次, 金额:{moneyAmount}");

            mFireworkEffectL.Stop();
            mFireworkEffectR.Stop();
            streamer.gameObject.SetActive(true);

            DG.Tweening.Sequence sequence = DOTween.Sequence();
            sequence.Append(mCongratulationEffect.transform.DOLocalMoveY(CongratulationEndY, GameDefines.CE_MoveTime));
            sequence.AppendInterval(GameDefines.CE_StayTime);
            sequence.OnComplete(() =>
            {
                streamer.gameObject.SetActive(false);
                sequence.Append(mCongratulationEffect.transform.DOLocalMoveY(CongratulationStartY, GameDefines.CE_MoveTime));
            });
        }

      
        #endregion


        private string GetRandomPlayerName()
        {
            nameChars = GameDefines.nameString.ToCharArray();
            int length = nameChars.Length;
            char c1 = nameChars[UnityEngine.Random.Range(0, length)];
            char c2 = nameChars[UnityEngine.Random.Range(0, length)];

            return $"{LocalizationManager.Instance.GetText("1005")}_{c1}{c2}";
        }

        private int GetRandomAttempt()
        {
            int attempt = (int)UnityEngine.Random.Range(GameDefines.CE_Content_attemptTimes.x, GameDefines.CE_Content_attemptTimes.y + 1);
            return attempt;
        }




        #region 飞行物体特效
        private void PlayFlyMoney(Transform startPoint, int count, float money, Action successAction, ERewardType targetType = ERewardType.Money)
        {
            bool hasTriggeredMoneyImpact = false;
            for (int i = 0; i < count; i++)
            {
                GameObject flyObj;
                Stack<GameObject> objectPool;
                Sprite iconSprite = null;

                if (targetType == ERewardType.Gem)
                {
                    if (mFlyGem != null)
                    {
                        objectPool = flyGemPool;
                        flyObj = objectPool.Count > 0 ? objectPool.Pop() : GameObject.Instantiate(GameDefines.ifIAA ? mFlyGem.gameObject : mFlyMoney.gameObject, mFlyEffectParent);
                    }
                    else
                    {
                        objectPool = flyMoneyPool;
                        flyObj = objectPool.Count > 0 ? objectPool.Pop() : GameObject.Instantiate(GameDefines.ifIAA ? mFlyGem.gameObject : mFlyMoney.gameObject, mFlyEffectParent);
                    }
                    iconSprite = FacadePayTypeExtend.GetSingleGem();
                }
                else
                {
                    objectPool = flyMoneyPool;
                    flyObj = objectPool.Count > 0 ? objectPool.Pop() : GameObject.Instantiate(GameDefines.ifIAA ? mFlyIAAMoney.gameObject : mFlyMoney.gameObject, mFlyEffectParent);
                    iconSprite = ResolveFlyMoneySprite(preserveTemplate: true);
                }

                flyObj.gameObject.SetActive(true);

                Image image = flyObj.GetComponent<Image>();
                if (image != null)
                {
                    if (iconSprite != null)
                        image.sprite = iconSprite;
                    else if (image.sprite == null && mFlyMoney != null)
                    {
                        Image template = mFlyMoney.GetComponent<Image>();
                        if (template != null && template.sprite != null)
                            image.sprite = template.sprite;
                    }
                }
                if (startPoint == null)
                {
                    startPoint.position = this.startPoint.position;
                }
                else
                {
                    flyObj.transform.position = startPoint.position;
                }
                float r1 = UnityEngine.Random.Range(0, GameDefines.FlyMoney_RandomSpawnDist);
                float r2 = UnityEngine.Random.Range(0, GameDefines.FlyMoney_RandomSpawnDist);
                flyObj.transform.GetComponent<RectTransform>().localPosition += new Vector3(r1, r2, 0);

                flyObj.transform.DOMove(ResolveFlyTargetPos(targetType), GameDefines.FlyMoney_MoveTime)
                    .SetDelay(GameDefines.FlyMoney_DelayTime + i * GameDefines.FlyMoney_IntervalTime)
                    .OnComplete(() =>
                    {
                       
                        successAction?.Invoke();

                        if (targetType == ERewardType.Gem && mFlyGem != null)
                        {
                            flyGemPool.Push(flyObj);
                        }
                        else
                        {
                            flyMoneyPool.Push(flyObj);
                        }

                        flyObj.gameObject.SetActive(false);

          
                        if (!hasTriggeredMoneyImpact && targetType == ERewardType.Money)
                        {
                            PlayCurMoneyImpact();
                            hasTriggeredMoneyImpact = true;
                        }
                    });
          
            }

            STimerManager.Instance.CreateSDelay(GameDefines.FlyMoneyTip_DelayTime, () =>
            {
                PlayFlyMoneyTip(money, targetType);
            });


        }

        private void PlayCurMoneyImpact()
        {
            RectTransform curMoneyRect = ResolveCurMoneyRect();
            if (curMoneyRect == null || !curMoneyRect.gameObject.activeInHierarchy)
            {
                return;
            }
            if (isMoneyImpactPlaying)
            {
                pendingMoneyImpacts.Enqueue(() => ExecuteMoneyImpact(curMoneyRect));
                return;
            }
            ExecuteMoneyImpact(curMoneyRect);
        }
        private void ExecuteMoneyImpact(RectTransform curMoneyRect)
        {
            isMoneyImpactPlaying = true;
            Vector3 originScale = curMoneyRect.localScale;

            // 停止当前正在运行的动画（如果有）
            if (moneyImpactTween != null && moneyImpactTween.IsActive())
            {
                moneyImpactTween.Kill();
            }

            // 确保从原始缩放开始
            curMoneyRect.localScale = originScale;

            DG.Tweening.Sequence pulseSequence = DOTween.Sequence();
            pulseSequence.Append(curMoneyRect.DOScale(originScale * 1.22f, 0.045f).SetEase(Ease.OutQuad));
            pulseSequence.Append(curMoneyRect.DOScale(originScale * 0.9f, 0.045f).SetEase(Ease.InQuad));
            pulseSequence.Append(curMoneyRect.DOScale(originScale * 1.16f, 0.05f).SetEase(Ease.OutQuad));
            pulseSequence.Append(curMoneyRect.DOScale(originScale * 0.94f, 0.05f).SetEase(Ease.InQuad));
            pulseSequence.Append(curMoneyRect.DOScale(originScale * 1.1f, 0.055f).SetEase(Ease.OutQuad));
            pulseSequence.Append(curMoneyRect.DOScale(originScale * 0.97f, 0.055f).SetEase(Ease.InQuad));
            pulseSequence.Append(curMoneyRect.DOScale(originScale * 1.04f, 0.06f).SetEase(Ease.OutQuad));
            pulseSequence.Append(curMoneyRect.DOScale(originScale, 0.08f).SetEase(Ease.OutSine));
            pulseSequence.SetUpdate(true);
            pulseSequence.OnComplete(() =>
            {
                curMoneyRect.localScale = originScale;
                moneyImpactTween = null;
                isMoneyImpactPlaying = false;

                // 执行队列中的下一个冲击动画
                if (pendingMoneyImpacts.Count > 0)
                {
                    var nextAction = pendingMoneyImpacts.Dequeue();
                    nextAction?.Invoke();
                }
            });

            moneyImpactTween = pulseSequence;
        }

        private RectTransform ResolveCurMoneyRect()
        {
            var gamePlayerView = UIManager.instance != null
                ? UIManager.instance.GetView<GamePlayerView>(EUIType.GamePlayerView)
                : null;
            RectTransform rect = FindNamedParentRect(gamePlayerView != null ? gamePlayerView.coinTxt?.transform : null, "CurMoney");
            if (rect != null)
                return rect;

            var gameView = UIManager.instance != null
                ? UIManager.instance.GetView<GameView>(EUIType.GameView)
                : null;
            rect = FindNamedParentRect(gameView != null ? gameView.coinTxt?.transform : null, "CurMoney");
            if (rect != null)
                return rect;

            // HostDriven：宿主 HUD 余额
            var hostMoney = GameObject.Find("CurMoney");
            if (hostMoney != null)
                return hostMoney.GetComponent<RectTransform>();

            return null;
        }
        private RectTransform FindNamedParentRect(Transform start, string targetName)
        {
            Transform current = start;
            while (current != null)
            {
                if (current.name == targetName)
                {
                    return current as RectTransform;
                }
                current = current.parent;
            }
            return null;
        }
        private void PlayFlyMoneyTip(float money, ERewardType targetType = ERewardType.Money)
        {
            GameObject flyObj;
            Stack<GameObject> objectPool;
            Sprite iconSprite = null;

            if (targetType == ERewardType.Gem)
            {
                objectPool = flyGemTipPool;
                flyObj = objectPool.Count > 0 ? objectPool.Pop() : GameObject.Instantiate(mFlyGemTip.gameObject, mFlyEffectParent);
                iconSprite = FacadePayTypeExtend.GetSingleGem();
            }
            else
            {
                objectPool = flyMoneyTipPool;
                flyObj = objectPool.Count > 0 ? objectPool.Pop() : GameObject.Instantiate(GameDefines.ifIAA ? mFlyIAAMoneyTip.gameObject : mFlyMoneyTip.gameObject, mFlyEffectParent);
                iconSprite = ResolveFlyMoneySprite(preserveTemplate: true);
            }

            flyObj.gameObject.SetActive(true);

            Transform iconTransform = flyObj.transform.GetChild(0).GetChild(0);
            if (iconTransform != null)
            {
                Image image = iconTransform.GetComponent<Image>();
                if (image != null && iconSprite != null)
                {
                    image.sprite = iconSprite;
                }
            }

            flyObj.transform.GetChild(0).GetComponent<Text>().text = FacadePayTypeExtend.RegionalChangeHandle(money);
            flyObj.transform.position = ResolveFlyTargetPos(targetType);
            float goldY = flyObj.transform.localPosition.y;
            flyObj.transform.DOLocalMoveY(goldY += GameDefines.FlyMoneyTip_MoveDist, GameDefines.FlyMoneyTip_MoveTime).OnComplete(() =>
            {
                if (targetType == ERewardType.Gem && mFlyGemTip != null)
                {
                    flyGemTipPool.Push(flyObj);
                }
                else
                {
                    flyMoneyTipPool.Push(flyObj);
                }

                flyObj.gameObject.SetActive(false);
            });
        }

        private void PlayFlyProp(Transform startPoint, ERewardType rewardType, Action successAction)
        {
            string flyIconPath = GameDefines.ERHammerIconPath;
            switch (rewardType)
            {
                case ERewardType.AddBottle:
                    flyIconPath = GameDefines.ERAddSpaceIconPath;
                    break;
                case ERewardType.Clear:
                    flyIconPath = GameDefines.ERClearIconPath;
                    break;
                case ERewardType.Undo:
                    flyIconPath = GameDefines.ERHammerIconPath;
                    break;
                case ERewardType.Prompt:
                    flyIconPath = GameDefines.ERAddPromptIconPath;
                    break;
                case ERewardType.Guid:
                    flyIconPath = GameDefines.ERAddGuidIconPath;
                    break;
            }

            GameObject flyObj = flyPropPool.Count > 0 ? flyPropPool.Pop() : GameObject.Instantiate(mFlyProp.gameObject, mFlyEffectParent);
            flyObj.gameObject.SetActive(true);
            flyObj.transform.position = startPoint.position;
            flyObj.GetComponent<Image>().sprite = Resources.Load<Sprite>(flyIconPath);


            flyObj.transform.DOMove(ResolveFlyTargetPos(rewardType), GameDefines.FlyProp_MoveTime).OnComplete(() =>
            {
                successAction?.Invoke();
                flyPropPool.Push(flyObj);
                flyObj.gameObject.SetActive(false);
            });
        }
        #endregion



        private void OnDestroy()
        {
            if (mIsEventsBound)
            {
                RemoveButtonEvents();
            }

            CleanupResources();
        }

        private void CleanupResources()
        {
            UnregisterEventListeners();

            // 停止协程
            if (getRewardEffectCoroutine != null)
            {
                StopCoroutine(getRewardEffectCoroutine);
                getRewardEffectCoroutine = null;
            }

            ERItemPool?.Clear();
            ERItemPool = null;

            curERItem?.Clear();
            curERItem = null;

            flyMoneyPool?.Clear();
            flyMoneyPool = null;

            flyMoneyTipPool?.Clear();
            flyMoneyTipPool = null;

            flyGemPool?.Clear();
            flyGemPool = null;

            flyGemTipPool?.Clear();
            flyGemTipPool = null;

            flyPropPool?.Clear();
            flyPropPool = null;

            clickPool?.Clear();
            clickPool = null;

            flyObjGoldDic?.Clear();
            flyObjGoldDic = null;

            LTEGoldTrans = null;
        }
    }
}
