// Page du site des recettes : recherche, page d'objet (arbre, coches, reste à obtenir), supplies, outils.
// Les calculs sont dans calc.js ; ce fichier ne fait que l'affichage. Textes du site en anglais (D-077).
import { buildTree, pooled, stacks, search, buildIndex, recipeFor, progress } from "./calc.js";

const MAP_URL = "https://bitjita.com/map";
const app = document.getElementById("app");
const searchBox = document.getElementById("search");
const results = document.getElementById("results");
let data = null;
let index = null;

// ---------- Outils d'affichage ----------

function h(tag, attrs = {}, ...kids) {
  const el = document.createElement(tag);
  for (const [k, v] of Object.entries(attrs || {})) {
    if (v === null || v === undefined || v === false) continue;
    if (k === "class") el.className = v;
    else if (k === "style") el.style.cssText = v;   // par le CSSOM : permis par la politique de sécurité (CSP)
    else if (k.startsWith("on")) el.addEventListener(k.slice(2), v);
    else if (k === "html") el.innerHTML = v;
    else el.setAttribute(k, v === true ? "" : v);
  }
  for (const kid of kids.flat()) {
    if (kid === null || kid === undefined || kid === false) continue;
    el.append(kid instanceof Node ? kid : document.createTextNode(String(kid)));
  }
  return el;
}

const fmt = (n) => Number(n).toLocaleString("en-US", { maximumFractionDigits: 2 });
const item = (k) => data.items[k];
const link = (k) => "#/item/" + k;

function icon(path, alt, cls = "ic") {
  if (!path) return h("span", { class: cls + " ic-none", title: alt }, (alt || "?").slice(0, 2));
  const img = h("img", { class: cls, src: "icons/" + path + ".webp", alt: alt || "", loading: "lazy" });
  img.addEventListener("error", () => img.replaceWith(h("span", { class: cls + " ic-none", title: alt }, (alt || "?").slice(0, 2))));
  return img;
}

function tierBadge(t) {
  return t > 0 ? h("span", { class: "tier t" + t }, "T" + t) : null;
}

function stackText(it, n) {
  const s = stacks(it, n);
  if (!s) return "";
  if (s.cargo) return s.stacks === 1 ? "1 cargo slot" : fmt(s.stacks) + " cargo slots";
  return (s.stacks === 1 ? "1 stack" : fmt(s.stacks) + " stacks") + " of " + s.per;
}

function originLabel(it) {
  return { craft: "Crafted", gather: "Gathered", buy: "Bought from a traveler", hunt: "Hunted", other: "Other source" }[it.o] || "";
}

function info(text) {
  // Bouton « i » : l'aide reste cachée tant qu'on ne clique pas (méthode de DOGGO-ESPORT).
  const tip = h("span", { class: "tip", hidden: true }, text);
  const b = h("button", { class: "info", type: "button", "aria-label": "Help", onclick: () => { tip.hidden = !tip.hidden; } }, "i");
  return h("span", { class: "info-wrap" }, b, tip);
}

// ---------- État gardé dans le navigateur (pas de compte) ----------

function loadState(key) {
  try {
    return JSON.parse(localStorage.getItem("rr:" + key)) || {};
  } catch {
    return {};
  }
}

function saveState(key, st) {
  try {
    localStorage.setItem("rr:" + key, JSON.stringify(st));
  } catch { /* stockage plein ou interdit : on continue sans sauvegarde */ }
}

// ---------- Recherche ----------

let active = -1;

function showResults() {
  const q = searchBox.value;
  const keys = q.trim() ? search(data, index, q, 12) : [];
  results.replaceChildren(...keys.map((k, i) => {
    const it = item(k);
    return h("a", { class: "res" + (i === active ? " active" : ""), href: link(k), onclick: closeResults },
      icon(it.ic, it.n), h("span", { class: "res-name" }, it.n), tierBadge(it.t), h("span", { class: "res-kind" }, originLabel(it)));
  }));
  if (q.trim() && keys.length === 0) results.append(h("div", { class: "res empty" }, "Nothing found"));
  results.hidden = !q.trim();
}

function closeResults() {
  results.hidden = true;
  active = -1;
  searchBox.value = "";
  searchBox.blur();
}

searchBox.addEventListener("input", () => { active = -1; showResults(); });
searchBox.addEventListener("keydown", (e) => {
  const rows = [...results.querySelectorAll("a.res")];
  if (e.key === "ArrowDown" || e.key === "ArrowUp") {
    e.preventDefault();
    if (!rows.length) return;
    active = (active + (e.key === "ArrowDown" ? 1 : -1) + rows.length) % rows.length;
    rows.forEach((r, i) => r.classList.toggle("active", i === active));
  } else if (e.key === "Enter") {
    const r = rows[active >= 0 ? active : 0];
    if (r) { location.hash = r.getAttribute("href"); closeResults(); }
  } else if (e.key === "Escape") {
    closeResults();
  }
});
document.addEventListener("click", (e) => {
  if (!e.target.closest(".searchbar")) results.hidden = true;
});
document.addEventListener("keydown", (e) => {
  if (e.key === "/" && document.activeElement !== searchBox && !/INPUT|TEXTAREA/.test(document.activeElement.tagName)) {
    e.preventDefault();
    searchBox.focus();
  }
});

// ---------- Pages ----------

function pageHome() {
  const feat = data.featured.map((k) => chip(k));
  return h("section", { class: "home" },
    h("h1", {}, "What do you want to craft?"),
    h("p", { class: "muted" }, "Type an item name in the search bar. Typos are fine."),
    h("div", { class: "shortcuts" },
      h("a", { class: "shortcut", href: "#/supplies" }, h("b", {}, "Claim supplies"), h("span", {}, "What fills your claim")),
      h("a", { class: "shortcut", href: "#/tools" }, h("b", {}, "Tools"), h("span", {}, "Every tool by profession and tier"))),
    h("h2", {}, "Popular items"),
    h("div", { class: "chips" }, feat));
}

function chip(k, extra) {
  const it = item(k);
  return h("a", { class: "chip", href: link(k) }, icon(it.ic, it.n), h("span", {}, it.n), tierBadge(it.t), extra || null);
}

function pageItem(key) {
  const it = item(key);
  if (!it) return h("section", {}, h("h1", {}, "Item not found"), h("a", { href: "#/" }, "Back"));
  const st = loadState(key);
  if (!st.qty) st.qty = 1;
  const wrap = h("section", { class: "item" });

  const head = h("div", { class: "item-head" },
    icon(it.ic, it.n, "ic big"),
    h("div", { class: "item-title" },
      h("h1", {}, it.n, " ", tierBadge(it.t)),
      h("div", { class: "muted" }, [it.g, originLabel(it), it.r.filter(Boolean).length > 1 ? it.r.join(" / ") : null].filter(Boolean).join(" · ")),
      it.d ? h("p", { class: "desc" }, it.d) : null));
  wrap.append(head);

  if (it.o === "craft") {
    wrap.append(craftSection(key, st));
  } else {
    wrap.append(whereSection(key));
  }
  wrap.append(usedInSection(key));
  return wrap;
}

function whereSection(key) {
  const it = item(key);
  const box = h("div", { class: "card" }, h("h2", {}, "Where to get it"));
  const src = data.sources[key] || [];
  if (src.length) {
    const list = h("div", { class: "sources" });
    src.slice(0, 8).forEach((s) => {
      list.append(h("div", { class: "source" + (s.rare ? " rare" : "") },
        icon(s.ic, s.res),
        h("div", {},
          h("b", {}, s.res), " ", tierBadge(s.rt),
          h("div", { class: "muted small" }, [
            s.fd ? "when fully harvested" : (s.p < 1 ? Math.round(s.p * 1000) / 10 + "% per action" : "every action"),
            s.tl ? s.tl[0] + " T" + s.tl[1] : "bare hands",
            s.s ? s.s[0] + " lvl " + s.s[1] : null,
            s.rare ? "rare spot" : null,
          ].filter(Boolean).join(" · ")))));
    });
    box.append(list);
    if (src.length > 8) box.append(h("p", { class: "muted small" }, "+ " + (src.length - 8) + " other sources"));
  } else if (it.o === "buy") {
    box.append(h("p", {}, it.d || "Sold by a traveler."));
  } else if (it.o === "hunt") {
    box.append(h("p", {}, "Hunt this animal, then process it at a hunting station."));
  } else {
    box.append(h("p", { class: "muted" }, "No crafting recipe and no gathering spot in the game data (loot, quest, event or market)."));
  }
  box.append(h("p", {}, h("a", { class: "btn", href: MAP_URL, target: "_blank", rel: "noopener" }, "Open the Bitjita map ↗"),
    " ", info("The game data does not say in which biome a resource grows. The Bitjita map shows resources by tier.")));
  return box;
}

function usedInSection(key) {
  const used = data.usedIn[key] || [];
  if (!used.length) return h("div");
  const box = h("div", { class: "card" }, h("h2", {}, "Used in ", h("span", { class: "muted" }, "(" + used.length + ")")));
  const grid = h("div", { class: "chips" });
  const show = (n) => grid.replaceChildren(...used.slice(0, n).map((k) => chip(k)));
  show(24);
  box.append(grid);
  if (used.length > 24) {
    const more = h("button", { class: "btn", type: "button", onclick: () => { show(used.length); more.remove(); } }, "Show all");
    box.append(more);
  }
  return box;
}

// ---------- Fabrication : arbre, coches, reste ----------

function craftSection(key, st) {
  const box = h("div", { class: "craft" });
  const save = () => saveState(key, st);
  st.owned = st.owned || {};
  st.checked = st.checked || {};
  st.recipe = st.recipe || {};
  st.collapsed = st.collapsed || {};
  st.view = st.view || (window.matchMedia("(max-width: 760px)").matches ? "list" : "tree");

  const qty = h("input", { type: "number", min: 1, max: 100000, value: st.qty, class: "qty", "aria-label": "Quantity" });
  const rid = recipeFor(data, key, st.recipe);
  const alts = [data.best[key], ...(data.alts[key] || [])].filter(Boolean);
  const sel = h("select", { class: "recipe-select", "aria-label": "Recipe" },
    alts.slice(0, 12).map((r, i) => h("option", { value: r, selected: r === rid }, recipeLabel(r, i === 0))));
  const bar = h("div", { class: "toolbar" },
    h("label", {}, "Quantity ", qty),
    alts.length > 1 ? h("label", {}, "Recipe ", sel) : h("span", { class: "muted" }, recipeLabel(rid, true)),
    h("div", { class: "seg" },
      h("button", { type: "button", class: st.view === "tree" ? "on" : "", onclick: () => { st.view = "tree"; save(); draw(); } }, "Tree"),
      h("button", { type: "button", class: st.view === "list" ? "on" : "", onclick: () => { st.view = "list"; save(); draw(); } }, "List")),
    h("button", { type: "button", class: "btn ghost", onclick: () => {
      st.owned = {}; st.checked = {}; save(); draw();
    } }, "Reset"),
    info("Tick an item when you have it (or already crafted it): its whole branch turns grey. Or type how many you already have. Everything is saved in this browser only."));

  qty.addEventListener("change", () => { st.qty = Math.max(1, Math.min(100000, Math.floor(Number(qty.value) || 1))); qty.value = st.qty; save(); draw(); });
  sel.addEventListener("change", () => { st.recipe[key] = sel.value; save(); draw(); });

  const progressBar = h("div", { class: "progress" }, h("div", { class: "fill" }), h("span", {}));
  const body = h("div", { class: "craft-body" });
  box.append(bar, progressBar, body);

  let view = null;   // vue de l'arbre gardée entre deux dessins (zoom et position)

  function draw() {
    const tree = buildTree(data, key, st.qty, st);
    const p = progress(data, key, st.qty, st);
    progressBar.querySelector(".fill").style.width = Math.round(p * 100) + "%";
    progressBar.querySelector("span").textContent = Math.round(p * 100) + "% of raw materials gathered";
    const { raw: left, steps } = pooled(data, tree);
    const main = st.view === "tree" ? treeView(tree, st, save, draw, view) : listView(tree, st, save, draw);
    if (st.view === "tree") view = main.view;
    body.replaceChildren(main.el, sidePanel(left, steps));
  }
  draw();
  return box;
}

function recipeLabel(rid, isBest) {
  const r = data.recipes[rid];
  if (!r) return "";
  const where = r.b ? r.b.n : "By hand";
  const names = r.in.map(([k, q]) => q + " " + item(k).n).join(", ");
  return (isBest ? "★ Easiest: " : "") + where + " — " + names + (r.oq > 1 ? " → " + r.oq : "");
}

function nodeControls(n, st, save, draw) {
  const it = item(n.key);
  const box = h("input", { type: "checkbox", checked: n.done, disabled: n.done && !n.checked, title: "I have it",
    "aria-label": "I have " + it.n });
  box.addEventListener("change", () => {
    if (box.checked) st.checked[n.path] = true; else delete st.checked[n.path];
    save(); draw();
  });
  const have = h("input", { type: "number", min: 0, class: "have", value: n.have || "", placeholder: "have",
    disabled: n.done, title: "How many you already have", "aria-label": "Already have" });
  have.addEventListener("change", () => {
    const v = Math.max(0, Math.floor(Number(have.value) || 0));
    if (v > 0) st.owned[n.path] = v; else delete st.owned[n.path];
    save(); draw();
  });
  return [box, have];
}

function nodeMeta(n) {
  const r = n.recipeId ? data.recipes[n.recipeId] : null;
  if (r) {
    return [r.b ? r.b.n : "By hand", r.s ? r.s[0] + " " + r.s[1] : null, n.crafts ? n.crafts + "× craft" : null].filter(Boolean).join(" · ");
  }
  if (n.cycle) return "Loop: see above";
  const it = item(n.key);
  const s = (data.sources[n.key] || [])[0];
  if (s) return "Gather: " + s.res + (s.tl ? " · " + s.tl[0] + " T" + s.tl[1] : "");
  return originLabel(it);
}

// Vue en arbre : de haut en bas, zoom (molette, boutons), déplacement (glisser), branches repliables.
function treeView(tree, st, save, draw, previous) {
  const W = 190, H = 150, GX = 18, GY = 46;
  const nodes = [];
  const edges = [];
  // Placement : chaque feuille prend une colonne ; un parent est centré sur ses enfants.
  let col = 0;
  (function place(n, depth) {
    const kids = st.collapsed[n.path] ? [] : n.children;
    let x;
    if (kids.length === 0) {
      x = col * (W + GX);
      col++;
    } else {
      const xs = kids.map((c) => place(c, depth + 1));
      x = (xs[0] + xs[xs.length - 1]) / 2;
      kids.forEach((c, i) => edges.push([x, depth, xs[i], depth + 1, c.done]));
    }
    nodes.push({ n, x, y: depth * (H + GY) });
    return x;
  })(tree, 0);
  const width = Math.max(col * (W + GX), W);
  const depthMax = Math.max(...nodes.map((o) => o.y)) + H;

  const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
  svg.setAttribute("width", width);
  svg.setAttribute("height", depthMax);
  svg.setAttribute("class", "edges");
  for (const [x1, d1, x2, d2, done] of edges) {
    const y1 = d1 * (H + GY) + H, y2 = d2 * (H + GY);
    const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
    const my = (y1 + y2) / 2;
    path.setAttribute("d", `M${x1 + W / 2},${y1} C${x1 + W / 2},${my} ${x2 + W / 2},${my} ${x2 + W / 2},${y2}`);
    path.setAttribute("class", done ? "edge done" : "edge");
    svg.append(path);
  }

  const canvas = h("div", { class: "canvas", style: `width:${width}px;height:${depthMax}px` }, svg);
  for (const { n, x, y } of nodes) {
    const it = item(n.key);
    const [box, have] = nodeControls(n, st, save, draw);
    const fold = n.children.length ? h("button", { type: "button", class: "fold", title: st.collapsed[n.path] ? "Expand" : "Collapse",
      onclick: () => { if (st.collapsed[n.path]) delete st.collapsed[n.path]; else st.collapsed[n.path] = true; save(); draw(); } },
    st.collapsed[n.path] ? "+" : "−") : null;
    canvas.append(h("div", { class: "node" + (n.done ? " done" : "") + (n.left === 0 && !n.done ? " ok" : ""),
      style: `left:${x}px;top:${y}px;width:${W}px;height:${H}px` },
    h("div", { class: "node-top" }, icon(it.ic, it.n), h("a", { href: link(n.key), class: "node-name", title: it.n }, it.n), tierBadge(it.t)),
    h("div", { class: "node-qty" }, h("b", {}, "×" + fmt(n.need)), n.have && !n.done ? h("span", { class: "muted" }, " (" + fmt(n.left) + " left)") : null),
    h("div", { class: "node-stack muted" }, stackText(it, n.need)),
    h("div", { class: "node-meta" }, nodeMeta(n)),
    h("div", { class: "node-ctl" }, box, have, fold)));
  }

  const viewport = h("div", { class: "viewport" }, canvas);
  const v = previous || { s: 1, x: 0, y: 0, fitted: false };
  const apply = () => { canvas.style.transform = `translate(${v.x}px,${v.y}px) scale(${v.s})`; };
  const zoomAt = (f, cx, cy) => {
    const s = Math.max(0.15, Math.min(2.5, v.s * f));
    v.x = cx - (cx - v.x) * (s / v.s);
    v.y = cy - (cy - v.y) * (s / v.s);
    v.s = s;
    apply();
  };
  const fit = () => {
    const r = viewport.getBoundingClientRect();
    const s = Math.max(0.15, Math.min(1, (r.width - 20) / width, (r.height - 20) / depthMax));
    v.s = s;
    v.x = (r.width - width * s) / 2;
    v.y = 10;
    v.fitted = true;
    apply();
  };
  // Première vue : lisible d'abord (pas plus petit que 60 %), centrée sur l'objet final, en haut.
  const start = () => {
    const r = viewport.getBoundingClientRect();
    const s = Math.max(0.6, Math.min(1, (r.width - 20) / width, (r.height - 20) / depthMax));
    const rootX = nodes[nodes.length - 1].x + W / 2;
    v.s = s;
    v.x = width * s <= r.width ? (r.width - width * s) / 2 : r.width / 2 - rootX * s;
    v.y = 10;
    v.fitted = true;
    apply();
  };
  viewport.addEventListener("wheel", (e) => {
    e.preventDefault();
    const r = viewport.getBoundingClientRect();
    zoomAt(e.deltaY < 0 ? 1.15 : 1 / 1.15, e.clientX - r.left, e.clientY - r.top);
  }, { passive: false });
  // Déplacement à la souris ou au doigt, sauf sur les cases et les champs.
  let drag = null;
  viewport.addEventListener("pointerdown", (e) => {
    if (e.target.closest("input,button,a,select")) return;
    drag = { x: e.clientX - v.x, y: e.clientY - v.y };
    viewport.setPointerCapture(e.pointerId);
    viewport.classList.add("dragging");
  });
  viewport.addEventListener("pointermove", (e) => { if (drag) { v.x = e.clientX - drag.x; v.y = e.clientY - drag.y; apply(); } });
  const stop = () => { drag = null; viewport.classList.remove("dragging"); };
  viewport.addEventListener("pointerup", stop);
  viewport.addEventListener("pointercancel", stop);

  const center = () => { const r = viewport.getBoundingClientRect(); return [r.width / 2, r.height / 2]; };
  const zoomBar = h("div", { class: "zoombar" },
    h("button", { type: "button", title: "Zoom in", onclick: () => zoomAt(1.25, ...center()) }, "+"),
    h("button", { type: "button", title: "Zoom out", onclick: () => zoomAt(0.8, ...center()) }, "−"),
    h("button", { type: "button", title: "Fit to screen", onclick: fit }, "Fit"));
  const el = h("div", { class: "tree-wrap" }, viewport, zoomBar);
  if (v.fitted || previous) requestAnimationFrame(apply); else requestAnimationFrame(start);
  return { el, view: v };
}

// Vue en liste (téléphone) : même contenu, en retrait.
function listView(tree, st, save, draw) {
  const ul = h("div", { class: "list" });
  (function walk(n, depth) {
    const it = item(n.key);
    const [box, have] = nodeControls(n, st, save, draw);
    const folded = st.collapsed[n.path];
    const fold = n.children.length ? h("button", { type: "button", class: "fold",
      onclick: () => { if (folded) delete st.collapsed[n.path]; else st.collapsed[n.path] = true; save(); draw(); } }, folded ? "+" : "−") : h("span", { class: "fold-space" });
    ul.append(h("div", { class: "row" + (n.done ? " done" : ""), style: `--d:${depth}` },
      fold, box, icon(it.ic, it.n),
      h("div", { class: "row-main" },
        h("div", {}, h("b", {}, "×" + fmt(n.need)), " ", h("a", { href: link(n.key) }, it.n), " ", tierBadge(it.t),
          n.have && !n.done ? h("span", { class: "muted" }, " (" + fmt(n.left) + " left)") : null),
        h("div", { class: "muted small" }, [stackText(it, n.need), nodeMeta(n)].filter(Boolean).join(" · "))),
      have));
    if (!folded) n.children.forEach((c) => walk(c, depth + 1));
  })(tree, 0);
  return { el: ul };
}

function sidePanel(left, steps) {
  const need = h("div", { class: "card side" }, h("h2", {}, "Still needed"),
    left.length ? h("div", { class: "need" }, left.map(({ key, left: n }) => {
      const it = item(key);
      const s = (data.sources[key] || [])[0];
      return h("a", { class: "need-row", href: link(key) }, icon(it.ic, it.n),
        h("div", {}, h("b", {}, "×" + fmt(n)), " ", it.n, " ", tierBadge(it.t),
          h("div", { class: "muted small" }, [stackText(it, n), s ? s.res : originLabel(it)].filter(Boolean).join(" · "))));
    })) : h("p", { class: "done-msg" }, "✔ You have everything!"));
  const order = h("div", { class: "card side" }, h("h2", {}, "Craft order ", info("From raw materials to the final item. Each line: how many times to craft, and where.")),
    steps.length ? h("ol", { class: "steps" }, steps.map((s) => {
      const r = data.recipes[s.recipeId];
      const it = item(s.key);
      return h("li", {}, h("b", {}, s.crafts + "×"), " ", it.n, " ", tierBadge(it.t),
        h("div", { class: "muted small" }, (r.b ? r.b.n : "By hand") + " · makes " + fmt(s.made)));
    })) : h("p", { class: "muted" }, "Nothing left to craft."));
  return h("div", { class: "side-col" }, need, order);
}

// ---------- Supplies et outils ----------

function pageSupplies() {
  const rows = [...data.supplies];
  const sortBy = { key: "t", dir: 1 };
  const table = h("table", { class: "table" });
  function draw() {
    rows.sort((a, b) => {
      const A = sortBy.key === "v" ? a[1] : sortBy.key === "n" ? item(a[0]).n : item(a[0]).t;
      const B = sortBy.key === "v" ? b[1] : sortBy.key === "n" ? item(b[0]).n : item(b[0]).t;
      return (A > B ? 1 : A < B ? -1 : 0) * sortBy.dir || a[1] - b[1];
    });
    const th = (k, label) => h("th", { onclick: () => { sortBy.dir = sortBy.key === k ? -sortBy.dir : 1; sortBy.key = k; draw(); } },
      label + (sortBy.key === k ? (sortBy.dir > 0 ? " ▲" : " ▼") : ""));
    table.replaceChildren(
      h("thead", {}, h("tr", {}, th("n", "Item"), th("t", "Tier"), th("v", "Supplies"))),
      h("tbody", {}, rows.map(([k, v]) => {
        const it = item(k);
        return h("tr", {}, h("td", {}, h("a", { href: link(k), class: "cell-item" }, icon(it.ic, it.n), it.n)),
          h("td", {}, tierBadge(it.t)), h("td", { class: "num" }, fmt(v)));
      })));
  }
  draw();
  return h("section", {}, h("h1", {}, "Claim supplies ", info("Cargo that refills a claim's supplies, and how many supplies each one gives. Click a column to sort.")), table);
}

function pageTools() {
  const groups = new Map();
  for (const [k, it] of Object.entries(data.items)) {
    if (!/ Tool$/.test(it.g) || it.t < 1 || it.o !== "craft") continue;
    const g = it.g.replace(/ Tool$/, "");
    if (!groups.has(g)) groups.set(g, {});
    const byTier = groups.get(g);
    (byTier[it.t] = byTier[it.t] || []).push(k);
  }
  const tiers = Array.from({ length: 10 }, (_, i) => i + 1);
  const cell = (k) => h("a", { href: link(k), title: item(k).n, class: "tool-cell" },
    icon(item(k).ic, item(k).n), h("span", {}, item(k).n.split(" ").slice(-1)[0]));
  const row = ([g, byTier]) => h("tr", {}, h("td", {}, h("b", {}, g)),
    tiers.map((t) => h("td", {}, (byTier[t] || []).map(cell))));
  const table = h("table", { class: "table tools" },
    h("thead", {}, h("tr", {}, h("th", {}, "Profession"), tiers.map((t) => h("th", {}, tierBadge(t))))),
    h("tbody", {}, [...groups.entries()].sort().map(row)));
  return h("section", {}, h("h1", {}, "Tools"), h("div", { class: "scroll-x" }, table));
}

// ---------- Navigation ----------

function route() {
  const hash = location.hash || "#/";
  let page;
  if (hash.startsWith("#/item/")) page = pageItem(decodeURIComponent(hash.slice(7)));
  else if (hash === "#/supplies") page = pageSupplies();
  else if (hash === "#/tools") page = pageTools();
  else page = pageHome();
  app.replaceChildren(page);
  const title = hash.startsWith("#/item/") && item(hash.slice(7)) ? item(hash.slice(7)).n + " — " : "";
  document.title = title + "Riverwood Recipes";
  window.scrollTo(0, 0);
}

async function start() {
  try {
    const r = await fetch("data.json", { cache: "no-cache" });
    if (!r.ok) throw new Error("HTTP " + r.status);
    data = await r.json();
  } catch (e) {
    app.replaceChildren(h("section", {}, h("h1", {}, "Could not load the recipes"), h("p", { class: "muted" }, String(e))));
    return;
  }
  index = buildIndex(data);
  document.getElementById("version").textContent = "Game data: " + (data.version.updatedAt || "").slice(0, 10);
  searchBox.disabled = false;
  searchBox.placeholder = "Search an item… (press / )";
  window.addEventListener("hashchange", route);
  route();
}

start();
