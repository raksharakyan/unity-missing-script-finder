# Unity Missing Script Finder

> Find and remove missing script components across your entire Unity project — scenes, prefabs, everything.
> Full Demo : https://youtu.be/PVFgiFCIs-0

![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black?logo=unity)
<img width="950" height="1202" alt="image" src="https://github.com/user-attachments/assets/09f860d8-93ef-485c-897b-8452feda1dd7" />



---

## The Problem

A missing script shows up as `(Missing Script)` in the Inspector — but finding *which* GameObject has it, especially across large scenes and prefab hierarchies, means clicking through everything manually.

## The Solution

One script. Drop it in your `Editor/` folder. Scan your current scene, every scene in the project, or all prefabs — and get a full list of every missing script with the exact hierarchy path. Then remove them individually or all at once.

---

## Installation

1. Download `MissingScriptFinder.cs`
2. Drop it into **any `Editor/` folder** in your Unity project
3. Done — no Package Manager, no imports, no setup

---

## Usage

**Tools → Missing Script Finder**

### Scan modes

| Mode | What it does |
|---|---|
| Current scene | Scans the open scene. Click any result to select the GameObject instantly |
| All scenes | Opens every scene in the project one by one, scans each, restores your original scene when done |
| All prefabs | Scans every prefab in Assets/ and can remove missing scripts directly from the prefab asset |

### Toolbar

| Button | What it does |
|---|---|
| 🔍 Scan | Runs the scan on the selected target |
| All / None | Tick or untick all result checkboxes at once |
| 🗑 Remove selected (N) | Removes only the checked results |
| 🗑 Remove all | Removes every missing script found |
| 💾 Export | Saves a .txt report to disk |

### Prefab support

Removing missing scripts from prefabs works differently to scenes — the tool handles it automatically:

1. Calls `GameObjectUtility.RemoveMonoBehavioursWithMissingScript` on the prefab's GameObjects
2. Saves the change back to disk via `PrefabUtility.SavePrefabAsset`
3. Flushes with `AssetDatabase.SaveAssets`

No manual save needed — prefab files are updated on disk immediately.

---

## Example output

```
[SampleScene]
  Player/Weapons/Gun  (component index: 2)
  UI/Canvas/HUD  (component index: 1)

[Assets/Prefabs/Enemy.prefab]
  Enemy/Boss/Hitbox  (component index: 3)
```

---

## Features

- ✅ Scan current scene, all scenes, or all prefabs
- ✅ Full hierarchy path for every result (`Player/Weapons/Gun/Muzzle`)
- ✅ Click any result → jumps straight to the GameObject in Hierarchy
- ✅ Checkbox per result — remove only the ones you want
- ✅ Remove selected OR remove all
- ✅ Works on both scene GameObjects and prefab assets
- ✅ Filter bar to search results by name or path
- ✅ Export .txt log report for sharing with your team
- ✅ Select All / None shortcuts

---

## Requirements

- Unity 2021.3 or newer
- No external dependencies

---

## License

MIT © [raksharakyan](https://github.com/raksharakyan)
