using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace WZSDK
{
    public class NewGamePlay : BaseView
    {
        public Image index;
        public Text dec;
        public Button Continue;
        private bool _isEventsBound;
        public override void InitView()
        {

        }

        public override void Start()
        {
            BindEvents();
        }

        private void BindEvents()
        {
            if (_isEventsBound) return;

            Continue.onClick.AddListener(OnContinueClick);
            _isEventsBound = true;
        }

        private void RemoveEvents()
        {
            Continue.onClick.RemoveAllListeners();
            _isEventsBound = false;
        }

        private void OnContinueClick()
        {
            SoundManager.Instance?.PlayUIClickSFX();
            UIManager.instance.CloseView(EUIType.NewGamePlay);
            GameManagerWZ.instance.levelGen.ExecuteStageSettings();
        }

        protected override void HandleViewArgs(object[] args)
        {
            if (args != null && args.Length > 0 && args[0] is int type)
            {
                SetContent(type);
            }
        }

        private void SetContent(int type)
        {
            string spritePath = "";
            string textKey = "";

            switch (type)
            {
                case 1: spritePath = "Game/New GamePlay/index1"; textKey = "1023"; break;
                case 2: spritePath = "Game/New GamePlay/index2"; textKey = "1024"; break;
                case 3: spritePath = "Game/New GamePlay/index3"; textKey = "1025"; break;
                case 4: spritePath = "Game/New GamePlay/index4"; textKey = "1026"; break;
            }

            if (!string.IsNullOrEmpty(spritePath) && index != null)
                index.sprite = Resources.Load<Sprite>(spritePath);
            index.SetNativeSize();

            if (!string.IsNullOrEmpty(textKey) && dec != null)
                dec.text = LocalizationManager.Instance.GetText(textKey);
        }

        private void OnDestroy()
        {
            if (_isEventsBound) RemoveEvents();
        }

        public override void Update() { }
    }
}
