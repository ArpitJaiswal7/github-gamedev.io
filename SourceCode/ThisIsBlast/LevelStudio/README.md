# 💥 This is Blast! - Level Studio & Cohort Progression Architect

A dedicated web-based procedural level generator, solvability auditor, and 7-cohort A/B test progression curve architect for **This is Blast!**.

---

## 🧭 Panel Overview & UI Map

The studio interface is divided into 4 clearly labeled panels:

* **[PANEL 1] 📈 Progression & Pacing Architect (Top Left)**:
  * Interactive canvas waveform ($0 \dots 100$ difficulty score).
  * **Dropdown menu** with all 7 proven puzzle pacing archetypes.
  * Real-time curve comparison, wave frequency, and relief depth controls.
* **[PANEL 2] 🎯 Target Board & Arsenal Stacks (Center)**:
  * Front-view target enemy grid visualizer ($7\times8$ up to $9\times11$) with exact colors.
  * Overlays for `Hidden` (❓), `Interlocked` (🔗), `Chest` (📦), `PiggyBank` (🐷), and `Key` (🔑).
  * 5 Arsenal columns $\times$ 4 stack depth with bullet counters and padlock locks.
* **[PANEL 3] ⚖️ Solvability Auditor (Top Right)**:
  * Real-time bullet economy calculation ($\text{Bullets} \ge \text{HP} \times 1.25$).
  * Key drop dependency and bench merge safety verification.
* **[PANEL 4] 🗂️ Batch Library Inspector (Bottom Drawer)**:
  * 200-level horizontal inspector with instant level jumping and tier filtering.

---

## 🎯 The 7 Pacing Archetypes

| Cohort | Pacing Archetype | Psychological Rhythm & Experience |
| :--- | :--- | :--- |
| **Cohort A** | **Linear Smooth** | Traditional monotonic ramp from Tier 1 to 5 across 200 levels. Smooth learning baseline. |
| **Cohort B** | **4-Arc Sawtooth** | 4 distinct 50-level chapters. Each arc ramps through *Warmup $\to$ Ramp $\to$ Climax Boss*, followed by a dopamine breather level to prevent churn. |
| **Cohort C** | **Early Pressure** | Aggressive early difficulty spike at levels 15–30 to test player resilience and ad conversion. |
| **Cohort D** | **Royal Rhythm** | High-dopamine *Royal Match / Toon Blast* rhythm: Hard spike at Lv 4 $\to$ Instant Breeze dopamine reward at Lv 5 $\to$ Super-Hard Boss at Lv 10 $\to$ 2 Celebration levels. |
| **Cohort E** | **Zen Flow Wave** | Harmonic sinusoidal waves (*Meowdoku / Meowsweeper* style). Keeps players in a continuous deep flow state. |
| **Cohort F** | **Staircase Mastery** | Step-function progression with 15-level practice plates. New mechanic introduced sharply, followed by a flat plateau where the player feels in control and skilled. |
| **Cohort G** | **Boss Rush** | 10-level high-stakes sprints. Vault battles with heavy chest and piggy bank rewards every 10 levels. |

---

## 🚀 Quick Start (Local Server)

1. **Double-click `run_studio.bat`** (Windows)  
   *OR* run from terminal:
   ```bash
   cd LevelStudio
   python server.py
   ```
2. Your browser will automatically open `http://localhost:8080`.
3. Click **"Save to Unity"** to write all 200 levels and `tracks_manifest.json` directly into `Assets/Resources/Levels/`.
