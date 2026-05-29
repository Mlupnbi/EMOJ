#!/usr/bin/env node
/**
 * 解析 batch-test 日志 + wiki 真值对照，输出 code accuracy vs wiki_match 对比报告。
 *
 * Usage:
 *   node Tools/AnalyzeBlueprintLog.js [path/to/simple.log]
 *   node Tools/AnalyzeBlueprintLog.js   # 自动用最新 EMOJ 会话
 */
const fs = require('fs');
const path = require('path');
const {
  AUDIT_DIR,
  auditLogIndex,
  buildAuditSummary,
  compareAllWikiSets,
  findLatestSimpleLog,
  formatConsoleReport,
} = require('./lib/wikiAudit');
const { parseBlueprintLog } = require('./lib/parseBlueprintLog');

function main() {
  const logPath = process.argv[2] || findLatestSimpleLog();
  if (!logPath || !fs.existsSync(logPath)) {
    console.error('Log not found. Pass path: node Tools/AnalyzeBlueprintLog.js <simple.log>');
    process.exit(1);
  }

  fs.mkdirSync(AUDIT_DIR, { recursive: true });

  const rawIndex = parseBlueprintLog(logPath);
  const index = auditLogIndex(rawIndex);
  const summary = buildAuditSummary(index);
  const wikiSetResults = compareAllWikiSets(rawIndex);

  const sessionTag = path.basename(path.dirname(logPath));
  summary.log_session = sessionTag;
  summary.log_path = logPath;

  fs.writeFileSync(
    path.join(AUDIT_DIR, 'batch_slots_index.json'),
    JSON.stringify(index, null, 2),
    'utf8'
  );
  fs.writeFileSync(
    path.join(AUDIT_DIR, 'wiki_compare_results.json'),
    JSON.stringify(wikiSetResults, null, 2),
    'utf8'
  );
  fs.writeFileSync(
    path.join(AUDIT_DIR, 'batch_summary.json'),
    JSON.stringify(summary, null, 2),
    'utf8'
  );

  const report = formatConsoleReport(
    summary,
    wikiSetResults,
    summary.accuracy_gap_cases
  );
  console.log(`Log: ${logPath}`);
  console.log(report);
  console.log('');
  console.log(`Wrote ${path.join(AUDIT_DIR, 'batch_summary.json')}`);
}

main();
