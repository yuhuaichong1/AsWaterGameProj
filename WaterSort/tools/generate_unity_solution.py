#!/usr/bin/env python3
"""Generate Visual Studio solution (.sln) and project (.csproj) files for this Unity project."""

from __future__ import annotations

import os
import re
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets"
UNITY_VERSION = "2022.3.62f2"
UNITY_MANAGED = Path(
    rf"C:\Program Files\Unity\Hub\Editor\{UNITY_VERSION}\Editor\Data\Managed"
)

PROJECT_NAME = "WaterSort"

ASSEMBLIES = [
    {
        "name": "Assembly-CSharp",
        "root": ASSETS,
        "exclude_dirs": {
            ASSETS / "Editor",
            ASSETS / "Spine",
            ASSETS / "Spine Examples",
        },
        "editor": False,
        "refs": ["spine-unity"],
    },
    {
        "name": "Assembly-CSharp-Editor",
        "root": ASSETS / "Editor",
        "exclude_dirs": set(),
        "editor": True,
        "refs": ["Assembly-CSharp", "spine-unity", "spine-unity-editor"],
    },
    {
        "name": "spine-unity",
        "root": ASSETS / "Spine" / "Runtime",
        "exclude_dirs": set(),
        "editor": False,
        "refs": [],
    },
    {
        "name": "spine-unity-editor",
        "root": ASSETS / "Spine" / "Editor",
        "exclude_dirs": set(),
        "editor": True,
        "refs": ["spine-unity"],
    },
    {
        "name": "spine-unity-examples",
        "root": ASSETS / "Spine Examples",
        "exclude_dirs": {ASSETS / "Spine Examples" / "Scripts" / "Sample Components" / "SkeletonUtility Modules" / "Editor"},
        "editor": False,
        "refs": ["spine-unity"],
    },
    {
        "name": "spine-unity-examples-editor",
        "root": ASSETS / "Spine Examples" / "Scripts" / "Sample Components" / "SkeletonUtility Modules" / "Editor",
        "exclude_dirs": set(),
        "editor": True,
        "refs": ["spine-unity", "spine-unity-examples"],
    },
]

UNITY_ENGINE_REFS = [
    "UnityEngine.dll",
    "UnityEngine.CoreModule.dll",
    "UnityEngine.UI.dll",
    "UnityEngine.UIModule.dll",
    "UnityEngine.TextRenderingModule.dll",
    "UnityEngine.InputLegacyModule.dll",
    "UnityEngine.PhysicsModule.dll",
    "UnityEngine.Physics2DModule.dll",
    "UnityEngine.AnimationModule.dll",
    "UnityEngine.AudioModule.dll",
    "UnityEngine.ParticleSystemModule.dll",
    "UnityEngine.SpriteMaskModule.dll",
    "UnityEngine.ImageConversionModule.dll",
    "UnityEngine.JSONSerializeModule.dll",
]

UNITY_EDITOR_REFS = [
    "UnityEditor.dll",
    "UnityEditor.CoreModule.dll",
]


def is_under(path: Path, parent: Path) -> bool:
    try:
        path.resolve().relative_to(parent.resolve())
        return True
    except ValueError:
        return False


def collect_cs_files(root: Path, exclude_dirs: set[Path]) -> list[Path]:
    if not root.exists():
        return []
    files: list[Path] = []
    for dirpath, dirnames, filenames in os.walk(root):
        current = Path(dirpath)
        if any(is_under(current, ex) for ex in exclude_dirs if ex != root):
            dirnames[:] = []
            continue
        if "Editor" in dirnames and root.name != "Editor" and not any(
            is_under(current, ASSETS / "Spine") or is_under(current, ASSETS / "Spine Examples") for _ in [0]
        ):
            # Keep Editor subfolders only when scanning dedicated editor assembly roots.
            if root.name != "Editor" and not str(current).endswith("Editor"):
                dirnames.remove("Editor")
        for filename in filenames:
            if filename.endswith(".cs"):
                files.append(Path(dirpath) / filename)
    return sorted(files)


def stable_guid(name: str) -> str:
    return str(uuid.uuid5(uuid.NAMESPACE_DNS, f"watersort.{name}")).upper()


def xml_escape(text: str) -> str:
    return (
        text.replace("&", "&amp;")
        .replace("<", "&lt;")
        .replace(">", "&gt;")
        .replace('"', "&quot;")
    )


def find_dll(name: str) -> str | None:
    for folder in [UNITY_MANAGED, UNITY_MANAGED / "UnityEngine"]:
        candidate = folder / name
        if candidate.exists():
            return str(candidate).replace("\\", "/")
    return None


def write_csproj(assembly: dict, cs_files: list[Path], project_guids: dict[str, str]) -> None:
    name = assembly["name"]
    editor = assembly["editor"]
    lines: list[str] = [
        '<?xml version="1.0" encoding="utf-8"?>',
        '<Project ToolsVersion="4.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">',
        "  <PropertyGroup>",
        "    <LangVersion>9.0</LangVersion>",
        "  </PropertyGroup>",
        "  <PropertyGroup>",
        "    <Configuration Condition=\" '$(Configuration)' == '' \">Debug</Configuration>",
        "    <Platform Condition=\" '$(Platform)' == '' \">AnyCPU</Platform>",
        "    <ProductVersion>10.0.20506</ProductVersion>",
        "    <SchemaVersion>2.0</SchemaVersion>",
        f"    <RootNamespace></RootNamespace>",
        f"    <ProjectGuid>{{{project_guids[name]}}}</ProjectGuid>",
        "    <OutputType>Library</OutputType>",
        "    <AppDesignerFolder>Properties</AppDesignerFolder>",
        f"    <AssemblyName>{xml_escape(name)}</AssemblyName>",
        "    <TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>",
        "    <FileAlignment>512</FileAlignment>",
        "    <BaseDirectory>.</BaseDirectory>",
        "  </PropertyGroup>",
        "  <PropertyGroup Condition=\" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' \">",
        "    <DebugSymbols>true</DebugSymbols>",
        "    <DebugType>full</DebugType>",
        "    <Optimize>false</Optimize>",
        "    <OutputPath>Temp\\Bin\\Debug\\</OutputPath>",
        "    <DefineConstants>UNITY_2022_3_62;UNITY_2022_3;UNITY_2022;UNITY_5_3_OR_NEWER;UNITY_5_4_OR_NEWER;UNITY_5_5_OR_NEWER;DEBUG;TRACE</DefineConstants>",
        "    <ErrorReport>prompt</ErrorReport>",
        "    <WarningLevel>4</WarningLevel>",
        "    <NoWarn>0169;USG0001</NoWarn>",
        "    <AllowUnsafeBlocks>False</AllowUnsafeBlocks>",
        "  </PropertyGroup>",
        "  <PropertyGroup Condition=\" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' \">",
        "    <DebugType>pdbonly</DebugType>",
        "    <Optimize>true</Optimize>",
        "    <OutputPath>Temp\\Bin\\Release\\</OutputPath>",
        "    <DefineConstants>UNITY_2022_3_62;UNITY_2022_3;UNITY_2022;UNITY_5_3_OR_NEWER;TRACE</DefineConstants>",
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

    for dll in UNITY_ENGINE_REFS:
        hint = find_dll(dll)
        if hint:
            lines.append(f'    <Reference Include="{dll[:-4]}">')
            lines.append(f'      <HintPath>{hint}</HintPath>')
            lines.append("    </Reference>")

    if editor:
        for dll in UNITY_EDITOR_REFS:
            hint = find_dll(dll)
            if hint:
                lines.append(f'    <Reference Include="{dll[:-4]}">')
                lines.append(f'      <HintPath>{hint}</HintPath>')
                lines.append("    </Reference>")

    lines.append("  </ItemGroup>")
    lines.append("  <ItemGroup>")

    for cs in cs_files:
        rel = cs.relative_to(ROOT).as_posix()
        lines.append(f'    <Compile Include="{rel}" />')

    lines.extend(["  </ItemGroup>", "  <Import Project=\"$(MSBuildToolsPath)\\Microsoft.CSharp.targets\" />", "</Project>", ""])

    (ROOT / f"{name}.csproj").write_text("\n".join(lines), encoding="utf-8-sig")


def write_sln(project_guids: dict[str, str]) -> None:
    lines = [
        "Microsoft Visual Studio Solution File, Format Version 12.00",
        "# Visual Studio 15",
    ]
    for name in project_guids:
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
            "\tEndGlobalSection",
            "\tGlobalSection(ProjectConfigurationPlatforms) = postSolution",
        ]
    )
    for guid in project_guids.values():
        lines.extend(
            [
                f"\t\t{{{guid}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU",
                f"\t\t{{{guid}}}.Debug|Any CPU.Build.0 = Debug|Any CPU",
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
    (ROOT / f"{PROJECT_NAME}.sln").write_text("\n".join(lines), encoding="utf-8-sig")


def main() -> None:
    if not UNITY_MANAGED.exists():
        raise SystemExit(f"Unity managed folder not found: {UNITY_MANAGED}")

    project_guids = {asm["name"]: stable_guid(asm["name"]) for asm in ASSEMBLIES}

    for assembly in ASSEMBLIES:
        cs_files = collect_cs_files(assembly["root"], assembly["exclude_dirs"])
        write_csproj(assembly, cs_files, project_guids)
        print(f"Wrote {assembly['name']}.csproj ({len(cs_files)} files)")

    write_sln(project_guids)
    print(f"Wrote {PROJECT_NAME}.sln")


if __name__ == "__main__":
    main()
