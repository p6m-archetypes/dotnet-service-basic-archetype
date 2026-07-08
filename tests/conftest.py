"""Shared fixtures: load manifest.yaml, render each case once per session."""

import shutil
import subprocess
from dataclasses import dataclass, field
from pathlib import Path

import pytest
import yaml

TESTS_DIR = Path(__file__).parent
ARCHETYPE_ROOT = TESTS_DIR.parent
RENDER_TIMEOUT = 300  # seconds; includes cloning library sources on a cold cache


@dataclass
class Case:
    name: str
    answers: Path
    project_dir: str
    expected_files: list[str] = field(default_factory=list)
    requires: list[str] = field(default_factory=list)
    build_steps: list[list[str]] = field(default_factory=list)
    env: dict[str, str] = field(default_factory=dict)
    yaml_globs: list[str] = field(default_factory=list)


def load_cases() -> list[Case]:
    manifest = yaml.safe_load((TESTS_DIR / "manifest.yaml").read_text())
    return [
        Case(
            name=raw["name"],
            answers=TESTS_DIR / raw["answers"],
            project_dir=raw["project_dir"],
            expected_files=raw.get("expected_files", []),
            requires=raw.get("requires", []),
            build_steps=raw.get("build_steps", []),
            env={k: str(v) for k, v in raw.get("env", {}).items()},
            yaml_globs=raw.get("yaml_globs", []),
        )
        for raw in manifest["cases"]
    ]


def pytest_addoption(parser):
    parser.addoption(
        "--offline",
        action="store_true",
        help="pass --offline to archetect (use only already-cached library sources)",
    )


@pytest.fixture(scope="session", params=load_cases(), ids=lambda c: c.name)
def case(request) -> Case:
    return request.param


@pytest.fixture(scope="session")
def rendered_project(case: Case, tmp_path_factory, request) -> Path:
    """Render the archetype headlessly for this case; returns the generated project dir."""
    if shutil.which("archetect") is None:
        pytest.fail(
            "archetect not found on PATH. Install it first: https://archetect.github.io/ "
            "(brew install archetect-cli or download a release binary)."
        )

    out_dir = tmp_path_factory.mktemp(f"render-{case.name}")
    cmd = [
        "archetect", "render", str(ARCHETYPE_ROOT),
        "--dest", str(out_dir),
        "-A", str(case.answers),
        "-D",  # use prompt defaults for anything the answers file doesn't cover
        "--headless",
    ]
    if request.config.getoption("--offline"):
        cmd.append("--offline")

    result = subprocess.run(cmd, capture_output=True, text=True, timeout=RENDER_TIMEOUT)
    if result.returncode != 0:
        pytest.fail(
            f"archetect render failed (exit {result.returncode})\n"
            f"command: {' '.join(cmd)}\n"
            f"--- stdout ---\n{result.stdout}\n--- stderr ---\n{result.stderr}"
        )

    project = out_dir / case.project_dir
    if not project.is_dir():
        rendered = [p.name for p in out_dir.iterdir()]
        pytest.fail(
            f"render succeeded but expected project dir {case.project_dir!r} is missing; "
            f"rendered top-level entries: {rendered}"
        )
    return project
