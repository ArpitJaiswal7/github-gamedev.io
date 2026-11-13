# 🎮 Unity Projects Source Code Repository

Welcome to the public engineering showcase for Arpit Jaiswal's portfolio labs and games.

This directory contains clean, standalone Unity project source code matching the interactive labs featured on [arpitjaiswal7.github.io/github-gamedev.io](https://arpitjaiswal7.github.io/github-gamedev.io/).

---

## 📂 Source Code Directory Map

| Tab / Project | Source Directory | Architecture & Subsystems | Playable WebGL Lab |
| :--- | :--- | :--- | :---: |
| 🐱 **MeowSweeper** | [`/SourceCode/MeowSweeper`](./MeowSweeper) | Algorithmic puzzle solver, deterministic hint cascade state machine, Level Sync OTA, and **Offline Leaderboard engine**. | [Play Demo ↗](../labs/unity-meowsweeper/index.html) |
| 🧩 **Calm Jigsaw** | [`/SourceCode/CalmJigsaw`](./CalmJigsaw) | Procedural $N \times N$ Bezier mesh slicer, 9 mathematical edge signatures, dynamic quad UV mesh generator, and custom shadowMask shaders. | [Play Demo ↗](../labs/unity-calm-jigsaw/index.html) |
| 🎴 **Solitaire** | [`/SourceCode/JigsawSolitaire`](./JigsawSolitaire) | Displacement swap & seam merge engine, multi-card stack solver, zero seam gaps. | [Play Demo ↗](../labs/unity-jigsaw-solitaire/index.html) |
| 🏹 **Arrow Strike** | [`/SourceCode/ArrowStrike`](./ArrowStrike) | Outside-in peeling algorithm with orthogonal sweep collision tests guaranteeing 100% solvable layouts. | [Play Demo ↗](../labs/unity-calm-arrow/index.html) |
| 🧱 **Blockade 2D** | [`/SourceCode/Blockade2D`](./Blockade2D) | Zero-alloc BFS dual-path escape route validation, counter-move heuristic AI bot, hold-and-drop blocker placement. | [Play Demo ↗](../labs/unity-blockade2d/index.html) |
| 🖼️ **Native Bridge** | [`/SourceCode/NativeImageBridge`](./NativeImageBridge) | Production multi-tier image buffer pipeline: JNI Android (`.java`), iOS Objective-C (`.mm`), and WebGL (`.jslib`) streaming byte arrays into Texture2D. | [View Lab ↗](../labs/native-image-bridge.html) |
| 🏆 **Leaderboard** | [`/SourceCode/MeowSweeper/Assets/Scripts/OfflineLeaderboardService.cs`](./MeowSweeper/Assets/Scripts/OfflineLeaderboardService.cs) | Autonomous simulated offline leaderboard with 29 seeded bot profiles, rank overtake convergence, and anti-tamper ledger. | [View Lab ↗](../labs/offline-leaderboard.html) |
| 📁 **Organizer** | [`/SourceCode/AssetOrganizerTool`](./AssetOrganizerTool) | Custom Unity Editor extension: dependency graph tree view, Project Window badges, hover tag-buttons, and `.unitypackage` exporter. | [View Lab ↗](../labs/asset-organizer.html) |
| 💥 **This is Blast!** | [`/SourceCode/ThisIsBlast`](./ThisIsBlast) | OmniPace LiveOps architecture, 7 difficulty archetypes, automated DDA (silent pity & win-streak challenge), deterministic solver. | [Play Demo ↗](../labs/unity-this-is-blast/index.html) |
| 🌐 **Argos Localization** | [`/SourceCode/ArgosLocalization`](./ArgosLocalization) | Automated multi-language translation pipeline via Python ArgosTranslate IPC bridge, TMPro font asset fallback builder. | [View Lab ↗](../labs/argos-localization.html) |

---

## 🛠️ How to Open Any Project in Unity

All projects in this directory are clean, non-bloated source repositories:
* `Assets/`: Scripts, Materials, Shaders, Prefabs, and Resources.
* `Packages/manifest.json`: Unity Package Manager dependencies.
* `ProjectSettings/`: Input, Graphics, Tags, and Physics layers.

To open any project:
1. Open **Unity Hub**.
2. Click **Add project from disk**.
3. Select any project folder (e.g., `SourceCode/MeowSweeper`, `SourceCode/Blockade2D`, or `SourceCode/ThisIsBlast`).
4. Unity will automatically restore the `Library/` cache and compile the project cleanly.
