using UnityEditor.Android;
using System.IO;
using UnityEngine;
using System.Text.RegularExpressions;
using System;
using System.Security.Cryptography;
using YRTT;

public class YRTTPostBuildProcessor : IPostGenerateGradleAndroidProject
{
    public int callbackOrder
    { get { return 1231; } }

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        PatchAndroidManifest(path);
        PatchLauncherManifest(path);
        ModifySettingGradle(path);
        ModifyProjectGradle(path);
        ModifyLauncherGradle(path);
        ModifyUnityGradle(path);
        ModifyProguardUser(path);
        ModifyGradleProperties(path);
        MoveGoogleServices(path);
        // 固定根 build.gradle 的 AGP 版本为 8.10.1
        ForceAndroidGradlePluginVersion(path);
        // 固定gradle 版本为 8.13
        FixGradleWrapperVersion(path);
        // 固定 ndkVersion，替换 ndkPath
        FixNdkVersionInGradle(path);
    }

    #region UnityLibraryManifest

    private void PatchAndroidManifest(string root)
    {
        var manifestFilePath = GetUnityLibraryManifestFilePath(root);
        var manifest = new YRTTAndroidManifest(manifestFilePath);

        var changed = false;

        changed = manifest.SetTheme() || changed;
        changed = manifest.SetLuacherActivityExport() || changed;

        changed = manifest.SetUsesCleartextTraffic() || changed;
        changed = manifest.SetHardwareAccelerated() || changed;
        changed = manifest.AddGoogleAdsApplicationIdMetaData() || changed;

        if (changed)
        {
            Debug.Log($"成功修改 UnityLibrary AndroidManifest 文件");
            manifest.Save();
        }
    }

    private string GetUnityLibraryManifestFilePath(string root)
    {
        string[] paths = { root, "src", "main", "AndroidManifest.xml" };
        var path = string.Empty;
        foreach (var item in paths)
        {
            path = Path.Combine(path, item);
        }
        Debug.Log($"UnityLibraryManifest Path: {path}");
        return path;
    }

    #endregion

    #region LauncherManifest

    private void PatchLauncherManifest(string root)
    {
        var manifestFilePath = GetLauncherManifestFilePath(root);
        try
        {
            // 检查文件是否存在
            if (!File.Exists(manifestFilePath))
            {
                Debug.LogError($"UnityGradle 文件不存在: {manifestFilePath}");
                return;
            }

            // 读取文件内容
            string fileContent = File.ReadAllText(manifestFilePath);

            if (fileContent.Contains("Unity移除权限，插入开始"))
            {
                Debug.Log($"LauncherManifest 文件已插入");
                return;
            }
            string contentToInsert = ReadEditorFile("YRTTSDK/Editor/AndroidPostBuildProcessor/LauncherManifest_RemovePermissions.txt");

            // 查找</manifest>标签的位置
            string manifestEndTag = "</manifest>";
            int manifestEndIndex = fileContent.LastIndexOf(manifestEndTag);

            if (manifestEndIndex != -1)
            {
                // 在</manifest>标签前插入内容（添加适当的缩进和换行）
                string contentWithNewLine = Environment.NewLine + contentToInsert + Environment.NewLine;
                string modifiedContent = fileContent.Insert(manifestEndIndex, contentWithNewLine);

                // 写回修改后的内容
                File.WriteAllText(manifestFilePath, modifiedContent);
                Debug.Log($"成功在</manifest>前插入内容到 LauncherManifest 文件");
            }
            else
            {
                Debug.LogError($"未找到</manifest>标签，无法插入内容");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"修改 LauncherManifest 文件时出错: {e.Message}");
        }
    }

    private string GetLauncherManifestFilePath(string root)
    {
        var parentPath = Directory.GetParent(root).FullName;
        string[] paths = { parentPath, "launcher", "src", "main", "AndroidManifest.xml" };
        var path = string.Empty;
        foreach (var item in paths)
        {
            path = Path.Combine(path, item);
        }
        Debug.Log($"Launcher Manifest Path: {path}");
        return path;
    }

    #endregion

    #region LauncherGradle

    private void ModifyLauncherGradle(string root)
    {
        var filePath = GetLauncherGradlePath(root);
        try
        {
            // 检查文件是否存在
            if (!File.Exists(filePath))
            {
                Debug.LogError($"LauncherGradle 文件不存在: {filePath}");
                return;
            }

            // 读取文件内容
            string fileContent = File.ReadAllText(filePath);

            if (fileContent.Contains("SDK插入"))
            {
                Debug.Log($"LauncherGradle 文件已插入");
                return;
            }

            // 定义要查找的行的正则表达式模式
            string pattern = @"implementation\s+project\(':unityLibrary'\)";

            // 使用正则表达式查找目标行
            Match match = Regex.Match(fileContent, pattern);

            if (match.Success)
            {
                string launcherGradleContentToInsert = ReadEditorFile("YRTTSDK/Editor/AndroidPostBuildProcessor/LauncherGradle.txt");

                // 构建要插入的完整内容（包括换行符）
                string contentWithNewLine = Environment.NewLine + launcherGradleContentToInsert;

                // 在匹配行之后插入新内容
                string modifiedContent = fileContent.Insert(match.Index + match.Length, contentWithNewLine);

                // 写回修改后的内容
                File.WriteAllText(filePath, modifiedContent);

                Debug.Log($"成功修改 LauncherGradle 文件");
            }
            else
            {
                Debug.LogError($"未找到 'implementation project(':unityLibrary')' 行");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"修改 LauncherGradle 文件时出错: {e.Message}");
        }
        InsertLifecycleForceConfigToLauncherGradle(filePath);
    }

    private string GetLauncherGradlePath(string root)
    {
        var parentPath = Directory.GetParent(root).FullName;
        string[] paths = { parentPath, "launcher", "build.gradle" };
        var path = string.Empty;
        foreach (var item in paths)
        {
            path = Path.Combine(path, item);
        }
        Debug.Log($"LauncherGradle Path: {path}");
        return path;
    }

    private void InsertLifecycleForceConfigToLauncherGradle(string launcherGradlePath)
    {
        // 文件存在判断，与上面一致
        if (!File.Exists(launcherGradlePath))
        {
            Debug.LogError($"LauncherGradle 文件不存在: {launcherGradlePath}");
            return;
        }

        string fileContent = File.ReadAllText(launcherGradlePath);
        string contentToInsert = ReadEditorFile("YRTTSDK/Editor/AndroidPostBuildProcessor/LauncherGradle_Lifecycle.txt");
        // 内容存在判断，风格与文件存在判断一致
        string forceConfigStart = "// UNITY_FORCE_LIFECYCLE_VERSION_START";
        if (fileContent.Contains(forceConfigStart))
        {
            Debug.Log("lifecycle 强制依赖已存在于 launcher build.gradle，无需重复插入。");
            return;
        }

        int idx = fileContent.IndexOf("\ndependencies", StringComparison.Ordinal);
        if (idx >= 0)
        {
            fileContent = fileContent.Insert(idx, contentToInsert + "\n");
            File.WriteAllText(launcherGradlePath, fileContent);
            Debug.Log("已在 launcher build.gradle dependencies 之前插入 lifecycle 强制依赖。");
        }
        else
        {
            fileContent = contentToInsert + "\n" + fileContent;
            File.WriteAllText(launcherGradlePath, fileContent);
            Debug.Log("未找到 dependencies，已插入 lifecycle 强制依赖到 launcher build.gradle 文件首部。");
        }
    }

    #endregion

    #region UnityGradle

    private void ModifyUnityGradle(string root)
    {
        var filePath = GetUnityGradlePath(root);
        try
        {
            // 检查文件是否存在
            if (!File.Exists(filePath))
            {
                Debug.LogError($"UnityGradle 文件不存在: {filePath}");
                return;
            }

            // 读取文件内容
            string fileContent = File.ReadAllText(filePath);

            if (fileContent.Contains("SDK插入"))
            {
                Debug.Log($"UnityGradle 文件已插入");
                return;
            }
            string contentToInsert = ReadEditorFile("YRTTSDK/Editor/AndroidPostBuildProcessor/UnityGradle.txt");

            // 构建要插入的完整内容（包括换行符）
            string contentWithNewLine = Environment.NewLine + contentToInsert;
            File.AppendAllText(filePath, contentWithNewLine);
            Debug.Log($"成功修改 UnityGradle 文件");
        }
        catch (Exception e)
        {
            Debug.LogError($"修改 UnityGradle 文件时出错: {e.Message}");
        }
    }

    private string GetUnityGradlePath(string root)
    {
        string[] paths = { root, "build.gradle" };
        var path = string.Empty;
        foreach (var item in paths)
        {
            path = Path.Combine(path, item);
        }
        Debug.Log($"UnityGradle Path: {path}");
        return path;
    }

    #endregion

    #region ProjectGradle

    private void ModifyProjectGradle(string root)
    {
        var filePath = GetProjectGradlePath(root);
        try
        {
            // 检查文件是否存在
            if (!File.Exists(filePath))
            {
                Debug.LogError($"ProjectGradle 文件不存在: {filePath}");
                return;
            }

            // 读取要插入的内容
            string contentToInsert = ReadEditorFile("YRTTSDK/Editor/AndroidPostBuildProcessor/ProjectGradle.txt");
            if (string.IsNullOrEmpty(contentToInsert))
            {
                Debug.LogError("ProjectGradle.txt 内容为空，跳过插入");
                return;
            }

            // 读取根 build.gradle 内容
            string fileContent = File.ReadAllText(filePath);

            // 防重复：若已包含关键依赖配置，则不重复插入
            bool alreadyHasKeyDeps =
                fileContent.Contains("com.github.megatronking.stringfog:gradle-plugin") ||
                fileContent.Contains("AppLovinQualityServiceGradlePlugin") ||
                fileContent.Contains("com.google.gms:google-services") ||
                fileContent.Contains("firebase-crashlytics-gradle");

            if (alreadyHasKeyDeps)
            {
                Debug.Log("ProjectGradle 关键依赖已存在于根 build.gradle，跳过插入。");
                return;
            }

            // 在文件顶部插入内容
            string modifiedContent = contentToInsert + Environment.NewLine + fileContent;
            File.WriteAllText(filePath, modifiedContent);
            Debug.Log("已在根目录 build.gradle 文件最上方插入 ProjectGradle.txt 内容");
        }
        catch (Exception e)
        {
            Debug.LogError($"修改 ProjectGradle 文件时出错: {e.Message}");
        }
    }

    private string GetProjectGradlePath(string root)
    {
        var parentPath = Directory.GetParent(root).FullName;
        string[] paths = { parentPath, "build.gradle" };
        var path = string.Empty;
        foreach (var item in paths)
        {
            path = Path.Combine(path, item);
        }
        Debug.Log($"ProjectGradle Path: {path}");
        return path;
    }

    #endregion

    #region SettingGradle

    private void ModifySettingGradle(string root)
    {
        var filePath = GetSettingGradlePath(root);
        try
        {
            // 检查文件是否存在
            if (!File.Exists(filePath))
            {
                Debug.LogError($"SettingGradle 文件不存在: {filePath}");
                return;
            }

            string content = ReadEditorFile("YRTTSDK/Editor/AndroidPostBuildProcessor/SettingsGradle.txt");
            // 覆盖写入最新内容
            File.WriteAllText(filePath, content);
            Debug.Log($"settings.gradle 更新内容");
        }
        catch (Exception e)
        {
            Debug.LogError($"修改 settings.gradle 文件时出错: {e.Message}");
        }
    }

    private string GetSettingGradlePath(string root)
    {
        var parentPath = Directory.GetParent(root).FullName;
        string[] paths = { parentPath, "settings.gradle" };
        var path = string.Empty;
        foreach (var item in paths)
        {
            path = Path.Combine(path, item);
        }
        Debug.Log($"settings.gradle Path: {path}");
        return path;
    }

    #endregion

    #region proguard-user

    private void ModifyProguardUser(string root)
    {
        var filePath = GetProguardUserPath(root);
        try
        {
            // 检查文件是否存在
            if (!File.Exists(filePath))
            {
                Debug.LogError($"ProguardUser 文件不存在: {filePath}");
                return;
            }

            // 读取文件内容
            string fileContent = File.ReadAllText(filePath);

            if (fileContent.Contains("SDK插入"))
            {
                Debug.Log($"ProguardUser 文件已插入");
                return;
            }

            string modifiedContent = ReadEditorFile("YRTTSDK/Editor/AndroidPostBuildProcessor/proguard-user.txt");
            // 在最后插入内容
            File.AppendAllText(filePath, modifiedContent);
            Debug.Log($"成功修改proguard-user文件");
        }
        catch (Exception e)
        {
            Debug.LogError($"修改 ProguardUser 文件时出错: {e.Message}");
        }
    }

    private string GetProguardUserPath(string root)
    {
        string[] paths = { root, "proguard-user.txt" };
        var path = string.Empty;
        foreach (var item in paths)
        {
            path = Path.Combine(path, item);
        }
        Debug.Log($"proguard-user Path: {path}");
        return path;
    }

    #endregion

    #region gradle.properties

    private void ModifyGradleProperties(string root)
    {
        var filePath = GetGradlePropertiesPath(root);
        try
        {
            // 检查文件是否存在
            if (!File.Exists(filePath))
            {
                Debug.LogError($"gradle.properties 文件不存在: {filePath}");
                return;
            }

            // 读取文件内容
            string fileContent = File.ReadAllText(filePath);
            bool changed = false;
            // 先移除 android.enableR8=true
            if (fileContent.Contains("android.enableR8=true"))
            {
                fileContent = fileContent.Replace("android.enableR8=true", string.Empty);
                Debug.Log($"gradle.properties 文件移除 android.enableR8=true");
                changed = true;
            }
            // 再判断是否需要添加 useAndroidX 和 enableJetifier
            if (!fileContent.Contains("android.useAndroidX=true"))
            {
                fileContent += Environment.NewLine + "android.useAndroidX=true";
                Debug.Log($"gradle.properties 文件已插入 android.useAndroidX=true");
                changed = true;
            }

            if (!fileContent.Contains("android.enableJetifier = true"))
            {
                fileContent += Environment.NewLine + "android.enableJetifier=true";
                Debug.Log($"gradle.properties 文件已插入 android.enableJetifier=true");
                changed = true;
            }

            // 如果有变更再写回文件
            if (changed)
            {
                File.WriteAllText(filePath, fileContent);
            }
            Debug.Log($"成功修改 gradle.properties 文件");
        }
        catch (Exception e)
        {
            Debug.LogError($"修改 gradle.properties 文件时出错: {e.Message}");
        }
    }

    private string GetGradlePropertiesPath(string root)
    {
        var parentPath = Directory.GetParent(root).FullName;
        string[] paths = { parentPath, "gradle.properties" };
        var path = string.Empty;
        foreach (var item in paths)
        {
            path = Path.Combine(path, item);
        }
        Debug.Log($"gradle.properties Path: {path}");
        return path;
    }

    #endregion

    #region FireBase

    private void FireBaseModifyLauncherGradle(string root)
    {
        var filePath = FireBaseGetLauncherGradlePath(root);
        try
        {
            // 检查文件是否存在
            if (!File.Exists(filePath))
            {
                Debug.LogError($"LauncherGradle 文件不存在: {filePath}");
                return;
            }

            // 读取文件内容
            string fileContent = File.ReadAllText(filePath);

            string pluginsToInsert =
                "apply plugin: 'com.google.gms.google-services'\n" +
                "apply plugin: 'com.google.firebase.crashlytics'\n";

            string pattern = @"apply plugin:\s*'com\.android\.application'";
            Match match = Regex.Match(fileContent, pattern);

            if (match.Success)
            {
                bool hasFirebasePlugin = fileContent.Contains("com.google.gms.google-services") || fileContent.Contains("com.google.firebase.crashlytics");
                bool hasAppLovinPlugin = fileContent.Contains("apply plugin: 'applovin-quality-service'");

                if (!hasFirebasePlugin || !hasAppLovinPlugin)
                {
                    int insertPos = match.Index + match.Length;
                    string modifiedContent = fileContent.Insert(insertPos, "\n" + pluginsToInsert);

                    // 插入 AppLovin 配置（追加到 Firebase 插件配置之后）
                    string appLovinConfig = BuildAppLovinQualityServiceConfig();
                    int appLovinInsertPos = modifiedContent.IndexOf(pluginsToInsert, StringComparison.Ordinal);
                    if (appLovinInsertPos != -1)
                    {
                        appLovinInsertPos += pluginsToInsert.Length;
                        modifiedContent = modifiedContent.Insert(appLovinInsertPos, "\n" + appLovinConfig);
                    }
                    else
                    {
                        modifiedContent = appLovinConfig + modifiedContent;
                    }

                    File.WriteAllText(filePath, modifiedContent);
                    Debug.Log("已插入 Firebase 插件和 AppLovin Quality Service 到 launcher/build.gradle");
                }
                else
                {
                    Debug.Log("Firebase 或 AppLovin 插件已存在，无需重复插入");
                }
            }
            else
            {
                Debug.LogError("未找到 apply plugin: 'com.android.application' 行，无法插入 Firebase 或 AppLovin 插件");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"修改 LauncherGradle 文件时出错: {e.Message}");
        }
    }

    private string BuildAppLovinQualityServiceConfig()
    {
        string appLovinApiKey = GetAppLovinApiKey();
        return
            "apply plugin: 'applovin-quality-service'\n" +
            "applovin {\n" +
            $"    apiKey '{appLovinApiKey}'\n" +
            "}\n";
    }

    /// <summary>
    /// 假装动态获取 apiKey，后续可替换为实际获取逻辑
    /// </summary>
    private string GetAppLovinApiKey()
    {
        // TODO: 替换为你的实际 apiKey 获取逻辑，如读取配置文件、环境变量或编辑器参数
        return YRTTConfig.MaxAdReviewKey;
    }

    private string FireBaseGetLauncherGradlePath(string root)
    {
        var parentPath = Directory.GetParent(root).FullName;
        string[] paths = { parentPath, "launcher", "build.gradle" };
        var path = string.Empty;
        foreach (var item in paths)
        {
            path = Path.Combine(path, item);
        }
        Debug.Log($"LauncherGradle Path: {path}");
        return path;
    }

    private void MoveGoogleServices(string root)
    {
        try
        {
            FireBaseModifyLauncherGradle(root);

            // 构建源文件完整路径
            string sourceFullPath = Path.Combine(Application.dataPath, "google-services.json");

            // 检查源文件是否存在
            if (!File.Exists(sourceFullPath))
            {
                Debug.LogError($"google-services.json 不存在: {sourceFullPath}");
                return;
            }

            // 确保目标目录存在
            var destinationPath = GetGoogleServicesTargetPath(root);

            // 如果目标文件已存在，比较内容
            if (File.Exists(destinationPath))
            {
                bool isSame = CompareFiles(sourceFullPath, destinationPath);
                if (isSame)
                {
                    Debug.Log("google-services.json 目标文件已存在且内容相同，无需覆盖");
                }
                else
                {
                    Debug.Log("google-services.json 目标文件存在但内容不同，执行覆盖");
                }
            }

            // 复制文件到目标路径
            File.Copy(sourceFullPath, destinationPath, true);
            Debug.Log($"google-services.json 已成功移动到: {destinationPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"复制 google-services.json 时出错: {e.Message}");
        }
    }

    private string GetGoogleServicesTargetPath(string root)
    {
        var parentPath = Directory.GetParent(root).FullName;
        string[] paths = { parentPath, "launcher", "google-services.json" };
        var path = string.Empty;
        foreach (var item in paths)
        {
            path = Path.Combine(path, item);
        }
        Debug.Log($"google-services TargetPath: {path}");
        return path;
    }

    /// <summary>
    /// 比较两个文件内容是否相同
    /// </summary>
    private bool CompareFiles(string file1, string file2)
    {
        // 计算文件哈希值进行比较
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash1 = GetFileHash(file1, sha256);
            byte[] hash2 = GetFileHash(file2, sha256);

            // 比较哈希值
            if (hash1.Length != hash2.Length)
                return false;

            for (int i = 0; i < hash1.Length; i++)
            {
                if (hash1[i] != hash2[i])
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 计算文件的哈希值
    /// </summary>
    private byte[] GetFileHash(string filePath, HashAlgorithm algorithm)
    {
        using (FileStream stream = File.OpenRead(filePath))
        {
            return algorithm.ComputeHash(stream);
        }
    }

    #endregion

    /// <summary>
    /// 读取编辑器目录下的文本文件内容
    /// </summary>
    /// <param name="relativePath">相对于Assets目录的文件路径</param>
    /// <returns>文件内容，如果出错则返回空字符串</returns>
    private string ReadEditorFile(string relativePath)
    {
        try
        {
            // 构建完整的文件路径
            string fullPath = Path.Combine(Application.dataPath, relativePath);

            // 检查文件是否存在
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"文件不存在: {fullPath}");
                return string.Empty;
            }

            // 读取文件内容
            string content = File.ReadAllText(fullPath);
            return content;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"读取文件时出错: {e.Message}");
            return string.Empty;
        }
    }

    // 固定根目录 build.gradle 的 com.android.* 插件版本到 8.10.1（修正分组替换问题）
    private void ForceAndroidGradlePluginVersion(string root)
    {
        var filePath = GetProjectGradlePath(root);
        try
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"ProjectGradle 文件不存在: {filePath}");
                return;
            }

            string content = File.ReadAllText(filePath);
            string updated = content;

            // 兼容 ' 或 " 引号
            string appPattern = @"(id\s+'com\.android\.application'\s+version\s+['""])([^'""]+)(['""]\s+apply\s+false)";
            string libPattern = @"(id\s+'com\.android\.library'\s+version\s+['""])([^'""]+)(['""]\s+apply\s+false)";

            // 使用 MatchEvaluator 安全组装替换文本
            updated = Regex.Replace(
                updated,
                appPattern,
                m => m.Groups[1].Value + "8.10.1" + m.Groups[3].Value,
                RegexOptions.Multiline
            );

            updated = Regex.Replace(
                updated,
                libPattern,
                m => m.Groups[1].Value + "8.10.1" + m.Groups[3].Value,
                RegexOptions.Multiline
            );

            if (updated != content)
            {
                File.WriteAllText(filePath, updated);
                Debug.Log("已将根目录 build.gradle 中的 Android Gradle Plugin 版本固定为 8.10.1");
            }
            else
            {
                Debug.Log("根 build.gradle 未检测到可替换的插件版本，或已是 8.10.1。");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"固定 Android Gradle Plugin 版本时出错: {e.Message}");
        }
    }

    // 同步修正 launcher 与 unity 的 build.gradle 中的 ndk 配置
    private void FixNdkVersionInGradle(string root)
    {
        // Unity 模块 build.gradle
        var unityGradle = GetUnityGradlePath(root);
        ReplaceNdkPathWithVersion(unityGradle);
    
        // Launcher 模块 build.gradle
        var launcherGradle = GetLauncherGradlePath(root);
        ReplaceNdkPathWithVersion(launcherGradle);
    }

    // 将所有 ndkPath "..." 或 ndkPath '...' 替换为 ndkVersion '29.0.14206865'
    // 并将已存在的 ndkVersion 统一为目标版本
    private void ReplaceNdkPathWithVersion(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"Gradle 文件不存在: {filePath}");
                return;
            }

            string content = File.ReadAllText(filePath);
            string updated = content;

            // 统一已存在的 ndkVersion
            updated = Regex.Replace(
                updated,
                @"^\s*ndkVersion\s*['""][^'""]+['""]",
                "ndkVersion '29.0.14206865'",
                RegexOptions.Multiline
            );

            // 替换 ndkPath（支持单双引号以及可能的等号写法）
            updated = Regex.Replace(
                updated,
                @"^\s*ndkPath\s*(?:=)?\s*[""'][^""']+[""']",
                "ndkVersion '29.0.14206865'",
                RegexOptions.Multiline
            );

            if (updated != content)
            {
                File.WriteAllText(filePath, updated);
                Debug.Log($"已在 {filePath} 中将 ndkPath/ndkVersion 统一为 ndkVersion '29.0.14206865'");
            }
            else
            {
                Debug.Log($"未在 {filePath} 检测到需要替换的 ndkPath/ndkVersion，或已是目标值。");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"替换 ndk 配置时出错 ({filePath}): {e.Message}");
        }
    }

    // 将 gradle-wrapper.properties 的 distributionUrl 固定为 gradle-8.13-bin.zip
    private void FixGradleWrapperVersion(string root)
    {
        var filePath = GetGradleWrapperPropertiesPath(root);
        try
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"gradle-wrapper.properties 文件不存在: {filePath}");
                return;
            }

            string content = File.ReadAllText(filePath);

            // 匹配并替换 distributionUrl（兼容 https:// 与 https\:// 以及 bin/all）
            string updated = Regex.Replace(
                content,
                @"^\s*distributionUrl\s*=\s*https\\?://services\.gradle\.org/distributions/gradle-[^-\s]+-(?:bin|all)\.zip\s*$",
                "distributionUrl=https\\://services.gradle.org/distributions/gradle-8.13-bin.zip",
                RegexOptions.Multiline
            );

            if (updated != content)
            {
                File.WriteAllText(filePath, updated);
                Debug.Log("已将 gradle-wrapper.properties 的 distributionUrl 固定为 gradle-8.13-bin.zip");
            }
            else
            {
                Debug.Log("未检测到可替换的 distributionUrl，或已是 gradle-8.13-bin.zip。");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"固定 Gradle Wrapper 版本时出错: {e.Message}");
        }
    }

    private string GetGradleWrapperPropertiesPath(string root)
    {
        var parentPath = Directory.GetParent(root).FullName;
        string[] paths = { parentPath, "gradle", "wrapper", "gradle-wrapper.properties" };
        var path = string.Empty;
        foreach (var item in paths)
        {
            path = Path.Combine(path, item);
        }
        Debug.Log($"gradle-wrapper.properties Path: {path}");
        return path;
    }
}


