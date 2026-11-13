import os
import sys
import json
import struct
import shutil
from pathlib import Path

if sys.stdout and hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8')

PROJECT_ROOT = Path(__file__).resolve().parent.parent
ROYAL_DIR = PROJECT_ROOT / "backend" / "games" / "blast" / "vault_v1" / "arc_royalRush"
B01_DIR = ROYAL_DIR / "b01"

BINARY_MAGIC_HEADER = 0x4C564C42
CURRENT_BINARY_VERSION = 1

def encode_binary(level_data):
    ms = bytearray()
    ms.extend(struct.pack('<IB', BINARY_MAGIC_HEADER, CURRENT_BINARY_VERSION))
    ms.extend(struct.pack('<6iBfi',
        int(level_data.get("levelId", 1)),
        int(level_data.get("tier", 1)),
        int(level_data.get("gridWidth", 7)),
        int(level_data.get("gridHeight", 8)),
        int(level_data.get("gridDepth", 1)),
        int(level_data.get("arsenalColumnsCount", 5)),
        int(level_data.get("winCondition", 0)),
        float(level_data.get("clearPercentageRequired", 100.0)),
        int(level_data.get("targetChestsRequired", 0))
    ))
    enemy_blocks = level_data.get("enemyBlocks", [])
    ms.extend(struct.pack('<i', len(enemy_blocks)))
    for b in enemy_blocks:
        ms.extend(struct.pack('<3i2B6i',
            int(b.get("x", 0)), int(b.get("y", 0)), int(b.get("z", 0)),
            int(b.get("type", 0)), int(b.get("color", 1)),
            int(b.get("hp", 1)), int(b.get("hitThreshold", 10)), int(b.get("gemReward", 5)),
            int(b.get("partnerX", -1)), int(b.get("partnerY", -1)), int(b.get("unlockColIndex", -1))
        ))
    cols = level_data.get("arsenalColumns", [])
    ms.extend(struct.pack('<i', len(cols)))
    for c in cols:
        blocks = c.get("blocks", [])
        ms.extend(struct.pack('<i?2i',
            int(c.get("columnIndex", 0)),
            bool(c.get("isLockedInitially", False)),
            int(c.get("unlockKeyId", 0)),
            len(blocks)
        ))
        for b in blocks:
            ms.extend(struct.pack('<B2i?',
                int(b.get("color", 1)),
                int(b.get("bulletCount", 20)),
                int(b.get("tier", 1)),
                bool(b.get("isHidden", False))
            ))
    return bytes(ms)

B01_DIR.mkdir(parents=True, exist_ok=True)

# Find all l*.json files in ROYAL_DIR
level_files = list(ROYAL_DIR.glob("l*.json"))
print(f"Found {len(level_files)} level files in arc_royalRush")

for lf in level_files:
    target_json = B01_DIR / lf.name
    shutil.move(str(lf), str(target_json))
    
    # Read and encode to .bin
    with open(target_json, "r", encoding="utf-8") as f:
        data = json.load(f)
    bin_bytes = encode_binary(data)
    target_bin = B01_DIR / f"{lf.stem}.bin"
    with open(target_bin, "wb") as f:
        f.write(bin_bytes)

# Move tracks_manifest.json if present
tm = ROYAL_DIR / "tracks_manifest.json"
if tm.exists():
    shutil.move(str(tm), str(B01_DIR / "tracks_manifest.json"))

# Create batch_manifest.json
manifest = {
    "batchId": "b01",
    "archetype": "arc_royalRush",
    "label": "Royal Rush Batch",
    "description": "High-intensity fast dopamine flow batch (100 levels) with rapid clear loops.",
    "generatedAt": "2026-11-09",
    "levelCount": 100,
    "locked": False
}
with open(B01_DIR / "batch_manifest.json", "w", encoding="utf-8") as f:
    json.dump(manifest, f, indent=2)

print("arc_royalRush/b01 populated successfully with json and bin files!")
