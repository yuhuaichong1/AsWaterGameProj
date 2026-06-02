# UI Prefab 自动生成工具

> **若要从 Cocos 还原真实文案与贴图**，请优先使用  
> **WaterSort → Cocos Import**（见 `CocosImport/README.md`），无需手写 YAML。

## 用法

1. 在 UI 脚本上标记路径并实现蓝图接口：

```csharp
[UIPrefabAsset("Assets/WaterGame/Resources/Prefabs/UI/Popups/YourPopup.prefab")]
public class YourPopupView : BasePopupView, IUIPrefabBlueprint
{
    public void BuildPrefabUI(UIPrefabBuildContext context)
    {
        context.CreateLabel("Title", "标题", 40, new Vector2(0, 300), new Vector2(400, 60));
        context.CreateButton("BtnOk", "确定", new Vector2(0, -200), new Vector2(200, 56));
    }
}
```

2. Unity 菜单：**WaterSort → UI Prefab Generator → Generate All**（或打开 Window 单独生成）

3. 输出到 `UIPrefabAsset` 指定路径，可在 Inspector 中继续换图、调布局、绑 SerializeField。

## 生成结构

```
YourPopupView (根，挂 UI 脚本)
├── Blocker (半透明遮罩，可选)
└── Content (面板)
    └── BuildPrefabUI 创建的子节点
```

## 与运行时关系

- **生成 Prefab**：仅 Editor，调用 `BuildPrefabUI`。
- **运行时**：仍可用 `PopupManager` 动态创建，或改为 `Resources.Load` / Addressables 实例化 Prefab（需自行改 `PopupManager`）。

## 扩展

- `context.GetOrCreatePath("top/btnClose")`：按路径建节点，便于字段绑定。
- 后续可加：从 YAML 配置、从 Cocos prefab 映射表批量生成等。
