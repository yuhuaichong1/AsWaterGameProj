using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    public class BloatView : BaseView
    {
        private Button clickBtn;
        private Button close;
        private LuckySpinOpenData luckySpinOpenData;
        private bool isInitialized;
        private bool hasClicked;

        protected override void HandleViewArgs(object[] args)
        {
            luckySpinOpenData = null;

            if (args == null)
                return;

            foreach (object arg in args)
            {
                if (arg is LuckySpinOpenData openData)
                {
                    luckySpinOpenData = openData;
                    break;
                }
            }
        }

        public override void InitView()
        {
            InitializeIfNeeded();
            hasClicked = false;
        }

        public override void Start()
        {
            InitializeIfNeeded();
        }

        public override void Update()
        {
        }

        private void InitializeIfNeeded()
        {
            if (isInitialized)
                return;

            clickBtn = FindChildUtility.FindChild(gameObject, "ClickBtn")?.GetComponent<Button>();
            close = FindChildUtility.FindChild(gameObject, "close")?.GetComponent<Button>();
            clickBtn?.onClick.AddListener(OnClick);
            close?.onClick.AddListener(OnClick);

            isInitialized = true;
        }

        private void OnClick()
        {
            if (hasClicked || UIManager.instance == null)
                return;

            hasClicked = true;
            UIManager.instance.CloseView(EUIType.BloatView);

            if (luckySpinOpenData != null)
                UIManager.instance.ShowView(EUIType.BonusWheelView, luckySpinOpenData);
            else
                UIManager.instance.ShowView(EUIType.BonusWheelView);
        }
    }
}
