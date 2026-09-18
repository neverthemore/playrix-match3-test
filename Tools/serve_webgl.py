#!/usr/bin/env python3
"""Serve a Unity WebGL build with the compression headers Unity expects.

Запускает Unity WebGL-сборку с корректными заголовками сжатия.
"""

from __future__ import annotations

import argparse
import functools
import http.server
import pathlib
import urllib.parse


class UnityWebGLHandler(http.server.SimpleHTTPRequestHandler):
    """HTTP handler that exposes the MIME type beneath .gz/.br wrappers.

    EN: Python's stock server identifies `game.wasm.gz` as application/gzip,
    while Unity needs application/wasm plus Content-Encoding: gzip.
    RU: Стандартный сервер Python определяет `game.wasm.gz` как
    application/gzip, а Unity нужны application/wasm и Content-Encoding: gzip.
    """

    _UNITY_TYPES = {
        ".data": "application/octet-stream",
        ".js": "application/javascript",
        ".json": "application/json",
        ".symbols": "application/octet-stream",
        ".wasm": "application/wasm",
    }

    def guess_type(self, path: str) -> str:
        unwrapped = path
        if unwrapped.endswith(".gz"):
            unwrapped = unwrapped[:-3]
        elif unwrapped.endswith(".br"):
            unwrapped = unwrapped[:-3]

        suffix = pathlib.Path(unwrapped).suffix.lower()
        return self._UNITY_TYPES.get(suffix, super().guess_type(unwrapped))

    def end_headers(self) -> None:
        request_path = urllib.parse.urlparse(self.path).path
        if request_path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        elif request_path.endswith(".br"):
            self.send_header("Content-Encoding", "br")

        # EN: Avoid a stale browser cache while iterating on local builds.
        # RU: Не используем устаревший кэш браузера при локальной разработке.
        self.send_header("Cache-Control", "no-store")
        super().end_headers()


def main() -> None:
    parser = argparse.ArgumentParser(description="Serve a Unity WebGL build locally")
    parser.add_argument("--root", default="Builds/WebGL", help="WebGL build directory")
    parser.add_argument("--port", type=int, default=8000, help="Local TCP port")
    args = parser.parse_args()

    root = pathlib.Path(args.root).resolve()
    if not (root / "index.html").is_file():
        raise SystemExit(f"Unity WebGL index.html not found under: {root}")

    handler = functools.partial(UnityWebGLHandler, directory=str(root))
    server = http.server.ThreadingHTTPServer(("127.0.0.1", args.port), handler)
    print(f"Serving {root} at http://127.0.0.1:{args.port}", flush=True)

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()


if __name__ == "__main__":
    main()
