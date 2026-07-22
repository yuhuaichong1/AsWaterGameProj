using UnityEngine;
using UnityEngine.UI;
using System;
namespace WZSDK
{
  
    public class Tutorial : BaseView
    {
        public static Tutorial instance;
        public enum TYPE
        {
            TYPE1,
            TYPE3,
            TYPE19,
        }


        public TYPE currentType;
        public GameObject hand1, mask, Hole, ClickPos, GuideTextFather, PosRoot;
        public Text GTContent;
        public int step;
        private Action currentStepCallback;


        public override void Start()
        {
            instance = this;
            // 不在此处 GoToStep / Hide：外部 ShowView 后会设 TYPE 并 GoToStep；
            // Start 若再清手指，会把第1关 WithDraw 引导立刻清掉。
        }

        public override void Close(params object[] args)
        {
            currentStepCallback = null;
            ForceHideGuideVisuals();
            base.Close(args);
            // BaseView.HideView 只改 alpha，对象仍 active，手指可能继续露在通关页上。
            gameObject.SetActive(false);
            isShow = false;
        }

        public void ForceHideGuideVisuals()
        {
            currentStepCallback = null;
            hand1?.SetActive(false);
            mask?.SetActive(false);
            Hole?.SetActive(false);
            ClickPos?.SetActive(false);
            GuideTextFather?.SetActive(false);

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            isShow = false;
        }

        public override void Update()
        {

        }

        public override void InitView()
        {

        }

        public void GoToNextStep()
        {
            step++;
            GoToStep(step);
        }

        // 设置指定步骤
        public void GoToStep(int targetStep)
        {
            switch (currentType)
            {
                case TYPE.TYPE1:
                    HandleType1Step(targetStep);
                    break;
                case TYPE.TYPE3:
                    HandleType3Step(targetStep);
                    break;
                case TYPE.TYPE19:
                    HandleType19Step(targetStep);
                    break;
            }
        }
        void HandleType19Step(int targetStep)
        {
            switch (targetStep)
            {
                case 0:
                    SetupType19Step0();
                    break;
            }
        }
        void SetupType19Step0()
        {
            GamePlayerView gamePlayerView = UIManager.instance.GetView<GamePlayerView>(EUIType.GamePlayerView);
            Transform lastCap = gamePlayerView.CoinBoard.transform;
            SetupHoleAndClickPos(lastCap);
            mask.SetActive(true);
            GuideTextFather.gameObject.SetActive(false);
            var GM = GameManagerWZ.instance;

            currentStepCallback = () =>
            {
                DataModel dataModel = new DataModel();
                dataModel.Level = GM.currentLv - 1;
                dataModel.Money = GM.GetCoin();
                dataModel.CloseType = 1;
                dataModel.Type = 2;
                dataModel.Num = GM.currentStage > 0 ? GM.currentStage : 1;
                GameApp.viewManager.Open(ViewType.WithDraw, dataModel);
                GameManagerWZ.instance.StartCoroutine(DelayedSetupType3());
            };
        }


        private System.Collections.IEnumerator DelayedSetupType3()
        {
            yield return null;

            UIManager.instance.CloseView(EUIType.Tutorial);
            UIManager.instance.ShowView(EUIType.Tutorial);
            Tutorial tutorial = UIManager.instance.GetView<Tutorial>(EUIType.Tutorial);
            tutorial.currentType = TYPE.TYPE3;
            tutorial.GoToStep(0);
        }
        void HandleType3Step(int targetStep)
        {
            switch (targetStep)
            {
                case 0:
                    SetupType3Step0();
                    break;
                case 1:
                    SetupType3Step2();
                    break;    
                case 3:
                    SetupType3Step3();
                    break;
            }
        }

        void HandleType1Step(int targetStep)
        {
            switch (targetStep)
            {
                case 0:
                    SetupStep0();
                    break;
                case 1:
                    SetupStep1();
                    break;
           
                case 3:
                    SetupStep3();
                    break;
                case 4:
                    SetupStep4();
                    break;
                case 5:
                    FinishTutorial();
                    break;
                default:
                    Debug.LogWarning($"未知的步骤: {targetStep}");
                    break;
            }
        }

        // TYPE3 步骤0
        void SetupType3Step0()
        {
            WithDraw withDraw = GameApp.viewManager != null
                ? GameApp.viewManager.GetView<WithDraw>((int)ViewType.WithDraw)
                : null;
            if (withDraw == null)
            {
                Debug.LogError("Tutorial TYPE3 step0: WithDraw view is missing.");
                GameManagerWZ.instance?.CloseActiveTutorial();
                return;
            }

            var WithDrawBtn = withDraw.Find<Button>("Panel/PanelB/WithDraw");
            var pos = withDraw.Find<Transform>("pos");
            if (WithDrawBtn == null || pos == null)
            {
                Debug.LogError("Tutorial TYPE3 step0: WithDraw button or pos is missing.");
                GameManagerWZ.instance?.CloseActiveTutorial();
                return;
            }

            Hole.transform.position = pos.position;
            hand1.transform.position = pos.position - new Vector3(2, 0, 0);
            ClickPos.transform.position = pos.position;
            ClickPos.SetActive(true);

            RectTransform holeRect = Hole.GetComponent<RectTransform>();
            RectTransform capRect = pos.GetComponent<RectTransform>();
            RectTransform clickRect = ClickPos.GetComponent<RectTransform>();

            if (holeRect != null && capRect != null)
            {
                holeRect.sizeDelta = capRect.sizeDelta;
                if (clickRect != null)
                    clickRect.sizeDelta = capRect.sizeDelta;
            }

            BindPenetrateTarget(clickRect);

            mask.SetActive(true);
            hand1.SetActive(true);
            GuideTextFather.gameObject.SetActive(false);
            Hole.SetActive(true);

            currentStepCallback = () =>
            {
                WithDrawBtn.onClick?.Invoke();
                UIManager.instance.CloseView(EUIType.Tutorial);
            };
        }

        void BindPenetrateTarget(RectTransform target)
        {
            if (mask == null || target == null)
                return;

            var penetrate = mask.GetComponent<SGuidePenetrate>();
            if (penetrate == null)
                return;

            if (penetrate.penetrateObjs == null)
                penetrate.penetrateObjs = new System.Collections.Generic.List<RectTransform>();

            penetrate.penetrateObjs.Clear();
            penetrate.penetrateObjs.Add(target);
        }
        void SetupType3Step2()
        {
            GamePlayerView gamePlayerView = UIManager.instance.GetView<GamePlayerView>(EUIType.GamePlayerView);
            Transform lastCap = gamePlayerView.GemBoard.transform;
            SetupHoleAndClickPos(lastCap);
            mask.SetActive(true);
            GuideTextFather.gameObject.SetActive(false);
            currentStepCallback = () =>
            {
                gamePlayerView.GemBoard.onClick.Invoke();
                UIManager.instance.CloseView(EUIType.Tutorial);
            };
        }

        void SetupType3Step3()
        {
            // Game2/宿主：对准 UIGamePlay.CMBtn；旧 WZ GamePlayerView.CoinBoard 仅作兜底。
            RectTransform target = XrCode.FacadeGamePlay.GetCashOutBtnRect?.Invoke();
            Button cashOutBtn = null;
            if (target != null)
                cashOutBtn = target.GetComponent<Button>() ?? target.GetComponentInChildren<Button>();

            if (target == null)
            {
                GamePlayerView gamePlayerView = UIManager.instance.GetView<GamePlayerView>(EUIType.GamePlayerView);
                if (gamePlayerView != null && gamePlayerView.CoinBoard != null)
                {
                    target = gamePlayerView.CoinBoard.transform as RectTransform;
                    cashOutBtn = gamePlayerView.CoinBoard;
                }
            }

            if (target == null)
            {
                Debug.LogWarning("Tutorial TYPE3 step3: CashOut/CMBtn missing, close tutorial.");
                UIManager.instance.CloseView(EUIType.Tutorial);
                ForceHideGuideVisuals();
                return;
            }

            SetupHoleAndClickPos(target);
            AlignHoleToTarget(target);

            mask.SetActive(true);
            hand1.SetActive(true);
            GuideTextFather.gameObject.SetActive(false);
            Hole.SetActive(true);

            // 阶段已推进时，引导应打开“刚完成的 Goal”（stage-1），例如第2关后开 Goal2。
            int goalStage = 1;
            if (GameManagerWZ.instance != null)
                goalStage = Mathf.Max(GameManagerWZ.instance.currentStage - 1, 1);

            currentStepCallback = () =>
            {
                if (!WaterSortWZBridge.TryOpenWithdraw(goalStage) && cashOutBtn != null)
                    cashOutBtn.onClick?.Invoke();

                if (GameManagerWZ.instance != null)
                    GameManagerWZ.instance.StartCoroutine(DelayedBindWithdrawGuideAfterCashOut());
            };
        }

        System.Collections.IEnumerator DelayedBindWithdrawGuideAfterCashOut()
        {
            // 若已直接进入 Method/Process1，不要再指 CMBtn。
            for (int i = 0; i < 12; i++)
            {
                yield return null;

                if (GameApp.viewManager != null
                    && (GameApp.viewManager.IsOpen((int)ViewType.WithDrawProcess1)
                        || GameApp.viewManager.IsOpen((int)ViewType.WithDrawMethod)))
                {
                    GameManagerWZ.instance?.CloseActiveTutorial();
                    yield break;
                }

                if (GameApp.viewManager != null
                    && GameApp.viewManager.GetView<WithDraw>((int)ViewType.WithDraw) != null)
                    break;
            }

            if (GameApp.viewManager == null
                || GameApp.viewManager.GetView<WithDraw>((int)ViewType.WithDraw) == null)
            {
                GameManagerWZ.instance?.CloseActiveTutorial();
                yield break;
            }

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            currentType = TYPE.TYPE3;
            step = 0;
            GoToStep(0);

            // 再等一帧确认绑到 WithDraw；若页面已切走则清引导。
            yield return null;
            if (GameApp.viewManager.GetView<WithDraw>((int)ViewType.WithDraw) == null
                || GameApp.viewManager.IsOpen((int)ViewType.WithDrawProcess1))
            {
                GameManagerWZ.instance?.CloseActiveTutorial();
            }
        }

        void AlignHoleToTarget(RectTransform target)
        {
            if (target == null || Hole == null)
                return;

            RectTransform holeRect = Hole.GetComponent<RectTransform>();
            RectTransform clickRect = ClickPos != null ? ClickPos.GetComponent<RectTransform>() : null;
            if (holeRect == null)
                return;

            RectTransform space = holeRect.parent as RectTransform;
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);

            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;
            for (int i = 0; i < 4; i++)
            {
                Vector3 local = space != null
                    ? space.InverseTransformPoint(corners[i])
                    : corners[i];
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            Vector2 size = max - min;
            if (size.x < 8f || size.y < 8f)
            {
                size = target.rect.size;
                if (size.x < 1f || size.y < 1f)
                    size = target.sizeDelta;
            }

            size += new Vector2(28f, 18f);

            holeRect.anchorMin = holeRect.anchorMax = new Vector2(0.5f, 0.5f);
            holeRect.pivot = new Vector2(0.5f, 0.5f);
            holeRect.sizeDelta = size;
            holeRect.position = (corners[0] + corners[2]) * 0.5f;

            if (clickRect != null)
            {
                clickRect.anchorMin = clickRect.anchorMax = new Vector2(0.5f, 0.5f);
                clickRect.pivot = new Vector2(0.5f, 0.5f);
                clickRect.sizeDelta = size;
                clickRect.position = holeRect.position;
            }

            if (hand1 != null)
                hand1.transform.position = holeRect.position - new Vector3(1f, 0f, 0f);

            BindPenetrateTarget(clickRect != null ? clickRect : target);
        }
        void SetupStep0()
        {
            PlaceHandOnBottle(0);
            hand1.SetActive(true);
            mask.SetActive(false);
            GTContent.text = LocalizationManager.Instance.GetText("1001"); //"Tap a bottle to pour into another bottle";
            SetGuidePosition("Pos1");
        }

        void SetupStep1()
        {
            PlaceHandOnBottle(1);
            SetGuidePosition("Pos1");
            hand1.SetActive(true);
            mask.SetActive(false);
        }

        void SetupStep2()
        {

        }

        void SetupStep3()
        {
            mask.SetActive(false);
            PlaceHandOnBottle(2);
            GTContent.text = LocalizationManager.Instance.GetText("1003");
            GuideTextFather.SetActive(true);
            SetGuidePosition("Pos1");
            HideHoleAndClickPos();
        }

        void SetupStep4()
        {
            mask.SetActive(false);
            PlaceHandOnBottle(3);
            GTContent.text = LocalizationManager.Instance.GetText("1003");
            SetGuidePosition("Pos1");
            HideHoleAndClickPos();
        }

        void PlaceHandOnBottle(int bottleIndex)
        {
        }

        void SetupHoleAndClickPos(Transform capTransform)
        {
            Hole.transform.position = capTransform.position;
            hand1.transform.position = capTransform.position;
            ClickPos.transform.position = capTransform.position;
            ClickPos.SetActive(true);

            RectTransform holeRect = Hole.GetComponent<RectTransform>();
            RectTransform capRect = capTransform.GetComponent<RectTransform>();
            RectTransform clickRect = ClickPos.GetComponent<RectTransform>();

            if (holeRect != null && capRect != null)
            {
                holeRect.sizeDelta = capRect.sizeDelta;
                if (clickRect != null)
                    clickRect.sizeDelta = capRect.sizeDelta;
            }

            BindPenetrateTarget(clickRect);
            Hole.SetActive(true);
            hand1.SetActive(true);
        }

        void HideHoleAndClickPos()
        {
            Hole?.SetActive(false);
            ClickPos?.SetActive(false);
        }

        void SetGuidePosition(string posName)
        {
            Transform pos = PosRoot.transform.Find(posName);
            if (pos != null)
            {
                GuideTextFather.GetComponent<RectTransform>().anchoredPosition =
                    pos.GetComponent<RectTransform>().anchoredPosition;
            }
        }

        void FinishTutorial()
        {
            UIManager.instance.CloseView(EUIType.Tutorial);
         
          /*  if (!GameDefines.ifIAA)
            {
                UIManager.instance.GetView<GamePlayerView>(EUIType.GamePlayerView).ShowWithdrawTop();
                FacadeEffectExtend.PlayCongratulationEffectHandle?.Invoke();
            }*/
        }


        public void ExecuteCurrentStepCallback()
        {
            Action callback = currentStepCallback;
            currentStepCallback = null;
            callback?.Invoke();
        }


    }
}
