"""Fabrique les données du site des recettes à partir des tables publiques du jeu.

    py -3 -B website-recipes/tools/build_data.py            télécharge la dernière version et reconstruit
    py -3 -B website-recipes/tools/build_data.py --offline  reconstruit depuis les tables déjà téléchargées

Source : dépôt public BitCraftToolBox/BitCraft_GameData (branche cereal/cs, dossier static/), comme le bot (D-033).
Aucune connexion au serveur du jeu. Images : reprises du dossier d'images du bot si présentes, sinon téléchargées
une fois depuis brico.app, sinon depuis bitjita (décision de Julien, 2026-11-26). Tout est écrit dans website-recipes/build/,
dossier non suivi par Git.
"""
import argparse
import json
import os
import shutil
import sys
import urllib.request

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import recipes_core as core  # noqa: E402

HERE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))     # dossier website-recipes
BUILD = os.path.join(HERE, "build")
RAW = os.path.join(BUILD, "raw")
ICONS = os.path.join(BUILD, "icons")
BOT_ICONS = os.path.join(os.path.dirname(HERE), "artifacts", "release", "donnees-jeu", "icones")
REPO = "BitCraftToolBox/BitCraft_GameData"
BRANCH = "cereal/cs"
# Sources d'images, dans l'ordre : brico.app (déjà utilisé par le bot), puis bitjita (plus complet, 2026-11-26).
CDNS = ["https://cdn.brico.app/sprites/", "https://bitjita.com/"]
UA = {"User-Agent": "website-recipes-donnees"}   # nom générique : rien qui identifie Julien
TABLES = ["item_desc", "cargo_desc", "crafting_recipe_desc", "extraction_recipe_desc", "resource_desc",
          "skill_desc", "tool_type_desc", "building_type_desc", "building_desc", "building_repairs_desc", "item_list_desc"]
# Objets mis en avant sur la page d'accueil (maquette, D-077) : noms exacts du jeu.
FEATURED = ["Emarium Axe", "Emarium Pickaxe", "Luminite Machete", "Sturdy Timber", "Sturdy Brick Slab",
            "Sturdy Cloth Tarp", "Emarium Frames", "Infused Experimental Compounds", "Sturdy Plank", "Emarium Ingot",
            "Sturdy Rope", "Sturdy Leather", "Sturdy Wood Trunk", "Simple Timber", "Rough Brick Slab", "Fine Timber",
            "Pyrelite Saw", "Emarium Hoe", "Sturdy Cloth", "Fine Plank"]


def read_json(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def get(url, timeout=60):
    with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=timeout) as r:
        return r.read()


def latest_commit():
    info = json.loads(get(f"https://api.github.com/repos/{REPO}/commits/{BRANCH.replace('/', '%2F')}"))
    return info["sha"], info["commit"]["committer"]["date"]


def download(commit, updated_at):
    folder = os.path.join(RAW, commit)
    os.makedirs(folder, exist_ok=True)
    for t in TABLES:
        path = os.path.join(folder, t + ".json")
        if not os.path.exists(path):
            print(f"  téléchargement de {t}")
            data = get(f"https://raw.githubusercontent.com/{REPO}/{commit}/static/{t}.json")
            json.loads(data)   # refuse un fichier abîmé
            with open(path + ".part", "wb") as f:
                f.write(data)
            os.replace(path + ".part", path)
    with open(os.path.join(RAW, "current.json"), "w", encoding="utf-8") as f:
        json.dump({"commit": commit, "updatedAt": updated_at}, f)
    return folder


def icon_candidates(path):
    name = path.rsplit("/", 1)[-1]
    seen = []
    for p in (path, "GeneratedIcons/Other/" + name, "GeneratedIcons/Items/" + name, "GeneratedIcons/Cargo/" + name,
              "Items/" + name, "GeneratedIcons/Other/Buildings/" + name):
        if p not in seen:
            seen.append(p)
    return seen


def fetch_icon(path, allow_download, missing):
    """Rend le chemin servi (« Items/Rope ») si l'image existe ou a pu être obtenue, sinon None."""
    for p in icon_candidates(path):
        dst = os.path.join(ICONS, p + ".webp")
        if os.path.exists(dst):
            return p
        src = os.path.join(BOT_ICONS, p + ".webp")
        if os.path.exists(src):
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            shutil.copyfile(src, dst)
            return p
    if not allow_download or path in missing:
        return None
    for cdn in CDNS:
        for p in icon_candidates(path):
            try:
                data = get(cdn + p + ".webp", timeout=20)
            except Exception:
                continue
            if not data.startswith(b"RIFF"):   # pas une image webp (page d'erreur)
                continue
            dst = os.path.join(ICONS, p + ".webp")
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            with open(dst, "wb") as f:
                f.write(data)
            return p
    missing.add(path)
    return None


def closure(data, start):
    """Toutes les clés nécessaires pour fabriquer les objets de départ (recette la plus facile et autres)."""
    seen, todo = set(), list(start)
    while todo:
        k = todo.pop()
        if k in seen:
            continue
        seen.add(k)
        for rid in [data["best"].get(k)] + data["alts"].get(k, [])[:3]:
            if rid:
                todo.extend(i for i, _ in data["recipes"][rid]["in"])
    return seen


def resolve_icons(data, download_for):
    missing_file = os.path.join(ICONS, "introuvables.txt")
    missing = set()
    if os.path.exists(missing_file):
        with open(missing_file, encoding="utf-8") as f:
            missing = set(f.read().split("\n"))
    found = 0
    targets = []
    for k, it in data["items"].items():
        targets.append((it, k in download_for))
    for r in data["recipes"].values():
        if r["b"]:
            targets.append((r["b"], True))
    for k, lst in data["sources"].items():
        for s in lst:
            targets.append((s, k in download_for))
    cache = {}
    for obj, allow in targets:
        paths = [p for p in [obj.get("ic")] + obj.pop("alt_ic", []) if p]
        obj["ic"] = None
        for p in paths:
            if (p, allow) not in cache:
                cache[(p, allow)] = fetch_icon(p, allow, missing)
            if cache[(p, allow)]:
                obj["ic"] = cache[(p, allow)]
                break
        found += obj["ic"] is not None
    os.makedirs(ICONS, exist_ok=True)
    with open(missing_file, "w", encoding="utf-8") as f:
        f.write("\n".join(sorted(m for m in missing if m)))
    return found, len(targets)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--offline", action="store_true", help="ne rien télécharger, reprendre les tables déjà là")
    args = ap.parse_args()
    if args.offline:
        cur = read_json(os.path.join(RAW, "current.json"))
        commit, updated_at = cur["commit"], cur["updatedAt"]
        folder = os.path.join(RAW, commit)
    else:
        commit, updated_at = latest_commit()
        print(f"Données du jeu : commit {commit[:10]} du {updated_at}")
        folder = download(commit, updated_at)
    tables = {t: read_json(os.path.join(folder, t + ".json")) for t in TABLES}
    data = core.build(tables, commit, updated_at, featured=[])
    data["featured"] = [k for k in (core.find_key(data["items"], n) for n in FEATURED) if k]
    need = closure(data, data["featured"]) | {k for k, _ in data["supplies"]}
    found, total = resolve_icons(data, set() if args.offline else need)
    out = os.path.join(BUILD, "data.json")
    with open(out + ".part", "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, separators=(",", ":"))
    os.replace(out + ".part", out)
    print(f"Objets : {len(data['items'])}, recettes gardées : {len(data['recipes'])}, "
          f"fabricables : {len(data['best'])}, matières récoltables : {len(data['sources'])}, "
          f"supplies : {len(data['supplies'])}, mis en avant : {len(data['featured'])}/{len(FEATURED)}")
    print(f"Images trouvées : {found}/{total} ; fichier : {os.path.getsize(out) // 1024} Ko")


if __name__ == "__main__":
    main()
