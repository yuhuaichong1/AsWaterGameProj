# WaterSort — Cocos → Unity（Google Play 版）

由 Cocos Creator 2.4.13 项目转换，目标平台 **Google Play**，已移除微信/抖音/快手等渠道逻辑。

## 当前完成度

| 模块 | 状态 |
|------|------|
| 核心倒水玩法 + 补间动画 | ✅ |
| 关卡 ConfTotal 全量 | ✅ |
| 场景 Loading → Home → Game | ✅ |
| 弹窗：设置/胜利/排行/图鉴/获收藏/体力/每日/新玩法/反馈 | ✅ |
| 道具 UI：打乱、撤回、加瓶 + 激励视频接口 | ✅ |
| 广告抽象 `IAdsService`（MAX 占位 + Editor 模拟） | ✅ |
| Spine 资源已复制 + `ISpinePlayer` 占位 | ⚠️ 需接 spine-unity |
| 微信/分享/渠道 Collection 弹窗 | ❌ 已排除 |

## 快速开始（游戏入口）

1. Unity **2022.3 LTS** 打开本目录  
2. 打开场景 **`Assets/WaterGame/Scenes/Game.unity`**（Build Settings 里只有这一个场景）  
3. **点顶部 ▶ Play**（场景 Hierarchy 几乎为空是正常的，UI 运行时生成）  
4. 若贴图仍是色块：**WaterSort → Fix Resources Sprite Import (修复贴图导入)**，再 Play  
5. 流程：**Loading → Home → 点「开始游戏」** 进入水排序玩法  
5. **（可选）** 菜单 **WaterSort → Cocos Import → ★ 一键导入全部弹窗 Prefab**

> 详见 `Assets/Scenes/游戏入口说明.md`  
> 若 Hierarchy 没有入口提示物体：**WaterSort → Setup Game Scene (添加入口物体)**

弹窗 Prefab 输出在 `Assets/ImportedFromCocos/UI/`（当前弹窗逻辑仍走代码 UI，Prefab 供对照与后续接入）。

## MAX 广告接入

见 [GOOGLE_PLAY_SETUP.md](./GOOGLE_PLAY_SETUP.md) 与 `Assets/Scripts/Ads/MaxAdsService.Template.cs`。

```csharp
AdsService.SetImplementation(new YourMaxAdsService());
```

## 目录

```
Assets/Scripts/
  Ads/          IAdsService, AdsService, MAX 占位
  Platform/     震动、统计占位
  Events/       EventBus
  UI/           PopupManager + 各弹窗
  Scenes/       Loading / Home / Game 流程
  Game/         GameController, Bottle, HUD, 道具
  Spine/        特效接口与占位实现
  Data/         存档、关卡、图鉴
```

## 源工程

`d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js`
