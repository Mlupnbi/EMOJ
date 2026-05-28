#!/usr/bin/env node
/**
 * 从用户提供的 8 个 wiki 源抓取家具套组，缓存到 Issues/audit_data/wiki_cache.json
 * 并同步导出 Data/wiki_furniture_cache.json（游戏 batch 内嵌 wiki_match 用）。
 */
const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');
const { WIKI_SETS: LOCAL_SETS } = require('./lib/wikiAudit');

const AUDIT_DIR = path.join(__dirname, '..', 'Issues', 'audit_data');
const MOD_DATA_PATH = path.join(__dirname, '..', 'Data', 'wiki_furniture_cache.json');
const CACHE_PATH = path.join(AUDIT_DIR, 'wiki_cache.json');
const GAPS_PATH = path.join(AUDIT_DIR, 'wiki_cache_gaps.json');
const META_PATH = path.join(AUDIT_DIR, 'wiki_cache_meta.json');

const WIKI_SLOTS = [
  'Block', 'Wall', 'Bathtub', 'Bed', 'Bookcase', 'Candelabra', 'Candle', 'Chandelier',
  'Chair', 'Chest', 'Clock', 'Door', 'Dresser', 'Lamp', 'Lantern', 'Piano', 'Platform',
  'Sink', 'Sofa', 'Table', 'Toilet', 'Workbench',
];

const SLOT_PARAM = {
  source: 'Block',
  wall: 'Wall',
  bathtub: 'Bathtub',
  bed: 'Bed',
  bookcase: 'Bookcase',
  candelabra: 'Candelabra',
  candle: 'Candle',
  chair: 'Chair',
  chandelier: 'Chandelier',
  chest: 'Chest',
  clock: 'Clock',
  door: 'Door',
  dresser: 'Dresser',
  lamp: 'Lamp',
  lantern: 'Lantern',
  piano: 'Piano',
  platform: 'Platform',
  sink: 'Sink',
  sofa: 'Sofa',
  table: 'Table',
  toilet: 'Toilet',
  workbench: 'Workbench',
  'work bench': 'Workbench',
};

/** @type {Array<{id:string,mod:string,page:string,baseUrl:string,parser:string}>} */
const SOURCES = [
  {
    id: 'terraria',
    mod: 'Terraria',
    page: 'Furniture_sets',
    baseUrl: 'https://terraria.wiki.gg',
    parser: 'row_implicit',
  },
  {
    id: 'calamity',
    mod: 'CalamityMod',
    page: 'Furniture_sets',
    baseUrl: 'https://calamitymod.wiki.gg',
    parser: 'row_hybrid',
  },
  {
    id: 'thorium',
    mod: 'ThoriumMod',
    page: 'Furniture_sets',
    baseUrl: 'https://thoriummod.wiki.gg',
    parser: 'row_implicit',
  },
  {
    id: 'spirit',
    mod: 'SpiritMod',
    page: 'Spirit_Reforged/Furniture_sets',
    baseUrl: 'https://spiritmod.wiki.gg',
    parser: 'row_hybrid',
  },
  {
    id: 'spooky',
    mod: 'SpookyMod',
    page: 'Spooky_Mod/Furniture_sets',
    baseUrl: 'https://terrariamods.wiki.gg',
    parser: 'spooky_table',
  },
];

/** CalValEX 内容来自 CV 子页（索引页无 22 槽，需逐套详情）。 */
const CV_DETAIL_SETS = [
  {
    set: 'Bloodstone',
    stylePrefix: 'Bloodstone',
    page: "Calamity's_Vanities/Bloodstone_Furniture",
  },
  {
    set: 'Phantowax',
    stylePrefix: 'Phantowax',
    page: "Calamity's_Vanities/Phantowax_Furniture",
  },
  {
    set: 'Xenomonolith',
    stylePrefix: 'Xenomonolith',
    page: "Calamity's_Vanities/Xenomonolith_Furniture",
  },
  {
    set: 'Auric',
    stylePrefix: 'Auric',
    page: "Calamity's_Vanities/Auric_Furniture",
  },
];

const CV_BASE_URL = 'https://terrariamods.wiki.gg';
const CV_MOD = "Calamity's Vanities";

/** API 抓取失败时的手工补录（HJ wiki 当前无法 API/raw） */
const MANUAL_SETS = [
  {
    mod: 'HomewardJourney',
    set: 'Death',
    wiki: 'https://homewardjourney.wiki.gg/zh/wiki/%E5%AE%B6%E5%85%B7',
    stylePrefix: 'ItemDeath',
    source: 'manual',
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
    notes: 'HJ wiki 无法 API 抓取；Chest 槽 wiki 登记为 Sink 件。',
  },
  {
    mod: 'HomewardJourney',
    set: 'Nothingness',
    wiki: 'https://homewardjourney.wiki.gg/zh/wiki/%E5%AE%B6%E5%85%B7',
    stylePrefix: 'ItemNothingness',
    source: 'manual',
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
    notes: 'manual',
  },
  {
    mod: 'HomewardJourney',
    set: 'Life',
    wiki: 'https://homewardjourney.wiki.gg/zh/wiki/%E5%AE%B6%E5%85%B7',
    stylePrefix: 'ItemLife',
    source: 'manual',
    slots: {
      Bookcase: 'ItemLifeBookcase',
      Candelabra: 'ItemLifeCandelabra',
      Candle: 'ItemLifeCandle',
      Chandelier: 'ItemLifeChandelier',
      Chair: 'ItemLifeChair',
      Clock: 'ItemLifeClock',
      Door: 'ItemLifeDoor',
      Dresser: 'ItemLifeDresser',
      Lamp: 'ItemLifeLamp',
      Lantern: 'ItemLifeLantern',
      Piano: 'ItemLifePiano',
      Bed: 'ItemLifeBed',
      Bathtub: 'ItemLifeBathtub',
      Table: 'ItemLifeTable',
      Chest: 'ItemLifeChest',
      Sink: 'ItemLifeSink',
    },
    notes: 'COJ 无结构化 wiki；由 Sets 日志 ItemLife* internal 补录。',
  },
  {
    mod: 'HomewardJourney',
    set: 'Matter',
    wiki: 'https://homewardjourney.wiki.gg/zh/wiki/%E5%AE%B6%E5%85%B7',
    stylePrefix: 'ItemMatter',
    source: 'manual',
    slots: {
      Bookcase: 'ItemMatterBookcase',
      Candelabra: 'ItemMatterCandelabra',
      Candle: 'ItemMatterCandle',
      Chandelier: 'ItemMatterChandelier',
      Chair: 'ItemMatterChair',
      Clock: 'ItemMatterClock',
      Door: 'ItemMatterDoor',
      Dresser: 'ItemMatterDresser',
      Lamp: 'ItemMatterLamp',
      Lantern: 'ItemMatterLantern',
      Piano: 'ItemMatterPiano',
      Bed: 'ItemMatterBed',
      Bathtub: 'ItemMatterBathtub',
      Table: 'ItemMatterTable',
      Chest: 'ItemMatterChest',
      Sofa: 'ItemMatterSofa',
      Sink: 'ItemMatterSink',
    },
    notes: 'COJ 无结构化 wiki；由 Sets 日志 ItemMatter* internal 补录。',
  },
  {
    mod: 'HomewardJourney',
    set: 'Time',
    wiki: 'https://homewardjourney.wiki.gg/zh/wiki/%E5%AE%B6%E5%85%B7',
    stylePrefix: 'ItemTime',
    source: 'manual',
    slots: {
      Bookcase: 'ItemTimeBookcase',
      Candelabra: 'ItemTimeCandelabra',
      Candle: 'ItemTimeCandle',
      Chandelier: 'ItemTimeChandelier',
      Chair: 'ItemTimeChair',
      Clock: 'ItemTimeClock',
      Door: 'ItemTimeDoor',
      Dresser: 'ItemTimeDresser',
      Lamp: 'ItemTimeLamp',
      Lantern: 'ItemTimeLantern',
      Piano: 'ItemTimePiano',
      Bed: 'ItemTimeBed',
      Bathtub: 'ItemTimeBathtub',
      Table: 'ItemTimeTable',
      Chest: 'ItemTimeChest',
      Toilet: 'ItemTimeToilet',
      Sink: 'ItemTimeSink',
    },
    notes: 'COJ 无结构化 wiki；由 Sets 日志 ItemTime* internal 补录。',
  },
  {
    mod: 'CalamityFables',
    set: 'Wulfrum',
    wiki: 'https://calamityfables.wiki.gg/wiki/Furniture',
    stylePrefix: 'Wulfrum',
    source: 'manual',
    slots: {
      Block: 'WulfrumPlating',
      Wall: 'WulfrumPlatingWall',
      Bathtub: 'WulfrumBathtub',
      Bed: 'WulfrumBed',
      Bookcase: 'WulfrumBookcase',
      Candelabra: 'WulfrumCandelabra',
      Candle: 'WulfrumCandle',
      Chair: 'WulfrumChair',
      Chandelier: 'WulfrumChandelier',
      Clock: 'WulfrumClock',
      Door: 'WulfrumDoor',
      Dresser: 'WulfrumDresser',
      Lamp: 'WulfrumLamp',
      Lantern: 'WulfrumLantern',
      Piano: 'WulfrumPiano',
      Platform: 'WulfrumPlatform',
      Sink: 'WulfrumSink',
      Sofa: 'WulfrumSofa',
      Table: 'WulfrumTable',
      Toilet: 'WulfrumToilet',
      Workbench: 'WulfrumWorkBench',
    },
    notes: 'Fables wiki 无 22 槽表；由 Sets 日志 Wulfrum* internal + wiki 单品页补录。',
  },
];

const BLOCK_OVERRIDES = {
  Wood: 'Wood',
  'Smooth Marble Block': 'MarbleBlock',
  '+Nest Block': 'SpiderBlock',
  Cactus: 'Cactus',
  'Bloodstained Block': 'BloodstainedBlock',
  'Thorium Brick': 'ThoriumBrick',
  'Thorium Block': 'ThoriumBlock',
  'Smooth Abyss Gravel': 'SmoothAbyssGravel',
  'Cosmilite Brick': 'CosmiliteBrick',
  'Statigel Block': 'StatigelBlock',
  Driftwood: 'Driftwood',
  Drywood: 'Drywood',
  'Carved Lapis': 'CarvedLapis',
  'Salt Block': 'SaltBlock',
};

const WALL_OVERRIDES = {
  LivingWood: 'LivingWoodWall',
  Marble: 'MarbleBlockWall',
  Spider: 'SpiderWall',
  Cactus: 'CactusWall',
  Thorium: 'ThoriumBrickWall',
};

function fetchWikitext(baseUrl, page) {
  const url = `${baseUrl}/api.php?action=parse&page=${encodeURIComponent(page)}&prop=wikitext&format=json`;
  const tmp = path.join(AUDIT_DIR, `_scrape_${Date.now()}.json`);
  try {
    execSync(`curl.exe -sL "${url}" -o "${tmp}"`, { stdio: 'pipe' });
    const j = JSON.parse(fs.readFileSync(tmp, 'utf8'));
    fs.unlinkSync(tmp);
    if (j.error) return { error: j.error.info || j.error.code, text: '' };
    return { text: j.parse?.wikitext?.['*'] || '', title: j.parse?.title };
  } catch (e) {
    try {
      fs.unlinkSync(tmp);
    } catch {
      /* */
    }
    return { error: String(e.message || e), text: '' };
  }
}

function toInternal(display) {
  if (!display || display === 'no') return null;
  return display
    .replace(/[''']/g, '')
    .replace(/\s+/g, '')
    .replace(/-/g, '');
}

function stylePrefixFromType(typeName) {
  return typeName.replace(/\s+/g, '');
}

function defaultSlotItem(prefix, slot) {
  if (slot === 'Toilet') return `Toilet${prefix}`;
  if (slot === 'Workbench') return `${prefix}WorkBench`;
  if (slot === 'Wall') return WALL_OVERRIDES[prefix] || `${prefix}Wall`;
  return `${prefix}${slot}`;
}

function parseRowParams(inner) {
  const params = { type: null };
  for (const part of inner.split('|')) {
    if (!part.includes('=')) {
      if (!params.type && part.trim()) params.type = part.trim();
      continue;
    }
    const eq = part.indexOf('=');
    const k = part.slice(0, eq).trim().toLowerCase();
    let v = part.slice(eq + 1).trim();
    if (v === 'no') v = null;
    params[k] = v;
  }
  return params;
}

function buildSlotsFromRow(typeName, params, mode) {
  const prefix = stylePrefixFromType(typeName);
  const slots = {};
  const blockSource = params.source || typeName;
  if (blockSource) {
    slots.Block = BLOCK_OVERRIDES[blockSource] || toInternal(blockSource);
  }
  if (params.wall) slots.Wall = toInternal(params.wall);

  const useImplicit = mode === 'row_implicit' || mode === 'row_hybrid';

  for (const [param, slot] of Object.entries(SLOT_PARAM)) {
    if (slot === 'Block' || slot === 'Wall') continue;
    if (Object.prototype.hasOwnProperty.call(params, param) && params[param] === null) continue;
    const raw = params[param];
    if (raw) {
      slots[slot] = toInternal(raw);
      continue;
    }
    if (useImplicit && !Object.prototype.hasOwnProperty.call(params, param)) {
      slots[slot] = defaultSlotItem(prefix, slot);
    }
  }

  return slots;
}

function parseRowTemplates(text, mode) {
  const sets = [];
  const re = /\{\{\/row\|([^}]+)\}\}/g;
  let m;
  while ((m = re.exec(text))) {
    const params = parseRowParams(m[1]);
    const typeName = params.type || params.link || params.text;
    if (!typeName) continue;
    const slots = buildSlotsFromRow(typeName, params, mode);
    sets.push({
      set: typeName,
      stylePrefix: stylePrefixFromType(typeName),
      slots,
      slotCount: Object.keys(slots).length,
    });
  }
  return sets;
}

function parseSpookyTable(text) {
  const sets = [];
  const chunks = text.split(/\n\|-\n/);
  for (const chunk of chunks) {
    const nameMatch = chunk.match(/\|\s*'''([^']+)'''/);
    if (!nameMatch) continue;
    const setName = nameMatch[1].trim();
    const items = [...chunk.matchAll(/\{\{item\|#([^}|]+)/g)].map((x) => x[1].trim());
    if (items.length < 2) continue;
    const block = items[0];
    const slots = { Block: toInternal(block) };
    const slotOrder = [
      'Bathtub', 'Bed', 'Bookcase', 'Candelabra', 'Candle', 'Chair', 'Chandelier', 'Chest',
      'Clock', 'Door', 'Dresser', 'Lamp', 'Lantern', 'Piano', 'Platform', 'Sink', 'Sofa',
      'Table', 'Workbench',
    ];
    let idx = 1;
    for (const slot of slotOrder) {
      while (idx < items.length && items[idx].toLowerCase() === 'no') idx++;
      if (idx >= items.length) break;
      slots[slot] = toInternal(items[idx++]);
    }
    sets.push({
      set: setName,
      stylePrefix: toInternal(block.split(/\s+/)[0]),
      slots,
      slotCount: Object.keys(slots).length,
    });
  }
  return sets;
}

function displayNameToSlot(displayName) {
  const n = displayName.trim();
  if (!n) return null;
  const lower = n.toLowerCase();
  if (lower.includes('grandfather clock')) return 'Clock';
  if (lower.includes('pipe organ')) return 'Piano';
  if (lower.includes('work bench')) return 'Workbench';
  if (lower.includes('sofa')) return 'Sofa';

  for (const slot of WIKI_SLOTS) {
    if (slot === 'Block' || slot === 'Wall' || slot === 'Workbench') continue;
    if (lower.endsWith(slot.toLowerCase())) return slot;
  }

  if (lower.endsWith('workbench')) return 'Workbench';
  return null;
}

function parseCvDetailPage(text, setName, stylePrefix) {
  const recipeMatch = text.match(/\{\{recipes[^}]*\|result=([^}|]+)/i);
  if (!recipeMatch) return null;

  const slots = {};
  const parts = recipeMatch[1]
    .split('/')
    .map((p) => p.replace(/^#+/, '').trim())
    .filter(Boolean);

  for (const part of parts) {
    const slot = displayNameToSlot(part);
    if (!slot) continue;
    slots[slot] = toInternal(part);
  }

  return {
    set: setName,
    stylePrefix,
    slots,
    slotCount: Object.keys(slots).length,
  };
}

function scrapeCvDetailSets() {
  const sets = [];
  const source = {
    id: 'calamity_vanities_detail',
    mod: CV_MOD,
    page: 'detail_pages',
    url: `${CV_BASE_URL}/wiki/Calamity's_Vanities/Furniture`,
    title: null,
    error: null,
    parser: 'cv_detail',
    setCount: 0,
  };

  for (const def of CV_DETAIL_SETS) {
    const pageUrl = `${CV_BASE_URL}/wiki/${def.page.replace(/ /g, '_')}`;
    const { text, title, error } = fetchWikitext(CV_BASE_URL, def.page);
    if (error) {
      source.error = source.error || error;
      continue;
    }

    const parsed = parseCvDetailPage(text, def.set, def.stylePrefix);
    if (!parsed || parsed.slotCount === 0) {
      source.error = source.error || `no_recipes:${def.set}`;
      continue;
    }

    sets.push({
      mod: CV_MOD,
      set: parsed.set,
      wiki: pageUrl,
      stylePrefix: parsed.stylePrefix,
      slots: parsed.slots,
      slotCount: parsed.slotCount,
      source: 'wiki_scrape',
      notes: title ? `cv_detail:${title}` : 'cv_detail',
    });
  }

  source.setCount = sets.length;
  return { source, sets };
}

function scrapeAll() {
  fs.mkdirSync(AUDIT_DIR, { recursive: true });
  const scrapedAt = new Date().toISOString();
  /** @type {import('./lib/wikiAudit').WikiCacheFile} */
  const cache = {
    scrapedAt,
    sources: [],
    sets: [],
  };

  for (const src of SOURCES) {
    const { text, title, error } = fetchWikitext(src.baseUrl, src.page);
    const entry = {
      id: src.id,
      mod: src.mod,
      page: src.page,
      url: `${src.baseUrl}/wiki/${src.page.replace(/ /g, '_')}`,
      title: title || null,
      error: error || null,
      parser: src.parser,
      setCount: 0,
    };

    if (text) {
      let sets = [];
      if (src.parser === 'row_explicit' || src.parser === 'row_hybrid')
        sets = parseRowTemplates(text, src.parser);
      else if (src.parser === 'row_implicit') sets = parseRowTemplates(text, 'row_implicit');
      else if (src.parser === 'spooky_table') sets = parseSpookyTable(text);

      for (const s of sets) {
        cache.sets.push({
          mod: src.mod,
          set: s.set,
          wiki: entry.url,
          stylePrefix: s.stylePrefix,
          slots: s.slots,
          slotCount: s.slotCount,
          source: 'wiki_scrape',
          notes: s.notes || '',
        });
      }
      entry.setCount = sets.length;
    }

    cache.sources.push(entry);
  }

  const cvDetail = scrapeCvDetailSets();
  cache.sources.push(cvDetail.source);
  for (const s of cvDetail.sets) cache.sets.push(s);

  for (const s of MANUAL_SETS) {
    cache.sets.push({
      mod: s.mod,
      set: s.set,
      wiki: s.wiki,
      stylePrefix: s.stylePrefix,
      slots: s.slots,
      slotCount: Object.keys(s.slots).length,
      source: s.source,
      notes: s.notes || '',
    });
  }

  cache.setCount = cache.sets.length;
  const json = JSON.stringify(cache, null, 2);
  fs.writeFileSync(CACHE_PATH, json, 'utf8');
  fs.mkdirSync(path.dirname(MOD_DATA_PATH), { recursive: true });
  fs.writeFileSync(MOD_DATA_PATH, json, 'utf8');
  fs.writeFileSync(META_PATH, JSON.stringify({ scrapedAt, sources: cache.sources }, null, 2), 'utf8');
  return cache;
}

function localKey(mod, set) {
  return `${mod}|${set}`.toLowerCase();
}

function cacheKey(mod, set) {
  return `${mod}|${set}`.toLowerCase();
}

function compareGaps(cache) {
  const localMap = new Map();
  for (const s of LOCAL_SETS) {
    localMap.set(localKey(s.mod, s.set), s);
  }

  const cacheMap = new Map();
  for (const s of cache.sets) {
    cacheMap.set(cacheKey(s.mod, s.set), s);
  }

  const missingInLocal = [];
  for (const s of cache.sets) {
    const k = cacheKey(s.mod, s.set);
    if (!localMap.has(k)) {
      missingInLocal.push({
        mod: s.mod,
        set: s.set,
        stylePrefix: s.stylePrefix,
        slotCount: s.slotCount,
        source: s.source,
        wiki: s.wiki,
        notes: s.notes,
      });
    }
  }

  const missingInCache = [];
  for (const s of LOCAL_SETS) {
    const k = localKey(s.mod, s.set);
    if (!cacheMap.has(k)) {
      missingInCache.push({
        mod: s.mod,
        set: s.set,
        stylePrefix: s.stylePrefix,
        wiki: s.wiki,
      });
    }
  }

  const quickNeeds = [
    { mod: 'Terraria', sets: ['Living Wood', 'Marble', 'Spider', 'Cactus', 'Frozen', 'Glass', 'Pumpkin', 'Honey'] },
    { mod: 'CalamityMod', sets: ['Abyss', 'Cosmilite', 'Statigel', 'Silva', 'Profaned', 'Void', 'Monolith', 'Botanic'] },
    { mod: 'HomewardJourney', sets: ['Death', 'Nothingness', 'Life', 'Matter', 'Time'] },
    { mod: 'ThoriumMod', sets: ['Thorium', 'Marine', 'Fossil', 'Bloodstained', 'Celestial'] },
    { mod: 'SpiritMod', sets: ['Driftwood', 'Drywood', 'Lapis', 'Salt'] },
    { mod: 'SpookyMod', sets: ['Glowshroom', 'Living Flesh', 'Occultist', 'Old Wood', 'Yellow Glowshroom', 'Yuletide'] },
  ];

  const quickMissing = [];
  for (const group of quickNeeds) {
    for (const setName of group.sets) {
      const found = cache.sets.find(
        (x) => x.mod === group.mod && x.set.toLowerCase() === setName.toLowerCase()
      );
      if (!found) {
        quickMissing.push({ mod: group.mod, set: setName, reason: 'not_in_wiki_cache' });
      } else if (found.slotCount < 8 && !found.notes?.includes('partial')) {
        quickMissing.push({
          mod: group.mod,
          set: setName,
          reason: 'partial_slots',
          slotCount: found.slotCount,
        });
      }
    }
  }

  missingInLocal.sort((a, b) => a.mod.localeCompare(b.mod) || a.set.localeCompare(b.set));

  const gaps = {
    scrapedAt: cache.scrapedAt,
    cacheSetCount: cache.sets.length,
    localSetCount: LOCAL_SETS.length,
    missingInLocalExpectations: missingInLocal,
    missingInWikiCache: missingInCache,
    quickRegressionMissing: quickMissing,
    failedSources: cache.sources.filter((s) => s.error || s.setCount === 0),
  };

  fs.writeFileSync(GAPS_PATH, JSON.stringify(gaps, null, 2), 'utf8');
  return gaps;
}

function printSummary(cache, gaps) {
  console.log(`Wiki cache: ${cache.setCount} sets -> ${CACHE_PATH}`);
  console.log(`Mod runtime cache: ${MOD_DATA_PATH}`);
  console.log(`Scraped: ${cache.scrapedAt}`);
  console.log('');
  console.log('Sources:');
  for (const s of cache.sources) {
    console.log(`  ${s.mod}: ${s.setCount} sets${s.error ? ` ERROR(${s.error})` : ''}`);
  }
  console.log(`  HomewardJourney+CalamityFables: ${MANUAL_SETS.length} manual sets`);
  console.log('');
  console.log(`Local wikiAudit expectations: ${gaps.localSetCount}`);
  console.log(`Missing in local (scraped but not in wikiAudit.js): ${gaps.missingInLocalExpectations.length}`);
  console.log(`Missing in cache (in wikiAudit but not scraped): ${gaps.missingInWikiCache.length}`);
  console.log(`QUICK regression sets still missing/partial: ${gaps.quickRegressionMissing.length}`);
  console.log('');
  if (gaps.missingInLocalExpectations.length) {
    console.log('--- Top missing in local expectations (need to add to wikiAudit) ---');
    for (const x of gaps.missingInLocalExpectations.slice(0, 25)) {
      console.log(`  ${x.mod}/${x.set}  slots=${x.slotCount}  ${x.source}`);
    }
    if (gaps.missingInLocalExpectations.length > 25) {
      console.log(`  ... +${gaps.missingInLocalExpectations.length - 25} more`);
    }
  }
  if (gaps.quickRegressionMissing.length) {
    console.log('');
    console.log('--- QUICK regression gaps ---');
    for (const x of gaps.quickRegressionMissing) {
      console.log(`  ${x.mod}/${x.set}  ${x.reason}${x.slotCount != null ? ` slots=${x.slotCount}` : ''}`);
    }
  }
  console.log('');
  console.log(`Gaps report -> ${GAPS_PATH}`);
}

function main() {
  const cache = scrapeAll();
  const gaps = compareGaps(cache);
  printSummary(cache, gaps);
}

main();
