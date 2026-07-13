# 拆关关卡数据

每关一个 JSON：`level_N.json`，为运行时与编辑器的**唯一真相**。

## 格式示例

```json
{
  "level": 1,
  "cups": [
    {
      "x": -120,
      "y": -194,
      "colors": [3, 3, 3],
      "whNums": 0,
      "isVideo": 0,
      "isLock": 0,
      "lockColor": 0,
      "lockNums": 0,
      "isNull": 0
    }
  ]
}
```

## 编辑器

菜单：**WaterGame → 关卡编辑器**

- **局内区域**：默认设计分辨率 1200×2132，上距顶 800、下距底 240、左右 0（可在编辑器顶部配置，按比例适配其它分辨率）
- **局内预览**：绿框为可摆放瓶子区域；可拖拽移动瓶子
- **撤销 / 重做**：Ctrl+Z / Ctrl+Y
- **加载 / 保存**：读写本目录对应 `level_N.json`
- **从 ConfTotal 导入本关**：仅迁移单关
- **WaterGame → 关卡 → 从 ConfTotal 拆分为单关 JSON**：一次性全量拆分（覆盖已有文件）

## 运行时

`LevelConfigLoader` 优先加载 `Resources/Levels/Split/`；若目录为空则回退 `ConfTotal.json`。
�s5J6q��	_7����'٣�D�-Y�,\#��L{�~N��)�)ADO��.Vry �0:8\1ʤlbf�^.D�������8��r�fp��;乼}���9l��^M;h���ᝥq����0�����