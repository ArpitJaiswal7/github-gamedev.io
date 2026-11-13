# 🎮 Unity Projects Source Code Repository

Welcome to the source code showcase for Arpit Jaiswal's portfolio labs and engineering prototypes.

This directory contains clean, standalone Unity source projects for playable WebGL labs featured on [arpitjaiswal7.github.io/github-gamedev.io](https://arpitjaiswal7.github.io/github-gamedev.io/).

---

## 📂 Projects Included

| Project | Target Engine | Key Engineering Architecture | Live WebGL Demo |
| :--- | :---: | :--- | :---: |
| [**ThisIsBlast**](./ThisIsBlast) | Unity 6 / URP | OmniPace LiveOps architecture, dynamic 7-archetype progression, automated DDA (silent pity & win-streak challenge), deterministic solver. | [Play Demo ↗](../labs/unity-this-is-blast/index.html) |
| [**Blockade2D**](./Blockade2D) | Unity 6 / 2D | Strategic grid game, BFS fair-play non-trapping path validation, counter-move heuristic AI bot, hold-and-drop blocker placement. | [Play Demo ↗](../labs/unity-blockade2d/index.html) |
| [**ArrowStrike**](./ArrowStrike) | Unity / 2D | Trajectory prediction physics, combat state machine, mobile touch drag-to-aim controls, obstacle ricochet dynamics. | [Play Demo ↗](../labs/unity-calm-arrow/index.html) |
| [**ArgosLocalization**](./ArgosLocalization) | Unity Editor | Automated multi-language translation pipeline via Python ArgosTranslate IPC bridge, TMPro font asset fallback builder. | [View Showcase ↗](../labs/argos-localization.html) |

---

## 🛠️ How to Open Any Project in Unity

All projects in this directory are clean source repositories:
* `Assets/`: Scripts, Materials, Shaders, Prefabs, and Resources.
* `Packages/manifest.json`: Unity Package Manager dependencies.
* `ProjectSettings/`: Input, Graphics, Tags, and Physics layers.

To open any project:
1. Open **Unity Hub**.
2. Click **Add project from disk**.
3. Select any project folder (e.g., `SourceCode/ThisIsBlast` or `SourceCode/Blockade2D`).
4. Unity will automatically restore the `Library/` cache and compile the project cleanly.
