# Archetype integration tests

Pytest harness that validates this archetype end to end: it renders the archetype
headlessly with a known answers file, checks the generated project (expected files
present, no unrendered `{{ placeholder }}` tokens left in paths or contents), and
then builds and tests the generated .NET solution.

## Prerequisites

- [archetect](https://archetect.github.io/) >= 3.0 on PATH (`brew install archetect`)
- [uv](https://docs.astral.sh/uv/) on PATH (`brew install uv`) - installs Python and
  test dependencies automatically on first run
- SSH access to GitHub (`git@github.com`) - the archetype composes prompt/CI libraries
  from `archetect-common` and `p6m-archetypes` git sources; archetect clones and caches
  them on first render
- .NET SDK for the build tier (optional - build tests skip with a notice when
  `dotnet` is not on PATH)

## Running

All commands run from this `tests/` directory:

```sh
uv run pytest                  # everything: render, static checks, dotnet build + test
uv run pytest -m "not build"   # fast tier only: render + static checks (no dotnet needed)
uv run pytest -m build         # build tier only
uv run pytest --offline        # don't hit the network; use archetect's cached libraries
uv run pytest -v -ra           # verbose, with skip/fail reasons
```

The first run clones the composed library repos and resolves Python dependencies, so
it is slower; subsequent runs use archetect's and uv's caches.

## CI

The same suite runs in GitHub Actions via
[.github/workflows/test.yaml](../.github/workflows/test.yaml) on every pull request
and on pushes to `dev`/`main`. The workflow installs archetect from its release
binaries and rewrites the libraries' `git@github.com:` sources to token-authenticated
HTTPS. On failure it uploads the rendered project as a build artifact.

## Inspecting rendered output

Each test session renders into a pytest temp directory, e.g.
`/tmp/pytest-of-<user>/pytest-<N>/render-default0/`. Pytest keeps the last 3 runs,
so after a failure you can open the generated project from the failing run directly.

## Adding a test case

Add an entry to [manifest.yaml](manifest.yaml) plus an answers file under
[answers/](answers/) - no test code changes needed. Each case declares the answers
file, the expected project directory name, files that must exist, and the build
steps to run inside the generated project. Prompt keys in answers files are
snake_case (`org_name`, `prefix_name`, ...); anything omitted falls back to the
prompt's default via `archetect render -D`.
