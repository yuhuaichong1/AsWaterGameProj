using UnityEngine;
using UnityEditor;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class BatchMD5Modifier : EditorWindow
{
    private string selectedFolderPath = "";
    private Vector2 mainPanelScrollPosition;
    private Vector2 logScrollPosition;
    private Vector2 fileListScrollPosition;
    private List<FileInfo> allFiles = new List<FileInfo>();
    private List<FileInfo> filteredFiles = new List<FileInfo>();
    private Dictionary<string, FileMD5Info> fileMD5Info = new Dictionary<string, FileMD5Info>();

    // 过滤选项
    private bool recursiveScan = true;
    private bool includeUnityAssets = false;
    private string includeExtensions = ".*";
    private string excludeExtensions = ".meta,.manifest,.cs,.md5backup";
    private bool selectAll = true;
    private long minFileSize = 0;
    private long maxFileSize = long.MaxValue;

    // 修改选项
    private bool createBackup = true;  // 是否创建备份
    private int randomDataSize = 2048; // 随机数据大小（字节）
    private string jsonPaddingFieldName = "__md5Salt"; // JSON 内写入的自定义字段名

    // 布局选项
    private float fileListAreaHeight = 200f;
    private float logAreaHeight = 160f;

    private const string PrefFileListHeight = "BatchMD5Modifier.FileListHeight";
    private const string PrefLogAreaHeight = "BatchMD5Modifier.LogAreaHeight";
    private const string PrefJsonFieldName = "BatchMD5Modifier.JsonFieldName";
    private const string BackupFolderName = "_Backup";

    // 进度显示
    private bool isProcessing = false;
    private float progress = 0;
    private string currentFile = "";
    private int processedCount = 0;
    private int successCount = 0;
    private int failCount = 0;

    // 显示选项
    private bool showOriginalMD5 = true;
    private bool showNewMD5 = true;
    private bool showMD5Diff = true;

    // 统计信息
    private int totalFileCount = 0;
    private long totalFileSize = 0;

    // 日志
    private List<string> logMessages = new List<string>();

    // 文件MD5信息类
    private class FileMD5Info
    {
        public string OriginalMD5 { get; set; }
        public string NewMD5 { get; set; }
        public bool IsModified { get; set; }
        public bool IsSelected { get; set; }
        public string BackupPath { get; set; }

        public FileMD5Info(string originalMD5)
        {
            OriginalMD5 = originalMD5;
            NewMD5 = originalMD5;
            IsModified = false;
            IsSelected = true;
            BackupPath = "";
        }

        public string GetDisplayMD5()
        {
            if (IsModified && !string.IsNullOrEmpty(NewMD5))
                return NewMD5;
            return OriginalMD5;
        }

        public bool IsMD5Changed()
        {
            return OriginalMD5 != NewMD5;
        }
    }

    [MenuItem("Tools/批量MD5修改工具")]
    public static void ShowWindow()
    {
        BatchMD5Modifier window = GetWindow<BatchMD5Modifier>("批量MD5修改工具");
        window.minSize = new Vector2(1200, 800);
        window.Show();
    }

    private void OnEnable()
    {
        fileListAreaHeight = EditorPrefs.GetFloat(PrefFileListHeight, 200f);
        logAreaHeight = EditorPrefs.GetFloat(PrefLogAreaHeight, 160f);
        jsonPaddingFieldName = EditorPrefs.GetString(PrefJsonFieldName, "__md5Salt");
    }

    private void OnDisable()
    {
        EditorPrefs.SetFloat(PrefFileListHeight, fileListAreaHeight);
        EditorPrefs.SetFloat(PrefLogAreaHeight, logAreaHeight);
        EditorPrefs.SetString(PrefJsonFieldName, jsonPaddingFieldName ?? "__md5Salt");
    }

    private void OnGUI()
    {
        mainPanelScrollPosition = EditorGUILayout.BeginScrollView(mainPanelScrollPosition);

        GUILayout.Space(10);

        EditorGUILayout.LabelField("批量MD5修改工具 - 直接修改原文件", EditorStyles.boldLabel);

        GUILayout.Space(10);

        DrawFolderSelection();

        GUILayout.Space(10);

        DrawFilterOptions();

        GUILayout.Space(10);

        DrawFileStatistics();

        GUILayout.Space(10);

        DrawFileList();

        GUILayout.Space(10);

        DrawModificationOptions();

        GUILayout.Space(10);

        DrawActionButtons();

        GUILayout.Space(10);

        if (isProcessing)
        {
            DrawProgressBar();
        }

        GUILayout.Space(10);

        DrawLogArea();

        GUILayout.Space(10);

        EditorGUILayout.EndScrollView();
    }

    private void DrawFolderSelection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("文件夹选择", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        selectedFolderPath = EditorGUILayout.TextField("文件夹路径:", selectedFolderPath);
        if (GUILayout.Button("浏览", GUILayout.Width(80)))
        {
            string path = EditorUtility.OpenFolderPanel("选择要处理的文件夹",
                string.IsNullOrEmpty(selectedFolderPath) ? Application.dataPath : selectedFolderPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                selectedFolderPath = path;
                ScanAllFiles();
            }
        }

        if (GUILayout.Button("刷新扫描", GUILayout.Width(80)))
        {
            ScanAllFiles();
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(selectedFolderPath) && Directory.Exists(selectedFolderPath))
        {
            EditorGUILayout.HelpBox($"已选择文件夹: {selectedFolderPath}", MessageType.Info);
        }
        else if (!string.IsNullOrEmpty(selectedFolderPath))
        {
            EditorGUILayout.HelpBox("文件夹不存在，请重新选择。", MessageType.Error);
        }

        EditorGUILayout.EndVertical();
    }

    private void ScanAllFiles()
    {
        if (string.IsNullOrEmpty(selectedFolderPath) || !Directory.Exists(selectedFolderPath))
            return;

        allFiles.Clear();
        fileMD5Info.Clear();
        totalFileSize = 0;

        try
        {
            EditorUtility.DisplayProgressBar("扫描文件", "正在扫描文件夹...", 0);

            SearchOption searchOption = recursiveScan ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string[] files = Directory.GetFiles(selectedFolderPath, "*.*", searchOption);

            int fileCount = files.Length;
            string backupRootFullPath = Path.GetFullPath(GetBackupRootDirectory());

            for (int i = 0; i < fileCount; i++)
            {
                string filePath = files[i];
                if (IsUnderBackupDirectory(filePath, backupRootFullPath))
                    continue;

                EditorUtility.DisplayProgressBar("扫描文件", $"正在扫描: {Path.GetFileName(filePath)}", (float)i / fileCount);

                FileInfo fileInfo = new FileInfo(filePath);
                allFiles.Add(fileInfo);
                totalFileSize += fileInfo.Length;
            }

            EditorUtility.ClearProgressBar();

            ApplyFilters();

            AddLog($"扫描完成！共找到 {allFiles.Count} 个文件，过滤后 {filteredFiles.Count} 个文件");
            AddLog($"总大小: {FormatFileSize(totalFileSize)}");
        }
        catch (System.Exception ex)
        {
            EditorUtility.ClearProgressBar();
            AddLog($"扫描失败: {ex.Message}");
        }
    }

    private void ApplyFilters()
    {
        filteredFiles.Clear();

        string[] includeExtList = includeExtensions.Split(new char[] { ',', ';' }, System.StringSplitOptions.RemoveEmptyEntries);
        string[] excludeExtList = excludeExtensions.Split(new char[] { ',', ';' }, System.StringSplitOptions.RemoveEmptyEntries);

        bool includeAll = includeExtensions.Trim() == ".*" || includeExtensions.Trim() == "*";

        foreach (var file in allFiles)
        {
            bool isInUnityProject = file.FullName.StartsWith(Application.dataPath);
            bool isUnityAsset = IsUnityResourceFile(file.FullName);

            if (!includeUnityAssets && isUnityAsset && isInUnityProject)
                continue;

            if (!includeAll && includeExtList.Length > 0)
            {
                bool extensionMatched = false;
                foreach (string ext in includeExtList)
                {
                    if (file.Extension.ToLower() == ext.ToLower().Trim())
                    {
                        extensionMatched = true;
                        break;
                    }
                }
                if (!extensionMatched) continue;
            }

            if (excludeExtList.Length > 0)
            {
                bool excluded = false;
                foreach (string ext in excludeExtList)
                {
                    if (file.Extension.ToLower() == ext.ToLower().Trim())
                    {
                        excluded = true;
                        break;
                    }
                }
                if (excluded) continue;
            }

            if (file.Length < minFileSize || file.Length > maxFileSize)
                continue;

            filteredFiles.Add(file);

            if (!fileMD5Info.ContainsKey(file.FullName))
            {
                string md5 = CalculateFileMD5(file.FullName);
                fileMD5Info[file.FullName] = new FileMD5Info(md5);
            }
            fileMD5Info[file.FullName].IsSelected = selectAll;
        }

        totalFileCount = filteredFiles.Count;
    }

    private bool IsUnityResourceFile(string filePath)
    {
        string[] unityExtensions = { ".unity", ".prefab", ".asset", ".mat", ".controller",
                                      ".anim", ".fbx", ".blend", ".unitypackage", ".shader",
                                      ".cginc", ".hlsl", ".compute" };
        string ext = Path.GetExtension(filePath).ToLower();
        return unityExtensions.Contains(ext);
    }

    private void DrawFilterOptions()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("文件过滤选项", EditorStyles.boldLabel);

        recursiveScan = EditorGUILayout.Toggle("递归扫描子文件夹", recursiveScan);
        includeUnityAssets = EditorGUILayout.Toggle("包含Unity资源文件（谨慎使用）", includeUnityAssets);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("包含扩展名:", GUILayout.Width(100));
        includeExtensions = EditorGUILayout.TextField(includeExtensions);
        EditorGUILayout.LabelField("(用逗号分隔，.*表示全部)", GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("排除扩展名:", GUILayout.Width(100));
        excludeExtensions = EditorGUILayout.TextField(excludeExtensions);
        EditorGUILayout.LabelField("(默认排除.meta,.manifest,.cs)", GUILayout.Width(250));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("文件大小范围:", GUILayout.Width(100));
        minFileSize = EditorGUILayout.LongField("最小(KB):", minFileSize / 1024) * 1024;
        maxFileSize = EditorGUILayout.LongField("最大(KB):", maxFileSize == long.MaxValue ? 0 : maxFileSize / 1024) * 1024;
        if (maxFileSize <= 0) maxFileSize = long.MaxValue;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        selectAll = EditorGUILayout.Toggle("默认全选", selectAll);
        if (GUILayout.Button("应用过滤", GUILayout.Width(80)))
        {
            ApplyFilters();
        }
        if (GUILayout.Button("重新计算MD5", GUILayout.Width(100)))
        {
            RecalculateAllMD5();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void RecalculateAllMD5()
    {
        AddLog("正在重新计算所有文件的MD5...");

        int count = 0;
        foreach (var file in filteredFiles)
        {
            string md5 = CalculateFileMD5(file.FullName);
            if (fileMD5Info.ContainsKey(file.FullName))
            {
                fileMD5Info[file.FullName].OriginalMD5 = md5;
                if (!fileMD5Info[file.FullName].IsModified)
                {
                    fileMD5Info[file.FullName].NewMD5 = md5;
                }
            }
            count++;
            if (count % 10 == 0)
            {
                EditorUtility.DisplayProgressBar("重新计算MD5", $"正在计算: {Path.GetFileName(file.FullName)}", (float)count / filteredFiles.Count);
            }
        }

        EditorUtility.ClearProgressBar();
        AddLog("MD5重新计算完成");
        Repaint();
    }

    private void DrawFileStatistics()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("文件统计", EditorStyles.boldLabel);

        int selectedCount = fileMD5Info.Count(kv => kv.Value.IsSelected);
        int modifiedCount = fileMD5Info.Count(kv => kv.Value.IsMD5Changed());

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"总文件数: {allFiles.Count}", GUILayout.Width(120));
        EditorGUILayout.LabelField($"过滤后: {filteredFiles.Count}", GUILayout.Width(100));
        EditorGUILayout.LabelField($"已选择: {selectedCount}", GUILayout.Width(100));
        EditorGUILayout.LabelField($"已修改: {modifiedCount}", GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawFileList()
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("显示选项:", GUILayout.Width(60));
        showOriginalMD5 = EditorGUILayout.Toggle("原MD5", showOriginalMD5, GUILayout.Width(80));
        showNewMD5 = EditorGUILayout.Toggle("新MD5", showNewMD5, GUILayout.Width(80));
        showMD5Diff = EditorGUILayout.Toggle("显示差异", showMD5Diff, GUILayout.Width(80));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"文件列表 (共{filteredFiles.Count}个文件)", EditorStyles.boldLabel, GUILayout.Width(200));
        EditorGUILayout.LabelField("列表高度:", GUILayout.Width(60));
        float newListHeight = EditorGUILayout.Slider(fileListAreaHeight, 80f, 600f);
        if (!Mathf.Approximately(newListHeight, fileListAreaHeight))
        {
            fileListAreaHeight = newListHeight;
            EditorPrefs.SetFloat(PrefFileListHeight, fileListAreaHeight);
        }
        EditorGUILayout.EndHorizontal();

        fileListScrollPosition = EditorGUILayout.BeginScrollView(fileListScrollPosition, GUILayout.Height(fileListAreaHeight));

        // 表头
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("", GUILayout.Width(20));
        EditorGUILayout.LabelField("文件名", GUILayout.Width(280));
        EditorGUILayout.LabelField("类型", GUILayout.Width(60));
        EditorGUILayout.LabelField("大小", GUILayout.Width(80));
        if (showOriginalMD5) EditorGUILayout.LabelField("原始MD5", GUILayout.Width(150));
        if (showNewMD5) EditorGUILayout.LabelField("当前MD5", GUILayout.Width(150));
        if (showMD5Diff) EditorGUILayout.LabelField("状态", GUILayout.Width(80));
        EditorGUILayout.LabelField("备注", GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        // 全选/全不选按钮
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("全选", GUILayout.Width(60)))
        {
            foreach (var file in filteredFiles)
            {
                if (fileMD5Info.ContainsKey(file.FullName))
                {
                    fileMD5Info[file.FullName].IsSelected = true;
                }
            }
        }
        if (GUILayout.Button("全不选", GUILayout.Width(60)))
        {
            foreach (var file in filteredFiles)
            {
                if (fileMD5Info.ContainsKey(file.FullName))
                {
                    fileMD5Info[file.FullName].IsSelected = false;
                }
            }
        }
        if (GUILayout.Button("选择已修改", GUILayout.Width(80)))
        {
            foreach (var file in filteredFiles)
            {
                if (fileMD5Info.ContainsKey(file.FullName) && fileMD5Info[file.FullName].IsMD5Changed())
                {
                    fileMD5Info[file.FullName].IsSelected = true;
                }
            }
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // 显示文件列表
        foreach (var file in filteredFiles)
        {
            if (!fileMD5Info.ContainsKey(file.FullName))
                continue;

            var md5Info = fileMD5Info[file.FullName];

            EditorGUILayout.BeginHorizontal();

            bool newSelected = EditorGUILayout.Toggle(md5Info.IsSelected, GUILayout.Width(20));
            if (newSelected != md5Info.IsSelected)
            {
                md5Info.IsSelected = newSelected;
            }

            string relativePath = GetRelativePath(file.FullName);
            string fileSize = FormatFileSize(file.Length);

            EditorGUILayout.LabelField(relativePath, GUILayout.Width(280));
            EditorGUILayout.LabelField(file.Extension, GUILayout.Width(60));
            EditorGUILayout.LabelField(fileSize, GUILayout.Width(80));

            if (showOriginalMD5)
            {
                GUI.color = Color.gray;
                EditorGUILayout.LabelField(md5Info.OriginalMD5, GUILayout.Width(150));
            }

            if (showNewMD5)
            {
                if (md5Info.IsMD5Changed())
                {
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField(md5Info.NewMD5, GUILayout.Width(150));
                }
                else
                {
                    GUI.color = Color.gray;
                    EditorGUILayout.LabelField(md5Info.NewMD5, GUILayout.Width(150));
                }
            }

            GUI.color = Color.white;

            if (showMD5Diff)
            {
                if (md5Info.IsMD5Changed())
                {
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField("✓ 已修改", GUILayout.Width(80));
                }
                else
                {
                    GUI.color = Color.gray;
                    EditorGUILayout.LabelField("未修改", GUILayout.Width(80));
                }
                GUI.color = Color.white;
            }

            if (IsUnityResourceFile(file.FullName) && file.FullName.StartsWith(Application.dataPath))
            {
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField("⚠️ Unity资源", GUILayout.Width(100));
                GUI.color = Color.white;
            }
            else if (md5Info.IsModified)
            {
                GUI.color = Color.cyan;
                EditorGUILayout.LabelField("✓ 已处理", GUILayout.Width(100));
                GUI.color = Color.white;
            }
            else
            {
                EditorGUILayout.LabelField("", GUILayout.Width(100));
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawModificationOptions()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("修改选项", EditorStyles.boldLabel);

        createBackup = EditorGUILayout.Toggle("修改前创建备份", createBackup);
        randomDataSize = EditorGUILayout.IntSlider("随机数据大小(字节):", randomDataSize, 128, 8192);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("JSON填充字段名:", GUILayout.Width(110));
        string newJsonFieldName = EditorGUILayout.TextField(jsonPaddingFieldName);
        if (newJsonFieldName != jsonPaddingFieldName)
        {
            jsonPaddingFieldName = newJsonFieldName;
            EditorPrefs.SetString(PrefJsonFieldName, jsonPaddingFieldName);
        }
        EditorGUILayout.EndHorizontal();

        string fieldNameHint = GetValidatedJsonFieldName(jsonPaddingFieldName, out _);
        if (string.IsNullOrEmpty(fieldNameHint))
        {
            EditorGUILayout.HelpBox(
                "JSON 字段名无效：仅支持字母、数字、下划线，且不能以数字开头（例如 __md5Salt、_buildTag）。",
                MessageType.Error);
        }

        EditorGUILayout.HelpBox(
            "• .json 文件：在根对象内写入/更新上述字段（值为随机 Base64），保持合法 JSON，游戏可正常解析。\n" +
            "• 其他文件：在文件末尾追加随机二进制数据。\n" +
            (createBackup ? $"✓ 已启用备份：原文件备份到所选目录下的 {BackupFolderName}/ 文件夹（保持相对路径）" : "⚠ 未启用备份，修改后无法恢复"),
            createBackup ? MessageType.Info : MessageType.Warning);

        EditorGUILayout.EndVertical();
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();

        int selectedCount = fileMD5Info.Count(kv => kv.Value.IsSelected);

        GUI.enabled = selectedCount > 0 && !isProcessing;
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button($"开始修改MD5 ({selectedCount}个文件)", GUILayout.Height(50)))
        {
            if (EditorUtility.DisplayDialog("确认修改",
                $"即将修改 {selectedCount} 个文件的MD5值。\n\n" +
                (createBackup ? "将自动创建备份文件。\n" : "未启用备份，修改后无法恢复！\n") +
                "确定要继续吗？",
                "确定", "取消"))
            {
                StartBatchModification();
            }
        }

        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("恢复所有备份", GUILayout.Height(50)))
        {
            RestoreAllBackups();
        }

        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawProgressBar()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("处理进度", EditorStyles.boldLabel);

        Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
        EditorGUI.ProgressBar(rect, progress, $"{progress * 100:F1}%");

        EditorGUILayout.LabelField($"当前文件: {currentFile}");
        EditorGUILayout.LabelField($"已处理: {processedCount}/{totalFileCount}  成功: {successCount}  失败: {failCount}");

        EditorGUILayout.EndVertical();
    }

    private void DrawLogArea()
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("操作日志", EditorStyles.boldLabel, GUILayout.Width(80));
        EditorGUILayout.LabelField("日志高度:", GUILayout.Width(60));
        float newLogHeight = EditorGUILayout.Slider(logAreaHeight, 80f, 400f);
        if (!Mathf.Approximately(newLogHeight, logAreaHeight))
        {
            logAreaHeight = newLogHeight;
            EditorPrefs.SetFloat(PrefLogAreaHeight, logAreaHeight);
        }
        EditorGUILayout.EndHorizontal();

        logScrollPosition = EditorGUILayout.BeginScrollView(logScrollPosition, GUILayout.Height(logAreaHeight));
        foreach (string log in logMessages.TakeLast(100))
        {
            EditorGUILayout.LabelField(log, EditorStyles.wordWrappedLabel);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("清空日志", GUILayout.Width(80)))
        {
            logMessages.Clear();
        }
        if (GUILayout.Button("导出日志", GUILayout.Width(80)))
        {
            ExportLog();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private async void StartBatchModification()
    {
        isProcessing = true;
        processedCount = 0;
        successCount = 0;
        failCount = 0;
        progress = 0;

        var selectedFiles = fileMD5Info.Where(kv => kv.Value.IsSelected).Select(kv => kv.Key).ToList();
        totalFileCount = selectedFiles.Count;

        string validatedJsonField = GetValidatedJsonFieldName(jsonPaddingFieldName, out string fieldError);
        if (selectedFiles.Any(f => f.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase))
            && string.IsNullOrEmpty(validatedJsonField))
        {
            EditorUtility.DisplayDialog("JSON 字段名无效", fieldError, "确定");
            isProcessing = false;
            return;
        }

        AddLog($"开始批量修改MD5，共 {totalFileCount} 个文件");
        AddLog($"随机数据大小: {randomDataSize} 字节");
        AddLog($"JSON 填充字段名: {(string.IsNullOrEmpty(validatedJsonField) ? "(无)" : validatedJsonField)}");
        AddLog($"备份模式: {(createBackup ? "已启用" : "未启用")}");
        if (createBackup && !string.IsNullOrEmpty(selectedFolderPath))
        {
            Directory.CreateDirectory(GetBackupRootDirectory());
            AddLog($"备份目录: {GetBackupRootDirectory()}");
        }

        foreach (string filePath in selectedFiles)
        {
            currentFile = GetRelativePath(filePath);
            processedCount++;
            progress = (float)processedCount / totalFileCount;

            try
            {
                bool result = await ModifyFileMD5Directly(filePath);
                if (result)
                {
                    successCount++;
                    AddLog($"✓ 成功修改: {GetRelativePath(filePath)}");

                    // 更新MD5显示
                    string newMD5 = CalculateFileMD5(filePath);
                    if (fileMD5Info.ContainsKey(filePath))
                    {
                        fileMD5Info[filePath].NewMD5 = newMD5;
                        fileMD5Info[filePath].IsModified = true;
                    }
                }
                else
                {
                    failCount++;
                    AddLog($"✗ 失败: {GetRelativePath(filePath)}");
                }
            }
            catch (System.Exception ex)
            {
                failCount++;
                AddLog($"✗ 错误: {GetRelativePath(filePath)} - {ex.Message}");
            }

            Repaint();
            await Task.Delay(10);
        }

        AddLog($"批量修改完成！成功: {successCount}, 失败: {failCount}");
        if (createBackup && successCount > 0 && !string.IsNullOrEmpty(selectedFolderPath))
        {
            AddLog($"备份文件已保存至: {GetBackupRootDirectory()}");
        }
        isProcessing = false;

        string backupHint = createBackup && successCount > 0
            ? $"\n备份目录: {GetBackupRootDirectory()}"
            : "";
        EditorUtility.DisplayDialog("批量修改完成",
            $"MD5修改完成！\n\n成功: {successCount}\n失败: {failCount}\n总计: {totalFileCount}{backupHint}",
            "确定");

        // 刷新文件列表显示
        Repaint();
    }

    private async System.Threading.Tasks.Task<bool> ModifyFileMD5Directly(string filePath)
    {
        return await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                // 创建备份到所选目录下的 _Backup 文件夹
                if (createBackup)
                {
                    string backupPath = ResolveBackupFilePath(filePath);
                    string backupDir = Path.GetDirectoryName(backupPath);
                    if (!string.IsNullOrEmpty(backupDir))
                    {
                        Directory.CreateDirectory(backupDir);
                    }
                    File.Copy(filePath, backupPath);

                    if (fileMD5Info.ContainsKey(filePath))
                    {
                        fileMD5Info[filePath].BackupPath = backupPath;
                    }
                }

                if (Path.GetExtension(filePath).Equals(".json", System.StringComparison.OrdinalIgnoreCase))
                {
                    return ModifyJsonFileMD5(filePath);
                }

                // 非 JSON：在文件末尾追加随机二进制
                string tempFile = filePath + ".tmp";
                File.Copy(filePath, tempFile, true);

                using (FileStream fs = new FileStream(tempFile, FileMode.Append, FileAccess.Write))
                {
                    byte[] randomData = new byte[randomDataSize];
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        rng.GetBytes(randomData);
                    }
                    fs.Write(randomData, 0, randomData.Length);
                }

                File.Delete(filePath);
                File.Move(tempFile, filePath);

                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"修改文件失败: {filePath}, 错误: {ex.Message}");
                return false;
            }
        });
    }

    private static string GetValidatedJsonFieldName(string rawName, out string error)
    {
        error = "";
        string name = rawName?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            error = "JSON 字段名不能为空。";
            return null;
        }

        if (!Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_]*$"))
        {
            error = $"字段名 \"{name}\" 不符合规则：仅支持字母、数字、下划线，且不能以数字开头。";
            return null;
        }

        return name;
    }

    private bool ModifyJsonFileMD5(string filePath)
    {
        string fieldName = GetValidatedJsonFieldName(jsonPaddingFieldName, out string error);
        if (string.IsNullOrEmpty(fieldName))
        {
            Debug.LogError($"修改 JSON 失败: {filePath}, {error}");
            return false;
        }

        string text = File.ReadAllText(filePath, Encoding.UTF8);
        JToken token;
        using (var reader = new JsonTextReader(new StringReader(text)))
        {
            token = JToken.ReadFrom(reader);
        }

        if (token.Type != JTokenType.Object)
        {
            Debug.LogError($"修改 JSON 失败: 根节点必须是对象 {{}}，文件: {filePath}");
            return false;
        }

        var jobj = (JObject)token;
        jobj[fieldName] = GenerateRandomBase64Salt(randomDataSize);

        File.WriteAllText(filePath, jobj.ToString(Formatting.Indented), new UTF8Encoding(false));
        return true;
    }

    private static string GenerateRandomBase64Salt(int byteCount)
    {
        byte[] data = new byte[byteCount];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(data);
        }
        return System.Convert.ToBase64String(data);
    }

    private void RestoreAllBackups()
    {
        var filesWithBackup = fileMD5Info.Where(kv => !string.IsNullOrEmpty(kv.Value.BackupPath) && File.Exists(kv.Value.BackupPath)).ToList();

        if (filesWithBackup.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "没有找到可恢复的备份文件", "确定");
            return;
        }

        if (EditorUtility.DisplayDialog("确认恢复",
            $"将恢复 {filesWithBackup.Count} 个文件的备份。\n\n原文件将被备份文件覆盖，确定要继续吗？",
            "确定", "取消"))
        {
            foreach (var item in filesWithBackup)
            {
                try
                {
                    string originalPath = item.Key;
                    string backupPath = item.Value.BackupPath;

                    if (File.Exists(backupPath))
                    {
                        File.Copy(backupPath, originalPath, true);
                        File.Delete(backupPath);

                        // 更新MD5信息
                        string originalMD5 = CalculateFileMD5(originalPath);
                        item.Value.OriginalMD5 = originalMD5;
                        item.Value.NewMD5 = originalMD5;
                        item.Value.IsModified = false;
                        item.Value.BackupPath = "";

                        AddLog($"✓ 已恢复: {GetRelativePath(originalPath)}");
                    }
                }
                catch (System.Exception ex)
                {
                    AddLog($"✗ 恢复失败: {GetRelativePath(item.Key)} - {ex.Message}");
                }
            }

            AddLog($"恢复完成，共恢复 {filesWithBackup.Count} 个文件");
            Repaint();
        }
    }

    private void ExportLog()
    {
        string logPath = Path.Combine(selectedFolderPath, $"md5_modify_log_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");
        File.WriteAllLines(logPath, logMessages);
        EditorUtility.DisplayDialog("导出成功", $"日志已保存到:\n{logPath}", "确定");
    }

    private string CalculateFileMD5(string filePath)
    {
        try
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                return string.Concat(hash.Select(b => b.ToString("x2")));
            }
        }
        catch
        {
            return "计算失败";
        }
    }

    private string GetBackupRootDirectory()
    {
        return Path.Combine(selectedFolderPath, BackupFolderName);
    }

    private static bool IsUnderBackupDirectory(string filePath, string backupRootFullPath)
    {
        if (string.IsNullOrEmpty(backupRootFullPath))
            return false;

        string fullPath = Path.GetFullPath(filePath);
        string normalizedRoot = backupRootFullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return fullPath.Equals(normalizedRoot, System.StringComparison.OrdinalIgnoreCase)
            || fullPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, System.StringComparison.OrdinalIgnoreCase)
            || fullPath.StartsWith(normalizedRoot + Path.AltDirectorySeparatorChar, System.StringComparison.OrdinalIgnoreCase);
    }

    private string ResolveBackupFilePath(string sourceFilePath)
    {
        string relativePath = GetRelativePath(sourceFilePath);
        string backupPath = Path.Combine(GetBackupRootDirectory(), relativePath + ".md5backup");

        int counter = 1;
        while (File.Exists(backupPath))
        {
            backupPath = Path.Combine(GetBackupRootDirectory(), relativePath + $".md5backup{counter}");
            counter++;
        }

        return backupPath;
    }

    private string GetRelativePath(string fullPath)
    {
        if (string.IsNullOrEmpty(selectedFolderPath))
            return Path.GetFileName(fullPath);

        string root = Path.GetFullPath(selectedFolderPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string full = Path.GetFullPath(fullPath);

        if (full.Equals(root, System.StringComparison.OrdinalIgnoreCase))
            return Path.GetFileName(fullPath);

        string rootPrefix = root + Path.DirectorySeparatorChar;
        string rootPrefixAlt = root + Path.AltDirectorySeparatorChar;
        if (full.StartsWith(rootPrefix, System.StringComparison.OrdinalIgnoreCase)
            || full.StartsWith(rootPrefixAlt, System.StringComparison.OrdinalIgnoreCase))
        {
            return full.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        return Path.GetFileName(fullPath);
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private void AddLog(string message)
    {
        logMessages.Add($"[{System.DateTime.Now:HH:mm:ss}] {message}");
        Repaint();
    }
}