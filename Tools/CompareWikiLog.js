#!/usr/bin/env node
/** @deprecated Prefer Tools/AnalyzeBlueprintLog.js */
const fs = require('fs');
const path = require('path');
const { AUDIT_DIR, compareAllWikiSets, findSeedByPrefix, SLOTS } = require('./lib/wikiAudit');

function main() {
  const indexPath = path.join(AUDIT_DIR, 'batch_slots_index.json');
  const index = JSON.parse(fs.readFileSync(indexPath, 'utf8'));

  const results = compareAllWikiSets(index);
  fs.writeFileSync(path.join(AUDIT_DIR, 'wiki_compare_results.json'), JSON.stringify(results, null, 2), 'utf8');

  const prefixes = [
    ['CalamityMod', 'Silva', 'Silva'],
    ['CalamityMod', 'Profaned', 'Profaned'],
    ['CalamityMod', 'Void', 'Void'],
    ['ThoriumMod', 'Rivulet', 'Rivulet'],
    ['SpiritMod', 'Reach', 'Reach'],
  ];

  const auto = [];
  for (const [mod, set, prefix] of prefixes) {
    const seed = findSeedByPrefix(index, prefix);
    if (!seed) {
      auto.push({ mod, set, prefix, seed: null, status: 'no_seed' });
      continue;
    }
    const entry = index[String(seed)];
    const internals = Object.fromEntries(
      SLOTS.filter((s) => entry.slots[s]?.internal).map((s) => [s, entry.slots[s].internal])
    );
    auto.push({
      mod,
      set,
      prefix,
      seed,
      material: entry.material,
      wiki_filled: entry.wiki_filled,
      wiki_audit: entry.wiki_audit,
      lineage_miss: entry.lineage_miss,
      slots: internals,
    });
  }

  fs.writeFileSync(path.join(AUDIT_DIR, 'mod_prefix_scan.json'), JSON.stringify(auto, null, 2), 'utf8');
  console.log(`Compared ${results.length} wiki sets -> wiki_compare_results.json`);
}

main();
