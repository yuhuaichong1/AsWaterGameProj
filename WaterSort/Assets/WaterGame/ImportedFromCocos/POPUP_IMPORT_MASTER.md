# 弹窗 Prefab 导入总对照表

生成时间: 2026-05-29 22:16:53
Cocos 工程: `d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js`
Unity 输出: `Assets/ImportedFromCocos`

## 一键导入
Unity 菜单: **WaterSort → Cocos Import → ★ 一键导入全部弹窗 Prefab**

## 主表

| 分类 | Cocos 源文件 | Unity Prefab | UI 脚本 | Label 数 | Sprite 节点 | 状态 | 备注 |
|------|-------------|--------------|---------|----------|-------------|------|------|
| 弹窗 | `assets/SettingPopup/Prefab/SettingPopup.prefab` | `Assets/ImportedFromCocos/UI/SettingPopup.prefab` | SettingPopupView | 16 | 29 | ✅ |  |
| 弹窗 | `assets/SuccessPopup/Prefab/SuccessPopup.prefab` | `Assets/ImportedFromCocos/UI/SuccessPopup.prefab` | SuccessPopupView | 2 | 6 | ✅ |  |
| 弹窗 | `assets/NewPlayPopup/Prefab/NewPlayPopup.prefab` | `Assets/ImportedFromCocos/UI/NewPlayPopup.prefab` | NewPlayPopupView | 2 | 6 | ✅ |  |
| 弹窗 | `assets/RankPopup/Prefab/RankPopup.prefab` | `Assets/ImportedFromCocos/UI/RankPopup.prefab` | RankPopupView | 17 | 22 | ✅ |  |
| 弹窗 | `assets/CollectPopup/Prefab/CollectPopup.prefab` | `Assets/ImportedFromCocos/UI/CollectPopup.prefab` | CollectPopupView | 3 | 9 | ✅ |  |
| 弹窗 | `assets/GetCollectPopup/Prefab/GetCollectPopup.prefab` | `Assets/ImportedFromCocos/UI/GetCollectPopup.prefab` | GetCollectPopupView | 2 | 4 | ✅ |  |
| 弹窗 | `assets/RecoverHeartPopup/Prefab/RecoverHeartPopup.prefab` | `Assets/ImportedFromCocos/UI/RecoverHeartPopup.prefab` | RecoverHeartPopupView | 8 | 18 | ✅ |  |
| 弹窗 | `assets/RecoverHeartPopup/Prefab/GetHeartPopup.prefab` | `Assets/ImportedFromCocos/UI/GetHeartPopup.prefab` | GetHeartPopupView | 3 | 8 | ✅ |  |
| 弹窗 | `assets/RecoverHeartPopup/Prefab/DailyHeartPopup.prefab` | `Assets/ImportedFromCocos/UI/DailyHeartPopup.prefab` | DailyHeartPopupView | 3 | 8 | ✅ |  |
| 弹窗 | `assets/FeedbackPopup/Prefab/FeedbackPopup.prefab` | `Assets/ImportedFromCocos/UI/FeedbackPopup.prefab` | FeedbackPopupView | 13 | 17 | ✅ |  |
| 系统 | `assets/ClickPopup/Prefab/ClickPopup.prefab` | `Assets/ImportedFromCocos/UI/ClickPopup.prefab` | - | 0 | 0 | ✅ |  |
| 子项 | `assets/RankPopup/Prefab/RankItem.prefab` | `Assets/ImportedFromCocos/UI/RankItem.prefab` | - | 4 | 5 | ✅ |  |
| 子项 | `assets/SuccessPopup/Prefab/RankUpItem.prefab` | `Assets/ImportedFromCocos/UI/RankUpItem.prefab` | - | 51 | 65 | ✅ |  |
| 子项 | `assets/CollectPopup/Prefab/CollectItem.prefab` | `Assets/ImportedFromCocos/UI/CollectItem.prefab` | - | 2 | 3 | ✅ |  |
| 子项 | `assets/GetCollectPopup/Prefab/GetCollectItem.prefab` | `Assets/ImportedFromCocos/UI/GetCollectItem.prefab` | - | 1 | 2 | ✅ |  |
| 子项 | `assets/Res/Prefab/HeartItem.prefab` | `Assets/ImportedFromCocos/UI/HeartItem.prefab` | - | 3 | 4 | ✅ |  |

## 已排除（Google Play 不导入）

- **WXCollectionPopup** — 渠道(已跳过): `WXCollectionPopup/Prefab/WXCollectionPopup.prefab`
- **TTCollectionPopup** — 渠道(已跳过): `(无资源包)`
- **KSCollectionPopup** — 渠道(已跳过): `(无资源包)`

## PopupType 与 Unity 路径对应

| PopupType | 建议 Prefab 路径 |
|-----------|------------------|
| Setting | `Assets/ImportedFromCocos/UI/SettingPopup.prefab` |
| Success | `Assets/ImportedFromCocos/UI/SuccessPopup.prefab` |
| Rank | `Assets/ImportedFromCocos/UI/RankPopup.prefab` |
| Collect | `Assets/ImportedFromCocos/UI/CollectPopup.prefab` |
| GetCollect | `Assets/ImportedFromCocos/UI/GetCollectPopup.prefab` |
| RecoverHeart | `Assets/ImportedFromCocos/UI/RecoverHeartPopup.prefab` |
| GetHeart | `Assets/ImportedFromCocos/UI/GetHeartPopup.prefab` |
| DailyHeart | `Assets/ImportedFromCocos/UI/DailyHeartPopup.prefab` |
| NewPlay | `Assets/ImportedFromCocos/UI/NewPlayPopup.prefab` |
| Feedback | `Assets/ImportedFromCocos/UI/FeedbackPopup.prefab` |

## 各弹窗 Label 明细

### SettingPopup

源: `assets/SettingPopup/Prefab/SettingPopup.prefab`

```
# 从 Cocos 自动提取的 Label 文本
# 源文件: d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js\assets\SettingPopup\Prefab\SettingPopup.prefab

SettingPopup/content/vibrationNode/label	震动
SettingPopup/content/soundNode/label	音效
SettingPopup/content/musicNode/label	音乐
SettingPopup/content/btnHome/label	返回主页
SettingPopup/content/btnRestart/label	重新开始
SettingPopup/content/btnRestart/img/label	-1
SettingPopup/content/btnContinue/label	继续游戏
SettingPopup/HeartItem/img/num	5
SettingPopup/HeartItem/time	02:33
SettingPopup/HeartItem/down	-1
SettingPopup/gm/layoutNode/btnStart/Background/Label	开始游戏
SettingPopup/gm/layoutNode/levelEditBox/PLACEHOLDER_LABEL	输入关卡
SettingPopup/gm/layoutNode/btnGameUIHide/Background/Label	隐藏游戏UI
SettingPopup/gm/layoutNode/btnGameUIShow/Background/Label	显示游戏UI
SettingPopup/gm/layoutNode/btnNext/Background/Label	下一关
SettingPopup/gm/layoutNode/btnSuccess/Background/Label	立即通关
```

### SuccessPopup

源: `assets/SuccessPopup/Prefab/SuccessPopup.prefab`

```
# 从 Cocos 自动提取的 Label 文本
# 源文件: d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js\assets\SuccessPopup\Prefab\SuccessPopup.prefab

SuccessPopup/content/btnNext/label	下 一 关
SuccessPopup/content/btnHome/label	回 主 页
```

### NewPlayPopup

源: `assets/NewPlayPopup/Prefab/NewPlayPopup.prefab`

```
# 从 Cocos 自动提取的 Label 文本
# 源文件: d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js\assets\NewPlayPopup\Prefab\NewPlayPopup.prefab

NewPlayPopup/content/desc/label	打包任意奶茶可解锁白色标签
NewPlayPopup/content/tip	点击任意位置继续
```

### RankPopup

源: `assets/RankPopup/Prefab/RankPopup.prefab`

```
# 从 Cocos 自动提取的 Label 文本
# 源文件: d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js\assets\RankPopup\Prefab\RankPopup.prefab

RankPopup/content/frame/label	排行榜
RankPopup/content/tabNode/btnFriends/label	好友
RankPopup/content/tabNode/btnTotal/label	总榜
RankPopup/content/tabNode/btnProvince/label	地区榜
RankPopup/content/rankNode/title/no	排名
RankPopup/content/rankNode/title/user	玩家
RankPopup/content/rankNode/title/bar	累计通关
RankPopup/content/rankNode/me/rank2/label	4
RankPopup/content/rankNode/me/rank3	未上榜
RankPopup/content/rankNode/me/name	玩家114514
RankPopup/content/rankNode/me/passLevel	0关
RankPopup/content/rankNode/me/province	广东
RankPopup/content/authUserTip/label	为了更好地展示排名， 

需要先获取您的头像和名称
RankPopup/content/authUserTip/btnAuth/label	点击授权
RankPopup/content/btnShare/label	炫耀一下
RankPopup/content/tip	每半小时刷新一次榜单
RankPopup/content/tip2	存档提示：移除小程序或清楚缓存会删除游戏存档!!!请谨慎操作！
```

### CollectPopup

源: `assets/CollectPopup/Prefab/CollectPopup.prefab`

```
# 从 Cocos 自动提取的 Label 文本
# 源文件: d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js\assets\CollectPopup\Prefab\CollectPopup.prefab

CollectPopup/content/frame/label	我的收藏
CollectPopup/content/tabNode/btnDrink/label	饮品
CollectPopup/content/tabNode/btnDessert/label	甜点
```

### GetCollectPopup

源: `assets/GetCollectPopup/Prefab/GetCollectPopup.prefab`

```
# 从 Cocos 自动提取的 Label 文本
# 源文件: d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js\assets\GetCollectPopup\Prefab\GetCollectPopup.prefab

GetCollectPopup/content/center/btnCollect/label	放入收藏
GetCollectPopup/content/center/desc	返回主页可查看
```

### RecoverHeartPopup

源: `assets/RecoverHeartPopup/Prefab/RecoverHeartPopup.prefab`

```
# 从 Cocos 自动提取的 Label 文本
# 源文件: d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js\assets\RecoverHeartPopup\Prefab\RecoverHeartPopup.prefab

RecoverHeartPopup/content/frame/label	恢复体力
RecoverHeartPopup/content/center/heart/num	0
RecoverHeartPopup/content/center/downTime/time	00:09:41
RecoverHeartPopup/content/center/btnAddFull/label	回满体力
RecoverHeartPopup/content/bottom/heart/label	无限体力
RecoverHeartPopup/content/bottom/desc	随机时长
30-60分钟
RecoverHeartPopup/content/bottom/btnVideo/label	立即获取
RecoverHeartPopup/content/bottom/btnVideo/num	(0/3)
```

### GetHeartPopup

源: `assets/RecoverHeartPopup/Prefab/GetHeartPopup.prefab`

```
# 从 Cocos 自动提取的 Label 文本
# 源文件: d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js\assets\RecoverHeartPopup\Prefab\GetHeartPopup.prefab

GetHeartPopup/content/heart/label	+5
GetHeartPopup/content/heart_infinite/downTime/time	00:09:41
GetHeartPopup/content/btnHome/label	收下
```

### DailyHeartPopup

源: `assets/RecoverHeartPopup/Prefab/DailyHeartPopup.prefab`

```
# 从 Cocos 自动提取的 Label 文本
# 源文件: d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js\assets\RecoverHeartPopup\Prefab\DailyHeartPopup.prefab

GetHeartPopup/content/heart_infinite/time	30分钟
GetHeartPopup/content/btnShare/label	分享获取
GetHeartPopup/content/btnNo/label	放弃
```

### FeedbackPopup

源: `assets/FeedbackPopup/Prefab/FeedbackPopup.prefab`

```
# 从 Cocos 自动提取的 Label 文本
# 源文件: d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js\assets\FeedbackPopup\Prefab\FeedbackPopup.prefab

FeedbackPopup/content/frame/label	意见反馈
FeedbackPopup/content/center/title	请选择反馈类型
FeedbackPopup/content/center/ToggleContainer/toggle0/desc	虚假宣传
FeedbackPopup/content/center/ToggleContainer/toggle1/desc	游戏卡死
FeedbackPopup/content/center/ToggleContainer/toggle2/desc	数据丢失
FeedbackPopup/content/center/ToggleContainer/toggle3/desc	其它BUG
FeedbackPopup/content/center/ToggleContainer/toggle4/desc	想法建议
FeedbackPopup/content/center/info	亲爱的玩家朋友，如果您有更好的想法， 
也可以反馈给我们，您的想法，没准就能实现哦!
FeedbackPopup/content/center/title1	内容描述(不少于10个字)
FeedbackPopup/content/center/EditBox/PLACEHOLDER_LABEL	请输入你的问题......
FeedbackPopup/content/center/tip	1分钟只能提交一次哦
FeedbackPopup/content/center/wordsNumLabel	(0/150)
FeedbackPopup/content/btnSubmit/label	提 交
```

### ClickPopup

源: `assets/ClickPopup/Prefab/ClickPopup.prefab`

```
# 从 Cocos 自动提取的 Label 文本
# 源文件: d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js\assets\ClickPopup\Prefab\ClickPopup.prefab
```

### RankItem

源: `assets/RankPopup/Prefab/RankItem.prefab`

```
# 从 Cocos 自动提取的 Label 文本
# 源文件: d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js\assets\RankPopup\Prefab\RankItem.prefab

RankItem/rank2/label	4
RankItem/name	玩家114514
RankItem/passLevel	998关
RankItem/province	广东
```

### RankUpItem

源: `assets/SuccessPopup/Prefab/RankUpItem.prefab`

```
# 从 Cocos 自动提取的 Label 文本
# 源文件: d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js\assets\SuccessPopup\Prefab\RankUpItem.prefab

RankUpItem/title/no	排名
RankUpItem/title/user	玩家
RankUpItem/title/bar	累计通关
RankUpItem/scrollview/view/content/item/rank2/label	4
RankUpItem/scrollview/view/content/item/name	玩家114514
RankUpItem/scrollview/view/content/item/passLevel	998关
RankUpItem/scrollview/view/content/item/rank2/label	4
RankUpItem/scrollview/view/content/item/name	玩家114514
RankUpItem/scrollview/view/content/item/passLevel	998关
RankUpItem/scrollview/view/content/item/rank2/label	4
RankUpItem/scrollview/view/content/item/name	玩家114514
RankUpItem/scrollview/view/content/item/passLevel	998关
RankUpItem/scrollview/view/content/item/rank2/label	4
RankUpItem/scrollview/view/content/item/name	玩家114514
RankUpItem/scrollview/view/content/item/passLevel	998关
RankUpItem/scrollview/view/content/item/rank2/label	4
RankUpItem/scrollview/view/content/item/name	玩家114514
RankUpItem/scrollview/view/content/item/passLevel	998关
RankUpItem/scrollview/view/content/item/rank2/label	4
RankUpItem/scrollview/view/content/item/name	玩家114514
RankUpItem/scrollview/view/content/item/passLevel	998关
RankUpItem/scrollview/view/content/item/rank2/label	4
RankUpItem/scrollview/view/content/item/name	玩家114514
RankUpItem/scrollview/view/content/item/passLevel	998关
RankUpItem/scrollview/view/content/item/rank2/label	4
RankUpItem/scrollview/view/content/item/name	玩家114514
RankUpItem/scrollview/view/content/item/passLevel	998关
RankUpItem/scrollview/view/content/item/rank2/label	4
RankUpItem/scrollview/view/content/item/name	玩家114514
RankUpItem/scrollview/view/content/item/passLevel	998关
RankUpItem/scrollview/view/content/item/rank2/label	4
RankUpItem/scrollview/view/content/item/name	玩家114514
RankUpItem/scrollview/view/content/item/passLevel	998关
RankUpItem/scrollview/view/content/item/rank2/label	4
RankUpItem/scrollview/view/content/item/name	玩家114514
RankUpItem/scrollview/view/content/item/passLevel	998关
RankUpItem/scrollview/view/content/item/rank2/label	4
RankUpItem/scrollview/view/content/item/name	玩家114514
RankUpItem/scrollview/view/content/item/passLevel	998关
RankUpItem/scrollview/view/content/item/rank2/label	4
RankUpItem/scrollview/view/content/item/name	玩家114514
RankUpItem/scrollview/view/content/item/passLevel	998关
RankUpItem/scrollview/view/content/item/rank2/label	4
RankUpItem/scrollview/view/content/item/name	玩家114514
RankUpItem/scrollview/view/content/item/passLevel	998关
RankUpItem/scrollview/view/content/item/rank2/label	4
RankUpItem/scrollview/view/content/item/name	玩家114514
RankUpItem/scrollview/view/content/item/passLevel	998关
RankUpItem/me/rank2/label	4
RankUpItem/me/name	玩家114514
RankUpItem/me/passLevel	0关
```

### CollectItem

源: `assets/CollectPopup/Prefab/CollectItem.prefab`

```
# 从 Cocos 自动提取的 Label 文本
# 源文件: d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js\assets\CollectPopup\Prefab\CollectItem.prefab

CollectItem/name	名字名字
CollectItem/no	No.1
```

### GetCollectItem

源: `assets/GetCollectPopup/Prefab/GetCollectItem.prefab`

```
# 从 Cocos 自动提取的 Label 文本
# 源文件: d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js\assets\GetCollectPopup\Prefab\GetCollectItem.prefab

GetCollectItem/name	名字名字
```

### HeartItem

源: `assets/Res/Prefab/HeartItem.prefab`

```
# 从 Cocos 自动提取的 Label 文本
# 源文件: d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js\assets\Res\Prefab\HeartItem.prefab

HeartItem/img/num	5
HeartItem/time	02:33
HeartItem/down	-1
```

