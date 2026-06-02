# 无需 Unity，从 Cocos .prefab JSON 提取 Label 文本，生成总对照表预览
$cocosRoot = "d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js"
$outDir = "d:\BaiduNetdiskDownload\水排序\WaterSort\Assets\ImportedFromCocos"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$entries = @(
    @{ Name="SettingPopup"; Path="assets\SettingPopup\Prefab\SettingPopup.prefab" },
    @{ Name="SuccessPopup"; Path="assets\SuccessPopup\Prefab\SuccessPopup.prefab" },
    @{ Name="NewPlayPopup"; Path="assets\NewPlayPopup\Prefab\NewPlayPopup.prefab" },
    @{ Name="RankPopup"; Path="assets\RankPopup\Prefab\RankPopup.prefab" },
    @{ Name="CollectPopup"; Path="assets\CollectPopup\Prefab\CollectPopup.prefab" },
    @{ Name="GetCollectPopup"; Path="assets\GetCollectPopup\Prefab\GetCollectPopup.prefab" },
    @{ Name="RecoverHeartPopup"; Path="assets\RecoverHeartPopup\Prefab\RecoverHeartPopup.prefab" },
    @{ Name="GetHeartPopup"; Path="assets\RecoverHeartPopup\Prefab\GetHeartPopup.prefab" },
    @{ Name="DailyHeartPopup"; Path="assets\RecoverHeartPopup\Prefab\DailyHeartPopup.prefab" },
    @{ Name="FeedbackPopup"; Path="assets\FeedbackPopup\Prefab\FeedbackPopup.prefab" },
    @{ Name="ClickPopup"; Path="assets\ClickPopup\Prefab\ClickPopup.prefab" },
    @{ Name="RankItem"; Path="assets\RankPopup\Prefab\RankItem.prefab" },
    @{ Name="RankUpItem"; Path="assets\SuccessPopup\Prefab\RankUpItem.prefab" },
    @{ Name="CollectItem"; Path="assets\CollectPopup\Prefab\CollectItem.prefab" },
    @{ Name="GetCollectItem"; Path="assets\GetCollectPopup\Prefab\GetCollectItem.prefab" },
    @{ Name="HeartItem"; Path="assets\Res\Prefab\HeartItem.prefab" }
)

$labelPattern = '"_string"\s*:\s*"([^"\\]*(?:\\.[^"\\]*)*)"'
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("# 弹窗 Label 对照表（脚本预扫描，Unity 一键导入后会更新）")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("| 弹窗 | Label 数 | 示例文案 |")
[void]$sb.AppendLine("|------|----------|----------|")

foreach ($e in $entries) {
    $full = Join-Path $cocosRoot $e.Path
    if (-not (Test-Path $full)) {
        [void]$sb.AppendLine("| $($e.Name) | - | 源文件缺失 |")
        continue
    }
    $text = Get-Content $full -Raw -Encoding UTF8
    $matches = [regex]::Matches($text, $labelPattern)
    $labels = @()
    foreach ($m in $matches) {
        $s = $m.Groups[1].Value -replace '\\n', "`n"
        if ($s.Trim().Length -gt 0) { $labels += $s }
    }
    $sample = ($labels | Select-Object -First 3) -join " / "
    if ($sample.Length -gt 40) { $sample = $sample.Substring(0, 40) + "..." }
    [void]$sb.AppendLine("| $($e.Name) | $($labels.Count) | $sample |")

    $labelFile = Join-Path $outDir "UI\$($e.Name).labels.preview.txt"
    $labelDir = Split-Path $labelFile -Parent
    New-Item -ItemType Directory -Force -Path $labelDir | Out-Null
  $lines = @("# $($e.Name)") + ($labels | ForEach-Object { $_ })
    Set-Content -Path $labelFile -Value $lines -Encoding UTF8
}

$masterPath = Join-Path $outDir "POPUP_IMPORT_MASTER.preview.md"
Set-Content -Path $masterPath -Value $sb.ToString() -Encoding UTF8
Write-Host "Wrote $masterPath"
