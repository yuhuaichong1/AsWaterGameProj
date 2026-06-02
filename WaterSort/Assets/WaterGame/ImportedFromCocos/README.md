# ImportedFromCocos

本目录由 **Cocos 弹窗批量导入** 自动生成。

## 你只需要在 Unity 里点一次

菜单：**WaterSort → Cocos Import → ★ 一键导入全部弹窗 Prefab**

完成后将得到：

```
ImportedFromCocos/
├── POPUP_IMPORT_MASTER.md      # 总对照表（含全部 Label 明细）
├── POPUP_IMPORT_MASTER.csv     # 表格版，可 Excel 打开
├── UI/
│   ├── SettingPopup.prefab
│   ├── SettingPopup.labels.txt
│   ├── SuccessPopup.prefab
│   └── ...
└── Textures/                   # 从 Cocos 复制的贴图
```

## 已包含的弹窗（16 个）

Setting、Success、NewPlay、Rank、Collect、GetCollect、RecoverHeart、GetHeart、DailyHeart、Feedback、Click，以及 RankItem / CollectItem / GetCollectItem / RankUpItem / HeartItem。

## 已排除

WXCollectionPopup、TTCollectionPopup、KSCollectionPopup（渠道向，Google Play 不需要）。

## 导入前

**WaterSort → Cocos Import → Settings...** 确认 Cocos 工程路径指向 `KaiGeDaNaoMen2.4.13js`。
