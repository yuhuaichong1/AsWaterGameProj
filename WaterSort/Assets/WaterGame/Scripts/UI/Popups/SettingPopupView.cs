using System;
using UnityEngine;
using UnityEngine.UI;
using AsGame.Core;
using AsGame.Data;
using AsGame.Events;
using AsGame.Water;
using AsGame.Scenes;
using AsGame.UI.PrefabGen;

namespace AsGame.UI.Popups
{
    [UIPrefabAsset("Assets/WaterGame/Resources/Prefabs/UI/Popups/SettingPopup.prefab")]
    public class SettingPopupView : BasePopupView, IUIPrefabBlueprint
    {
        const float BtnWidth = 284f;
        const float BtnHeight = 106f;

        [SerializeField] Button btnContinue;
        [SerializeField] Button btnRestart;
        [SerializeField] Button btnHome;
        [SerializeField] Button btnClose;

        public override void Setup(PopupContext ctx, RectTransform content, CanvasGroup blocker)
        {
            base.Setup(ctx, content, blocker);
            var inGame = SceneFlowManager.CurrentScene == SceneId.Game;
            if (content.childCount > 0)
                WirePrefabUi(content, inGame);
            else
                BuildLayout(content, inGame, bindListeners: true);
        }

        void WirePrefabUi(RectTransform content, bool inGame)
        {
            ApplyCocosLayout(content);

            SetActive(content, "btnClose", !inGame);
            SetActive(content, "info", !inGame);
            SetActive(content, "btnHome", inGame);
            SetActive(content, "btnContinue", inGame);
            SetActive(content, "btnRestart", inGame);

            var gm = transform.Find("gm");
            if (gm != null) gm.gameObject.SetActive(false);

            var heartItem = transform.Find("HeartItem");
            if (heartItem != null)
            {
                heartItem.gameObject.SetActive(inGame);
                if (inGame) RefreshHeartItem(heartItem);
            }

            WireSwitch(content, "musicNode", GameSaveData.Bgm, ToggleMusic);
            WireSwitch(content, "soundNode", GameSaveData.Sfx, v => GameSaveData.Sfx = v);
            WireSwitch(content, "vibrationNode", GameSaveData.Vibration, v => GameSaveData.Vibration = v);

            BindButton(content, "btnClose", Close);
            BindButton(content, "btnContinue", Close);
            BindButton(content, "btnHome", () =>
            {
                Close();
                SceneFlowManager.LoadScene(SceneId.Home);
            });
            BindButton(content, "btnRestart", OnRestart);
        }

        static void ApplyCocosLayout(RectTransform content)
        {
            SetRect(content, "frame", new Vector2(604f, 554f), new Vector2(0f, 162.668f));
            SetRect(content, "frame_3", new Vector2(516f, 382f), new Vector2(0f, 143.958f));
            SetRect(content, "frame_4", new Vector2(471f, 147f), new Vector2(0f, 432.421f));

            SetRect(content, "musicNode", new Vector2(500f, 70f), new Vector2(0f, 32.292f));
            SetRect(content, "soundNode", new Vector2(500f, 70f), new Vector2(0f, 142.62f));
            SetRect(content, "vibrationNode", new Vector2(500f, 70f), new Vector2(0f, 250.851f));

            FixToggleLabel(content, "musicNode");
            FixToggleLabel(content, "soundNode");
            FixToggleLabel(content, "vibrationNode");

            SetRect(content, "btnHome", new Vector2(BtnWidth, BtnHeight), new Vector2(0f, -195.112f));
            SetRect(content, "btnRestart", new Vector2(BtnWidth, BtnHeight), new Vector2(0f, -321.295f));
            SetRect(content, "btnContinue", new Vector2(BtnWidth, BtnHeight), new Vector2(0f, -451.742f));

            FixActionButtonLabel(content, "btnHome");
            FixActionButtonLabel(content, "btnContinue");
            FixRestartButtonLabel(content);
        }

        static void SetRect(Transform root, string path, Vector2 size, Vector2 anchoredPos)
        {
            var rt = root.Find(path) as RectTransform;
            if (rt == null) return;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            var img = rt.GetComponent<Image>();
            if (img != null && img.sprite != null)
                img.type = Image.Type.Sliced;
        }

        static void FixToggleLabel(Transform root, string nodeName)
        {
            var label = root.Find(nodeName + "/label") as RectTransform;
            if (label == null) return;
            label.anchorMin = label.anchorMax = new Vector2(0.5f, 0.5f);
            label.sizeDelta = new Vector2(90f, 57f);
            label.anchoredPosition = new Vector2(-136.783f, 0f);
            var text = label.GetComponent<Text>();
            if (text != null)
            {
                text.alignment = TextAnchor.MiddleCenter;
                text.color = new Color(0.525f, 0.337f, 0.275f);
            }
        }

        static void FixActionButtonLabel(Transform root, string buttonName)
        {
            var label = root.Find(buttonName + "/label") as RectTransform;
            if (label == null) return;
            label.anchorMin = Vector2.zero;
            label.anchorMax = Vector2.one;
            label.offsetMin = label.offsetMax = Vector2.zero;
            var text = label.GetComponent<Text>();
            if (text != null)
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        static void FixRestartButtonLabel(Transform root)
        {
            var label = root.Find("btnRestart/label") as RectTransform;
            if (label == null) return;
            label.anchorMin = label.anchorMax = new Vector2(0.5f, 0.5f);
            label.sizeDelta = new Vector2(176f, 61f);
            label.anchoredPosition = new Vector2(34.773f, 0f);
            var text = label.GetComponent<Text>();
            if (text == null) return;
            text.text = "重新开始";
            text.fontSize = 42;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        static void WireSwitch(Transform root, string nodeName, bool initial, Action<bool> onChanged)
        {
            var node = root.Find(nodeName);
            if (node == null) return;

            SetSwitchStatus(node, initial);

            var btn = node.Find("btn");
            if (btn == null) return;

            var button = btn.GetComponent<Button>() ?? btn.gameObject.AddComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                var next = !GetSwitchStatus(node);
                onChanged?.Invoke(next);
                SetSwitchStatus(node, next);
            });
        }

        static bool GetSwitchStatus(Transform node)
        {
            var open = node.Find("open");
            return open == null || open.gameObject.activeSelf;
        }

        static void SetSwitchStatus(Transform node, bool on)
        {
            var btn = node.Find("btn") as RectTransform;
            var open = node.Find("open");
            var close = node.Find("close");
            if (btn != null)
            {
                var pos = btn.anchoredPosition;
                pos.x = on ? 170f : 60f;
                btn.anchoredPosition = pos;
            }

            if (open != null) open.gameObject.SetActive(on);
            if (close != null) close.gameObject.SetActive(!on);
        }

        static void RefreshHeartItem(Transform heartItem)
        {
            var num = heartItem.Find("img/num")?.GetComponent<Text>();
            if (num != null) num.text = GameSaveData.Heart.ToString();

            var down = heartItem.Find("down")?.GetComponent<Text>();
            if (down != null) down.text = "-1";
        }

        static void BindButton(Transform root, string path, Action onClick)
        {
            var btn = root.Find(path)?.GetComponent<Button>();
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClick?.Invoke());
        }

        static void SetActive(Transform root, string path, bool active)
        {
            var node = root.Find(path);
            if (node != null) node.gameObject.SetActive(active);
        }

        static void ToggleMusic(bool on)
        {
            GameSaveData.Bgm = on;
            if (!on) AudioManager.Instance?.StopBgm();
            else AudioManager.Instance?.PlayBgm();
        }

        public void BuildPrefabUI(UIPrefabBuildContext context) =>
            BuildLayout(context.Content, inGame: true, bindListeners: false);

        void BuildLayout(RectTransform content, bool inGame, bool bindListeners)
        {
            UIFactory.CreateLabel(content, "设置", 40, new Vector2(0, 300));

            var music = UIFactory.CreateToggle(content, "音乐", new Vector2(0, 180), GameSaveData.Bgm);
            var sfx = UIFactory.CreateToggle(content, "音效", new Vector2(0, 110), GameSaveData.Sfx);
            var vib = UIFactory.CreateToggle(content, "震动", new Vector2(0, 40), GameSaveData.Vibration);

            if (bindListeners)
            {
                music.onValueChanged.AddListener(v => ToggleMusic(v));
                sfx.onValueChanged.AddListener(v => GameSaveData.Sfx = v);
                vib.onValueChanged.AddListener(v => GameSaveData.Vibration = v);
            }

            if (inGame)
            {
                var cont = UIFactory.CreateButton(content, "继续游戏", new Vector2(0, -452), new Vector2(BtnWidth, BtnHeight));
                var restart = UIFactory.CreateButton(content, "重新开始", new Vector2(0, -321), new Vector2(BtnWidth, BtnHeight));
                var home = UIFactory.CreateButton(content, "返回主页", new Vector2(0, -195), new Vector2(BtnWidth, BtnHeight));
                if (bindListeners)
                {
                    cont.onClick.AddListener(Close);
                    restart.onClick.AddListener(OnRestart);
                    home.onClick.AddListener(() =>
                    {
                        Close();
                        SceneFlowManager.LoadScene(SceneId.Home);
                    });
                }
            }
            else
            {
                var close = UIFactory.CreateButton(content, "关闭", new Vector2(0, -220), new Vector2(260, 56));
                if (bindListeners)
                    close.onClick.AddListener(Close);
            }
        }

        void OnRestart()
        {
            if (GameSaveData.UseHeart())
            {
                EventBus.Publish(GameEvents.Restart);
                Close();
            }
            else
            {
                PopupManager.Ensure();
                PopupManager.Instance.ShowAtOnce(new PopupContext { Type = PopupType.RecoverHeart });
                Close();
            }
        }
    }
}
