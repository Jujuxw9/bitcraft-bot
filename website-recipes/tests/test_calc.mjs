// Tests des calculs de la page (arbre, coches, « j'en ai déjà », totaux, stacks, recherche).
//   node --test website-recipes/tests/
// Données inventées pour les cas précis ; données réelles (build/data.json) pour les cas de Julien, si présentes.
import { test } from "node:test";
import assert from "node:assert/strict";
import { readFileSync, existsSync } from "node:fs";
import { fileURLToPath } from "node:url";
import path from "node:path";
import { buildTree, pooled, stacks, search, buildIndex, craftsNeeded, editDistance, progress } from "../public/calc.js";

// Données inventées : une hache = 1 manche + 2 lingots ; un lingot = 1 minerai + 1 bûche ; un manche = 1 bûche ;
// un tronc donne 6 bûches (recette « split »).
const D = {
  items: {
    axe: { n: "Test Axe", k: "i", t: 3, v: 0, st: null, o: "craft" },
    handle: { n: "Handle", k: "i", t: 1, v: 600, st: 10, o: "craft" },
    ingot: { n: "Test Ingot", k: "i", t: 1, v: 600, st: 10, o: "craft" },
    ore: { n: "Test Ore", k: "c", t: 1, v: 6000, st: 1, o: "gather" },
    log: { n: "Wood Log", k: "i", t: 1, v: 120, st: 50, o: "craft" },
    trunk: { n: "Wood Trunk", k: "c", t: 1, v: 6000, st: 1, o: "gather" },
  },
  recipes: {
    r_axe: { o: "axe", oq: 1, in: [["handle", 1], ["ingot", 2]], b: null },
    r_handle: { o: "handle", oq: 1, in: [["log", 4]], b: null },
    r_ingot: { o: "ingot", oq: 1, in: [["ore", 1], ["log", 1]], b: null },
    r_log: { o: "log", oq: 6, in: [["trunk", 1]], b: null },
    r_ingot2: { o: "ingot", oq: 1, in: [["ore", 5]], b: null },
  },
  best: { axe: "r_axe", handle: "r_handle", ingot: "r_ingot", log: "r_log" },
  alts: { ingot: ["r_ingot2"] },
  sources: {},
};

const rawOf = (res) => Object.fromEntries(res.raw.map((x) => [x.key, x.left]));

test("nombre de fabrications arrondi au-dessus", () => {
  assert.equal(craftsNeeded(0, 6), 0);
  assert.equal(craftsNeeded(1, 6), 1);
  assert.equal(craftsNeeded(6, 6), 1);
  assert.equal(craftsNeeded(7, 6), 2);
});

test("arbre : quantités par branche", () => {
  const t = buildTree(D, "axe", 3);
  assert.equal(t.need, 3);
  const [handle, ingot] = t.children;
  assert.equal(handle.need, 3);
  assert.equal(ingot.need, 6);
  assert.equal(handle.children[0].need, 12);          // 3 manches × 4 bûches
  assert.equal(handle.children[0].children[0].need, 2); // 12 bûches = 2 troncs
});

test("totaux mis en commun : pas de double arrondi", () => {
  // 1 hache : manche 4 bûches + 2 lingots × 1 bûche = 6 bûches = 1 seul tronc (2 si on arrondit par branche).
  const res = pooled(D, buildTree(D, "axe", 1));
  assert.deepEqual(rawOf(res), { trunk: 1, ore: 2 });
  const logStep = res.steps.find((s) => s.key === "log");
  assert.equal(logStep.crafts, 1);
  assert.equal(logStep.made, 6);
});

test("ordre de fabrication : de la matière vers l'objet final", () => {
  const keys = pooled(D, buildTree(D, "axe", 1)).steps.map((s) => s.key);
  assert.ok(keys.indexOf("log") < keys.indexOf("handle"));
  assert.ok(keys.indexOf("log") < keys.indexOf("ingot"));
  assert.equal(keys[keys.length - 1], "axe");
});

test("cocher un objet grise toute sa branche et l'enlève des totaux", () => {
  const t = buildTree(D, "axe", 1, { checked: { "axe/ingot": true } });
  const ingot = t.children[1];
  assert.ok(ingot.done && ingot.checked);
  assert.ok(ingot.children.every((c) => c.done));
  assert.deepEqual(rawOf(pooled(D, t)), { trunk: 1 });
});

test("cocher l'objet final : plus rien à faire", () => {
  const t = buildTree(D, "axe", 2, { checked: { axe: true } });
  const res = pooled(D, t);
  assert.equal(res.raw.length, 0);
  assert.equal(res.steps.length, 0);
});

test("« j'en ai déjà » réduit la branche", () => {
  const t = buildTree(D, "axe", 1, { owned: { "axe/ingot": 1 } });
  assert.equal(t.children[1].left, 1);
  assert.equal(t.children[1].children[0].need, 1);   // 1 seul minerai
  assert.deepEqual(rawOf(pooled(D, t)), { trunk: 1, ore: 1 });
  const all = buildTree(D, "axe", 1, { owned: { "axe/ingot": 5 } });
  assert.equal(all.children[1].left, 0);
});

test("choix d'une autre recette", () => {
  const t = buildTree(D, "axe", 1, { recipe: { ingot: "r_ingot2" } });
  assert.deepEqual(rawOf(pooled(D, t)), { ore: 10, trunk: 1 });
  // Recette inconnue ou d'un autre objet : on revient à la plus facile.
  const bad = buildTree(D, "axe", 1, { recipe: { ingot: "r_axe" } });
  assert.equal(bad.children[1].recipeId, "r_ingot");
});

test("progression de 0 à 100 %", () => {
  assert.equal(progress(D, "axe", 1, {}), 0);
  assert.equal(progress(D, "axe", 1, { checked: { axe: true } }), 1);
  const p = progress(D, "axe", 1, { checked: { "axe/ingot": true } });
  assert.ok(p > 0 && p < 1);
});

test("boucle de recettes : l'arbre s'arrête", () => {
  const L = structuredClone(D);
  L.recipes.r_loop = { o: "ore", oq: 1, in: [["ingot", 1]], b: null };
  L.best.ore = "r_loop";
  const t = buildTree(L, "axe", 1);
  const ore = t.children[1].children[0];
  assert.equal(ore.children[0].key, "ingot");
  assert.ok(ore.children[0].cycle);
  pooled(L, t);   // ne doit pas tourner sans fin
});

test("stacks : inventaire et cargo", () => {
  assert.deepEqual(stacks(D.items.ingot, 25), { per: 10, stacks: 3, cargo: false });
  assert.deepEqual(stacks(D.items.ore, 4), { per: 1, stacks: 4, cargo: true });
  assert.equal(stacks(D.items.axe, 1), null);
  assert.equal(stacks(D.items.ingot, 0), null);
});

test("recherche tolérante aux fautes", () => {
  const idx = buildIndex(D);
  assert.equal(search(D, idx, "axe")[0], "axe");
  assert.equal(search(D, idx, "ingto")[0], "ingot");      // faute de frappe
  assert.equal(search(D, idx, "wood tr")[0], "trunk");
  assert.deepEqual(search(D, idx, "t3"), ["axe"]);         // filtre par tier
  assert.deepEqual(search(D, idx, "zzzz"), []);
  assert.equal(editDistance("trunk", "trnuk"), 1);   // deux lettres inversées = une faute
  assert.equal(editDistance("plank", "plnak"), 1);
  assert.equal(editDistance("axe", "axxe"), 1);
});

// ---------- Cas réels de Julien (données du jeu) ----------
const here = path.dirname(fileURLToPath(import.meta.url));
const dataFile = path.join(here, "..", "build", "data.json");
const real = existsSync(dataFile) ? JSON.parse(readFileSync(dataFile, "utf8")) : null;
const key = (name) => Object.keys(real.items).find((k) => real.items[k].n === name);
const name = (k) => real.items[k].n;

test("réel — cas 1 : hache T3 (Emarium Axe)", { skip: !real }, () => {
  const k = key("Emarium Axe");
  const r = real.recipes[real.best[k]];
  const ins = r.in.map(([i]) => name(i));
  assert.ok(ins.includes("Pyrelite Axe") && ins.includes("Emarium Ingot"));
  assert.match(r.b.n, /Smithing Station/);
  const res = pooled(real, buildTree(real, k, 1));
  const raws = res.raw.map((x) => name(x.key));
  assert.ok(raws.includes("Emarium Ore Chunk"));
  assert.ok(raws.includes("Metalworking Flux"));
  assert.ok(!raws.some((n) => /Pirate/.test(n)), "pas de butin rare dans le chemin le plus facile");
});

test("réel — totaux : les troncs T3 sont mis en commun", { skip: !real }, () => {
  const k = key("Emarium Axe");
  const tree = buildTree(real, k, 1);
  let perBranch = 0;
  (function walk(n) { if (n.key === key("Sturdy Wood Trunk")) perBranch += n.need; n.children.forEach(walk); })(tree);
  const pooledTrunks = pooled(real, tree).raw.find((x) => x.key === key("Sturdy Wood Trunk")).left;
  assert.ok(pooledTrunks <= perBranch);
  assert.ok(pooledTrunks >= 1);
});

test("réel — cas 3 : machette T5 sans recette d'amélioration", { skip: !real }, () => {
  const k = key("Luminite Machete");
  const total = pooled(real, buildTree(real, k, 1)).raw.reduce((s, x) => s + x.left, 0);
  assert.ok(total < 2000, "total raisonnable (un autre site en trouvait 591 267) : " + total);
});

test("réel — cas 2 : supplies du claim", { skip: !real }, () => {
  assert.equal(real.supplies.length, 64);
  const timber = real.supplies.find(([k]) => name(k) === "Sturdy Timber");
  assert.equal(timber[1], 340);
});

test("réel — cas 5 : d'où viennent les troncs", { skip: !real }, () => {
  const src = real.sources[key("Sturdy Wood Trunk")];
  assert.ok(src.some((s) => s.fd && /Tree|Stump/.test(s.res) && s.tl && s.tl[0] === "Axe"));
});

test("réel — recherche", { skip: !real }, () => {
  const idx = buildIndex(real);
  assert.equal(name(search(real, idx, "emarium axe")[0]), "Emarium Axe");
  assert.equal(name(search(real, idx, "emarum axe")[0]), "Emarium Axe");
  assert.ok(search(real, idx, "trunk").map(name).includes("Sturdy Wood Trunk"));
  assert.ok(search(real, idx, "axe t3").map(name).includes("Emarium Axe"));
});
