#!/usr/bin/env python3
"""
Populates backend/games/blast/vault_v1 with 200 production levels for:
1. arc_linear (Linear monotonic ramp from Tier 1 to Tier 5)
2. arc_sawtooth (4-arc chapter progression with relief breathers and boss spikes)

Generates 3 seeds for each level:
  - _frg (Forgiving / Silent Pity: 1.40x ammo buffer)
  - _clb (Calibrated / Baseline: 1.25x ammo buffer)
  - _tgt (Tight / Challenge: 1.12x ammo buffer)

Saves both human-readable JSON (.json) and compact binary (.bin).
"""

import os
import sys
import json
import struct
import math
from pathlib import Path

if sys.stdout and hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8')

PROJECT_ROOT = Path(__file__).resolve().parent.parent
VAULT_DIR = PROJECT_ROOT / "backend" / "games" / "blast" / "vault_v1"
RESOURCES_DIR = PROJECT_ROOT / "Assets" / "Resources" / "Levels"

BINARY_MAGIC_HEADER = 0x4C564C42  # 'LVLB' in ASCII
CURRENT_BINARY_VERSION = 1


def create_rng(seed):
    s = seed & 0xFFFFFFFF
    def rng():
        nonlocal s
        s = (s + 0x6D2B79F5) & 0xFFFFFFFF
        t = ((s ^ (s >> 15)) * (1 | s)) & 0xFFFFFFFF
        t = (t + (((t ^ (t >> 7)) * (61 | t)) & 0xFFFFFFFF)) ^ t
        t = t & 0xFFFFFFFF
        return ((t ^ (t >> 14)) & 0xFFFFFFFF) / 4294967296.0
    return rng


def get_linear_difficulty(level_idx, total_levels=200):
    norm = level_idx / max(1, total_levels - 1)
    return 10.0 + norm * 85.0  # 10 to 95


def get_sawtooth_difficulty(level_idx, total_levels=200, chapters=4, relief=45):
    cycle_len = total_levels // chapters
    cycle_idx = min(chapters - 1, level_idx // cycle_len)
    idx_in_cycle = level_idx % cycle_len
    pos_in_cycle = idx_in_cycle / max(1, cycle_len - 1)

    cycle_peak = 35 + (cycle_idx * 16)
    cycle_start = max(10, cycle_peak * (1.0 - (relief / 100.0)))
    score = cycle_start + (pos_in_cycle ** 1.4) * (cycle_peak - cycle_start)

    # Boss climax spike on last 2 levels of cycle
    if idx_in_cycle == cycle_len - 1:
        score += 18.0
    elif idx_in_cycle == cycle_len - 2:
        score += 8.0

    return max(5.0, min(99.0, score))


def diff_to_tier(score):
    if score < 28:
        return 1
    elif score < 48:
        return 2
    elif score < 68:
        return 3
    elif score < 84:
        return 4
    else:
        return 5


def generate_level_data(level_id, tier, archetype_name):
    # Deterministic seed per level and archetype
    arch_hash = 777 if archetype_name == "arc_linear" else 999
    rng_core = create_rng(1337 + level_id * 101 + arch_hash)

    width = min(9, max(7, 7 + tier // 2))
    height = min(11, max(8, 7 + tier))
    depth = 1
    arsenal_cols_count = 5

    # Number of available colors scales with tier
    if tier == 1:
        palette = [1, 2, 3]
    elif tier == 2:
        palette = [1, 2, 3, 4]
    elif tier == 3:
        palette = [1, 2, 3, 4, 5]
    elif tier == 4:
        palette = [1, 2, 3, 4, 5, 6]
    else:
        palette = [1, 2, 3, 4, 5, 6, 7, 8]

    enemy_blocks = []
    color_grid = [[1 for _ in range(height)] for _ in range(width)]
    total_blocks_count = 0
    total_enemy_hp = 0

    for x in range(width):
        for y in range(height):
            if x > 0 and rng_core() < 0.55:
                picked = color_grid[x - 1][y]
            elif y > 0 and rng_core() < 0.55:
                picked = color_grid[x][y - 1]
            else:
                picked = palette[int(rng_core() * len(palette))]
            color_grid[x][y] = picked

            block = {
                "x": x, "y": y, "z": 0, "type": 0, "color": picked, "hp": 1,
                "hitThreshold": 10, "gemReward": 5, "partnerX": -1, "partnerY": -1, "unlockColIndex": -1
            }
            enemy_blocks.append(block)
            total_blocks_count += 1
            total_enemy_hp += 1

    # Tier 2+: Hidden blocks
    if tier >= 2:
        hidden_count = min(total_blocks_count // 6, 6 + tier)
        for _ in range(hidden_count):
            rx = int(rng_core() * width)
            ry = int(height // 2 + rng_core() * (height - height // 2))
            for b in enemy_blocks:
                if b["x"] == rx and b["y"] == ry and b["type"] == 0:
                    b["type"] = 1
                    break

    # Tier 3+: Chained blocks & Chests
    if tier >= 3:
        pairs_count = min(tier - 1, 3)
        for _ in range(pairs_count):
            ry = 1 + int(rng_core() * max(1, height // 2 - 1))
            rx = int(rng_core() * (width - 1))
            b1 = next((b for b in enemy_blocks if b["x"] == rx and b["y"] == ry and b["type"] == 0), None)
            b2 = next((b for b in enemy_blocks if b["x"] == rx + 1 and b["y"] == ry and b["type"] == 0), None)
            if b1 and b2:
                b1["type"] = 2; b1["partnerX"] = rx + 1; b1["partnerY"] = ry; b1["hp"] = 2
                b2["type"] = 2; b2["partnerX"] = rx; b2["partnerY"] = ry; b2["hp"] = 2
                total_enemy_hp += 2

        cx = 1 + int(rng_core() * max(1, width - 2))
        cy = int(height // 2 + rng_core() * max(1, height - 1 - height // 2))
        chest = next((b for b in enemy_blocks if b["x"] == cx and b["y"] == cy), None)
        if chest:
            chest["type"] = 5
            chest["hitThreshold"] = 6 + (tier * 2)
            chest["gemReward"] = 5 * tier
            total_enemy_hp += (chest["hitThreshold"] - 1)

    # Tier 4+: Piggies & Keys
    if tier >= 4:
        px = int(rng_core() * width)
        py = 1 + int(rng_core() * max(1, height // 2 - 1))
        piggy = next((b for b in enemy_blocks if b["x"] == px and b["y"] == py and b["type"] == 0), None)
        if piggy:
            piggy["type"] = 6; piggy["hp"] = 4; piggy["gemReward"] = 3
            total_enemy_hp += 3

        kx = int(rng_core() * width)
        ky = 2 + int(rng_core() * max(1, height - 3))
        key_block = next((b for b in enemy_blocks if b["x"] == kx and b["y"] == ky and b["type"] == 0), None)
        if key_block:
            key_block["type"] = 4; key_block["unlockColIndex"] = 1; key_block["hp"] = 2
            total_enemy_hp += 1

    enemy_hp_by_color = {c: 0 for c in range(1, 9)}
    for b in enemy_blocks:
        enemy_hp_by_color[b["color"]] += b["hp"]

    # Generate 3 seeds
    seeds = [
        generate_arsenal_seed(level_id, 1, 1.40, [15, 20], enemy_hp_by_color, tier, arsenal_cols_count, palette),
        generate_arsenal_seed(level_id, 2, 1.25, [10, 15, 20], enemy_hp_by_color, tier, arsenal_cols_count, palette),
        generate_arsenal_seed(level_id, 3, 1.12, [10, 15], enemy_hp_by_color, tier, arsenal_cols_count, palette)
    ]

    base_level = {
        "levelId": level_id,
        "tier": tier,
        "gridWidth": width,
        "gridHeight": height,
        "gridDepth": depth,
        "arsenalColumnsCount": arsenal_cols_count,
        "winCondition": 0,
        "clearPercentageRequired": 100.0,
        "targetChestsRequired": 1 if tier >= 3 else 0,
        "enemyBlocks": enemy_blocks
    }

    return base_level, seeds


def generate_arsenal_seed(level_id, seed_num, buffer_mult, ammo_options, enemy_hp_by_color, tier, col_count, palette):
    rng = create_rng(1337 + level_id * 101 + seed_num * 9999)
    stack_depth = 4
    total_blocks = col_count * stack_depth

    planned_colors = []
    shuffled_palette = list(palette)
    # Shuffle palette deterministically
    for i in range(len(shuffled_palette) - 1, 0, -1):
        j = int(rng() * (i + 1))
        shuffled_palette[i], shuffled_palette[j] = shuffled_palette[j], shuffled_palette[i]

    for i in range(5):
        c = shuffled_palette[i % len(shuffled_palette)]
        planned_colors.extend([c, c, c])
    while len(planned_colors) < total_blocks:
        planned_colors.append(palette[int(rng() * len(palette))])

    arsenal_cols = []
    color_idx = 0
    arsenal_bullets_by_color = {c: 0 for c in range(1, 9)}

    for c in range(col_count):
        is_locked = (tier >= 4 and c == 1)
        col = {
            "columnIndex": c,
            "isLockedInitially": is_locked,
            "unlockKeyId": 1,
            "blocks": []
        }
        for s in range(stack_depth):
            bullets = ammo_options[int(rng() * len(ammo_options))]
            b_color = planned_colors[color_idx % len(planned_colors)]
            color_idx += 1
            is_hidden = (tier >= 2 and s >= 2 and rng() < 0.65)
            col["blocks"].append({
                "color": b_color,
                "bulletCount": bullets,
                "tier": 1,
                "isHidden": is_hidden
            })
            arsenal_bullets_by_color[b_color] += bullets
        arsenal_cols.append(col)

    # Balance ammo: ensure each active enemy color has enough bullets
    for col_id, req_hp in enemy_hp_by_color.items():
        if req_hp > 0:
            target_min = int(math.ceil(req_hp * buffer_mult))
            current = arsenal_bullets_by_color[col_id]
            if current < target_min:
                needed = target_min - current
                for col in arsenal_cols:
                    if needed <= 0:
                        break
                    for b in col["blocks"]:
                        if needed <= 0:
                            break
                        surplus = arsenal_bullets_by_color[b["color"]] - b["bulletCount"]
                        min_surplus = enemy_hp_by_color[b["color"]] * buffer_mult
                        if b["color"] != col_id and surplus >= min_surplus:
                            arsenal_bullets_by_color[b["color"]] -= b["bulletCount"]
                            b["color"] = col_id
                            arsenal_bullets_by_color[col_id] += b["bulletCount"]
                            needed -= b["bulletCount"]

    return {"arsenalColumns": arsenal_cols}


def encode_binary(level_data):
    """Encodes LevelData into exact binary format expected by LevelDecoder.cs"""
    ms = bytearray()

    # Header & Version
    ms.extend(struct.pack('<IB', BINARY_MAGIC_HEADER, CURRENT_BINARY_VERSION))

    # Core Parameters
    ms.extend(struct.pack('<6iBfi',
        level_data["levelId"],
        level_data["tier"],
        level_data["gridWidth"],
        level_data["gridHeight"],
        level_data["gridDepth"],
        level_data["arsenalColumnsCount"],
        level_data["winCondition"],
        float(level_data["clearPercentageRequired"]),
        level_data["targetChestsRequired"]
    ))

    # Enemy Blocks
    enemy_blocks = level_data.get("enemyBlocks", [])
    ms.extend(struct.pack('<i', len(enemy_blocks)))
    for b in enemy_blocks:
        ms.extend(struct.pack('<3i2B6i',
            b["x"], b["y"], b["z"],
            b["type"], b["color"],
            b["hp"], b["hitThreshold"], b["gemReward"],
            b["partnerX"], b["partnerY"], b["unlockColIndex"]
        ))

    # Arsenal Columns
    cols = level_data.get("arsenalColumns", [])
    ms.extend(struct.pack('<i', len(cols)))
    for c in cols:
        ms.extend(struct.pack('<i?2i',
            c["columnIndex"],
            c["isLockedInitially"],
            c["unlockKeyId"],
            len(c.get("blocks", []))
        ))
        for b in c.get("blocks", []):
            ms.extend(struct.pack('<B2i?',
                b["color"],
                b["bulletCount"],
                b["tier"],
                b["isHidden"]
            ))

    return bytes(ms)


def populate_batch(archetype_name, count=200):
    batch_dir = VAULT_DIR / archetype_name / "b01"
    batch_dir.mkdir(parents=True, exist_ok=True)
    print(f"--> Populating {archetype_name}/b01 with {count} levels...")

    seeds_tags = [("frg", 0), ("clb", 1), ("tgt", 2)]

    for i in range(1, count + 1):
        if archetype_name == "arc_linear":
            diff = get_linear_difficulty(i - 1, count)
        else:
            diff = get_sawtooth_difficulty(i - 1, count)

        tier = diff_to_tier(diff)
        base_level, seeds = generate_level_data(i, tier, archetype_name)

        padded = f"{i:03d}"
        for tag, seed_idx in seeds_tags:
            lvl_copy = dict(base_level)
            lvl_copy["arsenalColumns"] = seeds[seed_idx]["arsenalColumns"]

            # 1. Save JSON
            json_path = batch_dir / f"l{padded}_{tag}.json"
            with open(json_path, "w", encoding="utf-8") as f:
                json.dump(lvl_copy, f, indent=2)

            # 2. Save Binary
            bin_path = batch_dir / f"l{padded}_{tag}.bin"
            bin_bytes = encode_binary(lvl_copy)
            with open(bin_path, "wb") as f:
                f.write(bin_bytes)

    # Update batch_manifest.json
    manifest_path = batch_dir / "batch_manifest.json"
    manifest_data = {
        "batchId": "b01",
        "archetype": archetype_name,
        "label": f"Core {archetype_name.replace('arc_', '').title()} Batch",
        "description": f"Compiled 200-level {archetype_name.replace('arc_', '').title()} batch with 3 seeds and binary files.",
        "generatedAt": "2026-11-09",
        "levelCount": count,
        "locked": False
    }
    with open(manifest_path, "w", encoding="utf-8") as f:
        json.dump(manifest_data, f, indent=2)

    print(f"    Done {archetype_name}: {count * 3 * 2} files generated.")


def generate_tracks_manifest(count=200):
    RESOURCES_DIR.mkdir(parents=True, exist_ok=True)
    manifest = {
        "track_linear_a": list(range(1, count + 1)),
        "track_sawtooth_b": [],
        "track_early_c": []
    }

    # Map sawtooth difficulty to level IDs (1..count)
    for i in range(count):
        score = get_sawtooth_difficulty(i, count)
        norm = max(0.0, min(1.0, (score - 10.0) / 85.0))
        mapped = min(count, max(1, round(norm * (count - 1)) + 1))
        manifest["track_sawtooth_b"].append(mapped)

    manifest_path = RESOURCES_DIR / "tracks_manifest.json"
    with open(manifest_path, "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=2)
    print(f"--> Saved tracks_manifest.json to {manifest_path}")

    # Also make sure Level_1.json exists in Resources/Levels for offline fallback
    sample_src = VAULT_DIR / "arc_linear" / "b01" / "l001_clb.json"
    if sample_src.exists():
        with open(sample_src, "r", encoding="utf-8") as sf:
            l1_data = json.load(sf)
        with open(RESOURCES_DIR / "Level_1.json", "w", encoding="utf-8") as tf:
            json.dump(l1_data, tf, indent=2)
        print(f"--> Seeded Resources/Levels/Level_1.json")


def main():
    print("=" * 60)
    print("⚡ Omnipace Batch & Level Generator")
    print("=" * 60)
    populate_batch("arc_linear", 200)
    populate_batch("arc_sawtooth", 200)
    generate_tracks_manifest(200)
    print("=" * 60)
    print("🎉 All 200 Linear & Sawtooth levels successfully populated!")
    print("=" * 60)


if __name__ == "__main__":
    main()
