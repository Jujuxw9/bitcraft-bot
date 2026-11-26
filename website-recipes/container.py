#!/usr/bin/env python3
"""Démarrage du site des recettes dans Docker (Unraid), sur l'image officielle python:3.13-slim.

Commande du conteneur (« Post Arguments » d'Unraid) :  python3 -B /app/container.py
Le dossier appdata (ex. /mnt/user/appdata/riverwood-recipes) est monté sur /app.

1. Donne le dossier build/ à l'utilisateur Unraid (99:100 = nobody:users) puis abandonne les droits root.
2. Fabrique les données au premier démarrage si elles manquent, puis vérifie chaque nuit (5 h, heure de Paris)
   si les données publiques du jeu ont changé ; si oui, les refait. En cas d'erreur, l'ancienne version reste.
3. Lance le serveur web (serve.py) sur le port 8790, pour tout le réseau local.
4. Après un déploiement (fichier repère .redemarrer écrit par tools/deploy.py), se relance avec le nouveau code,
   sans redémarrer le conteneur.
"""
import os
import subprocess
import sys
import threading
import time

APP = os.path.dirname(os.path.abspath(__file__))
BUILD = os.path.join(APP, "build")
MARKER = os.path.join(APP, ".redemarrer")
UPDATE_HOUR = 5
PORT = int(os.environ.get("RECIPES_PORT", "8790"))
TZ_PARIS = "CET-1CEST,M3.5.0,M10.5.0/3"   # règle de Paris sans fichier de fuseau


def log(message):
    print(time.strftime("%Y-%m-%d %H:%M:%S"), message, flush=True)


def paris_time():
    if hasattr(time, "tzset"):
        os.environ["TZ"] = "Europe/Paris" if os.path.isfile("/usr/share/zoneinfo/Europe/Paris") else TZ_PARIS
        time.tzset()


def drop_root():
    """Le serveur ne tourne jamais en root : build/ est donné à nobody:users, puis on change d'utilisateur."""
    if not hasattr(os, "getuid") or os.getuid() != 0:
        return
    uid, gid = int(os.environ.get("PUID", "99")), int(os.environ.get("PGID", "100"))
    os.makedirs(BUILD, exist_ok=True)
    try:
        os.chown(APP, uid, gid)   # le dossier du site : pour effacer le repère .redemarrer après un déploiement
    except OSError:
        pass
    for root, dirs, files in os.walk(BUILD):
        for name in [root] + [os.path.join(root, f) for f in files]:
            try:
                os.chown(name, uid, gid)
            except OSError:
                pass
    os.setgroups([])
    os.setgid(gid)
    os.setuid(uid)
    os.umask(0o002)
    log(f"Droits root abandonnés (utilisateur {uid}:{gid})")


def build_data():
    """Refait les données dans un processus à part (mémoire rendue à la fin). Rend True si tout s'est bien passé."""
    log("Mise à jour des données du jeu…")
    r = subprocess.run([sys.executable, "-B", os.path.join(APP, "tools", "build_data.py")],
                       cwd=APP, capture_output=True, text=True, timeout=3600)
    for line in (r.stdout + r.stderr).strip().splitlines()[-6:]:
        log("  " + line)
    log("Données à jour" if r.returncode == 0 else f"Échec de la mise à jour (code {r.returncode}) : ancienne version gardée")
    return r.returncode == 0


def updater():
    if not os.path.exists(os.path.join(BUILD, "data.json")):
        build_data()
    last_day = time.strftime("%Y-%m-%d")
    while True:
        time.sleep(300)
        now = time.localtime()
        day = time.strftime("%Y-%m-%d", now)
        if now.tm_hour == UPDATE_HOUR and day != last_day:
            last_day = day
            try:
                build_data()
            except Exception as e:   # jamais d'arrêt du site à cause de la mise à jour
                log(f"Erreur de mise à jour : {e}")


def watch_restart(server):
    while True:
        time.sleep(3)
        if os.path.exists(MARKER):
            log("Nouveau code déployé : relance")
            try:
                os.remove(MARKER)
            except OSError as e:
                log(f"Repère .redemarrer impossible à effacer : {e}")
            server.shutdown()
            return


def main():
    paris_time()
    drop_root()
    sys.path.insert(0, APP)
    import serve   # noqa: E402  (après le changement d'utilisateur)

    threading.Thread(target=updater, daemon=True).start()
    server = serve.make_server("0.0.0.0", PORT)
    threading.Thread(target=watch_restart, args=(server,), daemon=True).start()
    log(f"Site des recettes {serve.version()} : port {PORT}")
    server.serve_forever()
    server.server_close()
    os.execv(sys.executable, [sys.executable, "-B", os.path.abspath(__file__)])


if __name__ == "__main__":
    main()
