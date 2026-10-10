"""Testes de instalação reversíveis em HOME/XDG temporários. Executar em Linux."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location("package", Path(__file__).resolve().parents[1] / "tools/linux-package.py")
package = importlib.util.module_from_spec(spec)
spec.loader.exec_module(package)


class InstallationTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="gigadock-installer-tests-")
        self.root = Path(self.temporary.name)
        self.home = self.root / "home com espaços $ literal"
        self.home.mkdir()
        self.environment = patch.dict(os.environ, {"HOME": str(self.home), "XDG_CONFIG_HOME": str(self.home / "config"), "XDG_DATA_HOME": str(self.home / "data"), "XDG_STATE_HOME": str(self.home / "state")})
        self.environment.start()
        self.source = self.root / "source"
        self.source.mkdir()
        (self.source / "GigaDock").write_text('#!/bin/sh\nprintf "%s\\n" "$@"\n')
        os.chmod(self.source / "GigaDock", 0o755)
        (self.source / "install-linux.sh").write_text("placeholder")
        self.manifest()
        self.prefix = self.home / "opt/GigaDock com espaços"
        self.args = argparse.Namespace(source=str(self.source), prefix=str(self.prefix))

    def manifest(self):
        files = {name: package.sha(self.source / name) for name in package.files(self.source) if name != package.MANIFEST}
        (self.source / package.MANIFEST).write_text(json.dumps({"schema": 1, "product": package.PRODUCT, "version": "3.2.0", "files": files}))

    def tearDown(self):
        self.environment.stop()
        self.temporary.cleanup()

    def test_install_update_uninstall_preserves_user_data_and_argument_boundaries(self):
        notes = self.home / "data/gigadock/notes/personal.txt"
        notes.parent.mkdir(parents=True)
        notes.write_text("Minhas ideias")
        package.install(self.args)
        launcher, desktop = package.generated_paths()
        result = subprocess.run([str(launcher), "argumento com espaços", "$HOME; texto"], check=True, text=True, capture_output=True)
        self.assertEqual(result.stdout.splitlines(), ["argumento com espaços", "$HOME; texto"])
        self.assertIn('Name=GigaDock', desktop.read_text())
        package.install(self.args)
        self.assertEqual(notes.read_text(), "Minhas ideias")
        package.uninstall(self.args)
        self.assertFalse(self.prefix.exists())
        self.assertFalse(launcher.exists())
        self.assertFalse(desktop.exists())
        self.assertEqual(notes.read_text(), "Minhas ideias")

    def test_tampered_package_does_not_replace_existing_installation(self):
        package.install(self.args)
        original = (self.prefix / "GigaDock").read_bytes()
        (self.source / "GigaDock").write_text("alterado")
        with self.assertRaises(ValueError):
            package.install(self.args)
        self.assertEqual((self.prefix / "GigaDock").read_bytes(), original)

    def test_unmanaged_target_is_not_overwritten(self):
        self.prefix.mkdir(parents=True)
        document = self.prefix / "meu-documento.txt"
        document.write_text("preservar")
        with self.assertRaises(ValueError):
            package.install(self.args)
        self.assertEqual(document.read_text(), "preservar")

    def test_unknown_file_prevents_recursive_deletion(self):
        package.install(self.args)
        document = self.prefix / "meu-documento.txt"
        document.write_text("preservar")
        with self.assertRaises(ValueError):
            package.uninstall(self.args)
        self.assertTrue(document.exists())

    def test_symlink_destination_is_rejected(self):
        external = self.root / "outside"
        external.mkdir()
        self.prefix.parent.mkdir(parents=True)
        self.prefix.symlink_to(external, target_is_directory=True)
        with self.assertRaises(ValueError):
            package.install(self.args)
        self.assertEqual(list(external.iterdir()), [])

    def test_running_instance_blocks_installation(self):
        import fcntl
        state = self.home / "state/gigadock"
        state.mkdir(parents=True)
        with (state / "instance.lock").open("a") as lock:
            fcntl.flock(lock, fcntl.LOCK_EX)
            with self.assertRaises(ValueError):
                package.install(self.args)
        self.assertFalse(self.prefix.exists())

    def test_menu_failure_rolls_back_installation_and_preserves_old_launcher(self):
        package.install(self.args)
        original = (self.prefix / "GigaDock").read_bytes()
        launcher, desktop = package.generated_paths()
        old_launcher = launcher.read_bytes()
        write = package.write_atomic
        def fail_menu(path, text, mode=0o644):
            if path == desktop:
                raise OSError("falha simulada de gravação")
            write(path, text, mode)
        with patch.object(package, "write_atomic", side_effect=fail_menu):
            with self.assertRaises(OSError):
                package.install(self.args)
        self.assertEqual((self.prefix / "GigaDock").read_bytes(), original)
        self.assertEqual(launcher.read_bytes(), old_launcher)


if __name__ == "__main__":
    unittest.main()
