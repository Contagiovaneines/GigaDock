#!/usr/bin/env python3
"""Gera ZIP do código atual sem .git, arquivos ignorados ou histórico antigo."""
import json
import subprocess
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def main():
    subprocess.run([sys.executable, str(ROOT / "tools/audit-publication.py"), "--history"], cwd=ROOT, check=True)
    names = subprocess.check_output(["git", "ls-files", "--cached", "--others", "--exclude-standard", "-z"], cwd=ROOT).split(b"\0")
    candidates = sorted({n.decode("utf-8") for n in names if n})
    query = subprocess.run(["git", "check-ignore", "--no-index", "-z", "--stdin"], cwd=ROOT,
                           input=b"\0".join(n.encode() for n in candidates) + b"\0", stdout=subprocess.PIPE, check=False)
    if query.returncode not in (0, 1):
        raise RuntimeError("Falha ao conferir .gitignore.")
    ignored = {n.decode("utf-8") for n in query.stdout.split(b"\0") if n}
    selected = []
    for name in candidates:
        path = ROOT / name
        if name in ignored or not path.is_file():
            continue
        if path.is_symlink() or not path.resolve().is_relative_to(ROOT):
            raise RuntimeError("Exportação não aceita link simbólico ou arquivo fora do projeto.")
        if ".git" in Path(name).parts or any(part in {"bin", "obj", "local", "TestResults"} for part in Path(name).parts):
            raise RuntimeError("Arquivo de trabalho inesperado na exportação.")
        selected.append(name)
    version = json.loads((ROOT / "global.json").read_text(encoding="utf-8"))["sdk"]["version"]
    destination = ROOT / "dist/GigaDock-codigo-fonte.zip"
    destination.parent.mkdir(parents=True, exist_ok=True)
    temporary = destination.with_suffix(".zip.tmp")
    try:
        with zipfile.ZipFile(temporary, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
            for name in selected:
                archive.write(ROOT / name, "GigaDock/" + name)
        with zipfile.ZipFile(temporary) as archive:
            bad = archive.testzip()
            if bad:
                raise RuntimeError("Falha de integridade no ZIP.")
        temporary.replace(destination)
    finally:
        temporary.unlink(missing_ok=True)
    print(f"Exportação: dist/GigaDock-codigo-fonte.zip; {len(selected)} arquivos; SDK {version}; sem histórico Git.")


if __name__ == "__main__":
    main()
