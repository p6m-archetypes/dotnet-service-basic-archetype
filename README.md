# dotnet-service-basic-archetype

Baseline `main`. All implementation lives on the long-running `dev` branch and lands
here via a reviewed pull request (SOC 2 change management).

## Testing

Integration tests live in [tests/](tests/) - they render the archetype headlessly,
verify the generated project (expected files, no leftover template placeholders),
and build and test the generated .NET solution.

```sh
cd tests
uv run pytest                  # full suite: render + static checks + dotnet build/test
uv run pytest -m "not build"   # fast render/static tier only (no .NET SDK required)
```

Requires `archetect` and `uv` on PATH; see [tests/README.md](tests/README.md) for
details, offline mode, and how to add test cases.