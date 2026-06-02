# 弹窗导入清单（预览）

> Unity 中执行 **WaterSort → Cocos Import → ★ 一键导入全部弹窗 Prefab** 后，  
> 本文件会被完整版 `POPUP_IMPORT_MASTER.md` 覆盖（含每条 Label 明细）。

## 将导入的 16 个 Prefab

| 分类 | 名称 | Cocos 源路径 | Unity 输出 | UI 脚本 |
|------|------|-------------|------------|---------|
| 弹窗 | SettingPopup | assets/SettingPopup/Prefab/SettingPopup.prefab | ImportedFromCocos/UI/SettingPopup.prefab | SettingPopupView |
| 弹窗 | SuccessPopup | assets/SuccessPopup/Prefab/SuccessPopup.prefab | ImportedFromCocos/UI/SuccessPopup.prefab | SuccessPopupView |
| 弹窗 | NewPlayPopup | assets/NewPlayPopup/Prefab/NewPlayPopup.prefab | ImportedFromCocos/UI/NewPlayPopup.prefab | NewPlayPopupView |
| 弹窗 | RankPopup | assets/RankPopup/Prefab/RankPopup.prefab | ImportedFromCocos/UI/RankPopup.prefab | RankPopupView |
| 弹窗 | CollectPopup | assets/CollectPopup/Prefab/CollectPopup.prefab | ImportedFromCocos/UI/CollectPopup.prefab | CollectPopupView |
| 弹窗 | GetCollectPopup | assets/GetCollectPopup/Prefab/GetCollectPopup.prefab | ImportedFromCocos/UI/GetCollectPopup.prefab | GetCollectPopupView |
| 弹窗 | RecoverHeartPopup | assets/RecoverHeartPopup/Prefab/RecoverHeartPopup.prefab | ImportedFromCocos/UI/RecoverHeartPopup.prefab | RecoverHeartPopupView |
| 弹窗 | GetHeartPopup | assets/RecoverHeartPopup/Prefab/GetHeartPopup.prefab | ImportedFromCocos/UI/GetHeartPopup.prefab | GetHeartPopupView |
| 弹窗 | DailyHeartPopup | assets/RecoverHeartPopup/Prefab/DailyHeartPopup.prefab | ImportedFromCocos/UI/DailyHeartPopup.prefab | DailyHeartPopupView |
| 弹窗 | FeedbackPopup | assets/FeedbackPopup/Prefab/FeedbackPopup.prefab | ImportedFromCocos/UI/FeedbackPopup.prefab | FeedbackPopupView |
| 系统 | ClickPopup | assets/ClickPopup/Prefab/ClickPopup.prefab | ImportedFromCocos/UI/ClickPopup.prefab | （无，仅透明点击层） |
| 子项 | RankItem | assets/RankPopup/Prefab/RankItem.prefab | ImportedFromCocos/UI/RankItem.prefab | - |
| 子项 | RankUpItem | assets/SuccessPopup/Prefab/RankUpItem.prefab | ImportedFromCocos/UI/RankUpItem.prefab | - |
| 子项 | CollectItem | assets/CollectPopup/Prefab/CollectItem.prefab | ImportedFromCocos/UI/CollectItem.prefab | - |
| 子项 | GetCollectItem | assets/GetCollectPopup/Prefab/GetCollectItem.prefab | ImportedFromCocos/UI/GetCollectItem.prefab | - |
| 子项 | HeartItem | assets/Res/Prefab/HeartItem.prefab | ImportedFromCocos/UI/HeartItem.prefab | - |

## 已排除（Google Play）

- WXCollectionPopup、TTCollectionPopup、KSCollectionPopup

## Cocos Label 数量（预扫描）

| 弹窗 | 约 Label 条数 |
|------|---------------|
| SettingPopup | 16 |
| RankPopup | 17 |
| FeedbackPopup | 13 |
| RecoverHeartPopup | 8 |
| GetHeartPopup / DailyHeartPopup | 各 3 |
| CollectPopup | 3 |
| Success / NewPlay / GetCollect | 各 2 |
