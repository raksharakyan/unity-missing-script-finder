# Unity Missing Script Finder

> Scan your entire Unity project for missing script components — in one click.

![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black?logo=unity)
![License](https://img.shields.io/badge/license-MIT-green)
![Free](https://img.shields.io/badge/price-free-brightgreen)

---

## The Problem

A missing script in Unity shows up as `(Missing Script)` in the Inspector — but finding *which* GameObject has it, especially in large scenes with deep hierarchies, means clicking through everything manually.

## The Solution

One Editor window. Scan your current scene, every scene in the project, or all prefabs — and get a full list of every missing script with the exact hierarchy path.

---

## Installation

1. Download `MissingScriptFinder.cs`
2. Drop it into **any `Editor/` folder** in your Unity project
3. Done — no setup, no dependencies

---

## Usage

**Tools → Missing Script Finder**

### Scan modes

| Mode | What it does |
|---|---|
| Current scene | Scans the open scene. Click any result to select the GameObject instantly |
| All scenes | Opens every scene in your project, scans each, restores your original scene |
| All prefabs | Scans every prefab in Assets/ without opening any scenes |

### Features

- **Filter bar** — search results by GameObject name or hierarchy path
- **Select button** — click any result to ping & select the GameObject in Hierarchy
- **Remove all missing** — removes all broken components from the current scene in one click
- **Export log** — saves a `.txt` report with scene name, full path, and component index

---

## Example output

```
[SampleScene]
  Player/Weapons/Gun  (component index: 2)
  UI/Canvas/HUD  (component index: 1)

[GameScene]
  Enemy/Boss/Hitbox  (component index: 3)
```

---

## Requirements

- Unity 2021.3 or newer
- No external dependencies

---

## License

MIT © [raksharakyan](https://github.com/raksharakyan)
