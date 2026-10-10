#!/usr/bin/env python3
"""Empacotamento e instalação por usuário. Somente biblioteca padrão Python 3."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import shlex
import subprocess
import sys
import tarfile
import tempfile
import xml.etree.ElementTree as ET

PRODUCT = "GigaDock-linux-x64"
MANIFEST = "manifest.json"
MARKER = ".gigadock-install.json"


def sha(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def absolute(value):
    path = Path(value)
    if not path.is_absolute() or any(ord(c) < 32 for c in str(path)):
        raise ValueError("Use um caminho absoluto sem caracteres de controle.")
    # Não atravessar diretórios simbólicos em operações de instalação/remoção.
    for part in [path, *path.parents]:
        if part.is_symlink():
            raise ValueError(f"O destino contém link simbólico: {part}")
    return path.resolve()


def target(value):
    path = absolute(value)
    home = Path.home().resolve()
    if path in (Path("/"), home, home.parent, Path("/usr"), Path("/opt"), Path("/tmp")):
        raise ValueError("Escolha uma pasta exclusiva para o GigaDock.")
    return path


def files(root):
    result = set()
    for current, dirs, names in os.walk(root, followlinks=False):
        for name in [*dirs, *names]:
            path = Path(current) / name
            if path.is_symlink():
                raise ValueError(f"Links simbólicos não são aceitos no pacote: {path}")
        for name in names:
            path = Path(current) / name
            if not path.is_file():
                raise ValueError(f"Arquivo especial no pacote: {path}")
            result.add(path.relative_to(root).as_posix())
    return result


def verify(root, installed=False):
    manifest = json.loads((root / MANIFEST).read_text(encoding="utf-8"))
    if manifest.get("product") != PRODUCT or manifest.get("schema") != 1:
        raise ValueError("Manifesto não pertence ao GigaDock Linux.")
    expected = manifest["files"]
    if not isinstance(expected, dict) or not expected:
        raise ValueError("Manifesto vazio ou inválido.")
    for name, digest in expected.items():
        relative = Path(name)
        if relative.is_absolute() or ".." in relative.parts or not isinstance(digest, str) or len(digest) != 64:
            raise ValueError("Caminho ou hash inválido no manifesto.")
        path = root / relative
        if not path.is_file() or path.is_symlink() or sha(path) != digest:
            raise ValueError(f"Arquivo ausente ou alterado: {name}")
    allowed = set(expected) | {MANIFEST}
    if installed:
        allowed.add(MARKER)
    if files(root) != allowed:
        raise ValueError("Há arquivos desconhecidos na pasta. Não serão removidos ou sobrescritos.")
    if "GigaDock" not in expected or "install-linux.sh" not in expected:
        raise ValueError("Pacote incompleto.")
    return manifest


def desktop_quote(value):
    value = value.replace("\\", "\\\\\\\\").replace('"', '\\\\"').replace("`", "\\\\`").replace("$", "\\\\$").replace("%", "%%")
    return '"' + value + '"'


def xdg(name, fallback):
    value = os.environ.get(name, "")
    return absolute(value) if value.startswith("/") else absolute(str(Path.home() / fallback))


def generated_paths():
    return absolute(str(Path.home() / ".local/bin/gigadock")), xdg("XDG_DATA_HOME", ".local/share") / "applications/gigadock.desktop"


def ownership(prefix):
    return "GigaDock-managed:" + hashlib.sha256(str(prefix).encode()).hexdigest()


def ensure_owned(path, stamp):
    absolute(str(path))
    if path.exists() and stamp not in path.read_text(encoding="utf-8"):
        raise ValueError(f"O arquivo existente não é desta instalação: {path}")


def write_atomic(path, text, mode=0o644):
    path.parent.mkdir(parents=True, exist_ok=True)
    fd, temporary = tempfile.mkstemp(prefix=".gigadock-", dir=path.parent)
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as stream:
            stream.write(text)
        os.chmod(temporary, mode)
        os.replace(temporary, path)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)


def build(args):
    workspace, publish, output = map(lambda value: absolute(value), [args.workspace, args.publish, args.output])
    version = ET.parse(workspace / "Directory.Build.props").findtext(".//Version")
    output.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="gigadock-package-") as temporary:
        root = Path(temporary) / PRODUCT
        shutil.copytree(publish, root)
        # Artefato distribuível sem símbolos de depuração; preserva todos os arquivos de runtime.
        for pdb in root.glob("*.pdb"):
            pdb.unlink()
        shutil.copy(workspace / "assets/gigadock.png", root / "gigadock.png")
        shutil.copy(workspace / "LICENSE", root / "LICENSE")
        widget_guide = workspace / "docs/WIDGETS-LINUX.md"
        if widget_guide.is_file():
            shutil.copy(widget_guide, root / "WIDGETS-LINUX.md")
        for name in ("install-linux.sh", "uninstall-linux.sh", "linux-package.py"):
            shutil.copy(workspace / "tools" / name, root / name)
            os.chmod(root / name, 0o755)
        notices = root / "licenses/PokemonWalk"
        source = workspace / "src/DockWindows.App/Assets/PokemonWalk"
        notices.mkdir(parents=True)
        for name in ("NOTICE.md", "SOURCE-README.md", "spritebot_credits.txt"):
            shutil.copy(source / name, notices / name)
        for credit in source.glob("*/credits.txt"):
            dest = notices / credit.parent.name
            dest.mkdir()
            shutil.copy(credit, dest / credit.name)
        (root / "DEPENDENCIAS.txt").write_text("Linux x64 com X11/XWayland, glibc e ICU compatíveis com .NET 10; libfontconfig1, libfreetype6, libx11-6, libice6 e libsm6. Para abertura: gio (libglib2.0-bin). Opcional: playerctl para MPRIS, pactl para áudio PulseAudio/PipeWire, flatpak para aplicativos Flatpak, swaymsg/hyprctl nas respectivas sessões. Sensores dependem de hwmon do hardware. Scripts locais: execução manual conforme WIDGETS-LINUX.md. Instalação: bash + Python 3. Sem Wayland nativo, layer-shell ou ARM64 homologados. Bibliotecas não são instaladas por estes scripts.\n", encoding="utf-8")
        (root / "THIRD-PARTY.txt").write_text(".NET runtime: Microsoft, MIT (avisos no runtime). Avalonia e Fluent: MIT; SkiaSharp: MIT; Skia: BSD. Ical.Net: MIT; NodaTime: Apache-2.0. Dependências completas em GigaDock.deps.json. Sprites PMDCollab: direitos originais e atribuições em licenses/PokemonWalk; não abrangidos pela licença do código. Fontes de avisos e limites em docs/LINUX-VALIDACAO.md no repositório.\n", encoding="utf-8")
        manifest = {"schema": 1, "product": PRODUCT, "version": version, "runtime": "linux-x64", "files": {name: sha(root / name) for name in sorted(files(root))}}
        (root / MANIFEST).write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
        verify(root)
        archive = output / f"GigaDock-{version}-linux-x64.tar.gz"
        with tarfile.open(archive, "w:gz") as tar:
            tar.add(root, arcname=PRODUCT)
        (output / (archive.name + ".sha256")).write_text(sha(archive) + "  " + archive.name + "\n")
        print(f"Pacote criado: {archive}\nSHA-256: {sha(archive)}")


def install(args):
    source, prefix = absolute(args.source), target(args.prefix)
    manifest = verify(source)
    if os.uname().machine not in ("x86_64", "amd64"):
        raise ValueError("Este pacote é linux-x64; não instale em ARM64.")
    if prefix.exists():
        if not (prefix / MARKER).is_file():
            raise ValueError("A pasta existente não é uma instalação gerenciada do GigaDock.")
        verify(prefix, installed=True)
    state = xdg("XDG_STATE_HOME", ".local/state") / "gigadock"
    state.mkdir(parents=True, exist_ok=True)
    # Compatível com flock usado por FileShare.None no frontend .NET em Linux.
    import fcntl
    with (state / "instance.lock").open("a") as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise ValueError("Feche o GigaDock antes de instalar ou atualizar.")
        launcher, desktop = generated_paths()
        stamp = ownership(prefix)
        for path in (launcher, desktop):
            ensure_owned(path, stamp)
        prefix.parent.mkdir(parents=True, exist_ok=True)
        staging = Path(tempfile.mkdtemp(prefix=".gigadock-new-", dir=prefix.parent))
        previous = prefix.parent / (".gigadock-previous-" + os.urandom(8).hex())
        originals = {path: path.read_bytes() if path.exists() else None for path in (launcher, desktop)}
        moved_old = False
        committed = False
        try:
            shutil.copytree(source, staging, dirs_exist_ok=True)
            verify(staging)
            record = {"product": PRODUCT, "prefix": str(prefix), "launcher": str(launcher), "desktop": str(desktop)}
            (staging / MARKER).write_text(json.dumps(record) + "\n")
            if prefix.exists():
                os.rename(prefix, previous)
                moved_old = True
            os.rename(staging, prefix)
            committed = True
            write_atomic(launcher, "#!/bin/sh\n# " + stamp + "\nexec " + shlex.quote(str(prefix / "GigaDock")) + ' "$@"\n', 0o755)
            write_atomic(desktop, "[Desktop Entry]\nType=Application\nName=GigaDock\nComment=Ambientes, aplicativos e widgets locais\nExec=" + desktop_quote(str(launcher)) + "\nIcon=" + str(prefix / "gigadock.png") + "\nTerminal=false\nCategories=Utility;\nStartupWMClass=GigaDock\nX-GigaDock-Managed=" + stamp + "\n")
        except Exception:
            if committed:
                shutil.rmtree(prefix)
            if moved_old:
                os.rename(previous, prefix)
            for path, data in originals.items():
                if data is None:
                    if path.exists():
                        path.unlink()
                else:
                    path.write_bytes(data)
            raise
        finally:
            if staging.exists():
                shutil.rmtree(staging)
        if moved_old:
            shutil.rmtree(previous)
    print(f"GigaDock {manifest['version']} instalado em {prefix}\nAbra pelo menu ou {launcher}. Dados XDG preservados; autostart não foi ativado.")


def uninstall(args):
    prefix = target(args.prefix)
    record = json.loads((prefix / MARKER).read_text())
    if record.get("product") != PRODUCT or record.get("prefix") != str(prefix):
        raise ValueError("Marcador de instalação inválido.")
    verify(prefix, installed=True)
    import fcntl
    state = xdg("XDG_STATE_HOME", ".local/state") / "gigadock"
    state.mkdir(parents=True, exist_ok=True)
    with (state / "instance.lock").open("a") as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise ValueError("Feche o GigaDock antes de desinstalar.")
        stamp = ownership(prefix)
        for name in ("launcher", "desktop"):
            path = absolute(record[name])
            ensure_owned(path, stamp)
        autostart = xdg("XDG_CONFIG_HOME", ".config") / "autostart/gigadock.desktop"
        if autostart.exists():
            text = autostart.read_text()
            if "X-GigaDock-Managed=true" in text and desktop_quote(str(prefix / "GigaDock")) in text:
                autostart.unlink()
        for name in ("launcher", "desktop"):
            path = Path(record[name])
            if path.exists():
                path.unlink()
        shutil.rmtree(prefix)
    print("GigaDock desinstalado. Configurações, notas e demais dados foram preservados.")


def main():
    parser = argparse.ArgumentParser(description="GigaDock — instalação Linux por usuário")
    actions = parser.add_subparsers(dest="action", required=True)
    builder = actions.add_parser("build")
    for field in ("workspace", "publish", "output"):
        builder.add_argument("--" + field, required=True)
    installer = actions.add_parser("install")
    installer.add_argument("--source", required=True)
    for action in (installer, actions.add_parser("uninstall")):
        action.add_argument("--prefix", default=str(Path.home() / ".local/opt/gigadock"))
    args = parser.parse_args()
    try:
        {"build": build, "install": install, "uninstall": uninstall}[args.action](args)
    except (OSError, ValueError, KeyError, json.JSONDecodeError) as error:
        print("Não foi possível concluir: " + str(error), file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
