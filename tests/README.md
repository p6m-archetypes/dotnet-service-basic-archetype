# Archetype integration tests

This archetype is tested by the shared
[archetype-test-harness](https://github.com/p6m-archetypes/archetype-test-harness):
it renders the archetype headlessly with the answers in [answers/](answers/), checks
the generated project (expected files present, no unrendered `{{ placeholder }}`
tokens, `.platform`/workflow YAML parses), and builds and tests the generated .NET
solution. This directory holds only data - the test code lives in the harness repo.

## Prerequisites

- [archetect](https://archetect.github.io/) >= 3.0 on PATH (`brew install archetect`)
- [uv](https://docs.astral.sh/uv/) on PATH (`brew install uv`)
- SSH access to GitHub - the archetype composes prompt/CI libraries from
  `archetect-common` and `p6m-archetypes` git sources; archetect caches them after
  the first render
- .NET SDK for the build tier (optional - build tests skip with a notice when
  `dotnet` is not on PATH)

## Running

From the repo root (or this directory):

```sh
# in the flat org checkout, against the sibling harness:
uvx --from ../archetype-test-harness archetype-test

# anywhere, against the published harness:
uvx --from git+https://github.com/p6m-archetypes/archetype-test-harness@dev archetype-test
```

Extra arguments pass through to pytest:

```sh
archetype-test -m "not build"   # fast tier only: render + static checks, no dotnet needed
archetype-test -m build         # build tier only
archetype-test --offline        # use archetect's cached libraries, no network
archetype-test -v -ra           # verbose, with skip/fail reasons
```

## CI

[.github/workflows/test.yaml](../.github/workflows/test.yaml) calls the harness's
reusable workflow on every pull request, push, and manual dispatch. On failure it
uploads the rendered project as a build artifact.

## Adding a test case

Add an entry to [manifest.yaml](manifest.yaml) plus an answers file under
[answers/](answers/) - the schema is documented in the
[harness README](https://github.com/p6m-archetypes/archetype-test-harness#manifestyaml-schema).
Prompt keys in answers files are snake_case (`org_name`, `prefix_name`, ...);
anything omitted falls back to the prompt's default.

## Inspecting rendered output

Each run renders into a pytest temp directory, e.g.
`/tmp/pytest-of-<user>/pytest-<N>/render-default0/`. Pytest keeps the last 3 runs,
so after a failure you can open the generated project from the failing run directly.
