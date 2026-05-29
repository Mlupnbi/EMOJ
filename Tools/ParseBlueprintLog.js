#!/usr/bin/env node
/** @deprecated Prefer Tools/AnalyzeBlueprintLog.js ¡ª kept for compatibility. */
const path = require('path');
const { AUDIT_DIR, auditLogIndex, buildAuditSummary, findLatestSimpleLog } = require('./lib/wikiAudit');
const { parseBlueprintLog } = require('./lib/parseBlueprintLog');
const fs = require('fs');

function main() {
  const logPath = process.argv[2] || findLatestSimpleLog();
  const outDir = process.argv[3] || AUDIT_DIR;
  if (!logPath) {
    console.error('No log path');
    process.exit(1);
  }

  fs.mkdirSync(outDir, { recursive: true });
  const raw = parseBlueprintLog(logPath);
  const index = auditLogIndex(raw);
  const summary = buildAuditSummary(index);

  fs.writeFileSync(path.join(outDir, 'batch_slots_index.json'), JSON.stringify(index, null, 2), 'utf8');
  fs.writeFileSync(path.join(outDir, 'batch_summary.json'), JSON.stringify(summary, null, 2), 'utf8');
  console.log(`Parsed ${Object.keys(index).length} seeds (with wiki_audit) -> ${outDir}`);
  console.log(`code_accuracy_avg=${summary.code_accuracy_avg} wiki_match_avg=${summary.wiki_match_avg}`);
}

main();
