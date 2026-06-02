#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using AsGame.Core;
using AsGame.Water;
using AsGame.Scenes;
using AsGame.UI;

namespace AsGame.Editor
{
    public static class SceneUIPrefabGenerator
    {
        static string PrefabsRoot => ProjectPaths.PrefabsRootAssetPath;

        [MenuItem("AsGame/Generate All UI Prefabs", false, 10)]
        public static void GenerateAll()
        {
            GenerateAllInternal(true);
        }

        public static void GenerateAllFromBatchMode()
        {
            GenerateAllInternal(false);
        }

        static void GenerateAllInternal(bool showDialog)
        {
            EnsureFolder(PrefabsRoot + "/Scenes");
            EnsureFolder(PrefabsRoot + "/Game");
            EnsureFolder(PrefabsRoot + "/UI/Popups");

            BuildLoadingCanvas();
            BuildHomeCanvas();
            BuildGameplayCanvas();
            BuildPopupCanvas();
            BuildBottlePrefab();
            BuildBottleShadowPrefab();
            BuildPocketPrefab();
            SyncPopupPrefabs();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var msg = $"已生成/更新 Prefab：{PrefabsRoot}/\n策划请直接在此目录编辑。";
            Debug.Log("[SceneUIPrefabGenerator] " + msg);
            if (showDialog)
                EditorUtility.DisplayDialog("Generate All UI Prefabs", msg, "OK");
        }

        static void BuildLoadingCanvas()
        {
            const float progressWidth = 490f;
            const float progressHeight = 36f;
            const float progressBarLeft = -245.62f;

            var root = UICanvasFactory.CreateCanvas("LoadingCanvas");
            root.AddComponent<LoadingSceneController>();

            var bg = UICanvasFactory.CreateFullScreenImage(root.transform, "Bg", new Color(0.99f, 0.99f, 0.99f));
            UICanvasFactory.TrySetFullScreenSprite(bg, "Sprites/Loading/bj2", preserveAspect: false);

            var logoGo = new GameObject("Logo", typeof(RectTransform), typeof(Image));
            logoGo.transform.SetParent(root.transform, false);
            var logoRt = logoGo.GetComponent<RectTransform>();
            logoRt.anchorMin = logoRt.anchorMax = new Vector2(0.5f, 0.5f);
            logoRt.pivot = new Vector2(0.5f, 0.5f);
            logoRt.anchoredPosition = new Vector2(0f, 392.745f);
            logoRt.sizeDelta = new Vector2(530f, 305f);
            UICanvasFactory.TrySetSprite(logoGo.GetComponent<Image>(), "Sprites/Loading/logo");

            var progressRoot = new GameObject("ProgressNode", typeof(RectTransform));
            progressRoot.transform.SetParent(root.transform, false);
            var progressRootRt = progressRoot.GetComponent<RectTransform>();
            progressRootRt.anchorMin = progressRootRt.anchorMax = new Vector2(0.5f, 0.5f);
            progressRootRt.pivot = new Vector2(0.5f, 0.5f);
            progressRootRt.anchoredPosition = new Vector2(0f, -485.212f);
            progressRootRt.sizeDelta = new Vector2(496f, 40f);

            CreateBarImage(progressRoot.transform, "Track", "Sprites/Loading/bar_02", Vector2.zero, new Vector2(496f, 40f));

            var fillGo = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(progressRoot.transform, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = fillRt.anchorMax = new Vector2(0.5f, 0.5f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.anchoredPosition = new Vector2(progressBarLeft, 0.5f);
            fillRt.sizeDelta = new Vector2(0f, progressHeight);
            var fillImg = fillGo.GetComponent<Image>();
            var fillSp = GameResourceLoader.LoadSprite("Sprites/Loading/bar_01");
            if (fillSp != null)
            {
                fillImg.sprite = fillSp;
                fillImg.type = fillSp.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
                fillImg.color = Color.white;
            }

            var percent = CreateOutlinedText(progressRoot.transform, "PercentLabel", "0%", 24, Vector2.zero, 4);
            CreateOutlinedText(progressRoot.transform, "StatusLabel", "正在进入游戏中...", 24, new Vector2(0f, 59.667f), 2);

            var ctrl = root.GetComponent<LoadingSceneController>();
            Assign(ctrl, "progressFill", fillRt);
            Assign(ctrl, "percentLabel", percent);

            SaveAndMirror(root, PrefabsRoot + "/Scenes/LoadingCanvas.prefab");
        }

        static void BuildHomeCanvas()
        {
            const float bottomBtnY = -530f;
            var bottomBtnSize = new Vector2(150f, 88f);
            var startBtnSize = new Vector2(380f, 146f);

            var root = UICanvasFactory.CreateCanvas("HomeCanvas");
            root.AddComponent<HomeSceneController>();

            var bg = UICanvasFactory.CreateFullScreenImage(root.transform, "Bg", new Color(0.14f, 0.2f, 0.3f));
            UICanvasFactory.TrySetFullScreenSprite(bg, "Sprites/Home/bg_home");
            if (bg.sprite == null)
                UICanvasFactory.TrySetFullScreenSprite(bg, "Sprites/Loading/bj2");
            bg.raycastTarget = false;

            var logoGo = new GameObject("Logo", typeof(RectTransform), typeof(Image));
            logoGo.transform.SetParent(root.transform, false);
            var logoRt = logoGo.GetComponent<RectTransform>();
            logoRt.anchoredPosition = new Vector2(0, 360);
            logoRt.sizeDelta = new Vector2(464, 86);
            var logoSp = GameResourceLoader.LoadSprite("Sprites/Home/logo_home");
            if (logoSp != null)
            {
                var logoImg = logoGo.GetComponent<Image>();
                logoImg.sprite = logoSp;
                logoImg.SetNativeSize();
                logoImg.raycastTarget = false;
            }

            var levelLabel = UICanvasFactory.CreateText(root.transform, "LevelLabel", "第 1 关", 34, new Vector2(0, -206));
            levelLabel.raycastTarget = false;

            var btnStart = CreateHomeButton(root.transform, "Btn_StartGame", "开始游戏", new Vector2(0, -363), startBtnSize, true);
            var btnSettings = CreateHomeButton(root.transform, "Btn_Settings", "设置", new Vector2(-281, bottomBtnY), bottomBtnSize, false);
            var btnRank = CreateHomeButton(root.transform, "Btn_Leaderboard", "排行榜", new Vector2(-94, bottomBtnY), bottomBtnSize, false);
            var btnCollect = CreateHomeButton(root.transform, "Btn_Collection", "图鉴", new Vector2(94, bottomBtnY), bottomBtnSize, false);
            var btnFeedback = CreateHomeButton(root.transform, "Btn_Feedback", "反馈", new Vector2(281, bottomBtnY), bottomBtnSize, false);

            var ctrl = root.GetComponent<HomeSceneController>();
            Assign(ctrl, "levelLabel", levelLabel);
            Assign(ctrl, "btnStartGame", btnStart);
            Assign(ctrl, "btnSettings", btnSettings);
            Assign(ctrl, "btnLeaderboard", btnRank);
            Assign(ctrl, "btnCollection", btnCollect);
            Assign(ctrl, "btnFeedback", btnFeedback);
            Assign(ctrl, "startButtonPulseTarget", btnStart.transform);

            SaveAndMirror(root, PrefabsRoot + "/Scenes/HomeCanvas.prefab");
        }

        static void BuildGameplayCanvas()
        {
            var root = UICanvasFactory.CreateCanvas("GameplayCanvas");
            var game = root.AddComponent<GameController>();

            var bg = UICanvasFactory.CreateFullScreenImage(root.transform, "Bg", new Color(0.15f, 0.2f, 0.28f));
            UICanvasFactory.TrySetFullScreenSprite(bg, "Sprites/Water/game_bg_2");
            if (bg.sprite == null)
                UICanvasFactory.TrySetFullScreenSprite(bg, "Sprites/Home/bg_home");

            var content = CreateStretchChild(root.transform, "Content");
            CreateBench(content.transform);

            var shadowMgr = CreateCenterChild(content.transform, "ShadowMgr", new Vector2(750f, 1334f), Vector2.zero);
            var cupMgr = CreateCenterChild(content.transform, "CupMgr", new Vector2(750f, 1334f), Vector2.zero);
            var pocketMgr = CreateCenterChild(content.transform, "PocketMgr", new Vector2(750f, 300f), new Vector2(0f, 396.552f));
            var pocketLayout = CreateCenterChild(pocketMgr.transform, "Layout", new Vector2(630f, 200f), new Vector2(0f, -60.043f));

            var hudGo = new GameObject("HUD", typeof(RectTransform), typeof(GameHudController));
            hudGo.transform.SetParent(root.transform, false);
            StretchFull(hudGo.GetComponent<RectTransform>());
            BuildGameHud(hudGo.GetComponent<GameHudController>());

            Assign(game, "cupRoot", cupMgr.GetComponent<RectTransform>());
            Assign(game, "shadowRoot", shadowMgr.GetComponent<RectTransform>());
            Assign(game, "pocketRoot", pocketLayout.GetComponent<RectTransform>());
            Assign(game, "contentRoot", content.GetComponent<RectTransform>());
            Assign(game, "hud", hudGo.GetComponent<GameHudController>());

            SaveAndMirror(root, PrefabsRoot + "/Scenes/GameplayCanvas.prefab");
        }

        static void BuildGameHud(GameHudController hud)
        {
            var levelBanner = CreateHudImage(hud.transform, "LevelBanner", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -111f), new Vector2(314f, 98f));
            ApplyBannerSprite(levelBanner);

            var levelLabel = CreateHudText(levelBanner.transform, "LevelLabel", "第 1 关", Vector2.zero, 32);
            StretchFull(levelLabel.rectTransform);
            GameHudController.ApplyLevelLabelStyle(levelLabel);

            var btnSettingGo = CreateSettingsButton(hud.transform);

            var shuffle = BuildPropButton(hud.transform, "Prop_Shuffle", GameProp.Shuffle, "打乱", new Vector2(-240f, 75f));
            var undo = BuildPropButton(hud.transform, "Prop_Undo", GameProp.Undo, "撤回", new Vector2(0f, 75f));
            var addBottle = BuildPropButton(hud.transform, "Prop_AddBottle", GameProp.AddBottle, "增加瓶子", new Vector2(240f, 75f));

            var shuffleTip = new GameObject("ShuffleTip", typeof(RectTransform), typeof(Image));
            shuffleTip.transform.SetParent(hud.transform, false);
            var tipRt = shuffleTip.GetComponent<RectTransform>();
            tipRt.anchorMin = tipRt.anchorMax = new Vector2(0.5f, 1f);
            tipRt.pivot = new Vector2(0.5f, 1f);
            tipRt.anchoredPosition = new Vector2(0f, -402f);
            tipRt.sizeDelta = new Vector2(500f, 80f);
            shuffleTip.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
            var tipTxt = CreateHudText(shuffleTip.transform, "TipLabel", "点击要打乱的瓶子", Vector2.zero, 24);
            StretchFull(tipTxt.rectTransform);
            var btnDone = CreateHudButton(shuffleTip.transform, "Btn_Done", new Vector2(180f, 0f), new Vector2(100f, 44f),
                new Vector2(0.5f, 0.5f));
            var doneLabel = CreateHudText(btnDone.transform, "Label", "完成", Vector2.zero, 20);
            StretchFull(doneLabel.rectTransform);
            shuffleTip.SetActive(false);

            Assign(hud, "levelLabel", levelLabel);
            Assign(hud, "shuffle", shuffle);
            Assign(hud, "undo", undo);
            Assign(hud, "addBottle", addBottle);
            Assign(hud, "shuffleTip", shuffleTip);
            Assign(hud, "btnSetting", btnSettingGo.GetComponent<Button>());
            Assign(hud, "btnShuffleTipDone", btnDone.GetComponent<Button>());
        }

        static void BuildPopupCanvas()
        {
            var root = UICanvasFactory.CreateCanvas("PopupCanvas");
            root.GetComponent<Canvas>().sortingOrder = 100;
            PopupLayoutHelper.StretchFullScreen(root.GetComponent<RectTransform>());
            SaveAndMirror(root, PrefabsRoot + "/Scenes/PopupCanvas.prefab");
        }

        static void BuildBottlePrefab()
        {
            var temp = new GameObject("BottlePrefabTemp");
            var sprite = GameResourceLoader.LoadSprite("Sprites/Bottle/img_8")
                         ?? GameResourceLoader.LoadSprite("Sprites/Bottle/img_2");
            var bottle = BottleController.CreateLegacy(temp.transform, sprite);
            if (bottle != null)
                SaveAndMirror(bottle.gameObject, PrefabsRoot + "/Game/Bottle.prefab");
            Object.DestroyImmediate(temp);
        }

        static void BuildBottleShadowPrefab()
        {
            var temp = new GameObject("ShadowPrefabTemp");
            var sprite = GameResourceLoader.LoadSprite("Sprites/Bottle/img_9")
                         ?? GameResourceLoader.LoadSprite("Sprites/Bottle/sgsg");
            var shadow = BottleController.CreateShadowLegacy(temp.transform, sprite);
            if (shadow != null)
                SaveAndMirror(shadow.gameObject, PrefabsRoot + "/Game/BottleShadow.prefab");
            Object.DestroyImmediate(temp);
        }

        static void BuildPocketPrefab()
        {
            var temp = new GameObject("PocketPrefabTemp");
            var pocket = PocketController.CreateLegacy(temp.transform, true);
            if (pocket != null)
                SaveAndMirror(pocket.gameObject, PrefabsRoot + "/Game/Pocket.prefab");
            Object.DestroyImmediate(temp);
        }

        static void SyncPopupPrefabs()
        {
            const string srcDir = "Assets/ImportedFromCocos/UI";
            if (!Directory.Exists(srcDir)) return;

            foreach (var file in Directory.GetFiles(srcDir, "*.prefab"))
            {
                var name = Path.GetFileName(file);
                var dst = PrefabsRoot + "/UI/Popups/" + name;
                EnsureFolder(PrefabsRoot + "/UI/Popups");
                AssetDatabase.CopyAsset(NormalizePath(file), dst);
            }
        }

        static Button CreateHomeButton(Transform parent, string objectName, string label, Vector2 pos, Vector2 size, bool isMain)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            var btnPath = isMain ? "Sprites/UI/btn_main" : "Sprites/UI/btn_small";
            var btnSp = GameResourceLoader.LoadSprite(btnPath);
            if (btnSp != null)
            {
                img.sprite = btnSp;
                img.type = Image.Type.Sliced;
                img.color = new Color(1f, 1f, 1f, 0.01f);
            }
            else
                img.color = new Color(0.25f, 0.55f, 0.95f, 0.35f);

            var txt = UICanvasFactory.CreateText(go.transform, "Label", label, isMain ? 32 : 22, Vector2.zero);
            StretchFull(txt.rectTransform);
            txt.raycastTarget = false;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            return btn;
        }

        static PropButtonView BuildPropButton(Transform parent, string objectName, GameProp prop, string label, Vector2 pos)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(PropButtonView));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(200f, 150f);

            var bg = go.GetComponent<Image>();
            var btnSp = GameResourceLoader.LoadSprite("Sprites/UI/btn_prop") ?? GameResourceLoader.LoadSprite("Sprites/UI/btn_small");
            if (btnSp != null)
            {
                bg.sprite = btnSp;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }

            var iconPath = prop switch
            {
                GameProp.Shuffle => "Sprites/UI/icon_shuffle",
                GameProp.Undo => "Sprites/UI/icon_undo",
                _ => "Sprites/UI/icon_add_bottle"
            };
            var iconSp = GameResourceLoader.LoadSprite(iconPath);
            if (iconSp != null)
            {
                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(go.transform, false);
                var irt = iconGo.GetComponent<RectTransform>();
                irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.55f);
                irt.sizeDelta = new Vector2(72, 72);
                iconGo.GetComponent<Image>().sprite = iconSp;
            }

            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(Text));
            nameGo.transform.SetParent(go.transform, false);
            var nameT = nameGo.GetComponent<Text>();
            nameT.text = label;
            nameT.fontSize = 20;
            nameT.color = new Color(0.35f, 0.22f, 0.08f);
            nameT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameT.alignment = TextAnchor.LowerCenter;
            var nrt = nameGo.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0, 0);
            nrt.anchorMax = new Vector2(1, 0.45f);
            nrt.offsetMin = nrt.offsetMax = Vector2.zero;

            var countGo = new GameObject("Count", typeof(RectTransform), typeof(Text));
            countGo.transform.SetParent(go.transform, false);
            var countT = countGo.GetComponent<Text>();
            countT.fontSize = 26;
            countT.color = Color.white;
            countT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            countT.alignment = TextAnchor.MiddleCenter;
            countGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -10);

            var addGo = new GameObject("Add", typeof(RectTransform), typeof(Text));
            addGo.transform.SetParent(go.transform, false);
            var addT = addGo.GetComponent<Text>();
            addT.text = "+";
            addT.fontSize = 30;
            addT.color = Color.yellow;
            addT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var art = addGo.GetComponent<RectTransform>();
            art.anchorMin = art.anchorMax = new Vector2(1, 0);
            art.anchoredPosition = new Vector2(-20, 20);
            art.sizeDelta = new Vector2(40, 40);

            go.GetComponent<Button>().transition = Selectable.Transition.None;

            var view = go.GetComponent<PropButtonView>();
            AssignEnum(view, "prop", prop);
            Assign(view, "countLabel", countT);
            Assign(view, "addBadge", addGo);
            return view;
        }

        static void CreateBench(Transform parent)
        {
            var go = new GameObject("Bench", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(750f, 197f);
            rt.anchoredPosition = new Vector2(0f, -374f);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            UICanvasFactory.TrySetFullScreenSprite(img, "Sprites/Water/bj_3");
        }

        static GameObject CreateStretchChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            StretchFull(go.GetComponent<RectTransform>());
            return go;
        }

        static GameObject CreateCenterChild(Transform parent, string name, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return go;
        }

        static void ApplyBannerSprite(Image banner)
        {
            var bannerSp = GameResourceLoader.LoadSprite("Sprites/UI/img_01")
                           ?? GameResourceLoader.LoadSprite("Sprites/UI/frame_prop");
            if (bannerSp == null) return;
            banner.sprite = bannerSp;
            banner.type = bannerSp.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            banner.color = Color.white;
        }

        static GameObject CreateSettingsButton(Transform parent)
        {
            var go = new GameObject("Btn_Settings", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(86f, -112f);
            rt.sizeDelta = new Vector2(94f, 88f);

            var img = go.GetComponent<Image>();
            var iconSp = GameResourceLoader.LoadSprite("Sprites/UI/icon_2")
                         ?? GameResourceLoader.LoadSprite("Sprites/UI/btn_icon");
            if (iconSp != null)
            {
                img.sprite = iconSp;
                img.color = Color.white;
                img.SetNativeSize();
            }

            var wordGo = new GameObject("Word", typeof(RectTransform), typeof(Image));
            wordGo.transform.SetParent(go.transform, false);
            var wordRt = wordGo.GetComponent<RectTransform>();
            wordRt.anchorMin = wordRt.anchorMax = new Vector2(0.5f, 0.5f);
            wordRt.anchoredPosition = new Vector2(0f, -46f);
            wordRt.sizeDelta = new Vector2(63f, 37f);
            var wordSp = GameResourceLoader.LoadSprite("Sprites/UI/word_3");
            if (wordSp != null)
            {
                var wordImg = wordGo.GetComponent<Image>();
                wordImg.sprite = wordSp;
                wordImg.raycastTarget = false;
                wordImg.SetNativeSize();
            }

            return go;
        }

        static GameObject CreateHudButton(Transform parent, string name, Vector2 pos, Vector2 size, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            var sp = GameResourceLoader.LoadSprite("Sprites/UI/btn_small");
            if (sp != null)
            {
                img.sprite = sp;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            return go;
        }

        static Image CreateHudImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go.GetComponent<Image>();
        }

        static Text CreateHudText(Transform parent, string name, string text, Vector2 pos, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(300f, 50f);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.fontSize = size;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return t;
        }

        static GameObject CreateBarImage(Transform parent, string name, string spritePath, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            UICanvasFactory.TrySetSprite(go.GetComponent<Image>(), spritePath);
            return go;
        }

        static Text CreateOutlinedText(Transform parent, string name, string content, int size, Vector2 pos, float outlineWidth)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(320f, 40f);
            var t = go.GetComponent<Text>();
            t.text = content;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.45f);
            outline.effectDistance = new Vector2(outlineWidth, -outlineWidth);
            return t;
        }

        static void SaveAndMirror(GameObject root, string prefabAssetPath)
        {
            EnsureFolder(Path.GetDirectoryName(prefabAssetPath)?.Replace('\\', '/'));
            PrefabUtility.SaveAsPrefabAsset(root, prefabAssetPath, out var success);
            if (!success)
                Debug.LogError("[SceneUIPrefabGenerator] Save failed: " + prefabAssetPath);
            else
                Debug.Log("[SceneUIPrefabGenerator] Saved " + prefabAssetPath);

            Object.DestroyImmediate(root);
        }

        static void Assign(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[SceneUIPrefabGenerator] Field not found: {target.GetType().Name}.{fieldName}");
                return;
            }

            if (prop.propertyType == SerializedPropertyType.ObjectReference)
                prop.objectReferenceValue = value;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignEnum(UnityEngine.Object target, string fieldName, System.Enum value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[SceneUIPrefabGenerator] Enum field not found: {target.GetType().Name}.{fieldName}");
                return;
            }

            prop.intValue = System.Convert.ToInt32(value);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        static string NormalizePath(string path) => path.Replace('\\', '/');
    }
}
#endif
