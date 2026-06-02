#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""批量检查 Cocos 弹窗导入清单：源文件、JSON 解析、Label、Sprite UUID。"""
import json
import os
import re
import sys
from pathlib import Path

COCOS_ROOT = Path(r"d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js")
UNITY_ROOT = Path(r"d:\BaiduNetdiskDownload\水排序\WaterSort")
ASSETS = COCOS_ROOT / "assets"

ENTRIES = [
    ("SettingPopup", "SettingPopup/Prefab/SettingPopup.prefab", "SettingPopupView"),
    ("SuccessPopup", "SuccessPopup/Prefab/SuccessPopup.prefab", "SuccessPopupView"),
    ("NewPlayPopup", "NewPlayPopup/Prefab/NewPlayPopup.prefab", "NewPlayPopupView"),
    ("RankPopup", "RankPopup/Prefab/RankPopup.prefab", "RankPopupView"),
    ("CollectPopup", "CollectPopup/Prefab/CollectPopup.prefab", "CollectPopupView"),
    ("GetCollectPopup", "GetCollectPopup/Prefab/GetCollectPopup.prefab", "GetCollectPopupView"),
    ("RecoverHeartPopup", "RecoverHeartPopup/Prefab/RecoverHeartPopup.prefab", "RecoverHeartPopupView"),
    ("GetHeartPopup", "RecoverHeartPopup/Prefab/GetHeartPopup.prefab", "GetHeartPopupView"),
    ("DailyHeartPopup", "RecoverHeartPopup/Prefab/DailyHeartPopup.prefab", "DailyHeartPopupView"),
    ("FeedbackPopup", "FeedbackPopup/Prefab/FeedbackPopup.prefab", "FeedbackPopupView"),
    ("ClickPopup", "ClickPopup/Prefab/ClickPopup.prefab", None),
    ("RankItem", "RankPopup/Prefab/RankItem.prefab", None),
    ("RankUpItem", "SuccessPopup/Prefab/RankUpItem.prefab", None),
    ("CollectItem", "CollectPopup/Prefab/CollectItem.prefab", None),
    ("GetCollectItem", "GetCollectPopup/Prefab/GetCollectItem.prefab", None),
    ("HeartItem", "Res/Prefab/HeartItem.prefab", None),
]

# Cocos Creator 内置 SpriteFrame（仅存在于 library/imports，无 assets 下 png）
COCOS_BUILTIN_SPRITE_UUIDS = {
    "a23235d1-15db-4b95-8439-a2e005bfff91",  # default_sprite_splash
    "f0048c10-f03e-4c97-b9d3-3506e1d58952",  # default_btn_normal
    "4cd41fc5-6768-4578-9941-fb9a42ae9b04",  # default_btn_pressed
    "ec97937b-4c48-47ed-9066-24047794395a",  # default_btn_disabled
    "ff0e91c7-55c6-4086-a39f-cb6e457b8c3b",  # default_editbox_bg
}


def load_library_default_sprite_uuids(cocos_root: Path, uuid_index: dict) -> set:
    """library/imports 里 name 以 default_ 开头且无法映射到 assets/png 的 SpriteFrame。"""
    imports = cocos_root / "library" / "imports"
    if not imports.is_dir():
        return set()
    found = set()
    for p in imports.glob("**/*.json"):
        try:
            data = json.loads(p.read_text(encoding="utf-8", errors="ignore"))
        except (json.JSONDecodeError, OSError):
            continue
        if not isinstance(data, dict) or data.get("__type__") != "cc.SpriteFrame":
            continue
        content = data.get("content") or {}
        name = content.get("name") or ""
        frame_uuid = p.stem
        if frame_uuid in uuid_index:
            continue
        if name.startswith("default_"):
            found.add(frame_uuid)
    return found

SCRIPT_FILES = {
    "SettingPopupView": "Assets/Scripts/UI/Popups/SettingPopupView.cs",
    "SuccessPopupView": "Assets/Scripts/UI/Popups/SuccessPopupView.cs",
    "NewPlayPopupView": "Assets/Scripts/UI/Popups/NewPlayPopupView.cs",
    "RankPopupView": "Assets/Scripts/UI/Popups/RankPopupView.cs",
    "CollectPopupView": "Assets/Scripts/UI/Popups/CollectPopupView.cs",
    "GetCollectPopupView": "Assets/Scripts/UI/Popups/GetCollectPopupView.cs",
    "RecoverHeartPopupView": "Assets/Scripts/UI/Popups/RecoverHeartPopupView.cs",
    "GetHeartPopupView": "Assets/Scripts/UI/Popups/GetHeartPopupView.cs",
    "DailyHeartPopupView": "Assets/Scripts/UI/Popups/DailyHeartPopupView.cs",
    "FeedbackPopupView": "Assets/Scripts/UI/Popups/FeedbackPopupView.cs",
}


def build_uuid_index(assets_dir: Path) -> dict:
    idx = {}
    uuid_re = re.compile(r'"uuid"\s*:\s*"([a-f0-9\-]+)"', re.I)
    for meta in assets_dir.rglob("*.meta"):
        if not meta.with_suffix("").suffix.lower() in (".png", ".jpg"):
            continue
        try:
            text = meta.read_text(encoding="utf-8", errors="ignore")
        except OSError:
            continue
        png = meta.with_suffix("")
        for m in uuid_re.finditer(text):
            idx.setdefault(m.group(1), str(png))
    return idx


def parse_prefab(path: Path) -> dict:
    raw = path.read_text(encoding="utf-8", errors="ignore")
    arr = json.loads(raw)
    nodes = {}
    labels = []
    sprites = []
    for i, obj in enumerate(arr):
        if not isinstance(obj, dict):
            continue
        t = obj.get("__type__")
        if t == "cc.Node":
            nodes[i] = {
                "name": obj.get("_name", "?"),
                "parent": obj.get("_parent", {}).get("__id__"),
            }
        elif t == "cc.Label":
            s = obj.get("_string") or obj.get("_N$string") or ""
            if s.strip():
                labels.append(s.replace("\\n", "\n"))
        elif t == "cc.Sprite":
            sf = obj.get("_spriteFrame") or {}
            uid = sf.get("__uuid__")
            if uid:
                sprites.append(uid)
    return {"nodes": len(nodes), "labels": labels, "sprites": sprites}


def main():
    issues = []
    rows = []
    uuid_index = build_uuid_index(ASSETS)
    builtin_uuids = set(COCOS_BUILTIN_SPRITE_UUIDS) | load_library_default_sprite_uuids(
        COCOS_ROOT, uuid_index
    )
    print(f"UUID index: {len(uuid_index)} entries, builtins: {len(builtin_uuids)}")

    for name, rel, script in ENTRIES:
        full = ASSETS / rel.replace("/", os.sep)
        row = {"name": name, "ok": True, "notes": []}

        if not full.exists():
            row["ok"] = False
            row["notes"].append("源 prefab 不存在")
            rows.append(row)
            issues.append(f"{name}: 缺少 {full}")
            continue

        if script:
            sp = UNITY_ROOT / SCRIPT_FILES.get(script, "")
            if not sp.exists():
                row["ok"] = False
                row["notes"].append(f"Unity 脚本缺失: {script}")
                issues.append(f"{name}: 脚本不存在 {sp}")

        try:
            info = parse_prefab(full)
            row["labels"] = len(info["labels"])
            row["nodes"] = info["nodes"]
            missing = [u for u in info["sprites"] if u not in uuid_index]
            builtin = [u for u in missing if u in builtin_uuids]
            real_missing = [u for u in missing if u not in COCOS_BUILTIN_SPRITE_UUIDS]
            row["sprites"] = len(info["sprites"])
            row["unresolved_sprites"] = len(real_missing)
            row["builtin_sprites"] = len(builtin)
            if row["labels"] == 0:
                row["notes"].append("无 Label（可能纯图/Spine）")
            if builtin:
                row["notes"].append(f"{len(builtin)} 个为 Cocos 内置按钮/白块（导入时用 Unity 占位）")
            if real_missing:
                row["ok"] = False
                row["notes"].append(f"{len(real_missing)} 个贴图 UUID 无法映射到 png")
                issues.append(f"{name}: 缺失贴图 UUID {real_missing[:3]}")
            row["sample"] = " / ".join(info["labels"][:3])[:60]
        except json.JSONDecodeError as e:
            row["ok"] = False
            row["notes"].append(f"JSON 解析失败: {e}")
            issues.append(f"{name}: JSON 错误")

        rows.append(row)

    # Unity 侧
    manifest = UNITY_ROOT / "Packages/manifest.json"
    if "newtonsoft-json" not in manifest.read_text(encoding="utf-8"):
        issues.append("Packages/manifest.json 缺少 Newtonsoft.Json")

    editor_files = list((UNITY_ROOT / "Assets/Editor/CocosImport").glob("*.cs"))
    if len(editor_files) < 4:
        issues.append("Editor/CocosImport 脚本不完整")

    imported = UNITY_ROOT / "Assets/ImportedFromCocos/UI"
    prefab_count = len(list(imported.glob("*.prefab"))) if imported.exists() else 0

    # 写报告
    out = UNITY_ROOT / "Assets/ImportedFromCocos/PREFLIGHT_CHECK_REPORT.md"
    out.parent.mkdir(parents=True, exist_ok=True)
    lines = [
        "# 导入前检查报告（自动）",
        "",
        f"Cocos: `{COCOS_ROOT}`",
        f"Unity: `{UNITY_ROOT}`",
        "",
        "## 汇总",
        "",
        f"- 清单项: {len(ENTRIES)}",
        f"- 源文件齐全: {sum(1 for r in rows if '不存在' not in str(r.get('notes', [])))} / {len(ENTRIES)}",
        f"- 已生成 Unity Prefab: {prefab_count}（0 表示尚未在 Unity 内点一键导入）",
        f"- 阻塞问题: {len(issues)}",
        "",
        "## 明细",
        "",
        "| 弹窗 | 状态 | 节点 | Label | Sprite | 缺贴图 | 内置占位 | 说明 |",
        "|------|------|------|-------|--------|--------|----------|------|",
    ]
    for r in rows:
        has_warn = any(
            n for n in r.get("notes", [])
            if "无 Label" in n or "内置" in n
        )
        st = "✅" if r["ok"] and not has_warn else ("⚠️" if r["ok"] else "❌")
        notes = "; ".join(r.get("notes", [])) or "-"
        lines.append(
            f"| {r['name']} | {st} | {r.get('nodes', '-')} | {r.get('labels', '-')} | "
            f"{r.get('sprites', '-')} | {r.get('unresolved_sprites', 0)} | "
            f"{r.get('builtin_sprites', 0)} | {notes} |"
        )

    if issues:
        lines.extend(["", "## 需处理", ""])
        for i in issues:
            lines.append(f"- {i}")

    lines.extend([
        "",
        "## 结论",
        "",
    ])
    if not issues and prefab_count == 0:
        lines.append("**检查通过。** 请在 Unity 执行：`WaterSort → Cocos Import → ★ 一键导入全部弹窗 Prefab`")
    elif not issues and prefab_count > 0:
        lines.append(f"**已导入 {prefab_count} 个 Prefab。** 可打开 POPUP_IMPORT_MASTER.md 核对。")
    elif issues:
        lines.append("**存在问题，请先修复再一键导入。**")
    else:
        lines.append("请查看上表。")

    out.write_text("\n".join(lines), encoding="utf-8")
    print(f"Report: {out}")
    print(f"Issues: {len(issues)}")
    for i in issues:
        print(f"  - {i}")
    return 1 if issues else 0


if __name__ == "__main__":
    sys.exit(main())
