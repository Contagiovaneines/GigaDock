#!/usr/bin/env python3
"""Audita candidatos públicos e blobs do histórico sem imprimir valores sensíveis.

Executar da raiz: python tools/audit-publication.py --history
O relatório completo fica em docs/local/auditoria/publicacao.json (ignorado).
É uma busca por padrões; não garante a ausência de todos os tipos de segredo.
"""
import argparse
import json
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TEXT = {".cs", ".xaml", ".csproj", ".slnx", ".props", ".targets", ".json",
        ".yml", ".yaml", ".md", ".txt", ".ps1", ".bat", ".sh", ".py",
        ".html", ".css", ".js", ".xml", ".config", ".ini", ".toml"}
RULES = {
    "chave_privada": re.compile(r"-----BEGIN (?:RSA |EC |OPENSSH |DSA )?PRIVATE KEY-----"),
    "token_github": re.compile(r"\b(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,})\b"),
    "chave_api": re.compile(r"\b(?:sk-(?:proj-|svcacct-)?[A-Za-z0-9_-]{25,}|AKIA[A-Z0-9]{16}|AIza[A-Za-z0-9_-]{30,})\b"),
    "credencial_url": re.compile(r"https?://[^\s/@:]+:[^\s/@]+@", re.I),
    "caminho_usuario": re.compile(r"(?:[A-Z]:[\\/]+Users[\\/]+(?!Public\b|Default\b|<|\$)[A-Za-z0-9_.-]+|/mnt/[a-z]/Users/[A-Za-z0-9_.-]+|/home/(?!<|\$)[a-z0-9_.-]+)", re.I),
    "email": re.compile(r"[A-Za-z0-9_.+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}"),
}
PRIVATE_NAMES = {"settings.json", "notas.txt", ".env", "credentials.json", "id_rsa", "id_ed25519"}
PRIVATE_EXTENSIONS = {".pfx", ".p12", ".pem", ".key", ".keystore", ".jks", ".kdbx", ".db", ".sqlite", ".sqlite3"}
DEMO_URL = re.compile("https" + r"://(?:user:password|usuario:senha)@example\.com(?:/|[\"\s]|$)")
DEMO_FILES = {"src/GigaDock.App.Linux/SmokeScenario.cs",
              "tests/DockWindows.Tests/EstilosSistemaTests.cs",
              "tests/GigaDock.Tests.Core/SharedServicesTests.cs",
              "tests/GigaDock.Tests.Linux/LinuxFunctionalTests.cs"}


def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT)


def inspect(path, data):
    # Attribution vendorizada preservada; contatos públicos dos autores não são
    # dados privados do usuário. Não ignorar chaves/tokens nesses mesmos arquivos.
    attribution = path.startswith("src/DockWindows.App/Assets/PokemonWalk/")
    findings = []
    text = data.decode("utf-8", errors="replace")
    for line_no, line in enumerate(text.splitlines(), 1):
        for rule, pattern in RULES.items():
            if rule == "email" and attribution:
                continue
            if rule == "email" and ("example.com" in line or "example.org" in line):
                continue
            if rule == "credencial_url" and path in DEMO_FILES:
                matches = list(pattern.finditer(line))
                if matches and all(DEMO_URL.match(line, match.start()) for match in matches):
                    continue
            if pattern.search(line):
                findings.append({"file": path, "line": line_no, "category": rule})
    return findings


def candidate_paths():
    names = git("ls-files", "-z", "--cached", "--others", "--exclude-standard").split(b"\0")
    return sorted({n.decode("utf-8") for n in names if n})


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--history", action="store_true", help="Examinar blobs alcançáveis por todas as refs locais")
    args = parser.parse_args()
    candidates, ignored_tracked, private_files, findings = [], [], [], []
    tracked = {n.decode("utf-8") for n in git("ls-files", "-z").split(b"\0") if n}
    names = candidate_paths()
    ignored_query = subprocess.run(["git", "check-ignore", "--no-index", "-z", "--stdin"],
                                   input=b"\0".join(n.encode("utf-8") for n in names) + b"\0",
                                   cwd=ROOT, stdout=subprocess.PIPE, check=False)
    if ignored_query.returncode not in (0, 1):
        raise RuntimeError("Não foi possível determinar os arquivos ignorados pelo Git.")
    ignored_names = {n.decode("utf-8") for n in ignored_query.stdout.split(b"\0") if n}
    for name in names:
        path = ROOT / name
        if not path.is_file():
            continue
        # --no-index também detecta um arquivo já rastreado que passou a ser ignorado.
        if name in ignored_names:
            if name in tracked:
                ignored_tracked.append(name)
            continue
        candidates.append(name)
        if path.name.lower() in PRIVATE_NAMES or path.suffix.lower() in PRIVATE_EXTENSIONS or path.name.startswith(".env.") and path.name != ".env.example":
            private_files.append(name)
        if (path.suffix.lower() in TEXT or path.name in {"LICENSE", ".gitignore", ".gitattributes", ".editorconfig"}) and path.stat().st_size <= 2_000_000:
            findings.extend(inspect(name, path.read_bytes()))
    history = []
    checked = 0
    if args.history:
        objects = []
        for line in git("rev-list", "--objects", "--all").decode("utf-8").splitlines():
            object_id, _, name = line.partition(" ")
            if name and (Path(name).suffix.lower() in TEXT or Path(name).name == "LICENSE"):
                objects.append((object_id, name))
        # Cada blob é lido uma vez; conteúdo/valores encontrados nunca são impressos.
        metadata = subprocess.check_output(["git", "cat-file", "--batch-check"], cwd=ROOT,
                                           input="".join(oid + "\n" for oid, _ in objects).encode())
        selected = [(oid, name) for (oid, name), meta in zip(objects, metadata.decode().splitlines())
                    if meta.split()[1] == "blob" and int(meta.split()[2]) <= 2_000_000]
        batch = subprocess.check_output(["git", "cat-file", "--batch"], cwd=ROOT,
                                        input="".join(oid + "\n" for oid, _ in selected).encode())
        offset = 0
        for object_id, name in selected:
            end = batch.index(b"\n", offset)
            size = int(batch[offset:end].split()[2])
            data = batch[end + 1:end + 1 + size]
            offset = end + 2 + size
            checked += 1
            for finding in inspect(name, data):
                history.append(dict(finding, object=object_id))
    report = {"candidateFiles": len(candidates), "findings": findings,
              "privateFiles": private_files, "trackedButIgnored": ignored_tracked,
              "historicalBlobsChecked": checked, "historyFindings": history,
              "limitations": "Busca textual por padrões em arquivos até 2 MB; imagens, metadados Git de autoria, objetos inalcançáveis, LFS e remotes não são auditados."}
    destination = ROOT / "docs/local/auditoria/publicacao.json"
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(json.dumps({k: len(v) if isinstance(v, list) else v for k, v in report.items() if k != "limitations"}, ensure_ascii=False))
    print("Relatório local: docs/local/auditoria/publicacao.json; valores encontrados omitidos.")
    return 1 if findings or private_files or ignored_tracked else 0


if __name__ == "__main__":
    raise SystemExit(main())
