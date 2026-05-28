/** Parse EMOJ simple.log batch-test output into JSON index. */
const BLUEPRINT_RE = /\[Blueprint\]\s*(.+)$/;

const SEED_RE =
  /batch-test seed=(\d+)\s+name=(.+?)\s+material=(\d+)\s+wiki=(\d+)\/22\s+accuracy=(\d+)\/(\d+)\s+mismatch=(\d+)\s+lineage_miss=(\d+)\s+vanilla_leak=(\d+)(?:\s+material_miss=(\d+))?(?:\s+style_slot_match=(\d+)\/(\d+))?(?:\s+set_conf=(\w+)\s+style_align=(\d+)%)?(?:\s+candidates=(\d+))?/;

const SLOT_RE =
  /^\s*(Block|Wall|Bathtub|Bed|Bookcase|Candelabra|Candle|Chandelier|Chair|Chest|Clock|Door|Dresser|Lamp|Lantern|Piano|Platform|Sink|Sofa|Table|Toilet|Workbench)=(\d+)\|([^|]*)\|(.*)$/;

function parseBlueprintLog(filePath) {
  const fs = require('fs');
  const text = fs.readFileSync(filePath, 'utf8');
  const entries = {};
  let current = null;
  let currentSeed = null;
  let inSlots = false;

  const flush = () => {
    if (current && Object.keys(current.slots).length >= 20) {
      entries[String(currentSeed)] = current;
    }
  };

  for (const raw of text.split(/\r?\n/)) {
    const bm = raw.match(BLUEPRINT_RE);
    const line = bm ? bm[1] : raw;

    const sm = line.match(SEED_RE);
    if (sm) {
      flush();
      currentSeed = parseInt(sm[1], 10);
      let name = sm[2].replace(/\s+(incomplete|candidates=\d+).*$/, '').trim();
      current = {
        seed: currentSeed,
        name,
        material: parseInt(sm[3], 10),
        wiki_filled: parseInt(sm[4], 10),
        accuracy: parseInt(sm[5], 10),
        accuracy_denom: parseInt(sm[6], 10),
        slot_mismatch: parseInt(sm[7], 10),
        lineage_miss: parseInt(sm[8], 10),
        vanilla_leak: parseInt(sm[9], 10),
        material_miss: sm[10] ? parseInt(sm[10], 10) : 0,
        style_slot_match: sm[11] ? parseInt(sm[11], 10) : 0,
        style_slot_filled: sm[12] ? parseInt(sm[12], 10) : 0,
        set_conf: sm[13] || null,
        style_align: sm[14] ? parseInt(sm[14], 10) : null,
        candidates: sm[15] ? parseInt(sm[15], 10) : null,
        incomplete: raw.includes('incomplete'),
        slots: {},
      };
      inSlots = false;
      continue;
    }

    if (line.startsWith('batch-test slots seed=')) {
      const sid = parseInt(line.split('seed=')[1].split(/\s/)[0], 10);
      inSlots = sid === currentSeed;
      continue;
    }

    if (inSlots && current) {
      const tm = line.match(SLOT_RE);
      if (tm) {
        current.slots[tm[1]] = {
          type: parseInt(tm[2], 10),
          internal: tm[3],
          display: tm[4],
        };
      } else if (
        !line.trim().startsWith('Slot=') &&
        !line.startsWith('batch-test seed=') &&
        !line.startsWith('batch-test slots seed=')
      ) {
        if (Object.keys(current.slots).length >= 20) {
          flush();
        }
        inSlots = false;
      }
    }
  }
  flush();
  return entries;
}

module.exports = { parseBlueprintLog, SEED_RE, SLOT_RE };
