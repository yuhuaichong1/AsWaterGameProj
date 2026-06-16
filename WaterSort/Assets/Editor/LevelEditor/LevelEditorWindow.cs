using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsGame.Core;
using AsGame.Data;
using UnityEditor;
using UnityEngine;
using XrCode;

namespace AsGame.Editor.LevelEditor
{
    public class LevelEditorWindow : EditorWindow
    {
        static LevelEditorWindow s_FocusedInstance;

        const float PreviewMinHeight = 320f;
        const float LeftPanelMinWidth = 300f;
        const float LeftPanelDefaultWidth = 420f;
        const float RightPanelMinWidth = 300f;
        const float SplitterWidth = 5f;
        const float CupsSectionHeightMin = 120f;
        const float CupsSectionHeightMax = 720f;
        const float CupsSectionResizeGripHeight = 6f;
        const float DefaultBottleWidth = 91f;
        const float DefaultBottleHeight = 245f;
        /// <summary>Scene 视图：1 关卡 UI 单位 = 1 世界单位（勿用 0.01，否则线框过小看不见）。</summary>
        const float SceneWorldScale = 1f;
        const float AlignDashLength = 8f;
        const float AlignDashGap = 5f;

        int _levelIndex = 1;
        List<CupData> _cups = new();
        int _selectedCup = -1;
        Vector2 _leftPanelScroll;
        Vector2 _cupsSectionScroll;
        float _cupsSectionHeight = 360f;
        bool _showLayoutSettings;
        bool _showWaterRandomRules;
        bool _scenePreview;
        string _status = "";
        MessageType _statusLogType = MessageType.Info;
        Vector2 _statusLogScroll;
        List<LevelBatchCheckEntry> _lastBatchCheckEntries;

        [SerializeField] bool _snapGrid = true;
        [SerializeField] bool _clampToPlayArea = true;
        /// <summary>显示 CupMgr 原始 750×1334 区域（灰虚线，仅对照 JSON 坐标系）。</summary>
        [SerializeField] bool _showCupRawReference;

        readonly LevelEditorUndoStack _undo = new();
        GameplayScreenLayoutData _layout = new();
        float _bottleWidth = DefaultBottleWidth;
        float _bottleHeight = DefaultBottleHeight;
        /// <summary>预览网格当前使用的瓶宽/高（设计像素），由「刷新网格」同步自水瓶尺寸设定。</summary>
        float _gridBottleWidth = DefaultBottleWidth;
        float _gridBottleHeight = DefaultBottleHeight;
        int _waterColorCount = 3;
        int _waterTotalLayers = 12;
        int _waterQuestionLayers;
        WaterRefreshDifficulty _waterDifficulty = WaterRefreshDifficulty.超简单;
        LevelWaterDifficultyMetrics _levelDifficultyMetrics;

        float PreviewBottleWidth => Mathf.Max(1f, _bottleWidth);
        float PreviewBottleHeight => Mathf.Max(1f, _bottleHeight);
        float PreviewHalfBottleHeight => PreviewBottleHeight * 0.5f;

        int _dragCup = -1;
        bool _dragUndoRecorded;
        bool _previewPointerDown;
        float _leftPanelWidth = LeftPanelDefaultWidth;
        int _splitterControlId;
        bool _splitterDragging;
        bool _resizingCupsSection;
        float _cupsSectionResizeStartY;
        float _cupsSectionResizeStartHeight;

        public static void ShowWindow()
        {
            var w = GetWindow<LevelEditorWindow>("水排序关卡");
            w.minSize = new Vector2(1000, 640);
            w.Show();
        }

        void OnEnable()
        {
            var settings = LevelEditorLayoutSettings.Instance;
            _layout = settings.layout.Clone();
            _bottleWidth = settings.bottleWidth > 0f ? settings.bottleWidth : DefaultBottleWidth;
            _bottleHeight = settings.bottleHeight > 0f ? settings.bottleHeight : DefaultBottleHeight;
            _waterColorCount = Mathf.Max(1, settings.waterColorCount);
            _waterTotalLayers = Mathf.Max(4, settings.waterTotalLayers);
            _waterQuestionLayers = Mathf.Max(0, settings.waterQuestionLayers);
            _waterDifficulty = (WaterRefreshDifficulty)Mathf.Clamp(settings.waterDifficulty, 0, 4);
            _leftPanelWidth = settings.leftPanelWidth > LeftPanelMinWidth
                ? settings.leftPanelWidth
                : LeftPanelDefaultWidth;
            _cupsSectionHeight = NormalizeCupsSectionHeight(settings.sectionHeightCups);
            ApplyPreviewGridFromBottleSettings();
            SceneView.duringSceneGui -= OnSceneGuiGlobal;
            SceneView.duringSceneGui += OnSceneGuiGlobal;
            s_FocusedInstance = this;
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGuiGlobal;
            if (s_FocusedInstance == this)
                s_FocusedInstance = null;
            SaveLayoutSettings();
            SceneView.RepaintAll();
        }

        void OnFocus() => s_FocusedInstance = this;

        static void OnSceneGuiGlobal(SceneView view)
        {
            if (s_FocusedInstance != null)
                s_FocusedInstance.OnSceneGUI(view);
        }

        void SaveLayoutSettings()
        {
            var settings = LevelEditorLayoutSettings.Instance;
            settings.layout = _layout.Clone();
            settings.bottleWidth = PreviewBottleWidth;
            settings.bottleHeight = PreviewBottleHeight;
            settings.waterColorCount = _waterColorCount;
            settings.waterTotalLayers = _waterTotalLayers;
            settings.waterQuestionLayers = _waterQuestionLayers;
            settings.waterDifficulty = (int)_waterDifficulty;
            settings.leftPanelWidth = _leftPanelWidth;
            settings.sectionHeightCups = _cupsSectionHeight;
            settings.Save();
        }

        static float NormalizeCupsSectionHeight(float value) =>
            Mathf.Clamp(value > 0f ? value : 360f, CupsSectionHeightMin, CupsSectionHeightMax);

        void OnGUI()
        {
            HandleUndoRedoHotkeys();
            ClampLeftPanelWidth();

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
            DrawLeftEditorPanel();
            DrawPanelSplitter();
            DrawRightPreviewPanel();
            EditorGUILayout.EndHorizontal();
        }

        void ClampLeftPanelWidth()
        {
            var maxLeft = position.width - RightPanelMinWidth - SplitterWidth - 8f;
            _leftPanelWidth = Mathf.Clamp(_leftPanelWidth, LeftPanelMinWidth, Mathf.Max(LeftPanelMinWidth, maxLeft));
        }

        void DrawLeftEditorPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(_leftPanelWidth), GUILayout.ExpandHeight(true));
            _leftPanelScroll = EditorGUILayout.BeginScrollView(_leftPanelScroll, GUILayout.ExpandHeight(true));

            DrawLayoutSection();
            DrawLeftFixedBox("关卡操作", DrawLevelOpsSection);
            DrawLeftFixedBox("刷新水层", DrawWaterRefreshPanel);
            DrawResizableCupsSection();
            DrawLeftFixedBox("操作日志", DrawStatusLogSection);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawLeftFixedBox(string title, System.Action drawContent)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            drawContent();
            EditorGUILayout.EndVertical();
            GUILayout.Space(3);
        }

        void DrawLayoutSection()
        {
            EditorGUILayout.BeginVertical("box");
            _showLayoutSettings = EditorGUILayout.BeginFoldoutHeaderGroup(
                _showLayoutSettings, "屏幕与局内区域（设计分辨率）");
            if (_showLayoutSettings)
            {
                DrawLayoutSettingsPanel();
                EditorGUILayout.Space(6);
                DrawEditorOptions();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndVertical();
            GUILayout.Space(3);
        }

        void DrawResizableCupsSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("瓶子", EditorStyles.boldLabel);
            _cupsSectionScroll = EditorGUILayout.BeginScrollView(
                _cupsSectionScroll, GUILayout.Height(_cupsSectionHeight));
            DrawCupList();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            DrawCupsSectionResizeGrip();
            GUILayout.Space(3);
        }

        void DrawCupsSectionResizeGrip()
        {
            var gripRect = GUILayoutUtility.GetRect(0, CupsSectionResizeGripHeight, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(gripRect, new Color(0.32f, 0.32f, 0.32f, 0.85f));

            var centerX = gripRect.center.x;
            for (var i = -2; i <= 2; i++)
            {
                var dot = new Rect(centerX + i * 5f - 1.5f, gripRect.y + gripRect.height * 0.5f - 1.5f, 3f, 3f);
                EditorGUI.DrawRect(dot, new Color(0.55f, 0.55f, 0.55f, 0.9f));
            }

            EditorGUIUtility.AddCursorRect(gripRect, MouseCursor.ResizeVertical);

            const int controlIdHint = 0x4C454355; // "LECU"
            var controlId = GUIUtility.GetControlID(controlIdHint, FocusType.Passive);
            var e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown when gripRect.Contains(e.mousePosition) && e.button == 0:
                    GUIUtility.hotControl = controlId;
                    _resizingCupsSection = true;
                    _cupsSectionResizeStartY = e.mousePosition.y;
                    _cupsSectionResizeStartHeight = _cupsSectionHeight;
                    e.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == controlId && _resizingCupsSection:
                    var delta = e.mousePosition.y - _cupsSectionResizeStartY;
                    _cupsSectionHeight = Mathf.Clamp(
                        _cupsSectionResizeStartHeight + delta, CupsSectionHeightMin, CupsSectionHeightMax);
                    Repaint();
                    e.Use();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == controlId && _resizingCupsSection:
                    GUIUtility.hotControl = 0;
                    _resizingCupsSection = false;
                    SaveLayoutSettings();
                    e.Use();
                    break;
            }
        }

        void DrawLevelOpsSection()
        {
            DrawToolbar();
            EditorGUILayout.Space(6);
            DrawLevelMeta();
        }

        void DrawStatusLogSection()
        {
            if (string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.LabelField("（暂无日志）", EditorStyles.miniLabel);
                return;
            }

            EditorGUILayout.LabelField(GetStatusLogTypeLabel(_statusLogType), EditorStyles.miniBoldLabel);
            _statusLogScroll = EditorGUILayout.BeginScrollView(
                _statusLogScroll, GUILayout.MinHeight(96f), GUILayout.MaxHeight(280f));
            var logStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                richText = false
            };
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextArea(_status, logStyle, GUILayout.ExpandHeight(true));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndScrollView();
        }

        static string GetStatusLogTypeLabel(MessageType type) => type switch
        {
            MessageType.Error => "错误",
            MessageType.Warning => "警告",
            _ => "信息"
        };

        void SetStatusLog(string message, MessageType type = MessageType.Info)
        {
            _status = message ?? "";
            _statusLogType = type;
        }

        void DrawRightPreviewPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawPlayAreaPreview();
            EditorGUILayout.EndVertical();
        }

        void DrawPanelSplitter()
        {
            var splitterRect = GUILayoutUtility.GetRect(
                SplitterWidth, SplitterWidth, GUILayout.ExpandHeight(true), GUILayout.Width(SplitterWidth));
            EditorGUI.DrawRect(splitterRect, new Color(0.18f, 0.18f, 0.18f, 1f));

            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

            var e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown when splitterRect.Contains(e.mousePosition) && e.button == 0:
                    _splitterControlId = GUIUtility.GetControlID(FocusType.Passive);
                    GUIUtility.hotControl = _splitterControlId;
                    _splitterDragging = true;
                    e.Use();
                    break;
                case EventType.MouseDrag when _splitterDragging && GUIUtility.hotControl == _splitterControlId:
                    _leftPanelWidth = Mathf.Clamp(e.mousePosition.x, LeftPanelMinWidth,
                        position.width - RightPanelMinWidth - SplitterWidth);
                    Repaint();
                    e.Use();
                    break;
                case EventType.MouseUp when _splitterDragging && GUIUtility.hotControl == _splitterControlId:
                    GUIUtility.hotControl = 0;
                    _splitterDragging = false;
                    SaveLayoutSettings();
                    e.Use();
                    break;
            }
        }

        void DrawLayoutSettingsPanel()
        {
            EditorGUILayout.LabelField("设计分辨率", EditorStyles.miniBoldLabel);
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("设计宽", GUILayout.Width(44));
            _layout.designWidth = Mathf.Max(100f, EditorGUILayout.FloatField(_layout.designWidth));
            GUILayout.Space(8);
            EditorGUILayout.LabelField("设计高", GUILayout.Width(44));
            _layout.designHeight = Mathf.Max(100f, EditorGUILayout.FloatField(_layout.designHeight));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("局内区域边距（相对屏幕边缘，设计像素）", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("距顶", GUILayout.Width(32));
            _layout.insetTop = Mathf.Max(0f, EditorGUILayout.FloatField(_layout.insetTop));
            EditorGUILayout.LabelField("距底", GUILayout.Width(32));
            _layout.insetBottom = Mathf.Max(0f, EditorGUILayout.FloatField(_layout.insetBottom));
            EditorGUILayout.LabelField("距左", GUILayout.Width(32));
            _layout.insetLeft = Mathf.Max(0f, EditorGUILayout.FloatField(_layout.insetLeft));
            EditorGUILayout.LabelField("距右", GUILayout.Width(32));
            _layout.insetRight = Mathf.Max(0f, EditorGUILayout.FloatField(_layout.insetRight));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("水瓶尺寸（局内预览绘制，绿框内设计像素）", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("宽", GUILayout.Width(32));
            _bottleWidth = Mathf.Max(1f, EditorGUILayout.FloatField(_bottleWidth));
            GUILayout.Space(8);
            EditorGUILayout.LabelField("高", GUILayout.Width(32));
            _bottleHeight = Mathf.Max(1f, EditorGUILayout.FloatField(_bottleHeight));
            EditorGUILayout.EndHorizontal();

            var layoutChanged = false;
            if (GUILayout.Button("恢复默认 (1200×2132, 上800/下240, 瓶91×245)"))
            {
                _layout = GameplayScreenLayout.Default.Clone();
                _bottleWidth = DefaultBottleWidth;
                _bottleHeight = DefaultBottleHeight;
                layoutChanged = true;
            }

            if (EditorGUI.EndChangeCheck())
                layoutChanged = true;

            if (layoutChanged)
            {
                SaveLayoutSettings();
                Repaint();
            }

            var play = GameplayScreenLayout.GetPlayAreaRect(_layout);
            EditorGUILayout.HelpBox(
                $"绿框 = 局内区域（由边距算出）Y [{play.yMin:F0}, {play.yMax:F0}]\n" +
                "JSON 坐标存于 CupMgr 750×1334，预览时自动映射进绿框。\n" +
                "修改「水瓶尺寸」后，请在右侧预览栏点击「刷新网格」更新虚线网格与吸附步长。",
                MessageType.None);
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
            if (GUILayout.Button("检查", GUILayout.Width(44)))
                CheckCurrentLevelWater();
            if (GUILayout.Button("全部检查", GUILayout.Width(72)))
                InspectAllLevelsWater();
            if (GUILayout.Button("自动修复", GUILayout.Width(72)))
                AutoFixAllLevelsWater();
            if (GUILayout.Button("导出报告", GUILayout.Width(72)))
                ExportBatchCheckReport();
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

        void CheckCurrentLevelWater()
        {
            if (_cups == null || _cups.Count == 0)
            {
                SetStatusLog("请先加载或创建关卡后再检查。", MessageType.Warning);
                Repaint();
                return;
            }

            var validation = LevelWaterValidator.ValidateStructure(_cups, _levelIndex);
            var metrics = LevelWaterDifficultyAnalyzer.AnalyzeSolvability(
                _cups, _levelIndex, validation, fastSearch: false);
            RefreshLevelDifficultyMetrics();

            var passed = validation.IsValid && LevelWaterCheckPolicy.IsStepsAcceptable(metrics);
            var report = LevelWaterValidator.FormatReport(validation);
            if (passed)
            {
                SetStatusLog(
                    $"【检查】第 {_levelIndex} 关通过；最少步数 {metrics.MinStepsLabel}（上限 {LevelWaterCheckPolicy.MaxAllowedMinSteps}）。\n\n{report}",
                    MessageType.Info);
            }
            else
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"【检查未通过】第 {_levelIndex} 关（仅检查，未自动修复）");
                if (!validation.IsValid)
                    sb.AppendLine(report);
                else
                {
                    sb.AppendLine(report);
                    sb.AppendLine($"· 最少步数：{metrics.MinStepsLabel}（上限 {LevelWaterCheckPolicy.MaxAllowedMinSteps}）");
                    if (!string.IsNullOrEmpty(metrics.SolveNote))
                        sb.AppendLine("· " + metrics.SolveNote);
                }

                SetStatusLog(sb.ToString().TrimEnd(), MessageType.Error);
            }

            Repaint();
        }

        void InspectAllLevelsWater()
        {
            RunBatchLevelOperation(
                rerollOnFail: false,
                progressTitle: "全部检查",
                operationTitle: "全部检查",
                reloadCurrentIfRerolled: false);
        }

        void AutoFixAllLevelsWater()
        {
            RunBatchLevelOperation(
                rerollOnFail: true,
                progressTitle: "自动修复",
                operationTitle: "自动修复",
                reloadCurrentIfRerolled: true);
        }

        void RunBatchLevelOperation(
            bool rerollOnFail,
            string progressTitle,
            string operationTitle,
            bool reloadCurrentIfRerolled)
        {
            var entries = LevelBatchChecker.CheckAllOnDisk(
                showProgress: true, rerollOnFail: rerollOnFail, progressTitle: progressTitle);
            if (entries.Count == 0)
            {
                _lastBatchCheckEntries = null;
                SetStatusLog("未找到任何关卡 JSON（Assets/AssetBundleLocal/Json/Levels/level_*.json）。", MessageType.Warning);
                Repaint();
                return;
            }

            _lastBatchCheckEntries = entries;

            if (reloadCurrentIfRerolled &&
                entries.Any(e => e.LevelIndex == _levelIndex && e.RerollCount > 0))
            {
                LoadLevel();
                SyncWaterRefreshFieldsFromCups();
            }

            var allValid = entries.All(e => e.IsValid);
            SetStatusLog(
                LevelBatchChecker.FormatFullLog(entries, operationTitle, rerollOnFail),
                allValid ? MessageType.Info : MessageType.Warning);
            Repaint();
        }

        void ExportBatchCheckReport()
        {
            if (_lastBatchCheckEntries == null || _lastBatchCheckEntries.Count == 0)
            {
                if (!EditorUtility.DisplayDialog(
                        "导出报告",
                        "尚未执行全部检查或自动修复。是否现在全部检查并导出？",
                        "全部检查并导出",
                        "取消"))
                    return;

                InspectAllLevelsWater();
                if (_lastBatchCheckEntries == null || _lastBatchCheckEntries.Count == 0)
                    return;
            }

            if (LevelBatchChecker.TryExportReportWithDialog(_lastBatchCheckEntries))
                SetStatusLog("检查报告已导出。", MessageType.Info);
            Repaint();
        }

        void SyncWaterRefreshFieldsFromCups()
        {
            var total = LevelWaterAnalyzer.SumParticipatingLayers(_cups);
            if (total > 0)
            {
                _waterTotalLayers = total;
                _waterColorCount = Mathf.Max(1, total / 4);
            }

            RefreshLevelDifficultyMetrics();
        }

        void RefreshLevelDifficultyMetrics()
        {
            _levelDifficultyMetrics = LevelWaterDifficultyAnalyzer.Analyze(_cups, _levelIndex);
        }

        void DrawWaterRefreshPanel()
        {
            var participating = LevelWaterAnalyzer.CountParticipating(_cups);
            var lockCount = LevelWaterAnalyzer.CountLockCups(_cups);

            EditorGUILayout.LabelField(
                $"参与刷新水层：{participating}（普通瓶+锁瓶；不含空槽/广告瓶/空瓶；锁瓶 {lockCount} 个按 lockLayers 分配）",
                EditorStyles.miniLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("当前关卡评估", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"难度等级：{_levelDifficultyMetrics.DifficultyLabel}");
            EditorGUILayout.LabelField(
                $"反推最少完成步数：{_levelDifficultyMetrics.MinStepsLabel}（上限 {LevelWaterCheckPolicy.MaxAllowedMinSteps}）");
            if (!string.IsNullOrEmpty(_levelDifficultyMetrics.SolveNote) &&
                _levelDifficultyMetrics.ConfigValid && !_levelDifficultyMetrics.IsSolvable)
            {
                EditorGUILayout.LabelField(_levelDifficultyMetrics.SolveNote, EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField(
                    "步数=倒水次数；装袋/锁瓶解锁按游戏规则自动触发（含锁瓶；不含广告瓶/空槽）",
                    EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            _waterColorCount = Mathf.Clamp(EditorGUILayout.IntField("颜色种类总数", _waterColorCount), 1, 8);
            GUILayout.Space(8);
            _waterTotalLayers = Mathf.Max(0, EditorGUILayout.IntField("水层总数", _waterTotalLayers));
            GUILayout.Space(8);
            _waterQuestionLayers = Mathf.Max(0, EditorGUILayout.IntField("问号层数量", _waterQuestionLayers));
            EditorGUILayout.EndHorizontal();

            if (_waterColorCount > 0)
            {
                var minLayers = _waterColorCount * 4;
                var maxLayers = participating * 4;
                if (_waterTotalLayers % 4 != 0)
                    EditorGUILayout.HelpBox("水层总数须为 4 的倍数。", MessageType.Warning);
                else if (_waterTotalLayers < minLayers)
                    EditorGUILayout.HelpBox(
                        $"水层总数不能少于 {minLayers}（颜色种类×4）。",
                        MessageType.Warning);
                else if (participating > 0 && _waterTotalLayers > maxLayers)
                    EditorGUILayout.HelpBox(
                        $"水层总数不能超过参与瓶容量 {maxLayers}（{participating} 瓶×4）。",
                        MessageType.Warning);
                else if (_waterTotalLayers > minLayers)
                    EditorGUILayout.HelpBox(
                        $"当前 {minLayers}~{maxLayers} 层范围内；部分颜色将多于 4 层（4 的倍数，可多组消除）。",
                        MessageType.Info);

                if (_waterQuestionLayers > _waterTotalLayers)
                    EditorGUILayout.HelpBox(
                        $"问号层数量不能超过水层总数（当前 {_waterQuestionLayers}/{_waterTotalLayers}）。刷新时会自动截断。",
                        MessageType.Warning);
            }

            var partSum = LevelWaterAnalyzer.SumParticipatingLayers(_cups);
            if (partSum > 0 && partSum != _waterTotalLayers)
                EditorGUILayout.HelpBox(
                    $"参与瓶内现有水层合计 {partSum} 层，与「水层总数」{_waterTotalLayers} 不一致；" +
                    "刷新将按右侧水层总数/颜色数重新分配到普通瓶与锁瓶（空瓶不参与）。",
                    MessageType.Info);

            _waterDifficulty = (WaterRefreshDifficulty)EditorGUILayout.EnumPopup("难度", _waterDifficulty);

            var recommended = LevelWaterRandomizer.RecommendDifficulty(
                participating, _waterColorCount, _waterTotalLayers, lockCount);
            EditorGUILayout.HelpBox(
                $"当前配置推荐难度：{LevelWaterRandomizer.GetDifficultyDisplayName(recommended)}",
                MessageType.Info);

            if (EditorGUI.EndChangeCheck())
                SaveLayoutSettings();

            if (GUILayout.Button("刷新水层（覆盖参与随机的瓶子）"))
                RefreshWaterLayers();

            EditorGUILayout.Space(4);
            _showWaterRandomRules = EditorGUILayout.Foldout(
                _showWaterRandomRules, LevelWaterRandomizerRules.Summary, true);
            if (_showWaterRandomRules)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                foreach (var line in LevelWaterRandomizerRules.Lines)
                {
                    if (string.IsNullOrEmpty(line))
                        EditorGUILayout.Space(2);
                    else
                        EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);
                }
                EditorGUILayout.EndVertical();
            }
        }

        void RefreshWaterLayers()
        {
            if (_cups == null || _cups.Count == 0)
            {
                _status = "请先加载关卡";
                return;
            }

            RecordUndo();
            if (!LevelWaterRandomizer.TryRefresh(
                    _cups, _waterColorCount, _waterTotalLayers, _waterDifficulty, null, _levelIndex,
                    _waterQuestionLayers,
                    out var error, out var adjustLog))
            {
                _status = error ?? "刷新失败";
                EditorUtility.DisplayDialog("刷新水层", _status, "确定");
                SetStatusLog(_status, MessageType.Error);
                Repaint();
                return;
            }

            var sb = new System.Text.StringBuilder();
            var actualQuestionLayers = CountQuestionLayers(_cups);
            sb.AppendLine(
                $"已按「{LevelWaterRandomizer.GetDifficultyDisplayName(_waterDifficulty)}」刷新水层" +
                $"（{_waterColorCount} 色 / {_waterTotalLayers} 层 / {actualQuestionLayers} 问号层，" +
                $"写入 {LevelWaterAnalyzer.CountParticipating(_cups)} 个瓶）");
            var adjustText = LevelWaterLayerAllocator.FormatAdjustLog(adjustLog);
            if (!string.IsNullOrEmpty(adjustText))
                sb.AppendLine().Append(adjustText);
            SetStatusLog(sb.ToString().TrimEnd(), MessageType.Info);
            RefreshLevelDifficultyMetrics();
            SaveLayoutSettings();
            Repaint();
            RefreshScenePreview();
        }

        static int CountQuestionLayers(IList<CupData> cups)
        {
            var total = 0;
            if (cups == null)
                return total;

            foreach (var cup in cups)
            {
                if (cup == null || cup.colors == null)
                    continue;
                total += Mathf.Clamp(cup.whNums, 0, Mathf.Max(0, cup.colors.Count));
            }

            return total;
        }

        void DrawPlayAreaPreview()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("局内预览（竖屏）", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("刷新网格", GUILayout.Width(72)))
                ApplyPreviewGridFromBottleSettings();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(
                "绿框 = 局内可摆放区；虚线网格 = 水瓶尺寸单元格；拖动瓶子调整位置。",
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                $"当前网格：{_gridBottleWidth:F0} × {_gridBottleHeight:F0}（设计像素）",
                EditorStyles.miniLabel);
            _showCupRawReference = EditorGUILayout.Toggle("显示 CupMgr 原始区（灰虚线对照）", _showCupRawReference);
            var rect = GUILayoutUtility.GetRect(
                GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (rect.height < PreviewMinHeight)
                rect.height = PreviewMinHeight;
            DrawScreenPreviewGui(rect);
            DrawPreviewLegend(rect);
            HandlePreviewPointerEvents(rect);
        }

        void HandlePreviewPointerEvents(Rect rect)
        {
            var e = Event.current;
            var inPreview = rect.Contains(e.mousePosition);

            if (e.type == EventType.MouseDown && inPreview)
            {
                _previewPointerDown = true;
                GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                var ui = PreviewToUi(rect, e.mousePosition);
                var hit = HitTestCup(ui, rect);
                if (hit >= 0)
                {
                    _selectedCup = hit;
                    _dragCup = -1;
                    _dragUndoRecorded = false;
                }
                else
                    _selectedCup = -1;

                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseDrag && _previewPointerDown && _selectedCup >= 0 && _selectedCup < _cups.Count)
            {
                if (_dragCup < 0)
                    _dragCup = _selectedCup;

                if (!_dragUndoRecorded)
                {
                    RecordUndo();
                    _dragUndoRecorded = true;
                }

                var ui = PreviewToUi(rect, e.mousePosition);
                ui.y -= PreviewHalfBottleHeight;
                if (_snapGrid) ui = Snap(ui);
                if (_clampToPlayArea)
                    ui = GameplayCupSpace.ClampCupPosition(ui, PreviewBottleWidth, PreviewBottleHeight);
                _cups[_dragCup].position = ui;
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseUp && _previewPointerDown)
            {
                _previewPointerDown = false;
                _dragCup = -1;
                _dragUndoRecorded = false;
                GUIUtility.hotControl = 0;
                e.Use();
                Repaint();
            }
        }

        struct PreviewLayout
        {
            public float ox, oy, scale;
            public Rect playGui;
        }

        PreviewLayout GetPreviewLayout(Rect rect)
        {
            var scale = Mathf.Min(rect.width / _layout.designWidth, rect.height / _layout.designHeight);
            var drawW = _layout.designWidth * scale;
            var drawH = _layout.designHeight * scale;
            var ox = rect.x + (rect.width - drawW) * 0.5f;
            var oy = rect.y + (rect.height - drawH) * 0.5f;
            var play = GameplayScreenLayout.GetPlayAreaRect(_layout);
            var playGui = UiRectToGuiRect(play, ox, oy, scale);
            return new PreviewLayout { ox = ox, oy = oy, scale = scale, playGui = playGui };
        }

        Vector2 CupPositionToGui(Vector2 cupPoint, in PreviewLayout layout) =>
            UiToGui(GameplayLayoutMapping.CupToPlayArea(cupPoint, _layout), layout.ox, layout.oy, layout.scale);

        Vector2 GuiToCupPosition(Vector2 gui, in PreviewLayout layout) =>
            GameplayLayoutMapping.PlayAreaToCup(GuiToDesignUi(gui, layout), _layout);

        /// <summary>配置的宽高为绿框（局内区域）内设计像素，仅乘预览缩放。</summary>
        Vector2 GetPreviewCupGuiSize(in PreviewLayout layout) =>
            new Vector2(PreviewBottleWidth * layout.scale, PreviewBottleHeight * layout.scale);

        Vector2 GuiToDesignUi(Vector2 gui, in PreviewLayout layout)
        {
            var halfW = _layout.designWidth * 0.5f;
            var halfH = _layout.designHeight * 0.5f;
            return new Vector2(
                (gui.x - layout.ox) / layout.scale - halfW,
                halfH - (gui.y - layout.oy) / layout.scale);
        }

        void DrawScreenPreviewGui(Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;

            var layout = GetPreviewLayout(rect);
            var screen = GameplayScreenLayout.GetFullScreenRect(_layout.designWidth, _layout.designHeight);
            var play = GameplayScreenLayout.GetPlayAreaRect(_layout);

            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.14f, 1f));
            var screenRect = UiRectToGuiRect(screen, layout.ox, layout.oy, layout.scale);
            EditorGUI.DrawRect(screenRect, new Color(0.22f, 0.24f, 0.28f, 1f));

            var topHud = UiRectToGuiRect(
                Rect.MinMaxRect(screen.xMin, play.yMax, screen.xMax, screen.yMax), layout.ox, layout.oy, layout.scale);
            var bottomBar = UiRectToGuiRect(
                Rect.MinMaxRect(screen.xMin, screen.yMin, screen.xMax, play.yMin), layout.ox, layout.oy, layout.scale);
            EditorGUI.DrawRect(topHud, new Color(0.28f, 0.32f, 0.38f, 0.55f));
            EditorGUI.DrawRect(bottomBar, new Color(0.28f, 0.30f, 0.36f, 0.55f));

            Handles.BeginGUI();
            if (_showCupRawReference)
            {
                var cupGui = UiRectToGuiRect(GameplayCupSpace.CupAreaRect, layout.ox, layout.oy, layout.scale);
                DrawDashedGuiRectOutline(cupGui, new Color(0.55f, 0.55f, 0.55f, 0.75f));
            }

            Handles.color = new Color(0.2f, 0.85f, 0.45f, 0.95f);
            Handles.DrawSolidRectangleWithOutline(
                layout.playGui, new Color(0.2f, 0.85f, 0.45f, 0.08f), new Color(0.2f, 0.85f, 0.45f, 1f));
            Handles.EndGUI();

            DrawPlayAreaBottleGrid(layout.playGui, layout.scale);

            for (var i = 0; i < _cups.Count; i++)
            {
                var cup = _cups[i];
                if (!CupSlotKindUtility.ShowInLevelPreview(cup)) continue;
                DrawCupInPreview(cup, i == _selectedCup, layout);
            }

            if (_selectedCup >= 0 && _selectedCup < _cups.Count &&
                CupSlotKindUtility.ShowInLevelPreview(_cups[_selectedCup]))
            {
                var cupRect = GetCupGuiRect(_cups[_selectedCup], layout);
                DrawSelectionAlignmentGuides(cupRect, layout.playGui);
            }
        }

        Rect GetCupGuiRect(CupData cup, in PreviewLayout layout)
        {
            var bottomPlay = GameplayLayoutMapping.CupToPlayArea(cup.position, _layout);
            var bottomGui = UiToGui(bottomPlay, layout.ox, layout.oy, layout.scale);
            var size = GetPreviewCupGuiSize(layout);
            // IMGUI：Rect.y 为顶边；底边锚点对应 bottomGui，瓶身向上延伸
            return new Rect(bottomGui.x - size.x * 0.5f, bottomGui.y - size.y, size.x, size.y);
        }

        /// <summary>沿选中瓶子矩形的四条边，向局内区域延伸对齐虚线（顶/底边为水平线，左/右边为垂直线）。</summary>
        void DrawSelectionAlignmentGuides(Rect cupRect, Rect playGui)
        {
            var color = new Color(1f, 0.85f, 0.2f, 0.95f);
            // 顶边 y（GUI 坐标 y 越小越靠上）
            DrawDashedGuiLine(
                new Vector2(playGui.xMin, cupRect.yMin),
                new Vector2(playGui.xMax, cupRect.yMin),
                color);
            // 底边
            DrawDashedGuiLine(
                new Vector2(playGui.xMin, cupRect.yMax),
                new Vector2(playGui.xMax, cupRect.yMax),
                color);
            // 左边 x
            DrawDashedGuiLine(
                new Vector2(cupRect.xMin, playGui.yMin),
                new Vector2(cupRect.xMin, playGui.yMax),
                color);
            // 右边
            DrawDashedGuiLine(
                new Vector2(cupRect.xMax, playGui.yMin),
                new Vector2(cupRect.xMax, playGui.yMax),
                color);
        }

        static void DrawDashedGuiRectOutline(Rect r, Color color)
        {
            DrawDashedGuiLine(new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), color);
            DrawDashedGuiLine(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), color);
            DrawDashedGuiLine(new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax), color);
            DrawDashedGuiLine(new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.yMin), color);
        }

        void DrawPreviewLegend(Rect previewRect)
        {
            if (Event.current.type != EventType.Repaint) return;
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.92f, 0.92f, 0.92f, 1f) }
            };
            GUI.Label(new Rect(previewRect.x + 6, previewRect.y + 4, previewRect.width - 12, 18),
                "绿框 · 局内区域（边距参数）", style);
            GUI.Label(new Rect(previewRect.x + 6, previewRect.y + 20, previewRect.width - 12, 18),
                $"绿虚线 · 水瓶网格 {_gridBottleWidth:F0}×{_gridBottleHeight:F0}", style);
            if (_showCupRawReference)
            {
                GUI.Label(new Rect(previewRect.x + 6, previewRect.y + 36, previewRect.width - 12, 18),
                    "灰虚线 · CupMgr JSON 坐标系", style);
            }
        }

        void ApplyPreviewGridFromBottleSettings()
        {
            _gridBottleWidth = PreviewBottleWidth;
            _gridBottleHeight = PreviewBottleHeight;
            _snapGrid = true;
            Repaint();
            SceneView.RepaintAll();
        }

        void DrawPlayAreaBottleGrid(Rect playGui, float scale)
        {
            var cellW = _gridBottleWidth * scale;
            var cellH = _gridBottleHeight * scale;
            if (cellW < 2f || cellH < 2f) return;

            var color = new Color(0.2f, 0.85f, 0.45f, 0.45f);
            var x = playGui.xMin;
            while (x <= playGui.xMax + 0.5f)
            {
                DrawDashedGuiLine(new Vector2(x, playGui.yMin), new Vector2(x, playGui.yMax), color);
                x += cellW;
            }

            var y = playGui.yMin;
            while (y <= playGui.yMax + 0.5f)
            {
                DrawDashedGuiLine(new Vector2(playGui.xMin, y), new Vector2(playGui.xMax, y), color);
                y += cellH;
            }
        }

        static void DrawDashedGuiLine(Vector2 from, Vector2 to, Color color)
        {
            var delta = to - from;
            var len = delta.magnitude;
            if (len < 0.5f) return;

            var dir = delta / len;
            Handles.BeginGUI();
            Handles.color = color;
            var t = 0f;
            while (t < len)
            {
                var segEnd = Mathf.Min(t + AlignDashLength, len);
                Handles.DrawLine(from + dir * t, from + dir * segEnd);
                t += AlignDashLength + AlignDashGap;
            }

            Handles.EndGUI();
        }

        void DrawCupInPreview(CupData cup, bool selected, in PreviewLayout layout)
        {
            var r = GetCupGuiRect(cup, layout);

            var fill = selected ? new Color(0.3f, 0.9f, 0.4f, 0.35f) : new Color(0.35f, 0.55f, 0.9f, 0.28f);
            EditorGUI.DrawRect(r, fill);
            Handles.BeginGUI();
            Handles.color = selected ? Color.green : new Color(0.5f, 0.75f, 1f, 0.9f);
            Handles.DrawWireCube(r.center, new Vector3(r.width, r.height, 0));
            Handles.EndGUI();

            if (cup.colors == null || cup.colors.Count == 0) return;
            var layerH = r.height / Mathf.Max(4, GameConstants.WaterMaxCount);
            // colors[0]=底层(water1)，colors[^1]=顶层；IMGUI 的 Rect.y 为顶边，y 向下增大
            for (var layer = 0; layer < cup.colors.Count; layer++)
            {
                if (!GameConstants.GameColorData.TryGetValue(cup.colors[layer], out var pair)) continue;
                var ly = r.yMax - layerH * (layer + 0.5f);
                var lr = new Rect(r.xMin + r.width * 0.1f, ly - layerH * 0.35f, r.width * 0.8f, layerH * 0.7f);
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

        Vector2 UiToGui(Vector2 ui, float ox, float oy, float scale) =>
            UiToGui(ui, ox, oy, scale, _layout.designWidth, _layout.designHeight);

        static Vector2 UiToGui(Vector2 ui, float ox, float oy, float scale, float designWidth, float designHeight)
        {
            var halfW = designWidth * 0.5f;
            var halfH = designHeight * 0.5f;
            return new Vector2(
                ox + (ui.x + halfW) * scale,
                oy + (halfH - ui.y) * scale);
        }

        Vector2 PreviewToUi(Rect previewRect, Vector2 mousePos)
        {
            var layout = GetPreviewLayout(previewRect);
            return GuiToCupPosition(mousePos, layout);
        }

        int HitTestCup(Vector2 uiPos, Rect previewRect)
        {
            var layout = GetPreviewLayout(previewRect);
            var best = -1;
            var bestDist = float.MaxValue;
            for (var i = 0; i < _cups.Count; i++)
            {
                var cup = _cups[i];
                if (!CupSlotKindUtility.ShowInLevelPreview(cup)) continue;
                var r = GetCupGuiRect(cup, layout);
                var mouseGui = Event.current.mousePosition;
                if (r.Contains(mouseGui))
                    return i;
                var center = new Vector2(cup.position.x, cup.position.y + PreviewHalfBottleHeight);
                var d = Vector2.Distance(uiPos, center);
                if (d < Mathf.Max(PreviewBottleWidth, PreviewBottleHeight) * 0.55f && d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }

            return best;
        }

        void DrawEditorOptions()
        {
            EditorGUI.BeginChangeCheck();
            _scenePreview = EditorGUILayout.Toggle("Scene 视图辅助预览", _scenePreview);
            if (EditorGUI.EndChangeCheck())
                OnScenePreviewToggled();

            _clampToPlayArea = EditorGUILayout.Toggle("限制在 CupMgr 区域 (750×1334)", _clampToPlayArea);
            _snapGrid = EditorGUILayout.Toggle("吸附虚线网格", _snapGrid);
            if (_snapGrid)
            {
                EditorGUILayout.LabelField(
                    $"吸附单元格：{_gridBottleWidth:F0} × {_gridBottleHeight:F0}（与预览虚线网格一致，修改尺寸后请点「刷新网格」）",
                    EditorStyles.miniLabel);
            }

            if (_scenePreview)
                EditorGUILayout.HelpBox(
                    "Scene 视图：灰=设计屏，绿=局内区域，瓶子已映射；灰虚线=CupMgr 原始区（需勾选）。",
                    MessageType.Info);
        }

        void OnScenePreviewToggled()
        {
            if (_scenePreview)
            {
                FrameSceneToPlayArea();
                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.in2DMode = true;
            }

            SceneView.RepaintAll();
            Repaint();
        }

        void DrawLevelMeta()
        {
            var exists = File.Exists(ProjectPaths.GetSplitLevelAbsolute(_levelIndex));
            EditorGUILayout.LabelField("输出", ProjectPaths.GetSplitLevelAssetPath(_levelIndex));
            EditorGUILayout.LabelField("状态", exists ? "文件已存在" : "尚未保存");
        }

        void DrawCupList()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"共 {_cups.Count} 个瓶子", EditorStyles.miniLabel);
            if (GUILayout.Button("+ 添加", GUILayout.Width(64)))
                AddCup();
            if (GUILayout.Button("- 删除", GUILayout.Width(64)) && _selectedCup >= 0 && _selectedCup < _cups.Count)
                RemoveSelectedCup();
            EditorGUILayout.EndHorizontal();

            for (var i = 0; i < _cups.Count; i++)
                DrawCupInspector(i);
        }

        void DrawCupInspector(int index)
        {
            var cup = _cups[index];
            var isSel = index == _selectedCup;
            var layerCountForHeader = cup.colors?.Count ?? 0;
            var questionCountForHeader = Mathf.Clamp(cup.whNums, 0, layerCountForHeader);
            var header =
                $"#{index}  ({cup.position.x:F0}, {cup.position.y:F0})  层:{layerCountForHeader}  问号:{questionCountForHeader}";
            var tag = CupSlotKindUtility.GetKindShortTag(cup);
            if (!string.IsNullOrEmpty(tag)) header += $" [{tag}]";

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
            var kindChanged = DrawCupKindToolbar(ref cup);
            var kind = CupSlotKindUtility.GetKind(cup);
            if (kind != CupSlotKind.空槽)
            {
                if (kind == CupSlotKind.普通瓶 || kind == CupSlotKind.锁瓶)
                {
                    var layerCount = cup.colors?.Count ?? 0;
                    cup.whNums = Mathf.Clamp(cup.whNums, 0, layerCount);
                    if (kind == CupSlotKind.锁瓶)
                    {
                        cup.lockColor = EditorGUILayout.IntField("lockColor", cup.lockColor);
                        cup.lockNums = EditorGUILayout.IntField("lockNums", cup.lockNums);
                    }

                    DrawColorLayers(cup);
                }
                else if (kind == CupSlotKind.空瓶)
                {
                    EditorGUILayout.HelpBox("空瓶：局内有瓶、开局无水，不参与「刷新水层」。", MessageType.Info);
                }
                else if (kind == CupSlotKind.广告瓶)
                {
                    EditorGUILayout.HelpBox("广告瓶不参与「刷新水层」。", MessageType.Info);
                }
            }

            if (EditorGUI.EndChangeCheck() || kindChanged)
            {
                RecordUndo();
                pos = _snapGrid ? Snap(pos) : pos;
                if (_clampToPlayArea)
                    pos = GameplayCupSpace.ClampCupPosition(pos, PreviewBottleWidth, PreviewBottleHeight);
                cup.position = pos;
                cup.id = index;
                _cups[index] = cup;
                Repaint();
            }

            EditorGUILayout.EndVertical();
        }

        static bool DrawCupKindToolbar(ref CupData cup)
        {
            var kind = CupSlotKindUtility.GetKind(cup);
            EditorGUILayout.LabelField("瓶子类型（五选一）", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            var selected = GUILayout.Toolbar((int)kind, CupSlotKindUtility.EditorToolbarLabels, GUILayout.Height(22));
            EditorGUILayout.EndHorizontal();
            if (selected != (int)kind)
            {
                CupSlotKindUtility.SetKind(cup, (CupSlotKind)selected);
                return true;
            }

            return false;
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
            cup.whNums = Mathf.Clamp(cup.whNums, 0, cup.colors.Count);

            for (var layer = 0; layer < cup.colors.Count; layer++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"L{layer}", GUILayout.Width(28));
                cup.colors[layer] = EditorGUILayout.IntSlider(cup.colors[layer], 0, 8, GUILayout.Width(160));
                var sw = GUILayoutUtility.GetRect(24, 16, GUILayout.Width(24));
                if (GameConstants.GameColorData.TryGetValue(cup.colors[layer], out var pair))
                    EditorGUI.DrawRect(sw, pair.Base);
                GUILayout.Space(8);
                var isHidden = layer < cup.whNums;
                var nextHidden = EditorGUILayout.ToggleLeft("问号", isHidden, GUILayout.Width(56));
                if (nextHidden != isHidden)
                    cup.whNums = nextHidden
                        ? Mathf.Max(cup.whNums, layer + 1)
                        : Mathf.Min(cup.whNums, layer);
                EditorGUILayout.EndHorizontal();
            }
        }

        /// <summary>Scene 预览以局内区域中心为原点，与局内 2D 预览的相对关系一致。</summary>
        static Vector2 GetScenePivotUi(Rect playArea) => playArea.center;

        static Vector3 UiPointToScene(Vector2 uiPoint, Vector2 pivotUi, float scale = SceneWorldScale)
        {
            var d = uiPoint - pivotUi;
            return new Vector3(d.x * scale, d.y * scale, 0f);
        }

        static Vector3 UiPointToScene(float x, float y, Vector2 pivotUi, float scale = SceneWorldScale) =>
            UiPointToScene(new Vector2(x, y), pivotUi, scale);

        static void DrawSceneRectOutline(Rect uiRect, Vector2 pivotUi, Color color, float thickness = 2f)
        {
            var p0 = UiPointToScene(uiRect.xMin, uiRect.yMin, pivotUi);
            var p1 = UiPointToScene(uiRect.xMax, uiRect.yMin, pivotUi);
            var p2 = UiPointToScene(uiRect.xMax, uiRect.yMax, pivotUi);
            var p3 = UiPointToScene(uiRect.xMin, uiRect.yMax, pivotUi);
            Handles.color = color;
            Handles.DrawAAPolyLine(thickness, p0, p1, p2, p3, p0);
        }

        Bounds CalcScenePreviewBounds(Rect playArea, Vector2 pivotUi, List<CupData> cups)
        {
            var min = UiPointToScene(playArea.xMin, playArea.yMin, pivotUi);
            var max = UiPointToScene(playArea.xMax, playArea.yMax, pivotUi);

            if (cups != null)
            {
                var ext = new Vector3(PreviewBottleWidth * 0.5f, PreviewHalfBottleHeight, 0f) * SceneWorldScale;
                foreach (var cup in cups)
                {
                    if (cup == null || !CupSlotKindUtility.ShowInLevelPreview(cup)) continue;
                    var mapped = GameplayLayoutMapping.CupToPlayArea(
                        new Vector2(cup.position.x, cup.position.y + PreviewHalfBottleHeight),
                        _layout);
                    var cupCenter = UiPointToScene(mapped, pivotUi);
                    min = Vector3.Min(min, cupCenter - ext);
                    max = Vector3.Max(max, cupCenter + ext);
                }
            }

            var boundsCenter = (min + max) * 0.5f;
            var size = max - min;
            size.z = 50f;
            if (size.x < 200f) size.x = 200f;
            if (size.y < 200f) size.y = 200f;
            return new Bounds(boundsCenter, size);
        }

        void OnSceneGUI(SceneView view)
        {
            if (s_FocusedInstance != this || !_scenePreview) return;

            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            var play = GameplayScreenLayout.GetPlayAreaRect(_layout);
            var cupArea = GameplayCupSpace.CupAreaRect;
            var screen = GameplayScreenLayout.GetFullScreenRect(_layout.designWidth, _layout.designHeight);
            var pivot = play.center;

            DrawSceneRectOutline(screen, pivot, new Color(0.75f, 0.78f, 0.82f, 0.85f), 2f);
            DrawSceneRectOutline(play, pivot, new Color(0.2f, 0.9f, 0.4f, 1f), 3f);
            if (_showCupRawReference)
                DrawSceneRectOutline(cupArea, pivot, new Color(0.55f, 0.55f, 0.55f, 0.75f), 2f);

            if (_cups == null || _cups.Count == 0)
            {
                Handles.Label(Vector3.zero, "关卡编辑器：请先加载关卡", EditorStyles.whiteLargeLabel);
                return;
            }

            var cupSize = new Vector3(PreviewBottleWidth, PreviewBottleHeight, 0.01f) * SceneWorldScale;
            for (var i = 0; i < _cups.Count; i++)
            {
                var cup = _cups[i];
                if (!CupSlotKindUtility.ShowInLevelPreview(cup)) continue;
                var cupCenter = new Vector2(
                    cup.position.x,
                    cup.position.y + PreviewHalfBottleHeight);
                var center = UiPointToScene(GameplayLayoutMapping.CupToPlayArea(cupCenter, _layout), pivot);
                var isSel = i == _selectedCup;
                Handles.color = isSel ? Color.green : new Color(0.35f, 0.75f, 1f, 0.95f);
                Handles.DrawWireCube(center, cupSize);
            }
        }

        void FrameSceneToPlayArea()
        {
            var play = GameplayScreenLayout.GetPlayAreaRect(_layout);
            var pivot = play.center;
            var bounds = CalcScenePreviewBounds(play, pivot, _cups);

            foreach (var sceneView in SceneView.sceneViews)
            {
                if (sceneView is not SceneView sv) continue;
                sv.in2DMode = true;
                sv.orthographic = true;
                sv.rotation = Quaternion.identity;
                sv.pivot = bounds.center;
                sv.size = Mathf.Max(bounds.size.x, bounds.size.y) * 0.55f;
                sv.Frame(bounds, false);
            }
        }

        void RefreshScenePreview()
        {
            if (!_scenePreview) return;
            FrameSceneToPlayArea();
            SceneView.RepaintAll();
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

        /// <summary>将瓶子底边锚点吸附到局内预览虚线网格单元格（与 DrawPlayAreaBottleGrid 同源）。</summary>
        Vector2 Snap(Vector2 cupPosition)
        {
            if (!_snapGrid) return cupPosition;

            var cellW = _gridBottleWidth;
            var cellH = _gridBottleHeight;
            if (cellW <= 0.01f || cellH <= 0.01f) return cupPosition;

            var play = GameplayScreenLayout.GetPlayAreaRect(_layout);
            var playPos = GameplayLayoutMapping.CupToPlayArea(cupPosition, _layout);

            var localX = playPos.x - play.xMin;
            var localY = playPos.y - play.yMin;
            var cellX = Mathf.Round((localX - cellW * 0.5f) / cellW);
            var cellY = Mathf.Round(localY / cellH);
            var snappedPlay = new Vector2(
                play.xMin + cellX * cellW + cellW * 0.5f,
                play.yMin + cellY * cellH);

            return GameplayLayoutMapping.PlayAreaToCup(snappedPlay, _layout);
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
            SyncWaterRefreshFieldsFromCups();
            _status = $"已加载第 {_levelIndex} 关，共 {_cups.Count} 个瓶子。";
            Repaint();
            RefreshScenePreview();
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
            SyncWaterRefreshFieldsFromCups();
            _status = "已创建空白模板（2 瓶）。";
            Repaint();
            RefreshScenePreview();
        }

        void AddCup()
        {
            RecordUndo();
            var cupArea = GameplayCupSpace.CupAreaRect;
            var pos = new Vector2(0f, cupArea.yMin + 80f);
            _cups.Add(new CupData
            {
                id = _cups.Count,
                position = _clampToPlayArea
                    ? GameplayCupSpace.ClampCupPosition(pos, PreviewBottleWidth, PreviewBottleHeight)
                    : pos,
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
            SyncWaterRefreshFieldsFromCups();
            _status = $"已从 ConfTotal 导入 {key}。";
            Repaint();
            RefreshScenePreview();
        }

        void PlayTestLevel()
        {
            SaveLevel();
            LevelEditorPlaySession.SchedulePlayTest(_levelIndex);
            LevelConfigLoader.InvalidateCache();
            _status = $"试玩第 {_levelIndex} 关（已保存关卡 JSON）";
            EditorApplication.EnterPlaymode();
        }

        void ReindexCups()
        {
            for (var i = 0; i < _cups.Count; i++)
                _cups[i].id = i;
        }
    }
}
