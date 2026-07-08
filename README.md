# dotnet-service-basic-archetype

Baseline `main`. All implementation lives on the long-running `dev` branch and lands
here via a reviewed pull request (SOC 2 change management).

## Testing

This archetype is tested with the shared
[archetype-test-harness](https://github.com/p6m-archetypes/archetype-test-harness):
it renders the archetype headlessly, verifies the generated project (expected files,
no leftover template placeholders, valid YAML), and builds and tests the generated
.NET solution. Test cases and answers live in [tests/](tests/); the test code lives
in the harness repo.

```sh
uvx --from git+https://github.com/p6m-archetypes/archetype-test-harness@main archetype-test
uvx --from ../archetype-test-harness archetype-test   # sibling checkout variant
archetype-test -m "not build"                          # fast tier only (no .NET SDK required)
```

Requires `archetect` and `uv` on PATH; see [tests/README.md](tests/README.md) for
details, offline mode, and how to add test cases.