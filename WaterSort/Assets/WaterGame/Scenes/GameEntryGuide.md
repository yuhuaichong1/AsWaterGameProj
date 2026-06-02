# Game Scene & UI Prefabs

Single scene: `Assets/WaterGame/Scenes/Game.unity` (Camera + entry object only).

All UI prefabs live in **one folder**:

```
Assets/WaterGame/Resources/Prefabs/
├── Scenes/     LoadingCanvas, HomeCanvas, GameplayCanvas, PopupCanvas
├── Game/       Bottle, BottleShadow, Pocket
└── UI/Popups/  SettingPopup, RankPopup, ...
```

Runtime load path (no extension): `Resources.Load("Prefabs/Scenes/LoadingCanvas")` etc.

## Setup

1. **WaterSort → Generate All UI Prefabs** (first time or after code layout changes)
2. Edit prefabs under `Assets/WaterGame/Resources/Prefabs/`
3. Open `Game.unity` and press **Play**

## Flow

```
Play → LoadingCanvas → HomeCanvas → GameplayCanvas
Popups → Resources/Prefabs/UI/Popups/*
```
