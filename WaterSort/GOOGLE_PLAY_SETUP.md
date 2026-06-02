# Google Play 上架与 MAX 广告接入指南

## 你需要准备的权限（Android）

在 Unity **Player Settings → Android → Publishing Settings** 中，通过自定义 `AndroidManifest.xml` 声明。常用项如下：

| 权限 | 是否必须 | 用途 |
|------|----------|------|
| `INTERNET` | **必须** | 关卡配置、排行榜（若接服务器）、MAX 广告 |
| `ACCESS_NETWORK_STATE` | 建议 | 广告 SDK 检测网络 |
| `VIBRATE` | 可选 | 对应原 Cocos 震动反馈 |
| `com.google.android.gms.permission.AD_ID` | 若用广告/分析 | Android 13+ 广告标识 |

**不需要**（已从工程中排除）：

- 微信/抖音/快手相关 SDK 权限
- 录音、相册（除非你们自行加分享截图功能）

### 隐私与商店合规

- Google Play **数据安全表单**需声明：广告 SDK（AppLovin MAX）、设备 ID、崩溃日志等
- 若面向儿童，需遵守 Families Policy；本游戏含广告位，请按实际受众选择分级
- 提供**隐私政策 URL**（说明广告与本地存档）

## MAX SDK 接入步骤

1. 在 Unity 导入 **AppLovin MAX** 官方包（按你们现有方案）。
2. 新建类，例如 `Assets/Scripts/Ads/MaxAdsService.cs`，实现 `IAdsService`：

```csharp
public class MaxAdsService : IAdsService
{
    public void ShowRewardedVideo(RewardAdPlacement placement, Action<bool> onFinished)
    {
        // 将 placement 映射到 MAX ad unit id
        // 展示成功后 onFinished(true)
    }
    // ... 其余接口同理
}
```

3. 在游戏启动处（`SceneFlowManager.Awake`）注册：

```csharp
AdsService.SetImplementation(new MaxAdsService());
AdsService.Initialize();
```

4. **广告位映射**（与 Cocos `Constant.RewardAdScene` 一致）：

| `RewardAdPlacement` | 原 Cocos 场景 |
|---------------------|---------------|
| `GameShuffle` | game_shuffle |
| `GameUndo` | game_undo |
| `GameAddBottle` | game_addBottle |
| `UnlockBag` | unlock_bag |
| `UnlockBottle` | unlock_bottle |
| `HeartFull` | heart_full |
| `HeartInfinite` | heart_infinite |

Banner / 插屏：接口已保留在 `IAdsService`，原 Cocos 玩法层几乎未调用，可按需实现。

## 编辑器调试

- 默认使用 `EditorAdsService`：激励视频直接成功
- 真机 Release 默认 `MaxAdsServiceStub`：返回失败，避免误以为已接广告

## Spine 真特效

1. 安装 [spine-unity](https://esotericsoftware.com/spine-unity)（版本需与 `Assets/WaterGame/Resources/Spine` 下 JSON 导出版本一致）。
2. 将 SkeletonData 放入 `Resources/Spine/{文件夹名}/`。
3. 实现 `ISpinePlayer` 并 `SpineService.SetPlayer(new SpineUnityPlayer())`。
4. 当前 `SpinePlayerStub` 用缩放脉冲占位，不影响逻辑联调。

## 场景流程

```
Loading（进度条 + 配置加载）→ Home（主页）→ Game（关卡）
         ↑________________________|  胜利/设置回主页
```

冷启动与 Cocos 不同：Cocos 直进 Game；本 Unity 工程按 Google Play 习惯先进 **Home**。

## 可选：接你们服务器排行榜

`RankPopupView` 当前为本地演示列表。接入时替换为 HTTP API，无需微信 `SubContextView`。
