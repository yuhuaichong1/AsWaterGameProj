# Cocos Prefab → Unity Prefab（自动对照，无需手写 YAML）

## 你的顾虑

> Label 文本、贴图引用我不知道，还是要对着 Cocos 来。

**对。** 所以应用 **解析 Cocos 的 `.prefab`（JSON）**，而不是手写 YAML 或猜文案。

工具会读取：

| Cocos 字段 | Unity 结果 |
|-----------|-----------|
| `cc.Node._name` | GameObject 名 |
| `cc.Node._trs` / `_contentSize` | RectTransform 位置与尺寸 |
| `cc.Label._string` | `Text.text`（原文，如「继续游戏」「音乐」） |
| `cc.Sprite._spriteFrame.__uuid__` | 查 `.meta` 找到 png → 复制到 Unity 并挂 `Image` |
| `cc.Button` | `Button` 组件 |

生成后附带 **`SettingPopup.labels.txt`** 这类清单，方便你核对每条文案路径。

## 使用（推荐：一键）

1. **WaterSort → Cocos Import → Settings...**  
   确认 Cocos 工程路径指向 `KaiGeDaNaoMen2.4.13js`。

2. **WaterSort → Cocos Import → ★ 一键导入全部弹窗 Prefab**  
   等待进度条结束（约 1～3 分钟）。

3. 查看输出：  
   - `Assets/ImportedFromCocos/POPUP_IMPORT_MASTER.md` — **总对照表**  
   - `Assets/ImportedFromCocos/POPUP_IMPORT_MASTER.csv`  
   - `Assets/ImportedFromCocos/UI/*.prefab` — 各弹窗 Prefab  
   - `Assets/ImportedFromCocos/UI/*.labels.txt` — 各弹窗 Label 明细  

单独导入仍可用 **Import Cocos Prefab File...**

## 与「UI 脚本生成 Prefab」的关系

| 方式 | 适用 |
|------|------|
| **Cocos Import（本工具）** | 还原节点名、文案、贴图，对照原工程 |
| **UIPrefabGenerator + IUIPrefabBlueprint** | 无 Cocos 文件时快速搭骨架 |

推荐流程：**先 Cocos Import 生成 Prefab → 再在 Inspector 微调 → UI 脚本只负责逻辑**。

## 局限（可继续增强）

- 内置纯白贴图等 Cocos 内置 UUID 可能解析不到（需手动指定）
- Layout、RichText、Spine 组件需后续扩展
- 坐标与 Cocos 设计分辨率一致，复杂 Widget 可能需手动调锚点
