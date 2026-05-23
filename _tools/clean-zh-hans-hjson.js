const fs = require("fs");
const path = require("path");
const zhPath = path.join(__dirname, "..", "Localization", "zh-Hans_Mods.EvenMoreOverpoweredJourney.hjson");

let lines = fs.readFileSync(zhPath, "utf8").split(/\r?\n/);
lines[0] = "DisplayName: \u66f4\u8d85\u6a21\u7684\u65c5\u9014";

function isEnglishBlock(inner) {
  const text = inner.join(" ");
  const latin = (text.match(/[A-Za-z]/g) || []).length;
  const cjk = (text.match(/[\u4e00-\u9fff]/g) || []).length;
  return latin > 40 && latin > cjk * 2;
}

const out = [];
let i = 0;
while (i < lines.length) {
  const line = lines[i];
  if (/^\s*'''\s*$/.test(line)) {
    const start = i;
    i++;
    const inner = [];
    while (i < lines.length && !/^\s*'''\s*$/.test(lines[i])) {
      inner.push(lines[i]);
      i++;
    }
    if (i < lines.length) i++; // closing '''
    if (isEnglishBlock(inner)) continue;
    out.push(line);
    for (const l of inner) out.push(l);
    out.push(line);
    continue;
  }
  out.push(line);
  i++;
}

// Fix wrong Five.Label on PurpleLosslessGiveAmount (should be "5")
for (let j = 0; j < out.length; j++) {
  if (out[j].includes("PurpleLosslessGiveAmount") || (out[j - 1] && out[j - 1].includes("PurpleLosslessGiveAmount"))) {
    /* noop */
  }
  if (/^\s*Five\.Label:/.test(out[j]) && out[j].includes("\u83b7\u5f97\u4e94\u6b21")) {
    out[j] = out[j].replace(/Five\.Label:.*/, 'Five.Label: "5"');
  }
  if (/^\s*Ten\.Label:/.test(out[j])) out[j] = out[j].replace(/Ten\.Label:.*/, 'Ten.Label: "10"');
  if (/^\s*Fifty\.Label:/.test(out[j])) out[j] = out[j].replace(/Fifty\.Label:.*/, 'Fifty.Label: "50"');
}

fs.writeFileSync(zhPath, out.join("\n"), "utf8");
console.log("Cleaned", zhPath, "lines", lines.length, "->", out.length);
