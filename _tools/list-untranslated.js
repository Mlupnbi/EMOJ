const fs = require("fs");
const path = require("path");
const root = path.join(__dirname, "..", "Localization");
const zh = fs.readFileSync(path.join(root, "zh-Hans_Mods.EvenMoreOverpoweredJourney.hjson"), "utf8");
const en = fs.readFileSync(path.join(root, "en-US_Mods.EvenMoreOverpoweredJourney.hjson"), "utf8");
function map(s) {
  const m = new Map();
  for (const line of s.split(/\r?\n/)) {
    const x = line.match(/^\s*([A-Za-z0-9_.]+):\s*(.*)$/);
    if (x) m.set(x[1], x[2]);
  }
  return m;
}
const zm = map(zh), em = map(en);
const same = [];
for (const [k, v] of zm) {
  const ev = em.get(k);
  if (ev && v === ev && /[A-Za-z]{4,}/.test(v)) same.push(k + ": " + v.slice(0, 70));
}
console.log(same.join("\n"));
console.log("\nTotal:", same.length);
