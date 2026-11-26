"""Serveur web du site des recettes (maquette : sur le PC ; plus tard : dans le conteneur Unraid).

    py -3 -B website-recipes/serve.py                 http://127.0.0.1:8790
    options : --host 0.0.0.0 --port 8790

Sert la page (public/), le fichier de données et les images (build/, fabriqués par tools/build_data.py).
Bibliothèque standard seulement. En-têtes de sécurité et pas d'indexation par les moteurs de recherche.
"""
import argparse
import gzip
import mimetypes
import os
import posixpath
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import unquote, urlsplit

HERE = os.path.dirname(os.path.abspath(__file__))
_GZ_CACHE = {}   # fichier → (date de modification, contenu compressé) : on ne recompresse pas à chaque visite
PUBLIC = os.path.join(HERE, "public")
BUILD = os.path.join(HERE, "build")
TYPES = {".js": "text/javascript; charset=utf-8", ".css": "text/css; charset=utf-8", ".html": "text/html; charset=utf-8",
         ".json": "application/json; charset=utf-8", ".webp": "image/webp", ".png": "image/png", ".txt": "text/plain; charset=utf-8"}
HEADERS = {
    "X-Content-Type-Options": "nosniff",
    "X-Frame-Options": "DENY",
    "Referrer-Policy": "no-referrer",
    "X-Robots-Tag": "noindex, nofollow",
    "Content-Security-Policy": "default-src 'self'; img-src 'self' data:; style-src 'self'; script-src 'self'; "
                               "connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'",
    "Permissions-Policy": "camera=(), microphone=(), geolocation=()",
}


def version():
    try:
        with open(os.path.join(HERE, "VERSION"), encoding="utf-8") as f:
            return f.read().strip()
    except OSError:
        return "?"


def resolve(url_path):
    """Chemin demandé → fichier sur le disque, ou None. Refuse toute sortie des dossiers servis."""
    path = posixpath.normpath(unquote(urlsplit(url_path).path))
    if path in ("/", "/index.html", "."):
        return os.path.join(PUBLIC, "index.html")
    if path == "/robots.txt":
        return "robots"
    if path == "/version.txt":
        return "version"
    rel = path.lstrip("/")
    if rel == "data.json":
        base, rel = BUILD, "data.json"
    elif rel.startswith("icons/") and rel.endswith(".webp"):
        base = BUILD
    elif "/" not in rel and os.path.splitext(rel)[1] in (".js", ".css", ".png", ".html"):
        base = PUBLIC
    else:
        return None
    full = os.path.realpath(os.path.join(base, *rel.split("/")))
    if not full.startswith(os.path.realpath(base) + os.sep) or not os.path.isfile(full):
        return None
    return full


class Handler(BaseHTTPRequestHandler):
    server_version = "RiverwoodRecipes"
    sys_version = ""

    def log_message(self, fmt, *args):   # journal court, sans adresse de visiteur
        pass

    def send(self, code, body, ctype, cache="no-cache", source=None):
        gz = "gzip" in (self.headers.get("Accept-Encoding") or "") and ctype.split(";")[0] in (
            "application/json", "text/javascript", "text/css", "text/html") and len(body) > 1024
        if gz:
            stamp = os.path.getmtime(source) if source else None
            cached = _GZ_CACHE.get(source)
            if source and cached and cached[0] == stamp:
                body = cached[1]
            else:
                body = gzip.compress(body, 6)
                if source:
                    _GZ_CACHE[source] = (stamp, body)
        self.send_response(code)
        self.send_header("Content-Type", ctype)
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Cache-Control", cache)
        if gz:
            self.send_header("Content-Encoding", "gzip")
            self.send_header("Vary", "Accept-Encoding")
        for k, v in HEADERS.items():
            self.send_header(k, v)
        self.end_headers()
        if self.command != "HEAD":
            self.wfile.write(body)

    def do_GET(self):
        f = resolve(self.path)
        if f == "robots":
            return self.send(200, b"User-agent: *\nDisallow: /\n", TYPES[".txt"])
        if f == "version":
            return self.send(200, version().encode(), TYPES[".txt"])
        if not f:
            return self.send(404, b"Not found", TYPES[".txt"])
        ext = os.path.splitext(f)[1]
        with open(f, "rb") as fh:
            body = fh.read()
        cache = "public, max-age=604800" if ext == ".webp" else "no-cache"
        self.send(200, body, TYPES.get(ext) or mimetypes.guess_type(f)[0] or "application/octet-stream", cache, f)

    do_HEAD = do_GET


class Server(ThreadingHTTPServer):
    daemon_threads = True
    request_queue_size = 64


def make_server(host, port):
    return Server((host, port), Handler)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--host", default="127.0.0.1")
    ap.add_argument("--port", type=int, default=8790)
    args = ap.parse_args()
    if not os.path.exists(os.path.join(BUILD, "data.json")):
        print("Données absentes : lancer d'abord tools/build_data.py")
    srv = make_server(args.host, args.port)
    print(f"Site des recettes : http://{args.host}:{args.port}")
    srv.serve_forever()


if __name__ == "__main__":
    main()
