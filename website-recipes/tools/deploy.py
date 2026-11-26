"""Déploie le site des recettes sur le serveur Unraid par le partage réseau. Outil de Claude, lancé depuis le PC.

    py -3 -B website-recipes/tools/deploy.py --essai     montre ce qui serait copié, sans rien écrire
    py -3 -B website-recipes/tools/deploy.py             copie, demande la relance, attend la confirmation
    py -3 -B website-recipes/tools/deploy.py --retour    remet la version sauvegardée la plus récente

Toujours avec l'accord de Julien (D-077, méthode DOGGO-ESPORT).
Adresses privées : dans website-recipes/deploy.local.json (ignoré par Git), jamais dans le dépôt public :
    {"target": "//<serveur>/appdata/riverwood-recipes", "url": "http://<serveur>:8790"}
Règles :
- seuls les fichiers de FILES sont écrits ; build/ (données du serveur) n'est jamais écrasé, sauf les images
  qui manquent et data.json s'il n'existe pas encore ;
- le code du site doit être commité (sinon --force) ;
- avant d'écraser, l'ancienne version des fichiers modifiés est sauvegardée dans <cible>/.versions/ (5 gardées) ;
- chaque fichier est écrit à côté puis renommé ; le repère .redemarrer est écrit en dernier ;
- le script attend que le serveur annonce la nouvelle version (/version.txt).
"""
import argparse
import datetime
import json
import os
import shutil
import subprocess
import sys
import time
import urllib.request

HERE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # dossier website-recipes
CONFIG = os.path.join(HERE, "deploy.local.json")
FILES = ["container.py", "serve.py", "VERSION", "public/index.html", "public/app.js", "public/calc.js",
         "public/style.css", "tools/build_data.py", "tools/recipes_core.py"]
BACKUPS = ".versions"
KEEP = 5
WAIT = 10 * 60


def read(path):
    with open(path, "rb") as f:
        return f.read()


def local_version():
    return read(os.path.join(HERE, "VERSION")).decode().strip()


def write_atomic(dst, data):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    tmp = dst + ".part"
    with open(tmp, "wb") as f:
        f.write(data)
    os.replace(tmp, dst)


def server_version(url):
    try:
        with urllib.request.urlopen(url.rstrip("/") + "/version.txt", timeout=5) as r:
            return r.read().decode().strip()
    except Exception:
        return None


def repo_clean():
    r = subprocess.run(["git", "status", "--porcelain", "--", "."], cwd=HERE, capture_output=True, text=True)
    return r.returncode == 0 and not [l for l in r.stdout.splitlines() if not l[3:].startswith("website-recipes/build")]


def changed_files(target):
    out = []
    for rel in FILES:
        src, dst = os.path.join(HERE, rel), os.path.join(target, rel)
        if not os.path.exists(dst) or read(dst) != read(src):
            out.append(rel)
    return out


def missing_icons(target):
    src_root = os.path.join(HERE, "build", "icons")
    dst_root = os.path.join(target, "build", "icons")
    out = []
    for root, _, files in os.walk(src_root):
        for f in files:
            if not f.endswith(".webp"):
                continue
            rel = os.path.relpath(os.path.join(root, f), src_root)
            if not os.path.exists(os.path.join(dst_root, rel)):
                out.append(rel)
    return out


def rollback(target):
    folder = os.path.join(target, BACKUPS)
    saves = sorted(os.listdir(folder)) if os.path.isdir(folder) else []
    if not saves:
        sys.exit("Aucune sauvegarde à remettre.")
    last = os.path.join(folder, saves[-1])
    for root, _, files in os.walk(last):
        for f in files:
            rel = os.path.relpath(os.path.join(root, f), last)
            write_atomic(os.path.join(target, rel), read(os.path.join(root, f)))
            print(f"  remis : {rel}")
    write_atomic(os.path.join(target, ".redemarrer"), b"retour\n")
    print(f"Version {saves[-1]} remise ; relance demandée.")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--essai", action="store_true")
    ap.add_argument("--retour", action="store_true")
    ap.add_argument("--force", action="store_true")
    ap.add_argument("--sans-attente", action="store_true")
    args = ap.parse_args()
    if not os.path.exists(CONFIG):
        sys.exit(f"Fichier {CONFIG} absent (adresses privées du serveur).")
    with open(CONFIG, encoding="utf-8") as f:
        cfg = json.load(f)
    target, url = cfg["target"], cfg["url"]
    if not os.path.isdir(os.path.dirname(target.rstrip("/\\"))):
        sys.exit("Partage du serveur injoignable : le serveur est-il allumé ?")
    if args.retour:
        return rollback(target)

    version = local_version()
    files = changed_files(target)
    icons = missing_icons(target)
    data_missing = not os.path.exists(os.path.join(target, "build", "data.json"))
    print(f"Version {version} → {target}")
    print(f"  fichiers du site à copier : {len(files)} {files}")
    print(f"  images à ajouter : {len(icons)}")
    print(f"  données (data.json) : {'copiées (absentes sur le serveur)' if data_missing else 'gardées (le serveur les met à jour seul)'}")
    if args.essai:
        return
    if not args.force and not repo_clean():
        sys.exit("Le code du site n'est pas commité : commiter d'abord (ou --force).")

    stamp = f"{version}_{datetime.datetime.now():%Y%m%d-%H%M%S}"
    backup = os.path.join(target, BACKUPS, stamp)
    for rel in files:
        old = os.path.join(target, rel)
        if os.path.exists(old):
            write_atomic(os.path.join(backup, rel), read(old))
    saves = sorted(os.listdir(os.path.join(target, BACKUPS))) if os.path.isdir(os.path.join(target, BACKUPS)) else []
    for old in saves[:-KEEP]:
        shutil.rmtree(os.path.join(target, BACKUPS, old), ignore_errors=True)

    for rel in files:
        write_atomic(os.path.join(target, rel), read(os.path.join(HERE, rel)))
        print(f"  copié : {rel}")
    for i, rel in enumerate(icons, 1):
        write_atomic(os.path.join(target, "build", "icons", rel), read(os.path.join(HERE, "build", "icons", rel)))
        if i % 200 == 0:
            print(f"  images : {i}/{len(icons)}")
    if data_missing and os.path.exists(os.path.join(HERE, "build", "data.json")):
        write_atomic(os.path.join(target, "build", "data.json"), read(os.path.join(HERE, "build", "data.json")))
        print("  copié : build/data.json")

    before = server_version(url)
    write_atomic(os.path.join(target, ".redemarrer"), version.encode() + b"\n")
    if args.sans_attente:
        print("Relance demandée (sans attente).")
        return
    if before is None:
        print("Le serveur ne répond pas encore (conteneur pas créé ou arrêté) : les fichiers sont en place.")
        return
    print("Relance demandée ; attente de la confirmation du serveur…")
    end = time.time() + WAIT
    while time.time() < end:
        time.sleep(3)
        if server_version(url) == version and not os.path.exists(os.path.join(target, ".redemarrer")):
            print(f"OK : le serveur annonce la version {version}.")
            return
    sys.exit("Pas de confirmation après 10 min : regarder les journaux du conteneur (Unraid → Docker → Logs).")


if __name__ == "__main__":
    main()
