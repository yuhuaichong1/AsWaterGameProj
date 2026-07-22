using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    public class BloatProgressView : BaseView
    {
        private const int Stage5Id = 5;
        private const float RefreshInterval = 0.2f;

        private Image progress;
        private Image fill;
        private Text overallCount;
        private Text count;
        private RectTransform progressRoot;
        private Button clickBtn;
        private Button close;

        private bool isInitialized;
        private bool isEventsBound;
        private float nextRefreshTime;
        private float targetCoinOverride;

        private void Awake()
        {
            ResolveReferencesIfNeeded();
        }

        public override void InitView()
        {
            ResolveReferencesIfNeeded();
            BindEvents();
            RefreshView();
        }

        public override void Start()
        {
            ResolveReferencesIfNeeded();
            BindEvents();
            RefreshView();
        }

        public override void Update()
        {
            if (!isInitialized || Time.unscaledTime < nextRefreshTime)
                return;

            RefreshView();
            nextRefreshTime = Time.unscaledTime + RefreshInterval;
        }

        private void OnDestroy()
        {
            RemoveEvents();
        }

        protected override void HandleViewArgs(object[] args)
        {
            targetCoinOverride = 0f;

            if (args == null)
                return;

            foreach (object arg in args)
            {
                if (arg is float floatValue)
                    targetCoinOverride = floatValue;
                else if (arg is int intValue)
                    targetCoinOverride = intValue;
                else if (arg is double doubleValue)
                    targetCoinOverride = (float)doubleValue;
            }
        }

        private void ResolveReferencesIfNeeded()
        {
            if (isInitialized)
                return;

            progress = FindComponent<Image>("Progress");
            fill = FindComponent<Image>("Fill");
            overallCount = FindComponent<Text>("OverallCount");
            count = FindComponent<Text>("Count");
            progressRoot = FindComponent<RectTransform>("ProgressRoot");
            clickBtn = FindComponent<Button>("ClickBtn");
            close = FindComponent<Button>("close");

            isInitialized = progress != null
                || fill != null
                || overallCount != null
                || count != null
                || clickBtn != null;
        }

        private T FindComponent<T>(string childName) where T : Component
        {
            GameObject target = FindChildUtility.FindChild(gameObject, childName);
            return target != null ? target.GetComponent<T>() : null;
        }

        private void BindEvents()
        {
            if (isEventsBound)
                return;

            clickBtn?.onClick.AddListener(OnClick);
            close?.onClick.AddListener(OnClick);
            isEventsBound = true;
        }

        private void RemoveEvents()
        {
            if (!isEventsBound)
                return;

            clickBtn?.onClick.RemoveListener(OnClick);
            close?.onClick.RemoveListener(OnClick);
            isEventsBound = false;
        }

        private void OnClick()
        {
            UIManager.instance?.CloseView(EUIType.BloatProgressView);
        }

        private void RefreshView()
        {
            if (progressRoot != null && !progressRoot.gameObject.activeSelf)
                progressRoot.gameObject.SetActive(true);

            GameManagerWZ gm = GameManagerWZ.instance;
            float currentCoin = gm != null ? Mathf.Max(gm.currentCoin, 0f) : 0f;
            float targetCoin = GetTargetCoin(gm);
            float progressValue = Mathf.Clamp01(currentCoin / Mathf.Max(targetCoin, 1f));

            if (progress != null)
                progress.fillAmount = progressValue;

            if (fill != null)
                fill.fillAmount = progressValue;

            if (overallCount != null)
                overallCount.text = $"{Mathf.RoundToInt(progressValue * 100f)}%";

            if (count != null)
                count.text = $"{FormatCoin(currentCoin)}/{FormatCoin(targetCoin)}";
        }

        private float GetTargetCoin(GameManagerWZ gm)
        {
            if (targetCoinOverride > 0f)
                return targetCoinOverride;

            if (gm != null
                && gm.TryGetStageRequirement(Stage5Id, out int stageType, out int stageTarget)
                && stageType == 2
                && stageTarget > 0)
            {
                return stageTarget;
            }

            return GameManagerWZ.Stage5CompensationTargetCoin;
        }

        private string FormatCoin(float coin)
        {
            if (FacadePayTypeExtend.RegionalChangeHandle != null)
                return FacadePayTypeExtend.RegionalChangeHandle(coin);

            GameManagerWZ gm = GameManagerWZ.instance;
            string mark = gm != null ? gm.mark : "$";
            int decimals = gm != null ? Mathf.Max(gm.decimals, 0) : 2;
            return $"{mark}{coin.ToString($"F{decimals}")}";
        }
    }
}
