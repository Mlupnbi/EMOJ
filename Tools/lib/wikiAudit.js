/**
 * Wiki 期望表 + 与 batch 日志槽位对照（开发审计用，非游戏 runtime）。
 */
const fs = require('fs');
const path = require('path');

const AUDIT_DIR = path.join(__dirname, '..', '..', 'Issues', 'audit_data');
const WIKI_CACHE_PATH = path.join(AUDIT_DIR, 'wiki_cache.json');

function loadWikiSetsFromCache() {
  if (!fs.existsSync(WIKI_CACHE_PATH)) return null;
  try {
    const cache = JSON.parse(fs.readFileSync(WIKI_CACHE_PATH, 'utf8'));
    const sets = (cache.sets || [])
      .filter((s) => s.slots && Object.keys(s.slots).length > 0)
      .map((s) => ({
        mod: s.mod,
        set: s.set,
        wiki: s.wiki,
        seed: s.seed ?? null,
        alternateSeeds: s.alternateSeeds || [],
        stylePrefix: s.stylePrefix,
        slots: s.slots,
        notes: [s.notes, s.source ? `cache:${s.source}` : ''].filter(Boolean).join(' '),
      }));
    return sets.length > 0 ? sets : null;
  } catch {
    return null;
  }
}

const SLOTS = [
  'Block', 'Wall', 'Bathtub', 'Bed', 'Bookcase', 'Candelabra', 'Candle', 'Chandelier',
  'Chair', 'Chest', 'Clock', 'Door', 'Dresser', 'Lamp', 'Lantern', 'Piano', 'Platform',
  'Sink', 'Sofa', 'Table', 'Toilet', 'Workbench',
];

/** @type {Array<{mod:string,set:string,wiki:string,seed:number|null,alternateSeeds?:number[],stylePrefix:string,slots:Record<string,string>,notes?:string}>} */
const WIKI_SETS_INLINE = [
  {
    mod: 'Terraria',
    set: 'Living Wood',
    wiki: 'https://terraria.wiki.gg/wiki/Living_Wood_furniture',
    seed: 829,
    stylePrefix: 'LivingWood',
    slots: {
      Block: 'Wood',
      Wall: 'LivingWoodWall',
      Bathtub: 'LivingWoodBathtub',
      Bed: 'LivingWoodBed',
      Bookcase: 'LivingWoodBookcase',
      Candelabra: 'LivingWoodCandelabra',
      Candle: 'LivingWoodCandle',
      Chandelier: 'LivingWoodChandelier',
      Chair: 'LivingWoodChair',
      Chest: 'LivingWoodChest',
      Clock: 'LivingWoodClock',
      Door: 'LivingWoodDoor',
      Dresser: 'LivingWoodDresser',
      Lamp: 'LivingWoodLamp',
      Lantern: 'LivingWoodLantern',
      Piano: 'LivingWoodPiano',
      Platform: 'LivingWoodPlatform',
      Sink: 'LivingWoodSink',
      Sofa: 'LivingWoodSofa',
      Table: 'LivingWoodTable',
      Toilet: 'ToiletLivingWood',
      Workbench: 'LivingWoodWorkBench',
    },
  },
  {
    mod: 'Terraria',
    set: 'Marble',
    wiki: 'https://terraria.wiki.gg/wiki/Marble_furniture',
    seed: 3154,
    alternateSeeds: [3163],
    stylePrefix: 'Marble',
    slots: {
      Block: 'MarbleBlock',
      Wall: 'MarbleBlockWall',
      Bathtub: 'MarbleBathtub',
      Bed: 'MarbleBed',
      Bookcase: 'MarbleBookcase',
      Candelabra: 'MarbleCandelabra',
      Candle: 'MarbleCandle',
      Chandelier: 'MarbleChandelier',
      Chair: 'MarbleChair',
      Chest: 'MarbleChest',
      Clock: 'MarbleClock',
      Door: 'MarbleDoor',
      Dresser: 'MarbleDresser',
      Lamp: 'MarbleLamp',
      Lantern: 'MarbleLantern',
      Piano: 'MarblePiano',
      Platform: 'MarblePlatform',
      Sink: 'MarbleSink',
      Sofa: 'MarbleSofa',
      Table: 'MarbleTable',
      Toilet: 'ToiletMarble',
      Workbench: 'MarbleWorkBench',
    },
    notes: '3163 = Marble Bed regression seed (same wiki set as 3154).',
  },
  {
    mod: 'Terraria',
    set: 'Spider',
    wiki: 'https://terraria.wiki.gg/wiki/Spider_furniture',
    seed: 3932,
    stylePrefix: 'Spider',
    slots: {
      Block: 'SpiderBlock',
      Wall: 'SpiderWall',
      Bathtub: 'SpiderBathtub',
      Bed: 'SpiderBed',
      Bookcase: 'SpiderBookcase',
      Candelabra: 'SpiderCandelabra',
      Candle: 'SpiderCandle',
      Chandelier: 'SpiderChandelier',
      Chair: 'SpiderChair',
      Chest: 'SpiderChest',
      Clock: 'SpiderClock',
      Door: 'SpiderDoor',
      Dresser: 'SpiderDresser',
      Lamp: 'SpiderLamp',
      Lantern: 'SpiderLantern',
      Piano: 'SpiderPiano',
      Platform: 'SpiderPlatform',
      Sink: 'SpiderSinkSpiderSinkDoesWhateverASpiderSinkDoes',
      Sofa: 'SpiderSofa',
      Table: 'SpiderTable',
      Toilet: 'ToiletSpider',
      Workbench: 'SpiderWorkbench',
    },
  },
  {
    mod: 'Terraria',
    set: 'Cactus',
    wiki: 'https://terraria.wiki.gg/wiki/Cactus_furniture',
    seed: 812,
    stylePrefix: 'Cactus',
    slots: {
      Block: 'Cactus',
      Wall: 'CactusWall',
      Workbench: 'CactusWorkBench',
      Table: 'CactusTable',
      Chair: 'CactusChair',
      Bed: 'CactusBed',
      Door: 'CactusDoor',
      Platform: 'CactusPlatform',
    },
    notes: 'Partial wiki row — not all 22 slots documented on wiki.',
  },
  {
    mod: 'CalamityMod',
    set: 'Abyss',
    wiki: 'https://calamitymod.wiki.gg/wiki/Furniture_sets',
    seed: 6707,
    stylePrefix: 'Abyss',
    slots: {
      Block: 'SmoothAbyssGravel',
      Wall: 'SmoothAbyssGravelWall',
      Bathtub: 'AbyssBathtub',
      Bed: 'AbyssBed',
      Bookcase: 'AbyssBookcase',
      Candelabra: 'AbyssCandelabra',
      Candle: 'AbyssCandle',
      Chandelier: 'AbyssChandelier',
      Chair: 'AbyssChair',
      Chest: 'AbyssChest',
      Clock: 'AbyssClock',
      Door: 'AbyssDoor',
      Lamp: 'AbyssLamp',
      Lantern: 'AbyssLantern',
      Piano: 'AbyssSynth',
      Platform: 'SmoothAbyssGravelPlatform',
      Sink: 'AbyssSink',
      Sofa: 'AbyssSofa',
      Table: 'AbyssTable',
    },
    notes: 'Calamity Abyss — wiki subset; Piano = AbyssSynth.',
  },
  {
    mod: 'CalamityMod',
    set: 'Statigel',
    wiki: 'https://calamitymod.wiki.gg/wiki/Furniture_sets',
    seed: 7095,
    stylePrefix: 'Statigel',
    slots: {
      Block: 'StatigelBlock',
      Wall: 'StatigelWall',
      Bathtub: 'StatigelBathtub',
      Bed: 'StatigelBed',
      Bookcase: 'StatigelBookcase',
      Candelabra: 'StatigelCandelabra',
      Candle: 'StatigelCandle',
      Chandelier: 'StatigelChandelier',
      Chair: 'StatigelChair',
      Chest: 'StatigelChest',
      Clock: 'StatigelClock',
      Door: 'StatigelDoor',
      Dresser: 'StatigelDresser',
      Lamp: 'StatigelLamp',
      Lantern: 'StatigelLantern',
      Piano: 'StatigelPiano',
      Platform: 'StatigelPlatform',
      Sink: 'StatigelSink',
      Sofa: 'StatigelSofa',
      Table: 'StatigelTable',
      Toilet: 'StatigelToilet',
      Workbench: 'StatigelWorkBench',
    },
  },
  {
    mod: 'HomewardJourney',
    set: 'Death',
    wiki: 'https://homewardjourney.wiki.gg/zh/wiki/家具',
    seed: 13843,
    alternateSeeds: [13846],
    stylePrefix: 'ItemDeath',
    slots: {
      Bookcase: 'ItemDeathBookcase',
      Candelabra: 'ItemDeathCandelabra',
      Candle: 'ItemDeathCandle',
      Chandelier: 'ItemDeathChandelier',
      Chair: 'ItemDeathChair',
      Clock: 'ItemDeathClock',
      Door: 'ItemDeathDoor',
      Dresser: 'ItemDeathDresser',
      Lamp: 'ItemDeathLamp',
      Lantern: 'ItemDeathLantern',
      Piano: 'ItemDeathPiano',
      Sofa: 'ItemDeathSofa',
      Table: 'ItemDeathTable',
      Toilet: 'ItemDeathToilet',
      Chest: 'ItemDeathSink',
    },
    notes: 'HJ Death decorative subset; Chest slot = ItemDeathSink on wiki.',
  },
  {
    mod: 'HomewardJourney',
    set: 'Nothingness',
    wiki: 'https://homewardjourney.wiki.gg/zh/wiki/家具',
    seed: 13904,
    stylePrefix: 'ItemNothingness',
    slots: {
      Bookcase: 'ItemNothingnessBookcase',
      Candelabra: 'ItemNothingnessCandelabra',
      Candle: 'ItemNothingnessCandle',
      Chandelier: 'ItemNothingnessChandelier',
      Chair: 'ItemNothingnessChair',
      Clock: 'ItemNothingnessClock',
      Door: 'ItemNothingnessDoor',
      Dresser: 'ItemNothingnessDresser',
      Lamp: 'ItemNothingnessLamp',
      Lantern: 'ItemNothingnessLantern',
      Piano: 'ItemNothingnessPiano',
      Sofa: 'ItemNothingnessSofa',
      Table: 'ItemNothingnessTable',
      Toilet: 'ItemNothingnessToilet',
      Chest: 'ItemNothingnessSink',
    },
  },
  {
    mod: 'ThoriumMod',
    set: 'Thorium',
    wiki: 'https://thoriummod.wiki.gg/wiki/Furniture_sets',
    seed: 10531,
    stylePrefix: 'Thorium',
    slots: {
      Block: 'ThoriumBrick',
      Wall: 'ThoriumBrickWall',
      Bathtub: 'ThoriumBathtub',
      Bed: 'ThoriumBed',
      Bookcase: 'ThoriumBookcase',
      Candelabra: 'ThoriumCandelabra',
      Candle: 'ThoriumCandle',
      Chandelier: 'ThoriumChandelier',
      Chair: 'ThoriumChair',
      Chest: 'ThoriumChest',
      Clock: 'ThoriumClock',
      Door: 'ThoriumDoor',
      Dresser: 'ThoriumDresser',
      Lamp: 'ThoriumLamp',
      Lantern: 'ThoriumLantern',
      Piano: 'ThoriumPiano',
      Platform: 'ThoriumPlatform',
      Sink: 'ThoriumSink',
      Sofa: 'ThoriumSofa',
      Table: 'ThoriumTable',
      Toilet: 'ThoriumToilet',
      Workbench: 'ThoriumWorkbench',
    },
  },
];

const WIKI_SETS = loadWikiSetsFromCache() || WIKI_SETS_INLINE;

function classifySlot(expected, actual) {
  if (!expected) {
    if (!actual || actual.type === 0) return 'empty_ok';
    return 'wiki_extra';
  }
  if (!actual || actual.type === 0) return 'empty_miss';
  if (actual.internal === expected) return 'match';
  if (actual.internal.includes(expected) || expected.includes(actual.internal)) return 'match_fuzzy';
  return 'wrong_item';
}

function entryHasStylePrefix(entry, prefix) {
  if (!entry?.slots || !prefix) return false;
  let hits = 0;
  for (const slot of Object.values(entry.slots)) {
    if (slot?.internal && slot.internal.startsWith(prefix)) hits++;
  }
  return hits >= 3;
}

function findSeedByPrefix(index, prefix) {
  for (const [key, entry] of Object.entries(index)) {
    if (entryHasStylePrefix(entry, prefix)) return parseInt(key, 10);
  }
  return null;
}

function resolveLogSeed(wikiSet, index, preferredSeed) {
  const candidates = [
    preferredSeed,
    wikiSet.seed,
    ...(wikiSet.alternateSeeds || []),
  ].filter((s) => s != null);

  for (const seed of candidates) {
    if (index[String(seed)]) return seed;
  }
  return findSeedByPrefix(index, wikiSet.stylePrefix);
}

function findWikiSetForEntry(seed, entry) {
  for (const set of WIKI_SETS) {
    if (set.seed === seed) return set;
    if (set.alternateSeeds?.includes(seed)) return set;
  }
  for (const set of WIKI_SETS) {
    if (entryHasStylePrefix(entry, set.stylePrefix)) return set;
  }
  return null;
}

function compareSet(wikiSet, index, preferredSeed = null) {
  const seed = resolveLogSeed(wikiSet, index, preferredSeed ?? wikiSet.seed);
  const entry = seed ? index[String(seed)] : null;
  const diffs = [];
  let match = 0;
  let checked = 0;

  for (const slot of SLOTS) {
    const expected = wikiSet.slots[slot];
    if (!expected) continue;
    checked++;
    const actual = entry?.slots?.[slot];
    const status = classifySlot(expected, actual);
    if (status === 'match' || status === 'match_fuzzy') match++;
    else {
      diffs.push({
        slot,
        status,
        expected,
        actual_type: actual?.type ?? 0,
        actual_internal: actual?.internal ?? '',
      });
    }
  }

  const codeAccurate = entry?.accuracy ?? 0;
  const codeFilled = entry?.accuracy_denom ?? 0;
  const codeRate = codeFilled > 0 ? codeAccurate / codeFilled : null;
  const wikiRate = checked > 0 ? match / checked : null;

  return {
    mod: wikiSet.mod,
    set: wikiSet.set,
    wiki: wikiSet.wiki,
    seed,
    material: entry?.material ?? null,
    wiki_filled: entry?.wiki_filled ?? null,
    code_accuracy: entry ? `${codeAccurate}/${codeFilled}` : 'no_log',
    code_accuracy_rate: codeRate,
    wiki_match: match,
    wiki_checked: checked,
    wiki_match_rate: wikiRate,
    accuracy_gap:
      codeRate != null && wikiRate != null
        ? Math.round((codeRate - wikiRate) * 1000) / 1000
        : null,
    diffs,
    notes: wikiSet.notes || '',
  };
}

/** 为日志 index 中每个 seed 附加 wiki_audit（若能在期望表中找到套组）。 */
function auditLogIndex(index) {
  const audited = {};
  for (const [key, entry] of Object.entries(index)) {
    const seed = parseInt(key, 10);
    const wikiSet = findWikiSetForEntry(seed, entry);
    if (!wikiSet) {
      audited[key] = { ...entry, wiki_audit: null };
      continue;
    }
    const cmp = compareSet(wikiSet, index, seed);
    audited[key] = {
      ...entry,
      wiki_audit: {
        mod: cmp.mod,
        set: cmp.set,
        wiki: cmp.wiki,
        wiki_match: cmp.wiki_match,
        wiki_checked: cmp.wiki_checked,
        wiki_match_rate: cmp.wiki_match_rate,
        code_accuracy_rate: cmp.code_accuracy_rate,
        accuracy_gap: cmp.accuracy_gap,
        diffs: cmp.diffs,
      },
    };
  }
  return audited;
}

function compareAllWikiSets(index) {
  return WIKI_SETS.map((s) => compareSet(s, index));
}

function buildAuditSummary(index) {
  const entries = Object.values(index);
  const scored = entries.filter((e) => e.accuracy_denom > 0);
  const withWiki = entries.filter((e) => e.wiki_audit?.wiki_checked > 0);

  const codeAccSum = scored.reduce((s, e) => s + e.accuracy, 0);
  const codeAccDen = scored.reduce((s, e) => s + e.accuracy_denom, 0);
  const wikiMatchSum = withWiki.reduce((s, e) => s + e.wiki_audit.wiki_match, 0);
  const wikiCheckedSum = withWiki.reduce((s, e) => s + e.wiki_audit.wiki_checked, 0);

  const gapCases = withWiki
    .filter((e) => (e.wiki_audit.accuracy_gap ?? 0) > 0.001)
    .map((e) => ({
      seed: e.seed,
      name: e.name,
      set: `${e.wiki_audit.mod}/${e.wiki_audit.set}`,
      code_accuracy: `${e.accuracy}/${e.accuracy_denom}`,
      wiki_match: `${e.wiki_audit.wiki_match}/${e.wiki_audit.wiki_checked}`,
      accuracy_gap: e.wiki_audit.accuracy_gap,
      top_diffs: e.wiki_audit.diffs.slice(0, 5),
    }))
    .sort((a, b) => b.accuracy_gap - a.accuracy_gap || a.seed - b.seed);

  return {
    total_seeds: entries.length,
    scored: scored.length,
    avg_wiki_filled: entries.length
      ? Math.round((entries.reduce((s, e) => s + e.wiki_filled, 0) / entries.length) * 100) / 100
      : 0,
    code_accuracy_avg: codeAccDen > 0 ? Math.round((codeAccSum / codeAccDen) * 1000) / 1000 : 0,
    wiki_match_avg: wikiCheckedSum > 0 ? Math.round((wikiMatchSum / wikiCheckedSum) * 1000) / 1000 : 0,
    wiki_audited_seeds: withWiki.length,
    accuracy_gap_cases: gapCases,
  };
}

function formatConsoleReport(summary, wikiSetResults, gapCases) {
  const lines = [];
  lines.push('=== Blueprint batch: code accuracy vs wiki ground truth ===');
  lines.push(
    `seeds=${summary.total_seeds}  code_accuracy_avg=${summary.code_accuracy_avg}  wiki_match_avg=${summary.wiki_match_avg}  wiki_audited=${summary.wiki_audited_seeds}`
  );
  lines.push('');
  lines.push('--- Wiki set table (canonical seeds) ---');
  for (const r of wikiSetResults) {
    const wikiPct = r.wiki_checked ? `${Math.round(r.wiki_match_rate * 100)}%` : 'n/a';
    const codePct = r.code_accuracy_rate != null ? `${Math.round(r.code_accuracy_rate * 100)}%` : 'n/a';
    const gap =
      r.accuracy_gap != null && r.accuracy_gap > 0
        ? `  GAP=${r.accuracy_gap.toFixed(2)}`
        : '';
    lines.push(
      `  ${r.mod}/${r.set} seed=${r.seed ?? '?'}  code=${r.code_accuracy}(${codePct})  wiki=${r.wiki_match}/${r.wiki_checked}(${wikiPct})${gap}`
    );
    for (const d of r.diffs.slice(0, 3)) {
      lines.push(
        `    ! ${d.slot}: expected ${d.expected} got ${d.actual_internal || '(empty)'} [${d.status}]`
      );
    }
  }
  if (gapCases.length) {
    lines.push('');
    lines.push('--- Largest code-vs-wiki gaps (per log seed) ---');
    for (const g of gapCases.slice(0, 15)) {
      lines.push(
        `  seed=${g.seed} ${g.name} [${g.set}] code=${g.code_accuracy} wiki=${g.wiki_match} gap=${g.accuracy_gap}`
      );
      for (const d of g.top_diffs) {
        lines.push(`    ${d.slot}: want ${d.expected} got ${d.actual_internal || '(empty)'}`);
      }
    }
  }
  return lines.join('\n');
}

function findLatestSimpleLog() {
  const root = path.join(
    process.env.USERPROFILE || process.env.HOME || '',
    'Documents/My Games/Terraria/tModLoader/Logs/EMOJ'
  );
  if (!fs.existsSync(root)) return null;
  const dirs = fs
    .readdirSync(root)
    .map((name) => {
      const full = path.join(root, name);
      const log = path.join(full, 'simple.log');
      if (!fs.existsSync(log)) return null;
      return { name, mtime: fs.statSync(log).mtimeMs, log };
    })
    .filter(Boolean)
    .sort((a, b) => b.mtime - a.mtime);
  return dirs[0]?.log ?? null;
}

module.exports = {
  AUDIT_DIR,
  SLOTS,
  WIKI_SETS,
  classifySlot,
  findSeedByPrefix,
  findWikiSetForEntry,
  compareSet,
  compareAllWikiSets,
  auditLogIndex,
  buildAuditSummary,
  formatConsoleReport,
  findLatestSimpleLog,
};
