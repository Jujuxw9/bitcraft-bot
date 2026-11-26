// Calculs du site des recettes : arbre de fabrication, coches, « j'en ai déjà », reste à récolter, stacks.
// Module sans accès à la page : testé par tests/test_calc.mjs (Node.js).

// Arbre de fabrication d'un objet.
// state : { owned: {chemin: nombre}, checked: {chemin: true}, recipe: {clé: id de recette} }
// Chaque nœud : { key, path, need, have, done, left, recipeId, crafts, perCraft, children, cycle, depth }
//   need : quantité demandée par le parent ; have : déjà possédé ; done : coché (lui ou un parent) ;
//   left : reste à obtenir (0 si coché) ; crafts : nombre de fabrications pour « left ».
export function buildTree(data, key, qty, state = {}, maxDepth = 25) {
  const owned = state.owned || {};
  const checked = state.checked || {};
  const choice = state.recipe || {};

  function node(k, need, path, ancestors, depth, parentDone) {
    const p = path ? path + "/" + k : k;
    const isChecked = !!checked[p];
    const done = parentDone || isChecked;
    const have = Math.max(0, Math.floor(Number(owned[p]) || 0));
    const left = done ? 0 : Math.max(0, need - have);
    const n = { key: k, path: p, need, have, done, checked: isChecked, left, recipeId: null, crafts: 0,
      perCraft: 0, children: [], cycle: false, depth };
    const rid = recipeFor(data, k, choice);
    if (!rid) return n;
    if (ancestors.has(k) || depth >= maxDepth) {
      n.cycle = true;
      return n;
    }
    const r = data.recipes[rid];
    n.recipeId = rid;
    n.perCraft = r.oq;
    // Les enfants gardent la quantité totale (pour l'affichage), mais un parent coché les marque faits.
    const craftsTotal = craftsNeeded(done ? need : left, r.oq);
    n.crafts = done ? 0 : craftsNeeded(left, r.oq);
    const next = new Set(ancestors);
    next.add(k);
    for (const [ik, q] of r.in) {
      n.children.push(node(ik, q * craftsTotal, p, next, depth + 1, done));
    }
    return n;
  }

  return node(key, Math.max(0, Math.floor(Number(qty) || 0)), "", new Set(), 0, false);
}

// Recette utilisée pour un objet : choix de l'utilisateur s'il est valable, sinon la plus facile.
export function recipeFor(data, key, choice = {}) {
  const c = choice[key];
  if (c && data.recipes[c] && data.recipes[c].o === key) return c;
  return data.best[key] || null;
}

// Nombre de fabrications pour obtenir au moins `need` unités.
export function craftsNeeded(need, perCraft) {
  if (!(need > 0)) return 0;
  return Math.ceil(need / perCraft - 1e-9);
}

// Totaux mis en commun : un même objet demandé par plusieurs branches est fabriqué en une fois
// (ex. 4 bûches + 6 bûches = 10 bûches = 2 troncs, et pas 1 + 1 tronc par branche puis 4 au total).
// « J'en ai déjà » et les coches sont additionnés par objet. Rend { raw: [{key, left}], steps: [...] }.
export function pooled(data, tree) {
  const recipeOf = new Map();       // clé → recette utilisée dans l'arbre
  const have = new Map();           // clé → quantité déjà possédée (nombres tapés + cases cochées)
  const edges = new Map();          // clé → clés des ingrédients
  (function walk(n, parentDone) {
    if (parentDone) return;
    if (n.checked) have.set(n.key, (have.get(n.key) || 0) + n.need);
    else if (n.have) have.set(n.key, (have.get(n.key) || 0) + n.have);
    if (n.checked) return;
    if (n.recipeId && !n.cycle) {
      recipeOf.set(n.key, n.recipeId);
      const set = edges.get(n.key) || new Set();
      n.children.forEach((c) => set.add(c.key));
      edges.set(n.key, set);
    }
    n.children.forEach((c) => walk(c, false));
  })(tree, false);

  // Ordre : un objet passe après tous ceux qui l'utilisent (parcours en profondeur, ordre inverse).
  const order = [];
  const seen = new Set();
  const onPath = new Set();
  (function visit(k) {
    if (seen.has(k) || onPath.has(k)) return;
    onPath.add(k);
    for (const c of edges.get(k) || []) visit(c);
    onPath.delete(k);
    seen.add(k);
    order.push(k);
  })(tree.key);
  order.reverse();

  const demand = new Map([[tree.key, tree.need]]);
  const raw = [];
  const steps = [];
  order.forEach((k, i) => {
    const want = demand.get(k) || 0;
    const left = Math.max(0, want - (have.get(k) || 0));
    if (left <= 0) return;
    const rid = recipeOf.get(k);
    if (!rid) {
      raw.push({ key: k, left });
      return;
    }
    const r = data.recipes[rid];
    const crafts = craftsNeeded(left, r.oq);
    steps.push({ key: k, crafts, made: crafts * r.oq, recipeId: rid, rank: order.length - i });
    for (const [ik, q] of r.in) demand.set(ik, (demand.get(ik) || 0) + q * crafts);
  });
  raw.sort((a, b) => b.left - a.left);
  steps.sort((a, b) => a.rank - b.rank);
  return { raw, steps };
}

// Stacks : { per: unités par case, stacks: nombre de cases } ; null si le volume est inconnu.
export function stacks(item, count) {
  if (!item || !item.st || !(count > 0)) return null;
  return { per: item.st, stacks: Math.ceil(count / item.st), cargo: item.k === "c" };
}

// Progression : part des matières premières déjà réunies (0 à 1).
export function progress(data, key, qty, state) {
  const full = pooled(data, buildTree(data, key, qty, { recipe: state.recipe })).raw;
  const now = pooled(data, buildTree(data, key, qty, state)).raw;
  const total = full.reduce((s, x) => s + x.left, 0);
  const left = now.reduce((s, x) => s + x.left, 0);
  return total > 0 ? Math.max(0, Math.min(1, 1 - left / total)) : 1;
}

// ---------- Recherche tolérante aux fautes ----------

export function normalize(s) {
  return String(s || "").toLowerCase().normalize("NFD").replace(/[̀-ͯ]/g, "")
    .replace(/[^a-z0-9]+/g, " ").trim();
}

// Distance d'édition (lettre en trop, en moins, changée, ou deux lettres inversées), limitée : rend max+1 au-delà.
export function editDistance(a, b, max = 2) {
  if (Math.abs(a.length - b.length) > max) return max + 1;
  let prev2 = null;
  let prev = Array.from({ length: b.length + 1 }, (_, i) => i);
  for (let i = 1; i <= a.length; i++) {
    const cur = [i];
    let rowMin = i;
    for (let j = 1; j <= b.length; j++) {
      let v = Math.min(prev[j] + 1, cur[j - 1] + 1, prev[j - 1] + (a[i - 1] === b[j - 1] ? 0 : 1));
      if (prev2 && i > 1 && j > 1 && a[i - 1] === b[j - 2] && a[i - 2] === b[j - 1]) v = Math.min(v, prev2[j - 2] + 1);
      cur.push(v);
      rowMin = Math.min(rowMin, v);
    }
    if (rowMin > max) return max + 1;
    prev2 = prev;
    prev = cur;
  }
  return prev[b.length];
}

// Index de recherche : un tableau d'entrées { key, words, name }.
export function buildIndex(data) {
  return Object.entries(data.items).map(([key, it]) => ({ key, name: normalize(it.n), words: normalize(it.n).split(" ") }));
}

// Recherche : chaque mot tapé doit trouver un mot du nom (début, milieu ou faute légère).
// « t3 » ou « tier 3 » filtre le tier. Rend les clés triées par pertinence.
export function search(data, index, query, limit = 30) {
  let q = normalize(query);
  let tier = null;
  q = q.replace(/\b(?:tier|t)\s?(\d{1,2})\b/, (_, d) => { tier = Number(d); return " "; }).trim();
  const tokens = q.split(" ").filter(Boolean);
  if (tokens.length === 0 && tier === null) return [];
  const out = [];
  for (const e of index) {
    const it = data.items[e.key];
    if (tier !== null && it.t !== tier) continue;
    let score = 0;
    let ok = true;
    for (const t of tokens) {
      let best = 0;
      for (const w of e.words) {
        if (w === t) best = Math.max(best, 10);
        else if (w.startsWith(t)) best = Math.max(best, 7);
        else if (t.length >= 3 && w.includes(t)) best = Math.max(best, 4);
        else if (t.length >= 4) {
          const d = editDistance(t, w.slice(0, Math.max(t.length, Math.min(w.length, t.length + 1))), t.length >= 7 ? 2 : 1);
          if (d <= (t.length >= 7 ? 2 : 1)) best = Math.max(best, 3 - d);
        }
      }
      if (best === 0) { ok = false; break; }
      score += best;
    }
    if (!ok) continue;
    if (e.name === q) score += 20;
    if (it.o === "craft") score += 2;
    if (it.o === "gather") score += 1;
    out.push({ key: e.key, score, len: e.name.length, t: it.t });
  }
  out.sort((a, b) => b.score - a.score || a.len - b.len || a.t - b.t);
  return out.slice(0, limit).map((x) => x.key);
}
