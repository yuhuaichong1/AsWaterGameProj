#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Fix Unity texture .meta under Resources: Sprite (Single) + alpha."""
import json
import re
import uuid
from pathlib import Path

RESOURCES = Path(r"d:\BaiduNetdiskDownload\水排序\WaterSort\Assets\Resources")

UNITY_SPRITE_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def extract_guid(meta_path: Path) -> str:
    text = meta_path.read_text(encoding="utf-8", errors="ignore")
    if text.lstrip().startswith("{"):
        try:
            data = json.loads(text)
            uid = data.get("uuid", "")
            return uid.replace("-", "")
        except json.JSONDecodeError:
            pass
    m = re.search(r"^guid:\s*([a-f0-9]+)", text, re.M | re.I)
    if m:
        return m.group(1)
    return uuid.uuid4().hex


def patch_unity_meta(text: str) -> str:
    text = re.sub(r"enableMipMap:\s*1", "enableMipMap: 0", text)
    text = re.sub(r"textureType:\s*0", "textureType: 8", text)
    text = re.sub(r"spriteMode:\s*0", "spriteMode: 1", text)
    text = re.sub(r"alphaIsTransparency:\s*0", "alphaIsTransparency: 1", text)
    text = re.sub(r"nPOTScale:\s*1", "nPOTScale: 0", text)
    if "spriteID:" in text and "5e97eb03825dee720800000000000000" not in text:
        text = re.sub(r"spriteID:\s*\n", "spriteID: 5e97eb03825dee720800000000000000\n", text)
        text = re.sub(r"spriteID:\s*$", "spriteID: 5e97eb03825dee720800000000000000", text, flags=re.M)
    return text


def main():
    fixed = 0
    replaced = 0
    for png in RESOURCES.rglob("*.png"):
        meta = png.with_suffix(png.suffix + ".meta")
        if not meta.exists():
            guid = uuid.uuid4().hex
            meta.write_text(UNITY_SPRITE_META.format(guid=guid), encoding="utf-8")
            replaced += 1
            continue
        text = meta.read_text(encoding="utf-8", errors="ignore")
        if text.lstrip().startswith("{"):
            guid = extract_guid(meta)
            meta.write_text(UNITY_SPRITE_META.format(guid=guid), encoding="utf-8")
            replaced += 1
        elif "TextureImporter:" in text:
            new_text = patch_unity_meta(text)
            if new_text != text:
                meta.write_text(new_text, encoding="utf-8")
                fixed += 1
    print(f"Replaced Cocos/missing meta: {replaced}, patched Unity meta: {fixed}")


if __name__ == "__main__":
    main()
