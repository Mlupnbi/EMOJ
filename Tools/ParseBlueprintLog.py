#!/usr/bin/env python3
"""Parse EMOJ simple.log batch-test output into JSON index."""
import json
import re
import sys
from pathlib import Path

LOG_PATH = Path.home() / "Documents/My Games/Terraria/tModLoader/Logs/EMOJ/2026-05-27_12-28-08/simple.log"
OUT_DIR = Path(__file__).resolve().parent.parent / "Issues" / "audit_data"

SLOTS = [
    "Block", "Wall", "Bathtub", "Bed", "Bookcase", "Candelabra", "Candle", "Chandelier",
    "Chair", "Chest", "Clock", "Door", "Dresser", "Lamp", "Lantern", "Piano", "Platform",
    "Sink", "Sofa", "Table", "Toilet", "Workbench",
]

SEED_RE = re.compile(
    r"batch-test seed=(\d+)\s+name=(.+?)\s+material=(\d+)\s+wiki=(\d+)/22\s+"
    r"accuracy=(\d+)/(\d+)\s+mismatch=(\d+)\s+lineage_miss=(\d+)\s+vanilla_leak=(\d+)"
)
SLOT_RE = re.compile(
    r"^\s+(Block|Wall|Bathtub|Bed|Bookcase|Candelabra|Candle|Chandelier|"
    r"Chair|Chest|Clock|Door|Dresser|Lamp|Lantern|Piano|Platform|"
    r"Sink|Sofa|Table|Toilet|Workbench)=(\d+)\|([^|]*)\|(.*)$"
)
BLUEPRINT_RE = re.compile(r"\[Blueprint\]\s*(.+)$")


def parse_log(path: Path) -> dict:
    entries = {}
    current = None
    current_seed = None
    in_slots = False

    def flush():
        nonlocal current, current_seed
        if current and len(current["slots"]) >= 20:
            entries[str(current_seed)] = current

    text = path.read_text(encoding="utf-8", errors="replace")
    for raw in text.splitlines():
        m = BLUEPRINT_RE.search(raw)
        line = m.group(1) if m else raw

        sm = SEED_RE.search(line)
        if sm:
            flush()
            current_seed = int(sm.group(1))
            name = sm.group(2)
            name = re.sub(r"\s+(incomplete|candidates=\d+).*$", "", name).strip()
            current = {
                "seed": current_seed,
                "name": name,
                "material": int(sm.group(3)),
                "wiki_filled": int(sm.group(4)),
                "accuracy": int(sm.group(5)),
                "accuracy_denom": int(sm.group(6)),
                "slot_mismatch": int(sm.group(7)),
                "lineage_miss": int(sm.group(8)),
                "vanilla_leak": int(sm.group(9)),
                "incomplete": "incomplete" in raw,
                "slots": {},
            }
            in_slots = False
            continue

        if line.startswith("batch-test slots seed="):
            sid = int(line.split("seed=")[1].split()[0])
            in_slots = sid == current_seed
            continue

        if in_slots and current:
            tm = SLOT_RE.match(line)
            if tm:
                slot = tm.group(1)
                current["slots"][slot] = {
                    "type": int(tm.group(2)),
                    "internal": tm.group(3),
                    "display": tm.group(4),
                }
            elif not line.strip().startswith("Slot="):
                flush()
                in_slots = False

    flush()
    return entries


def main():
    log_path = Path(sys.argv[1]) if len(sys.argv) > 1 else LOG_PATH
    out_dir = Path(sys.argv[2]) if len(sys.argv) > 2 else OUT_DIR
    out_dir.mkdir(parents=True, exist_ok=True)

    entries = parse_log(log_path)
    index_path = out_dir / "batch_slots_index.json"
    index_path.write_text(json.dumps(entries, ensure_ascii=False, indent=2), encoding="utf-8")

    all_entries = list(entries.values())
    scored = [e for e in all_entries if e["accuracy_denom"] > 0]
    high_lineage = sorted(
        [e for e in scored if e["lineage_miss"] >= 5],
        key=lambda e: (-e["lineage_miss"], e["seed"]),
    )[:30]

    summary = {
        "total_seeds": len(all_entries),
        "scored": len(scored),
        "avg_wiki": round(sum(e["wiki_filled"] for e in all_entries) / max(len(all_entries), 1), 2),
        "total_lineage_miss": sum(e["lineage_miss"] for e in scored),
        "high_lineage": [
            {k: e[k] for k in ("seed", "name", "material", "wiki_filled", "lineage_miss", "accuracy", "accuracy_denom")}
            for e in high_lineage
        ],
    }
    (out_dir / "batch_summary.json").write_text(
        json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(f"Parsed {len(all_entries)} seeds -> {index_path}")


if __name__ == "__main__":
    main()
