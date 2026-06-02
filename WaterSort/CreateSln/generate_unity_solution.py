#!/usr/bin/env python3
"""
Portable Unity .sln / .csproj generator.
Place the CreateSln folder next to Assets, then double-click GenerateSln.bat.
"""

from __future__ import annotations

import json
import os
import re
import sys
import uuid
from dataclasses import dataclass, field
from pathlib import Path

TOOL_DIR = Path(__file__).resolve().parent
ROOT = TOOL_DIR.parent
ASSETS = ROOT / "Assets"


@dataclass
class AsmDef:
    name: str
    folder: Path
    references: list[str] = field(default_factory=list)
    is_editor: bool = False
    auto_referenced: bool = True


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig")


def parse_unity_version() -> str:
    version_file = ROOT / "ProjectSettings" / "ProjectVersion.txt"
    if not version_file.exists():
        raise SystemExit(f"找不到 Unity 版本文件: {version_file}")
    match = re.search(r"m_EditorVersion:\s*(\S+)", read_text(version_file))
    if not match:
        raise SystemExit("无法解析 ProjectSettings/ProjectVersion.txt 中的 Unity 版本。")
    return match.group(1)


def parse_project_name() -> str:
    # 解决方案名 = Assets 的父级文件夹名（Unity 工程根目录名）
    return sanitize_filename(ROOT.name)


def sanitize_filename(name: str) -> str:
    cleaned = re.sub(r'[<>:"/\\|?*]', "_", name).strip()
    return cleaned or "UnityProject"


def unity_define_constants(version: str) -> str:
    parts = version.split(".")
    major = parts[0] if len(parts) > 0 else "0"
    minor = parts[1] if len(parts) > 1 else "0"
    patch = parts[2] if len(parts) > 2 else "0"
    patch_num = re.sub(r"[^0-9].*$", "", patch) or "0"
    tokens = [
        f"UNITY_{major}_{minor}_{patch_num}",
        f"UNITY_{major}_{minor}",
        f"UNITY_{major}",
        "UNITY_5_3_OR_NEWER",
        "UNITY_5_4_OR_NEWER",
        "UNITY_5_5_OR_NEWER",
        "DEBUG",
        "TRACE",
    ]
    return ";".join(tokens)


def find_unity_managed(version: str) -> Path:
    candidates: list[Path] = []

    hub_root = Path(r"C:\Program Files\Unity\Hub\Editor")
    if hub_root.exists():
        exact = hub_root / version / "Editor" / "Data" / "Managed"
        candidates.append(exact)
        for editor_dir in sorted(hub_root.iterdir(), reverse=True):
            if editor_dir.is_dir() and editor_dir.name.startswith(version.split(".")[0]):
                candidates.append(editor_dir / "Editor" / "Data" / "Managed")

    for env_key in ("UNITY_EDITOR_PATH", "UNITY_PATH"):
        env = os.environ.get(env_key, "").strip().strip('"')
        if env:
            editor_exe = Path(env)
            if editor_exe.name.lower() == "unity.exe":
                candidates.append(editor_exe.parent / "Data" / "Managed")
            elif (editor_exe / "Editor" / "Unity.exe").exists():
                candidates.append(editor_exe / "Editor" / "Data" / "Managed")

    config = TOOL_DIR / "config.txt"
    if config.exists():
        for line in read_text(config).splitlines():
            line = line.strip()
            if not line or line.startswith("#"):
                continue
            if line.lower().startswith("unity="):
                custom = Path(line.split("=", 1)[1].strip().strip('"'))
                if custom.name.lower() == "managed":
                    candidates.append(custom)
                elif (custom / "Data" / "Managed").exists():
                    candidates.append(custom / "Data" / "Managed")
                elif (custom / "Editor" / "Data" / "Managed").exists():
                    candidates.append(custom / "Editor" / "Data" / "Managed")

    seen: set[str] = set()
    for path in candidates:
        key = str(path).lower()
        if key in seen:
            continue
        seen.add(key)
        if path.exists():
            return path

    raise SystemExit(
        "找不到 Unity 安装目录。\n"
        f"  目标版本: {version}\n"
        "  请安装对应 Unity 版本，或在 CreateSln/config.txt 中设置:\n"
        "  Unity=C:\\Program Files\\Unity\\Hub\\Editor\\2022.3.62f2\\Editor"
    )


def discover_asmdefs() -> list[AsmDef]:
    asmdefs: list[AsmDef] = []
    if not ASSETS.exists():
        raise SystemExit(f"找不到 Assets 目录: {ASSETS}")

    for asmdef_path in sorted(ASSETS.rglob("*.asmdef")):
        data = json.loads(read_text(asmdef_path))
        include = data.get("includePlatforms") or []
        exclude = data.get("excludePlatforms") or []
        is_editor = include == ["Editor"] or (include and "Editor" in include and len(include) == 1)
        asmdefs.append(
            AsmDef(
                name=data.get("name") or asmdef_path.stem,
                folder=asmdef_path.parent,
                references=list(data.get("references") or []),
                is_editor=is_editor,
                auto_referenced=bool(data.get("autoReferenced", True)),
            )
        )
    return asmdefs


def is_under(path: Path, parent: Path) -> bool:
    try:
        path.resolve().relative_to(parent.resolve())
        return True
    except ValueError:
        return False


def nearest_asmdef(file_path: Path, asmdefs: list[AsmDef]) -> AsmDef | None:
    current = file_path.parent
    best: AsmDef | None = None
    best_len = -1
    while is_under(current, ASSETS):
        for asm in asmdefs:
            if current.resolve() == asm.folder.resolve():
                depth = len(current.parts)
                if depth > best_len:
                    best = asm
                    best_len = depth
        if current == ASSETS:
            break
        current = current.parent
    return best


def is_default_editor_script(path: Path) -> bool:
    rel_parts = path.relative_to(ASSETS).parts
    return "Editor" in rel_parts


def is_editor_assembly(name: str, asmdefs: list[AsmDef]) -> bool:
    if name == "Assembly-CSharp-Editor":
        return True
    lower = name.lower()
    if lower.endswith(".editor") or lower.endswith("-editor"):
        return True
    for asm in asmdefs:
        if asm.name == name:
            return asm.is_editor
    return False


def assign_scripts(asmdefs: list[AsmDef]) -> dict[str, dict]:
    buckets: dict[str, list[Path]] = {}
    asm_by_name = {a.name: a for a in asmdefs}

    for cs in sorted(ASSETS.rglob("*.cs")):
        asm = nearest_asmdef(cs, asmdefs)
        if asm is not None:
            buckets.setdefault(asm.name, []).append(cs)
            continue
        if is_default_editor_script(cs):
            buckets.setdefault("Assembly-CSharp-Editor", []).append(cs)
        else:
            buckets.setdefault("Assembly-CSharp", []).append(cs)

    if "Assembly-CSharp" not in buckets:
        buckets["Assembly-CSharp"] = []
    if any(is_default_editor_script(p) for p in ASSETS.rglob("*.cs")) or (ROOT / "Assets" / "Editor").exists():
        buckets.setdefault("Assembly-CSharp-Editor", [])

    # Resolve references for default assemblies
    default_runtime_refs = [a.name for a in asmdefs if a.auto_referenced and not a.is_editor]
    default_editor_refs = ["Assembly-CSharp"] + [a.name for a in asmdefs if not a.is_editor]
    if asmdefs:
        default_editor_refs.extend(a.name for a in asmdefs if a.is_editor)

    assemblies: dict[str, dict] = {}

    for name, files in buckets.items():
        if not files and name not in ("Assembly-CSharp", "Assembly-CSharp-Editor"):
            continue
        assemblies[name] = {
            "name": name,
            "files": files,
            "editor": is_editor_assembly(name, asmdefs),
            "refs": [],
        }

    for asm in asmdefs:
        if asm.name not in assemblies:
            assemblies[asm.name] = {
                "name": asm.name,
                "files": buckets.get(asm.name, []),
                "editor": asm.is_editor,
                "refs": [],
            }

    for asm in asmdefs:
        refs: list[str] = []
        for ref in asm.references:
            if ref in assemblies:
                refs.append(ref)
        assemblies[asm.name]["refs"] = refs

    if "Assembly-CSharp" in assemblies:
        refs = [n for n in default_runtime_refs if n in assemblies and n != "Assembly-CSharp"]
        assemblies["Assembly-CSharp"]["refs"] = refs

    if "Assembly-CSharp-Editor" in assemblies:
        refs = [n for n in default_editor_refs if n in assemblies and n != "Assembly-CSharp-Editor"]
        assemblies["Assembly-CSharp-Editor"]["refs"] = sorted(set(refs))

    return assemblies


def collect_unity_dll_refs(managed: Path, include_editor: bool) -> list[Path]:
    dlls: list[Path] = []
    for folder in [managed, managed / "UnityEngine"]:
        if not folder.exists():
            continue
        for dll in sorted(folder.glob("*.dll")):
            if dll.name.startswith("UnityEngine"):
                dlls.append(dll)
            elif include_editor and dll.name.startswith("UnityEditor"):
                dlls.append(dll)

    # Ensure core assemblies exist
    for required in ["UnityEngine.dll", "UnityEditor.dll"]:
        path = managed / required
        if path.exists() and path not in dlls:
            dlls.append(path)

    unique: list[Path] = []
    seen: set[str] = set()
    for dll in dlls:
        key = dll.name.lower()
        if key in seen:
            continue
        seen.add(key)
        unique.append(dll)
    return unique


def stable_guid(project_key: str, assembly_name: str) -> str:
    return str(uuid.uuid5(uuid.NAMESPACE_URL, f"{project_key}:{assembly_name}")).upper()


def xml_escape(text: str) -> str:
    return text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;").replace('"', "&quot;")


def write_csproj(
    assembly: dict,
    project_guids: dict[str, str],
    managed: Path,
    define_constants: str,
) -> None:
    name = assembly["name"]
    editor = assembly["editor"]
    lines: list[str] = [
        '<?xml version="1.0" encoding="utf-8"?>',
        '<Project ToolsVersion="4.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">',
        "  <PropertyGroup>",
        "    <LangVersion>9.0</LangVersion>",
        "  </PropertyGroup>",
        "  <PropertyGroup>",
        '    <Configuration Condition=" \'$(Configuration)\' == \'\' ">Debug</Configuration>',
        '    <Platform Condition=" \'$(Platform)\' == \'\' ">AnyCPU</Platform>',
        "    <ProductVersion>10.0.20506</ProductVersion>",
        "    <SchemaVersion>2.0</SchemaVersion>",
        "    <RootNamespace></RootNamespace>",
        f"    <ProjectGuid>{{{project_guids[name]}}}</ProjectGuid>",
        "    <OutputType>Library</OutputType>",
        "    <AppDesignerFolder>Properties</AppDesignerFolder>",
        f"    <AssemblyName>{xml_escape(name)}</AssemblyName>",
        "    <TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>",
        "    <FileAlignment>512</FileAlignment>",
        "    <BaseDirectory>.</BaseDirectory>",
        "  </PropertyGroup>",
        '  <PropertyGroup Condition=" \'$(Configuration)|$(Platform)\' == \'Debug|AnyCPU\' ">',
        "    <DebugSymbols>true</DebugSymbols>",
        "    <DebugType>full</DebugType>",
        "    <Optimize>false</Optimize>",
        "    <OutputPath>Temp\\Bin\\Debug\\</OutputPath>",
        f"    <DefineConstants>{define_constants}</DefineConstants>",
        "    <ErrorReport>prompt</ErrorReport>",
        "    <WarningLevel>4</WarningLevel>",
        "    <NoWarn>0169;USG0001</NoWarn>",
        "    <AllowUnsafeBlocks>False</AllowUnsafeBlocks>",
        "  </PropertyGroup>",
        '  <PropertyGroup Condition=" \'$(Configuration)|$(Platform)\' == \'Release|AnyCPU\' ">',
        "    <DebugType>pdbonly</DebugType>",
        "    <Optimize>true</Optimize>",
        "    <OutputPath>Temp\\Bin\\Release\\</OutputPath>",
        f"    <DefineConstants>{define_constants.replace(';DEBUG', '')}</DefineConstants>",
        "    <ErrorReport>prompt</ErrorReport>",
        "    <WarningLevel>4</WarningLevel>",
        "    <NoWarn>0169;USG0001</NoWarn>",
        "    <AllowUnsafeBlocks>False</AllowUnsafeBlocks>",
        "  </PropertyGroup>",
        "  <ItemGroup>",
    ]

    for ref_name in assembly["refs"]:
        if ref_name in project_guids:
            lines.extend(
                [
                    f'    <ProjectReference Include="{ref_name}.csproj">',
                    f"      <Project>{{{project_guids[ref_name]}}}</Project>",
                    f"      <Name>{ref_name}</Name>",
                    "    </ProjectReference>",
                ]
            )

    for dll in collect_unity_dll_refs(managed, include_editor=editor):
        ref_name = dll.stem
        hint = str(dll.resolve()).replace("\\", "/")
        lines.extend(
            [
                f'    <Reference Include="{ref_name}">',
                f"      <HintPath>{hint}</HintPath>",
                "    </Reference>",
            ]
        )

    lines.append("  </ItemGroup>")
    lines.append("  <ItemGroup>")
    for cs in assembly["files"]:
        rel = cs.relative_to(ROOT).as_posix()
        lines.append(f'    <Compile Include="{rel}" />')
    lines.extend(["  </ItemGroup>", '  <Import Project="$(MSBuildToolsPath)\\Microsoft.CSharp.targets" />', "</Project>", ""])

    (ROOT / f"{name}.csproj").write_text("\n".join(lines), encoding="utf-8-sig")


def write_sln(project_name: str, project_guids: dict[str, str]) -> None:
    lines = [
        "Microsoft Visual Studio Solution File, Format Version 12.00",
        "# Visual Studio 15",
    ]
    for name in sorted(project_guids.keys()):
        lines.extend(
            [
                f'Project("{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}") = "{name}", "{name}.csproj", "{{{project_guids[name]}}}"',
                "EndProject",
            ]
        )
    lines.extend(
        [
            "Global",
            "\tGlobalSection(SolutionConfigurationPlatforms) = preSolution",
            "\t\tDebug|Any CPU = Debug|Any CPU",
            "\t\tRelease|Any CPU = Release|Any CPU",
            "\tEndGlobalSection",
            "\tGlobalSection(ProjectConfigurationPlatforms) = postSolution",
        ]
    )
    for guid in project_guids.values():
        lines.extend(
            [
                f"\t\t{{{guid}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU",
                f"\t\t{{{guid}}}.Debug|Any CPU.Build.0 = Debug|Any CPU",
                f"\t\t{{{guid}}}.Release|Any CPU.ActiveCfg = Release|Any CPU",
                f"\t\t{{{guid}}}.Release|Any CPU.Build.0 = Release|Any CPU",
            ]
        )
    lines.extend(
        [
            "\tEndGlobalSection",
            "\tGlobalSection(SolutionProperties) = preSolution",
            "\t\tHideSolutionNode = FALSE",
            "\tEndGlobalSection",
            "EndGlobal",
            "",
        ]
    )
    (ROOT / f"{project_name}.sln").write_text("\n".join(lines), encoding="utf-8-sig")


def validate_project_root() -> None:
    if not ASSETS.is_dir():
        raise SystemExit(
            "CreateSln 文件夹必须放在 Unity 工程根目录（与 Assets 同级）。\n"
            f"  当前检测到工程根目录: {ROOT}\n"
            f"  但未找到: {ASSETS}"
        )
    if not (ROOT / "ProjectSettings").is_dir():
        raise SystemExit(f"未找到 ProjectSettings 目录，请确认这是 Unity 工程: {ROOT / 'ProjectSettings'}")


def main() -> None:
    validate_project_root()
    version = parse_unity_version()
    project_name = parse_project_name()
    managed = find_unity_managed(version)
    define_constants = unity_define_constants(version)

    asmdefs = discover_asmdefs()
    assemblies = assign_scripts(asmdefs)

    # Drop empty optional assemblies except defaults
    filtered = {}
    for name, asm in assemblies.items():
        if asm["files"] or name in ("Assembly-CSharp", "Assembly-CSharp-Editor"):
            filtered[name] = asm
    assemblies = filtered

    project_key = str(ROOT.resolve()).lower()
    project_guids = {name: stable_guid(project_key, name) for name in assemblies}

    print(f"Unity 工程: {ROOT}")
    print(f"Unity 版本: {version}")
    print(f"Unity Managed: {managed}")
    print(f"解决方案名: {project_name}.sln")
    print(f"程序集数量: {len(assemblies)}")
    print("")

    for name in sorted(assemblies.keys()):
        asm = assemblies[name]
        write_csproj(asm, project_guids, managed, define_constants)
        print(f"  [OK] {name}.csproj ({len(asm['files'])} 个脚本)")

    write_sln(project_name, project_guids)
    print("")
    print(f"完成: {ROOT / (project_name + '.sln')}")


if __name__ == "__main__":
    try:
        main()
    except SystemExit as exc:
        print(str(exc))
        sys.exit(1 if str(exc) else 0)
