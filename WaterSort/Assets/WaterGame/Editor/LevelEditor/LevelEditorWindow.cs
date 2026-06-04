using System.Collections.Generic;
using System.IO;
using AsGame.Core;
using AsGame.Data;
using UnityEditor;
using UnityEngine;

namespace AsGame.Editor.LevelEditor
{
    public class LevelEditorWindow : EditorWindow
    {
        const float CupPreviewW = GameConstants.BottleWidth;
        const float CupPreviewH = GameConstants.BottleHeight;
        const float PreviewMinHeight = 360f;

        int _levelIndex = 1;
        List<CupData> _cups = new();
        int _selectedCup = -1;
        Vector2 _scroll;
        bool _showLayoutSettings = true;
        bool _scenePreview;
        string _status = "";

        [SerializeField] bool _snapGrid = true;
        [SerializeField] float _snapSize = 10f;
        [SerializeField] bool _clampToPlayArea = true;

        readonly LevelEditorUndoStack _undo = new();
        GameplayScreenLayoutData _layout = new();

        int _dragCup = -1;
        bool _dragUndoRecorded;

        public static void ShowWindow()
        {
            var w = GetWindow<LevelEditorWindow>("水排序关卡");
            w.minSize = new Vector2(480, 720);
            w.Show();
        }

        void OnEnable()
        {
            var settings = LevelEditorLayoutSettings.Instance;
            _layout = settings.layout.Clone();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SaveLayoutSettings();
        }

        void SaveLayoutSettings()
        {
            var settings = LevelEditorLayoutSettings.Instance;
            settings.layout = _layout.Clone();
            settings.Save();
        }

        void OnGUI()
        {
            DrawLayoutSettingsPanel();
            EditorGUILayout.Space(4);
            DrawToolbar();
            EditorGUILayout.Space(4);
            DrawPlayAreaPreview();
            EditorGUILayout.Space(4);
            DrawLevelMeta();
            EditorGUILayout.Space(4);
            DrawCupList();
            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, MessageType.Info);

            HandleUndoRedoHotkeys();
        }

        void DrawLayoutSettingsPanel()
        {
            _showLayoutSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showLayoutSettings, "屏幕与局内区域（设计分辨率）");
            if (_showLayoutSettings)
            {
                EditorGUI.BeginChangeCheck();
                _layout.designWidth = Mathf.Max(100f, EditorGUILayout.FloatField("设计宽", _layout.designWidth));
                _layout.designHeight = Mathf.Max(100f, EditorGUILayout.FloatField("设计高", _layout.designHeight));
                EditorGUILayout.LabelField("局内区域边距（相对屏幕边缘，设计像素）", EditorStyles.miniLabel);
                _layout.insetTop = Mathf.Max(0f, EditorGUILayout.FloatField("距顶", _layout.insetTop));
                _layout.insetBottom = Mathf.Max(0f, EditorGUILayout.FloatField("距底", _layout.insetBottom));
                _layout.insetLeft = Mathf.Max(0f, EditorGUILayout.FloatField("距左", _layout.insetLeft));
                _layout.insetRight = Mathf.Max(0f, EditorGUILayout.FloatField("距右", _layout.insetRight));

                if (GUILayout.Button("恢复默认 (1200×2132, 上800/下240/左右0)"))
                {
                    RecordUndo();
                    _layout = GameplayScreenLayout.Default.Clone();
                }

                if (EditorGUI.EndChangeCheck())
                    SaveLayoutSettings();

                var play = GameplayScreenLayout.GetPlayAreaRect(_layout);
                EditorGUILayout.HelpBox(
                    $"局内区域（中心原点）: X [{play.xMin:F0}, {play.xMax:F0}]  Y [{play.yMin:F0}, {play.yMax:F0}]\n" +
                    $"区域高 {play.height:F0}，上方为菜单/口袋，下方为道具栏",
                    MessageType.None);
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("关卡", GUILayout.Width(32));
            _levelIndex = Mathf.Max(1, EditorGUILayout.IntField(_levelIndex, GUILayout.Width(56)));

            if (GUILayout.Button("加载", GUILayout.Width(44))) LoadLevel();
            if (GUILayout.Button("保存", GUILayout.Width(44))) SaveLevel();
            if (GUILayout.Button("新建", GUILayout.Width(44))) NewLevel();
            if (GUILayout.Button("试玩", GUILayout.Width(44))) PlayTestLevel();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!_undo.CanUndo))
            {
                if (GUILayout.Button("撤销 Ctrl+Z", GUILayout.Width(100)))
                    PerformUndo();
            }

            using (new EditorGUI.DisabledScope(!_undo.CanRedo))
            {
                if (GUILayout.Button("重做 Ctrl+Y", GUILayout.Width(100)))
                    PerformRedo();
            }

            if (GUILayout.Button("从 ConfTotal 导入", GUILayout.Width(120)))
                ImportFromConfTotal();

            if (GUILayout.Button("打开 JSON 目录", GUILayout.Width(110)))
            {
                Directory.CreateDirectory(ProjectPaths.LevelsSplitAbsolute);
                EditorUtility.RevealInFinder(ProjectPaths.LevelsSplitAbsolute);
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawPlayAreaPreview()
        {
            EditorGUILayout.LabelField("局内预览（拖拽移动瓶子）", EditorStyles.boldLabel);
            var rect = GUILayoutUtility.GetRect(10, PreviewMinHeight, GUILayout.ExpandWidth(true));
            DrawScreenPreviewGui(rect);

            var e = Event.current;
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                var ui = PreviewToUi(rect, e.mousePosition);
                var hit = HitTestCup(ui);
                if (hit >= 0)
                {
                    _selectedCup = hit;
                    _dragCup = hit;
                    _dragUndoRecorded = false;
                    e.Use();
                    Repaint();
                }
                else
                    _selectedCup = -1;
            }
            else if (e.type == EventType.MouseDrag && _dragCup >= 0 && _dragCup < _cups.Count)
            {
                if (!_dragUndoRecorded)
                {
                    RecordUndo();
                    _dragUndoRecorded = true;
                }

                var ui = PreviewToUi(rect, e.mousePosition);
                ui.y -= GameConstants.HalfBottleHeight;
                if (_snapGrid) ui = Snap(ui);
                if (_clampToPlayArea)
                    ui = GameplayScreenLayout.ClampCupPosition(ui, _layout);
                _cups[_dragCup].position = ui;
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseUp)
            {
                _dragCup = -1;
                _dragUndoRecorded = false;
                e.Use();
                Repaint();
            }
        }

        void DrawScreenPreviewGui(Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;

            var screen = GameplayScreenLayout.GetFullScreenRect(_layout.designWidth, _layout.designHeight);
            var play = GameplayScreenLayout.GetPlayAreaRect(_layout);
            var scale = Mathf.Min(rect.width / _layout.designWidth, rect.height / _layout.designHeight);
            var drawW = _layout.designWidth * scale;
            var drawH = _layout.designHeight * scale;
            var ox = rect.x + (rect.width - drawW) * 0.5f;
            var oy = rect.y + (rect.height - drawH) * 0.5f;

            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.14f, 1f));
            var screenRect = UiRectToGuiRect(screen, ox, oy, scale);
            EditorGUI.DrawRect(screenRect, new Color(0.22f, 0.24f, 0.28f, 1f));

            var topHud = UiRectToGuiRect(
                Rect.MinMaxRect(screen.xMin, play.yMax, screen.xMax, screen.yMax), ox, oy, scale);
            var bottomBar = UiRectToGuiRect(
                Rect.MinMaxRect(screen.xMin, screen.yMin, screen.xMax, play.yMin), ox, oy, scale);
            EditorGUI.DrawRect(topHud, new Color(0.28f, 0.32f, 0.38f, 0.55f));
            EditorGUI.DrawRect(bottomBar, new Color(0.28f, 0.30f, 0.36f, 0.55f));

            var playGui = UiRectToGuiRect(play, ox, oy, scale);
            Handles.BeginGUI();
            Handles.color = new Color(0.2f, 0.85f, 0.45f, 0.95f);
            Handles.DrawSolidRectangleWithOutline(playGui, new Color(0.2f, 0.85f, 0.45f, 0.08f), new Color(0.2f, 0.85f, 0.45f, 1f));
            Handles.EndGUI();

            for (var i = 0; i < _cups.Count; i++)
            {
                var cup = _cups[i];
                if (cup.isNull != 0) continue;
                DrawCupInPreview(cup, i == _selectedCup, ox, oy, scale);
            }
        }

        void DrawCupInPreview(CupData cup, bool selected, float ox, float oy, float scale)
        {
            var bottom = cup.position;
            var center = new Vector2(bottom.x, bottom.y + GameConstants.HalfBottleHeight);
            var guiCenter = UiToGui(center, ox, oy, scale);
            var w = CupPreviewW * scale;
            var h = CupPreviewH * scale;
            var r = new Rect(guiCenter.x - w * 0.5f, guiCenter.y - h * 0.5f, w, h);

            var fill = selected ? new Color(0.3f, 0.9f, 0.4f, 0.35f) : new Color(0.35f, 0.55f, 0.9f, 0.28f);
            EditorGUI.DrawRect(r, fill);
            Handles.BeginGUI();
            Handles.color = selected ? Color.green : new Color(0.5f, 0.75f, 1f, 0.9f);
            Handles.DrawWireCube(r.center, new Vector3(r.width, r.height, 0));
            Handles.EndGUI();

            if (cup.colors == null || cup.colors.Count == 0) return;
            var layerH = h / Mathf.Max(4, GameConstants.WaterMaxCount);
            for (var l = 0; l < cup.colors.Count; l++)
            {
                if (!GameConstants.GameColorData.TryGetValue(cup.colors[l], out var pair)) continue;
                var ly = r.yMin + layerH * (l + 0.5f);
                var lr = new Rect(r.xMin + w * 0.1f, ly - layerH * 0.35f, w * 0.8f, layerH * 0.7f);
                EditorGUI.DrawRect(lr, pair.Base);
            }
        }

        Rect UiRectToGuiRect(Rect ui, float ox, float oy, float scale)
        {
            var min = UiToGui(new Vector2(ui.xMin, ui.yMin), ox, oy, scale);
            var max = UiToGui(new Vector2(ui.xMax, ui.yMax), ox, oy, scale);
            return Rect.MinMaxRect(
                Mathf.Min(min.x, max.x),
                Mathf.Min(min.y, max.y),
                Mathf.Max(min.x, max.x),
                Mathf.Max(min.y, max.y));
        }

        Vector2 UiToGui(Vector2 ui, float ox, float oy, float scale)
        {
            var halfW = _layout.designWidth * 0.5f;
            var halfH = _layout.designHeight * 0.5f;
            return new Vector2(
                ox + (ui.x + halfW) * scale,
                oy + (halfH - ui.y) * scale);
        }

        Vector2 PreviewToUi(Rect previewRect, Vector2 mousePos)
        {
            var scale = Mathf.Min(previewRect.width / _layout.designWidth, previewRect.height / _layout.designHeight);
            var drawW = _layout.designWidth * scale;
            var drawH = _layout.designHeight * scale;
            var ox = previewRect.x + (previewRect.width - drawW) * 0.5f;
            var oy = previewRect.y + (previewRect.height - drawH) * 0.5f;
            var halfW = _layout.designWidth * 0.5f;
            var halfH = _layout.designHeight * 0.5f;
            var uiX = (mousePos.x - ox) / scale - halfW;
            var uiY = halfH - (mousePos.y - oy) / scale;
            return new Vector2(uiX, uiY);
        }

        int HitTestCup(Vector2 uiPos)
        {
            var best = -1;
            var bestDist = float.MaxValue;
            for (var i = 0; i < _cups.Count; i++)
            {
                var cup = _cups[i];
                if (cup.isNull != 0) continue;
                var center = new Vector2(cup.position.x, cup.position.y + GameConstants.HalfBottleHeight);
                var d = Vector2.Distance(uiPos, center);
                if (d < Mathf.Max(CupPreviewW, CupPreviewH) * 0.55f && d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }

            return best;
        }

        void DrawLevelMeta()
        {
            var exists = File.Exists(ProjectPaths.GetSplitLevelAbsolute(_levelIndex));
            EditorGUILayout.LabelField("输出", ProjectPaths.GetSplitLevelAssetPath(_levelIndex));
            EditorGUILayout.LabelField("状态", exists ? "文件已存在" : "尚未保存");
            _scenePreview = EditorGUILayout.Toggle("Scene 视图辅助预览", _scenePreview);
            _clampToPlayArea = EditorGUILayout.Toggle("限制在局内区域", _clampToPlayArea);
            _snapGrid = EditorGUILayout.Toggle("吸附网格", _snapGrid);
            if (_snapGrid)
                _snapSize = EditorGUILayout.FloatField("网格步长", _snapSize);
        }

        void DrawCupList()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"瓶子 ({_cups.Count})", EditorStyles.boldLabel);
            if (GUILayout.Button("+ 添加", GUILayout.Width(64)))
                AddCup();
            if (GUILayout.Button("- 删除", GUILayout.Width(64)) && _selectedCup >= 0 && _selectedCup < _cups.Count)
                RemoveSelectedCup();
            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(220));
            for (var i = 0; i < _cups.Count; i++)
                DrawCupInspector(i);
            EditorGUILayout.EndScrollView();
        }

        void DrawCupInspector(int index)
        {
            var cup = _cups[index];
            var isSel = index == _selectedCup;
            var header = $"#{index}  ({cup.position.x:F0}, {cup.position.y:F0})  层:{cup.colors?.Count ?? 0}";
            if (cup.isNull != 0) header += " [空槽]";

            EditorGUILayout.BeginVertical(isSel ? "SelectionRect" : "box");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(isSel ? "●" : "○", GUILayout.Width(22)))
            {
                _selectedCup = index;
                Repaint();
            }

            if (GUILayout.Button(header, EditorStyles.label))
            {
                _selectedCup = index;
                Repaint();
            }

            EditorGUILayout.EndHorizontal();

            if (!isSel)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUI.BeginChangeCheck();
            var pos = EditorGUILayout.Vector2Field("位置 (底边锚点)", cup.position);
            cup.whNums = EditorGUILayout.IntField("问号层数", cup.whNums);
            cup.isVideo = EditorGUILayout.Toggle("广告瓶", cup.isVideo != 0) ? 1 : 0;
            cup.isLock = EditorGUILayout.Toggle("锁瓶", cup.isLock != 0) ? 1 : 0;
            if (cup.isLock != 0)
            {
                cup.lockColor = EditorGUILayout.IntField("lockColor", cup.lockColor);
                cup.lockNums = EditorGUILayout.IntField("lockNums", cup.lockNums);
            }

            cup.isNull = EditorGUILayout.Toggle("空槽", cup.isNull != 0) ? 1 : 0;
            DrawColorLayers(cup);

            if (EditorGUI.EndChangeCheck())
            {
                RecordUndo();
                pos = _snapGrid ? Snap(pos) : pos;
                if (_clampToPlayArea)
                    pos = GameplayScreenLayout.ClampCupPosition(pos, _layout);
                cup.position = pos;
                cup.id = index;
                _cups[index] = cup;
                Repaint();
            }

            EditorGUILayout.EndVertical();
        }

        void DrawColorLayers(CupData cup)
        {
            if (cup.colors == null)
                cup.colors = new List<int>();

            var layerCount = EditorGUILayout.IntSlider("水层数", cup.colors.Count, 0, GameConstants.WaterMaxCount);
            while (cup.colors.Count < layerCount)
                cup.colors.Add(0);
            while (cup.colors.Count > layerCount)
                cup.colors.RemoveAt(cup.colors.Count - 1);

            for (var layer = 0; layer < cup.colors.Count; layer++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"L{layer}", GUILayout.Width(28));
                cup.colors[layer] = EditorGUILayout.IntSlider(cup.colors[layer], 0, 8, GUILayout.Width(160));
                var sw = GUILayoutUtility.GetRect(24, 16, GUILayout.Width(24));
                if (GameConstants.GameColorData.TryGetValue(cup.colors[layer], out var pair))
                    EditorGUI.DrawRect(sw, pair.Base);
                EditorGUILayout.EndHorizontal();
            }
        }

        void OnSceneGUI(SceneView view)
        {
            if (!_scenePreview) return;

            var play = GameplayScreenLayout.GetPlayAreaRect(_layout);
            var corners = new[]
            {
                new Vector3(play.xMin, play.yMin, 0) * 0.01f,
                new Vector3(play.xMax, play.yMin, 0) * 0.01f,
                new Vector3(play.xMax, play.yMax, 0) * 0.01f,
                new Vector3(play.xMin, play.yMax, 0) * 0.01f,
            };
            Handles.color = new Color(0.2f, 0.9f, 0.4f, 0.9f);
            Handles.DrawLine(corners[0], corners[1]);
            Handles.DrawLine(corners[1], corners[2]);
            Handles.DrawLine(corners[2], corners[3]);
            Handles.DrawLine(corners[3], corners[0]);

            for (var i = 0; i < _cups.Count; i++)
            {
                var cup = _cups[i];
                if (cup.isNull != 0) continue;
                var center = new Vector3(cup.position.x, cup.position.y + GameConstants.HalfBottleHeight, 0) * 0.01f;
                var isSel = i == _selectedCup;
                Handles.color = isSel ? Color.green : Color.cyan;
                Handles.DrawWireCube(center, new Vector3(CupPreviewW, CupPreviewH, 1f) * 0.01f);
            }
        }

        void HandleUndoRedoHotkeys()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;
            if (e.control && e.keyCode == KeyCode.Z)
            {
                PerformUndo();
                e.Use();
            }
            else if (e.control && e.keyCode == KeyCode.Y)
            {
                PerformRedo();
                e.Use();
            }
        }

        void RecordUndo() => _undo.Push(_cups, _selectedCup);

        void PerformUndo()
        {
            if (_undo.Undo(_cups, ref _selectedCup))
            {
                _status = "已撤销";
                Repaint();
            }
        }

        void PerformRedo()
        {
            if (_undo.Redo(_cups, ref _selectedCup))
            {
                _status = "已重做";
                Repaint();
            }
        }

        Vector2 Snap(Vector2 v)
        {
            if (_snapSize <= 0.01f) return v;
            return new Vector2(
                Mathf.Round(v.x / _snapSize) * _snapSize,
                Mathf.Round(v.y / _snapSize) * _snapSize);
        }

        void LoadLevel()
        {
            LevelConfigLoader.InvalidateCache();
            if (File.Exists(ProjectPaths.GetSplitLevelAbsolute(_levelIndex)))
                _cups = LevelConfigLoader.LoadSplitLevelFromDisk(_levelIndex);
            else
            {
                var legacy = LevelConfigLoader.LoadLevel(_levelIndex);
                _cups = new List<CupData>();
                for (var i = 0; i < legacy.Count; i++)
                    _cups.Add(legacy[i].Clone());
            }

            ReindexCups();
            _selectedCup = _cups.Count > 0 ? 0 : -1;
            _undo.Clear();
            _status = $"已加载第 {_levelIndex} 关，共 {_cups.Count} 个瓶子。";
            Repaint();
        }

        void SaveLevel()
        {
            ReindexCups();
            if (LevelConfigLoader.SaveSplitLevel(_levelIndex, _cups))
                _status = $"已保存 → {ProjectPaths.GetSplitLevelAssetPath(_levelIndex)}";
            else
                _status = "保存失败，请查看 Console。";
        }

        void NewLevel()
        {
            RecordUndo();
            _cups = new List<CupData>
            {
                new CupData
                {
                    id = 0,
                    position = new Vector2(-120, -194),
                    colors = new List<int> { 1, 1, 1 }
                },
                new CupData
                {
                    id = 1,
                    position = new Vector2(120, -194),
                    colors = new List<int> { 1 }
                }
            };
            _selectedCup = 0;
            _status = "已创建空白模板（2 瓶）。";
            Repaint();
        }

        void AddCup()
        {
            RecordUndo();
            var play = GameplayScreenLayout.GetPlayAreaRect(_layout);
            var pos = new Vector2(play.center.x, play.center.y - GameConstants.HalfBottleHeight);
            _cups.Add(new CupData
            {
                id = _cups.Count,
                position = _clampToPlayArea ? GameplayScreenLayout.ClampCupPosition(pos, _layout) : pos,
                colors = new List<int>()
            });
            _selectedCup = _cups.Count - 1;
            Repaint();
        }

        void RemoveSelectedCup()
        {
            RecordUndo();
            _cups.RemoveAt(_selectedCup);
            ReindexCups();
            _selectedCup = Mathf.Clamp(_selectedCup, 0, _cups.Count - 1);
            if (_cups.Count == 0) _selectedCup = -1;
            Repaint();
        }

        void ImportFromConfTotal()
        {
            var path = ProjectPaths.LevelsConfTotalAbsolute;
            if (!File.Exists(path))
            {
                var abPath = Path.Combine(Application.dataPath, "AssetBundleLocal/Json/Levels/ConfTotal.json");
                if (File.Exists(abPath)) path = abPath;
            }

            if (!File.Exists(path))
            {
                _status = "未找到 ConfTotal.json";
                return;
            }

            var all = LevelConfigLoader.ParseConfTotal(File.ReadAllText(path));
            var key = "level_" + _levelIndex;
            if (!all.TryGetValue(key, out var raw))
            {
                _status = $"ConfTotal 中不存在 {key}";
                return;
            }

            RecordUndo();
            _cups = LevelConfigLoader.MapCupData(raw);
            ReindexCups();
            _selectedCup = 0;
            _status = $"已从 ConfTotal 导入 {key}。";
            Repaint();
        }

        void PlayTestLevel()
        {
            SaveLevel();
            GameSaveData.CurrentLevel = _levelIndex;
            LevelConfigLoader.InvalidateCache();
            EditorApplication.EnterPlaymode();
        }

        void ReindexCups()
        {
            for (var i = 0; i < _cups.Count; i++)
                _cups[i].id = i;
        }
    }
}
