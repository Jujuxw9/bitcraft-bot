"""Cœur du calcul des données du site (fonctions pures, testées par tests/test_core.py).

Entrée : les tables du jeu déjà lues (listes de dictionnaires, une par fichier *_desc.json).
Sortie : un dictionnaire prêt à écrire en JSON pour la page (voir build()).

Règles (D-077) :
- un objet est regroupé par (genre, nom) : les variantes de rareté d'un même objet ne font qu'un ;
- les objets « X Output » du jeu s'ouvrent aussitôt (table item_list_desc) : on garde le résultat le plus probable ;
- un objet récoltable est une matière première ; sans recette ni récolte : « autre source » (butin, marchand…) ;
- recette la plus facile = celle qui demande le moins de matières premières au total, calculé jusqu'à
  stabilité (une boucle ne fait jamais baisser le coût, donc les paquets sont écartés d'office) ;
  le recyclage (« Salvage »), l'amélioration (« Recraft ») et les paquets (« Package », « Unpack ») sont écartés ; une « autre source » coûte très cher ;
  à égalité : la machine de plus bas tier, puis le moins d'ingrédients.
"""
import math
import re

POCKET_VOLUME = 6000          # volume d'une case d'inventaire du joueur (player_create.rs)
INF = float("inf")
EPS = 1e-9
OTHER_SOURCE_COST = 1000.0    # objet sans recette ni récolte : à éviter dans le choix des recettes
EXCLUDED_PREFIXES = ("Salvage", "Recraft", "Package", "Unpack")   # recyclage, amélioration, mise en paquet
# Ressources rares ou ponctuelles (ruines, épaves, coffres, quêtes…) : une source à éviter dans le calcul.
RARE_RESOURCE_TAGS = {"Ancient Loot", "Dungeon", "Dungeon Obstacle", "Dungeon Resource", "Hexite Meteor", "Hidden Resource",
                      "Note", "Profession Dungeon Resource", "Prospecting Clue", "Quest", "Quest Resource", "Rare Research",
                      "Research", "Ruin Resource", "Seasonal", "Seasonal Resource", "Shipwreck Materials", "Summer Resource",
                      "Treasure", "Uncharted Resource", "Uncharted Scholar", "Uncharted Tailoring Resource", "Wonder Resource",
                      "World Event", "Depleted Resource"}
RARE_FACTOR = 50.0
RARITY_ORDER = {"Common": 0, "Uncommon": 1, "Rare": 2, "Epic": 3, "Legendary": 4, "Mythic": 5}


def key_of(kind, item_id):
    """Clé courte d'un objet brut du jeu : 'i' + id pour un objet, 'c' + id pour un cargo."""
    return ("c" if kind == "Cargo" else "i") + str(item_id)


def normalize_icon(icon):
    """Chemin d'image du jeu → chemin de fichier (sans « [,3,10] », sans préfixe répété)."""
    if not icon:
        return None
    path = icon.split("[", 1)[0].strip().strip("/")
    while "GeneratedIcons/Other/GeneratedIcons/" in path:
        path = path.replace("GeneratedIcons/Other/GeneratedIcons/", "GeneratedIcons/", 1)
    if not path or not re.fullmatch(r"[A-Za-z0-9/_\-. ]+", path):
        return None
    # Jamais de « . » ni de « .. » : le chemin sert à écrire un fichier dans build/icons.
    if any(part in ("", ".", "..") for part in path.split("/")):
        return None
    return path


def canonical_items(items, cargos):
    """Regroupe les variantes. Rend (objets canoniques par clé, table clé brute → clé canonique)."""
    groups = {}
    for kind, table in (("Item", items), ("Cargo", cargos)):
        for x in table:
            if not x.get("name") or x.get("tag") == "DEVELOPER ITEM":
                continue
            groups.setdefault((kind, x["name"]), []).append(x)
    canon, alias = {}, {}
    for (kind, name), variants in groups.items():
        # Variante de référence : la plus commune, puis le plus petit id (stable d'une mise à jour à l'autre).
        ref = min(variants, key=lambda v: (RARITY_ORDER.get(v.get("rarity"), 9), v["id"]))
        k = key_of(kind, ref["id"])
        canon[k] = {
            "n": name,
            "k": "c" if kind == "Cargo" else "i",
            "t": ref.get("tier", -1),
            "g": ref.get("tag", ""),
            "d": ref.get("description", ""),
            "v": ref.get("volume", 0),
            "ic": normalize_icon(ref.get("icon_asset_name")),
            # Images des autres raretés, essayées si celle de référence manque (retirées avant l'écriture).
            "alt_ic": sorted({normalize_icon(v.get("icon_asset_name")) for v in variants} - {None, normalize_icon(ref.get("icon_asset_name"))}),
            "r": sorted({v.get("rarity", "") for v in variants}, key=lambda r: RARITY_ORDER.get(r, 9)),
        }
        for v in variants:
            alias[key_of(kind, v["id"])] = k
    return canon, alias


def stack_size(item):
    """Nombre d'unités par case : 6 000 ÷ volume pour un objet, 1 pour un cargo. None si inconnu."""
    if item["k"] == "c":
        return 1
    v = item.get("v") or 0
    return POCKET_VOLUME // v if v > 0 else None


def building_names(building_desc, building_types):
    """(type, tier) → (nom, image) du bâtiment de ce tier, ex. (25, 3) → « Sturdy Smithing Station »."""
    names = {}
    for b in building_desc:
        if b.get("name", "").startswith("Ancient"):
            continue
        for f in b.get("functions", []):
            k = (f.get("function_type"), f.get("level"))
            slots = f.get("crafting_slots", 0) + f.get("refining_slots", 0) + f.get("refining_cargo_slots", 0)
            if k not in names and slots > 0:
                names[k] = (b["name"], normalize_icon(b.get("icon_asset_name")))
    type_names = {t["id"]: t["name"] for t in building_types}
    return names, type_names


def building_label(names, type_names, req):
    """Machine demandée par une recette : {n: nom, t: tier, ic: image}, ou None (à la main)."""
    if not req or not req.get("building_type"):
        return None
    t, tier = req["building_type"], max(req.get("tier", 0), 0)
    for lvl in range(max(tier, 1), 11):
        if (t, lvl) in names:
            name, icon = names[(t, lvl)]
            return {"n": name, "t": tier, "ic": icon}
    return {"n": type_names.get(t, f"Building {t}") + (f" T{tier}" if tier else ""), "t": tier, "ic": None}


def list_expander(items, item_lists, alias):
    """Rend une fonction (genre, id, quantité) → [(clé, quantité)] qui ouvre les objets « Output ».

    Ex. Ferralith Ingot Output : 95 % un lingot, 5 % un lingot de qualité → on garde le lingot.
    """
    lists = {x["id"]: x for x in item_lists}
    list_of = {("Item", i["id"]): i.get("item_list_id", 0) for i in items}

    def expand(kind, item_id, qty, depth=0):
        lid = list_of.get((kind, item_id), 0)
        if lid and lid in lists and depth < 4:
            poss = lists[lid].get("possibilities") or []
            if poss:
                main = max(poss, key=lambda p: p.get("probability", 0))
                out = []
                for st in main.get("items", []):
                    out.extend(expand(st.get("item_type", "Item"), st["item_id"], st["quantity"] * qty, depth + 1))
                return out
        k = alias.get(key_of(kind, item_id))
        return [(k, qty)] if k else []

    return expand


def _plain_expander(alias):
    def expand(kind, item_id, qty, depth=0):
        k = alias.get(key_of(kind, item_id))
        return [(k, qty)] if k else []
    return expand


def _req(lst):
    return (lst or [{}])[0]


def parse_recipes(crafting, alias, skills, tools, names, type_names, expand=None):
    """Recettes de fabrication simplifiées, avec des clés canoniques. Les recettes sans ingrédient sont écartées."""
    expand = expand or _plain_expander(alias)
    out = {}
    for r in crafting:
        if r.get("name", "").startswith(EXCLUDED_PREFIXES):
            continue
        ins = {}
        for s in r.get("consumed_item_stacks", []):
            k = alias.get(key_of(s.get("item_type"), s["item_id"]))
            if k is None or s.get("quantity", 0) <= 0:
                ins = None
                break
            ins[k] = ins.get(k, 0) + s["quantity"]
        if not ins:
            continue
        products = []
        for c in r.get("crafted_item_stacks", []):
            if c.get("quantity", 0) > 0:
                products.extend(expand(c.get("item_type", "Item"), c["item_id"], c["quantity"]))
        lvl, tool = _req(r.get("level_requirements")), _req(r.get("tool_requirements"))
        for o, oq in products:
            rid = f"{r['id']}:{o}"
            if o in ins or rid in out:
                continue
            out[rid] = {
                "o": o,
                "oq": oq,
                "in": [[k, q] for k, q in ins.items()],
                "b": building_label(names, type_names, r.get("building_requirement")),
                "s": [skills.get(lvl.get("skill_id"), "?"), lvl.get("level", 0)] if lvl.get("skill_id", 1) not in (0, 1) else None,
                "tl": [tools.get(tool.get("tool_type"), "?"), tool.get("level", 0)] if tool.get("tool_type") else None,
                "a": r.get("actions_required", 0),
                "rn": r.get("name", ""),
            }
    return out


def parse_sources(extraction, resources, alias, skills, tools, expand=None):
    """Où récolter chaque matière première : clé → liste de sources (ressource, outil, métier, chance)."""
    expand = expand or _plain_expander(alias)
    res = {r["id"]: r for r in resources}
    sources = {}
    first_req = {}

    def add(k, r, lvl, tool, p, felled):
        entry = {
            "res": r["name"],
            "rt": r.get("tier", -1),
            "ic": normalize_icon(r.get("icon_asset_name")),
            "tl": [tools.get(tool.get("tool_type"), "?"), tool.get("level", 0)] if tool.get("tool_type") else None,
            "s": [skills.get(lvl.get("skill_id"), "?"), lvl.get("level", 0)] if lvl.get("skill_id", 1) not in (0, 1) else None,
            "p": round(p, 4),
            "rare": r.get("tag") in RARE_RESOURCE_TAGS,
            "fd": felled,            # True : obtenu quand la ressource est entièrement récoltée (arbre abattu)
        }
        lst = sources.setdefault(k, [])
        old = next((x for x in lst if x["res"] == entry["res"]), None)
        if old is None:
            lst.append(entry)
        elif entry["p"] > old["p"]:
            lst[lst.index(old)] = entry

    for e in extraction:
        r = res.get(e.get("resource_id"))
        if not r:
            continue
        lvl, tool = _req(e.get("level_requirements")), _req(e.get("tool_requirements"))
        first_req.setdefault(r["id"], (lvl, tool))
        for st in e.get("extracted_item_stacks", []):
            s = st.get("item_stack") or {}
            for k, _ in expand(s.get("item_type", "Item"), s.get("item_id"), s.get("quantity", 1)):
                add(k, r, lvl, tool, st.get("probability", 1.0), False)
    # Ce que donne la ressource une fois entièrement récoltée (ex. un arbre abattu donne un tronc).
    for r in resources:
        for y in r.get("on_destroy_yield") or []:
            lvl, tool = first_req.get(r["id"], ({}, {}))
            for k, _ in expand(y.get("item_type", "Item"), y.get("item_id"), y.get("quantity", 1)):
                add(k, r, lvl, tool, 1.0, True)
    for lst in sources.values():
        lst.sort(key=lambda x: (x["rare"], -x["p"], x["rt"], x["res"]))
    return sources


def gather_cost(lst):
    """Coût d'une matière récoltée : 1 ÷ meilleure chance, ×50 si la source est rare (ruines, épaves…)."""
    best = OTHER_SOURCE_COST
    for x in lst:
        if x["p"] > 0:
            best = min(best, (1.0 / x["p"]) * (RARE_FACTOR if x["rare"] else 1.0))
    return best


def best_recipes(recipes, raw, raw_cost=None):
    """Coût (matières premières par unité) et recette la plus facile de chaque objet fabricable.

    Calcul jusqu'à stabilité (Bellman-Ford) : coût d'une matière première = 1 (ou raw_cost) ; coût d'une
    recette = somme(quantité × coût de l'ingrédient) ÷ quantité produite. Une boucle ne fait jamais baisser le coût.
    """
    raw_cost = raw_cost or {}
    cost = {k: raw_cost.get(k, 1.0) for k in raw}
    best = {}
    by_out = {}
    for rid, r in recipes.items():
        # Une matière première peut aussi se fabriquer : la recette gagne si elle coûte moins cher.
        if r["o"] not in raw or cost[r["o"]] > 1.0:
            by_out.setdefault(r["o"], []).append(rid)

    def rank(rid, c):
        r = recipes[rid]
        return (round(c, 6), (r["b"] or {}).get("t", 0), len(r["in"]), rid)

    for _ in range(300):
        changed = False
        for o, rids in by_out.items():
            for rid in rids:
                r = recipes[rid]
                total = 0.0
                for k, q in r["in"]:
                    total += q * cost.get(k, INF)
                    if total == INF:
                        break
                if total == INF:
                    continue
                c = total / r["oq"]
                cur = best.get(o)
                if cur is None and o in cost and c >= cost[o] - EPS * max(1.0, c):
                    continue
                if cur == rid:
                    if c < cost[o] - EPS * max(1.0, c):
                        cost[o] = c
                        changed = True
                    continue
                if cur is None or c < cost[o] - EPS * max(1.0, c) or (
                    abs(c - cost[o]) <= EPS * max(1.0, c) and rank(rid, c) < rank(cur, cost[o])
                ):
                    cost[o], best[o] = c, rid
                    changed = True
        if not changed:
            break
    return cost, best


def other_origin(item):
    """Origine d'un objet sans recette ni récolte : « buy » (vendu par un voyageur), « hunt » (gibier), « other »."""
    if item["g"] == "Trade Good":
        return "buy"
    if item["k"] == "c" and item["g"] in ("Animal", "Monster"):
        return "hunt"
    return "other"


def recipe_cost(recipe, cost):
    return sum(q * cost.get(k, INF) for k, q in recipe["in"]) / recipe["oq"]


def build(tables, commit="", updated_at="", featured=()):
    """Assemble toutes les données du site. `tables` : nom de table → liste lue depuis le JSON."""
    canon, alias = canonical_items(tables["item_desc"], tables["cargo_desc"])
    skills = {s["id"]: s["name"] for s in tables["skill_desc"]}
    tools = {t["id"]: t["name"] for t in tables["tool_type_desc"]}
    names, type_names = building_names(tables.get("building_desc", []), tables["building_type_desc"])
    expand = list_expander(tables["item_desc"], tables.get("item_list_desc", []), alias)
    recipes = parse_recipes(tables["crafting_recipe_desc"], alias, skills, tools, names, type_names, expand)
    sources = parse_sources(tables["extraction_recipe_desc"], tables["resource_desc"], alias, skills, tools, expand)

    raw = set(sources)
    craftable = {r["o"] for r in recipes.values()}
    # Matière récoltée : coût = 1 ÷ meilleure chance d'obtention (un butin rare coûte cher, D-077).
    raw_cost = {k: gather_cost(lst) for k, lst in sources.items()}
    for k in canon:
        if k not in craftable and k not in sources:
            raw.add(k)
            raw_cost[k] = 1.0 if other_origin(canon[k]) in ("buy", "hunt") else OTHER_SOURCE_COST
    cost, best = best_recipes(recipes, raw, raw_cost)
    # Un objet dont aucune recette n'aboutit devient une « autre source ».
    for k in craftable:
        if k not in best and k not in raw:
            raw.add(k)
            cost[k] = OTHER_SOURCE_COST

    alts = {}
    for rid, r in recipes.items():
        if r["o"] in best and rid != best[r["o"]] and recipe_cost(r, cost) < INF:
            alts.setdefault(r["o"], []).append(rid)
    for lst in alts.values():
        lst.sort(key=lambda rid: (recipe_cost(recipes[rid], cost), rid))

    used = {}
    for r in recipes.values():
        for k, _ in r["in"]:
            used.setdefault(k, set()).add(r["o"])

    supplies = []
    for s in tables.get("building_repairs_desc", []):
        k = alias.get(key_of("Cargo", s["cargo_id"]))
        if k:
            supplies.append([k, s["repair_value"]])
    supplies.sort(key=lambda x: (canon[x[0]]["t"], canon[x[0]]["n"]))

    keep = set(best.values()) | {rid for lst in alts.values() for rid in lst}
    for k, item in canon.items():
        item["st"] = stack_size(item)
        item["c"] = round(cost[k], 4) if cost.get(k, INF) < INF else None   # matières premières par unité (classement)
        # Origine : « craft » (recette), « gather » (récolte), « buy », « hunt » ou « other » (butin, quête…).
        item["o"] = "craft" if k in best else ("gather" if k in sources else other_origin(item))
    return {
        "version": {"commit": commit, "updatedAt": updated_at},
        "items": canon,
        "recipes": {rid: recipes[rid] for rid in sorted(keep)},
        "best": best,
        "alts": alts,
        "sources": sources,
        "usedIn": {k: sorted(v, key=lambda o: (canon[o]["t"], canon[o]["n"])) for k, v in used.items() if k in canon},
        "supplies": supplies,
        "featured": [alias[f] for f in featured if f in alias],
    }


def find_key(items, name, kind=None):
    """Clé d'un objet par son nom exact (outil des tests et du script)."""
    for k, v in items.items():
        if v["n"] == name and (kind is None or v["k"] == kind):
            return k
    return None


def crafts_needed(need, per_craft):
    """Nombre de fabrications pour obtenir au moins `need` unités."""
    return math.ceil(need / per_craft - 1e-9) if need > 0 else 0
