using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace WZSDK
{
    public class WithdrawView : BaseView
    {
        public Text curGem;//当前钻石
        public Text CashCount;//当前钻石能兑换多少钱   1个钻石1美金
        public Button WithdrawBtn;//确定兑换按钮
        public Button ExitBtn;//退出按钮
        public RectTransform WTGroup;//下面有四个toggle来选择对应兑现的货币  第一个500个钻石能兑换500美金  第二个1000个钻石能兑换1000美金  第三个1500个钻石能兑换1500美金  第四个2000兑换2000个
        public Slider Process;//进度条：当前钻石到选中档位的进度
        public Text num;//显示当前钻石/选中的金额 (如：100/500)
        public Text Withdrawal;//显示选中的档位金额
        public Text juli;//显示距离选中档位还差多少钻石
        public Text mark;//货币

        [Header("兑换配置")]
        public int[] gemAmounts = new int[] { 500, 1000, 1500, 2000 }; // 钻石消耗
        public float[] cashAmounts = new float[] { 500f, 1000f, 1500f, 2000f }; // 获得金币

        private int currentSelectedIndex = -1;
        private Toggle[] toggles;
        private bool _isEventsBound;
        private GameManagerWZ GM => GameManagerWZ.instance;

        public override void InitView()
        {
            if (WTGroup != null)
            {
                toggles = WTGroup.GetComponentsInChildren<Toggle>();
            }
            if (GM != null)
            {
                RefreshAllData();
            }

            BindEvents();
            RefreshAllData();
        }

        private void BindEvents()
        {
            if (_isEventsBound) RemoveEvents();

            if (toggles != null)
            {
                for (int i = 0; i < toggles.Length && i < gemAmounts.Length; i++)
                {
                    int index = i;
                    toggles[i].onValueChanged.AddListener((isOn) => OnToggleValueChanged(index, isOn));
                    Transform labelTransform = toggles[i].transform.Find("WTTI2Label");
                    if (labelTransform != null)
                    {
                        Text labelText = labelTransform.GetComponent<Text>();
                        if (labelText != null)
                        {
                            labelText.text = GameManagerWZ.instance.mark + gemAmounts[i].ToString();
                        }
                    }
                }
            }

            WithdrawBtn.onClick.AddListener(OnWithdrawBtnClick);
            ExitBtn.onClick.AddListener(ExitBtnClick);

            if (toggles != null && toggles.Length > 0)
            {
                toggles[0].isOn = true;
                OnToggleValueChanged(0, true);
            }
            _isEventsBound = true;
        }

        private void ExitBtnClick()
        {
            UIManager.instance.CloseView(EUIType.WithdrawView);
        }

        private void RemoveEvents()
        {
            if (toggles != null)
            {
                foreach (var toggle in toggles)
                {
                    if (toggle != null)
                        toggle.onValueChanged.RemoveAllListeners();
                }
            }

            WithdrawBtn.onClick.RemoveAllListeners();
            ExitBtn.onClick.RemoveAllListeners();

            _isEventsBound = false;
        }

        private void RefreshAllData()
        {
            if (GM == null) return;

            RefreshGemInfo();
            UpdateCashCount();
            UpdateProcessAndTexts();
        }

        private void RefreshGemInfo()
        {
            if (curGem != null && GM != null)
            {
                curGem.text = GM.currentGems.ToString();//FacadePayTypeExtend.RegionalChangeHandle();
            }
        }

        private void UpdateCashCount()
        {
            if (GM == null || CashCount == null) return;

            float currentGems = GM.currentGems;
            CashCount.text = GameManagerWZ.instance.mark + currentGems;
        }

        private void UpdateProcessAndTexts()
        {
            if (currentSelectedIndex < 0 || currentSelectedIndex >= gemAmounts.Length) return;

            int targetGems = gemAmounts[currentSelectedIndex];
            float currentGems = GM.currentGems;


            if (Withdrawal != null)
            {
                Withdrawal.text = string.Format(LocalizationManager.Instance.GetText("2028"), targetGems);
            }


            if (Process != null)
            {
                float progress = currentGems / targetGems;
                Process.value = Mathf.Clamp01(progress);
            }


            if (num != null)
            {
                num.text = $"{(int)currentGems}/{targetGems}";
            }


            if (juli != null)
            {
                int remainingGems = targetGems - (int)currentGems;
                if (remainingGems <= 0)
                {
                    juli.text = string.Format(LocalizationManager.Instance.GetText("2030"), targetGems, 0);
                }
                else
                {
                    juli.text = string.Format(LocalizationManager.Instance.GetText("2030"), targetGems, remainingGems);
                }
            }
            mark.text = "1=" + GameManagerWZ.instance.mark + "1";
            RefreshGemInfo();
        }

        private void OnToggleValueChanged(int index, bool isOn)
        {
            if (isOn)
            {
                currentSelectedIndex = index;
                UpdateProcessAndTexts();
            }
        }

        private void OnWithdrawBtnClick()
        {
            PlayClick();

            if (currentSelectedIndex < 0)
            {
                WithdrawApiService.LogPaymentNotice("WithdrawView", LocalizationManager.Instance.GetText("2143"), LogType.Warning);
                return;
            }

            int requiredGems = gemAmounts[currentSelectedIndex];
            float currentGems = GM.currentGems;
            if (currentGems < requiredGems)
            {
                WithdrawApiService.LogPaymentNotice("WithdrawView", LocalizationManager.Instance.GetText("2142"), LogType.Warning);
                return;
            }

            GM.SubGem(requiredGems);
            float earnedCash = cashAmounts[currentSelectedIndex];
            GM.AddCoin(earnedCash);

            WithdrawApiService.LogPaymentNotice("WithdrawView", LocalizationManager.Instance.GetText("2058"), LogType.Log);
            RefreshAllData();
            UIManager.instance.CloseView(EUIType.WithdrawView);
        }

        private void PlayClick()
        {
            SoundManager.Instance?.PlayUIClickSFX();
        }

        private void OnDestroy()
        {
            if (_isEventsBound) RemoveEvents();
        }

        public override void Start() { }

        public override void Update() { }
    }
}
