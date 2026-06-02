using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AsGame.Core;
using AsGame.Data;
using AsGame.Platform;

namespace AsGame.UI.Popups
{
    public class RankPopupView : BasePopupView
    {
        enum RankTab
        {
            Friend = 0,
            Total = 1,
            Province = 2
        }

        const float ItemHeight = 87f;

        RectTransform _content;
        Transform _listContent;
        Transform _meNode;
        Transform _tabNode;
        Transform _rankList;
        Transform _rankNode;
        Transform _friendTip;
        Text _titleUserLabel;
        RankTab _tab = RankTab.Total;
        GameObject _itemPrefab;
        Coroutine _lightSpin;

        public override void Setup(PopupContext ctx, RectTransform content, CanvasGroup blocker)
        {
            base.Setup(ctx, content, blocker);
            if (content.childCount == 0)
            {
                BuildFallback(content);
                return;
            }

            WirePrefab(content, blocker);
        }

        void WirePrefab(RectTransform content, CanvasGroup blocker)
        {
            _content = content;
            EnsureDimmer(blocker);
            ApplyCocosLayout(content);
            HideWechatOnlyNodes(content);

            _tabNode = content.Find("tabNode");
            _rankNode = content.Find("rankNode");
            _rankList = _rankNode != null ? _rankNode.Find("rankList") : null;
            _listContent = _rankList != null ? _rankList.Find("view/content") : null;
            _meNode = _rankNode != null ? _rankNode.Find("me") : null;
            _friendTip = content.Find("autoFriendTip");
            _titleUserLabel = _rankNode?.Find("title/user")?.GetComponent<Text>();

            _itemPrefab = Resources.Load<GameObject>(PrefabPaths.PopupsRoot + "RankItem");

            BindButton(content, "btnClose", Close);
            BindButton(content, "btnShare", OnShare);

            if (_tabNode != null)
            {
                var tabs = new[] { "btnFriends", "btnTotal", "btnProvince" };
                for (var i = 0; i < tabs.Length; i++)
                {
                    var idx = i;
                    var tabBtn = _tabNode.Find(tabs[i]);
                    if (tabBtn == null) continue;
                    var btn = tabBtn.GetComponent<Button>() ?? tabBtn.gameObject.AddComponent<Button>();
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => SwitchTab((RankTab)idx));
                }
            }

            StartLightSpin(content.Find("light"));
            SwitchTab(RankTab.Total);
        }

        static void EnsureDimmer(CanvasGroup blocker)
        {
            if (blocker == null) return;
            var rt = blocker.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            var img = blocker.GetComponent<Image>();
            if (img != null && img.color.a < 0.2f)
                img.color = new Color(0f, 0f, 0f, 0.58f);
        }

        static void ApplyCocosLayout(RectTransform content)
        {
            SetRect(content, new Vector2(750f, 1334f), Vector2.zero);

            SetRect(content, "frame", new Vector2(471f, 147f), new Vector2(0f, 362.307f));
            SetRect(content, "cup", new Vector2(430f, 200f), new Vector2(0f, 420f));
            SetRect(content, "tabNode", new Vector2(516f, 56f), new Vector2(0f, 261.981f));
            SetRect(content, "rankNode", new Vector2(516f, 593f), new Vector2(0f, -97.823f));
            SetRect(content, "btnShare", new Vector2(284f, 106f), new Vector2(0f, -543.678f));
            SetRect(content, "btnClose", new Vector2(61f, 63f), new Vector2(285.546f, 339.784f));
            SetRect(content, "tip", new Vector2(500f, 40f), new Vector2(0f, -460f));

            var tip2 = content.Find("tip2") as RectTransform;
            if (tip2 != null)
                tip2.gameObject.SetActive(false);

            var rankNode = content.Find("rankNode") as RectTransform;
            if (rankNode == null) return;

            SetRect(rankNode, "rankList", new Vector2(500f, 420f), new Vector2(0f, 228f));
            var rankList = rankNode.Find("rankList") as RectTransform;
            if (rankList != null)
            {
                rankList.pivot = new Vector2(0.5f, 1f);
                EnsureScrollList(rankList);
            }

            SetRect(rankNode, "me", new Vector2(488f, 87f), new Vector2(0f, -242.572f));
            SetRect(rankNode, "title", new Vector2(500f, 50f), new Vector2(0f, 261.981f));
        }

        static void EnsureScrollList(RectTransform rankList)
        {
            var view = rankList.Find("view") as RectTransform;
            var listContent = rankList.Find("view/content") as RectTransform;
            if (view == null || listContent == null) return;

            view.sizeDelta = new Vector2(500f, 420f);
            listContent.pivot = new Vector2(0.5f, 1f);
            listContent.anchorMin = new Vector2(0, 1);
            listContent.anchorMax = new Vector2(1, 1);

            if (!rankList.TryGetComponent<ScrollRect>(out var scroll))
                scroll = rankList.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = view;
            scroll.content = listContent;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            if (!view.TryGetComponent<Mask>(out _))
            {
                var maskImg = view.GetComponent<Image>() ?? view.gameObject.AddComponent<Image>();
                maskImg.color = Color.white;
                view.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            }
        }

        static void HideWechatOnlyNodes(RectTransform content)
        {
            SetActive(content, "authUserTip", false);
            SetActive(content, "autoFriendTip", false);
            SetActive(content, "subContext", false);
        }

        void SwitchTab(RankTab tab)
        {
            _tab = tab;
            SetTabVisual(tab);

            var isFriend = tab == RankTab.Friend;
            var isProvince = tab == RankTab.Province;

            if (_titleUserLabel != null)
                _titleUserLabel.text = isProvince ? "省份" : "玩家";

            if (_friendTip != null)
                _friendTip.gameObject.SetActive(false);

            if (_meNode != null)
                _meNode.gameObject.SetActive(!isFriend);

            if (isFriend)
            {
                if (_rankList != null)
                    _rankList.gameObject.SetActive(true);
                ShowFriendPlaceholder();
            }
            else
            {
                PopulateList(isProvince);
                UpdateMeRow(isProvince);
            }
        }

        void SetTabVisual(RankTab tab)
        {
            if (_tabNode == null) return;
            var names = new[] { "btnFriends", "btnTotal", "btnProvince" };
            for (var i = 0; i < names.Length; i++)
            {
                var child = _tabNode.Find(names[i]);
                if (child == null) continue;
                var img = child.GetComponent<Image>();
                if (img != null)
                    img.enabled = (int)tab == i;
            }
        }

        void ShowFriendPlaceholder()
        {
            if (_listContent == null) return;
            ClearList();
            CreatePlaceholderLabel(_listContent,
                "好友榜需平台好友关系。\nGoogle Play 版请查看「总榜」。", new Vector2(0f, -180f));
        }

        void PopulateList(bool province)
        {
            if (_listContent == null) return;
            ClearList();

            if (_itemPrefab == null)
            {
                CreatePlaceholderLabel(_listContent, "排行榜条目资源缺失", Vector2.zero);
                return;
            }

            var rows = province ? BuildProvinceRows() : BuildTotalRows();
            for (var i = 0; i < rows.Count; i++)
                SpawnItem(rows[i], i, province);

            var listRt = _listContent as RectTransform;
            if (listRt != null)
                listRt.sizeDelta = new Vector2(500f, Mathf.Max(420f, rows.Count * ItemHeight));
        }

        void SpawnItem(RankRow row, int index, bool province)
        {
            var go = Instantiate(_itemPrefab, _listContent);
            go.name = "item_" + index;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(488f, ItemHeight);
            rt.anchoredPosition = new Vector2(0f, -index * ItemHeight);

            BindRankItem(go.transform, row, province);
        }

        void UpdateMeRow(bool province)
        {
            if (_meNode == null) return;
            var level = GameSaveData.CurrentLevel;
            var row = province
                ? new RankRow(12, "你的地区", level, true)
                : new RankRow(Mathf.Max(1, 1451 - level * 2), "你", level, false);
            BindRankItem(_meNode, row, province, highlight: true);
        }

        static void BindRankItem(Transform root, RankRow row, bool province, bool highlight = false)
        {
            if (root is RectTransform rt)
            {
                var pos = rt.anchoredPosition;
                rt.sizeDelta = new Vector2(488f, ItemHeight);
                if (!highlight)
                    rt.anchoredPosition = pos;
            }

            SetLabel(root, "rank2/label", row.Rank.ToString());
            if (province)
            {
                SetLabel(root, "name", "");
                SetLabel(root, "passLevel", row.Level + "关");
            }
            else
            {
                SetLabel(root, "name", row.Name);
                SetLabel(root, "passLevel", row.Level + "关");
            }

            var prov = root.Find("province");
            if (prov != null)
            {
                prov.gameObject.SetActive(province);
                if (province) SetLabel(prov, null, row.Name);
            }

            var rank3 = root.Find("rank3");
            if (rank3 != null)
                rank3.gameObject.SetActive(false);

            var rank1 = root.Find("rank1");
            if (rank1 != null)
                rank1.gameObject.SetActive(row.Rank == 1);

            if (highlight)
            {
                var img = root.GetComponent<Image>();
                if (img != null)
                    img.color = new Color(1f, 0.96f, 0.85f, 1f);
            }
        }

        static List<RankRow> BuildTotalRows()
        {
            var level = GameSaveData.CurrentLevel;
            return new List<RankRow>
            {
                new(1, "玩家A", 128),
                new(2, "玩家B", 105),
                new(3, "玩家C", 92),
                new(4, "你", level),
                new(5, "玩家D", 76),
                new(6, "玩家E", 64),
                new(7, "玩家F", 51),
            };
        }

        static List<RankRow> BuildProvinceRows()
        {
            return new List<RankRow>
            {
                new(1, "广东", 982, true),
                new(2, "浙江", 876, true),
                new(3, "江苏", 801, true),
                new(4, "山东", 754, true),
                new(5, "四川", 690, true),
            };
        }

        void ClearList()
        {
            if (_listContent == null) return;
            for (var i = _listContent.childCount - 1; i >= 0; i--)
            {
                var child = _listContent.GetChild(i);
                if (child.name == "content") continue;
                Destroy(child.gameObject);
            }
        }

        static void CreatePlaceholderLabel(Transform parent, string msg, Vector2 pos)
        {
            var go = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(460f, 160f);
            rt.anchoredPosition = pos;
            var t = go.GetComponent<Text>();
            t.text = msg;
            t.font = DefaultFont;
            t.fontSize = 26;
            t.color = new Color(0.45f, 0.28f, 0.18f);
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        void OnShare()
        {
            PlatformService.SendEvent("rank_share");
            ToastService.Show("分享功能可在 Google Play 版接入系统分享");
        }

        void StartLightSpin(Transform light)
        {
            if (light == null) return;
            if (_lightSpin != null) StopCoroutine(_lightSpin);
            _lightSpin = StartCoroutine(SpinLight(light));
        }

        static IEnumerator SpinLight(Transform light)
        {
            while (light != null)
            {
                light.Rotate(0f, 0f, 45f * Time.deltaTime);
                yield return null;
            }
        }

        void OnDestroy()
        {
            if (_lightSpin != null)
                StopCoroutine(_lightSpin);
        }

        void BuildFallback(RectTransform content)
        {
            UIFactory.CreateLabel(content, "排行榜", 40, new Vector2(0, 300));
            UIFactory.CreateButton(content, "关闭", new Vector2(0, -280), new Vector2(240, 56))
                .onClick.AddListener(Close);
        }

        static void BindButton(Transform root, string path, Action onClick)
        {
            var btn = root.Find(path)?.GetComponent<Button>();
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClick?.Invoke());
        }

        static void SetLabel(Transform root, string path, string value)
        {
            Transform node;
            if (string.IsNullOrEmpty(path))
                node = root;
            else
                node = root.Find(path);

            if (node == null) return;
            var t = node.GetComponent<Text>();
            if (t != null) t.text = value;
        }

        static void SetActive(Transform root, string path, bool active)
        {
            var node = root.Find(path);
            if (node != null) node.gameObject.SetActive(active);
        }

        static void SetRect(RectTransform rt, Vector2 size, Vector2 pos)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        static void SetRect(Transform root, string path, Vector2 size, Vector2 pos)
        {
            var rt = root.Find(path) as RectTransform;
            SetRect(rt, size, pos);
        }

        readonly struct RankRow
        {
            public readonly int Rank;
            public readonly string Name;
            public readonly int Level;
            public readonly bool Province;

            public RankRow(int rank, string name, int level, bool province = false)
            {
                Rank = rank;
                Name = name;
                Level = level;
                Province = province;
            }
        }
    }
}
