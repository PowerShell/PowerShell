### Copilot / AI Code Assistant Instructions for PowerShell

Purpose: concise, actionable guidance for an AI coding agent to be productive in this repository.

1) Big picture
- This repo builds the PowerShell product: multiple managed projects under `src/` (host, engine, cmdlets, modules) plus native components under `powershell-native` / `powershell-win-core`.
- Major boundaries: `src/powershell` is the host CLI, `System.Management.Automation` contains engine types, `Modules/` holds shipped modules, and `powershell-native` contains native host/platform code. Changes touching public APIs often require cross-project edits and test updates.

2) Build / bootstrap (explicit commands)
- Bootstrapping and building uses the repo `build.psm1` helper. Examples (run from repo root in PowerShell):
  - `Import-Module .\build.psm1; Start-PSBootstrap`
  - `Import-Module .\build.psm1; Start-PSBuild -Output (Join-Path $PWD debug)`
  - Clean build: `Import-Module .\build.psm1; Start-PSBuild -Clean -Output (Join-Path $PWD debug)`
- The repo also provides VS Code/Task definitions that call the same module (see `.vscode` tasks and repository-level tasks).

3) Tests & test layout
- Tests live under `test/` (many flavors: unit, integration, perf). Use the same `Start-PSBuild` bootstrap to produce test binaries, and consult `test/README.md` and `docs/testing-guidelines` for CI specifics.

4) Project conventions & patterns to follow
- C# style, analyzer rules and stylecop are enforced (`Settings.StyleCop`, `stylecop.json`). Match existing patterns (no new analyzer rule breaks).
- Global SDK pinned in `global.json` — use the dotnet sdk that matches it when building locally.
- Native vs managed separation: prefer changes that keep platform-specific code in `powershell-native` and managed code in `src/` unless a cross-cutting fix is required.

5) Integration points & external dependencies
- Native interop and COM: see `src/System.Management.Automation/engine/ComInterop/README.md` for areas requiring special care.
- Packaging and installers use `assets/` and `wix/` — changes here affect downstream packaging pipelines.

6) Codegen / PR guidance for AI edits
- Small, scoped changes are preferred. Large refactors must include:
  - Updated unit tests under `test/`.
  - Build script verification via the `Start-PSBuild` command.
  - No changes to signing or shipping metadata without maintainers' approval.
- When editing project files, update `PowerShell.sln` or run `Start-PSBootstrap` to refresh dependencies.

7) Files to inspect for context (examples)
- Repo root README: [README.md](README.md)
- Build helpers: [build.psm1](build.psm1)
- Solution: [PowerShell.sln](PowerShell.sln)
- Style & analyzers: [Settings.StyleCop](Settings.StyleCop), [stylecop.json](stylecop.json)
- Engine notes: [src/System.Management.Automation/engine/ComInterop/README.md](src/System.Management.Automation/engine/ComInterop/README.md)
- Testing guidance: [test/README.md](test/README.md) and [docs/testing-guidelines/testing-guidelines.md](docs/testing-guidelines/testing-guidelines.md)

8) What AI should not do autonomously
- Do not change release packaging, signing, or installer metadata. Flag these for human review.
- Do not remove or broadly relax analyzer or StyleCop rules; suggest changes and open PRs for discussion.

9) Ask for maintainers' help when:
- Changes touch native host code (`powershell-native`) or packaging (`assets/`, `wix/`).
- You need access to CI secrets or to trigger official release pipelines.

If anything here is unclear or you'd like additional examples (build output paths, common test commands, or specific subproject notes), say which area to expand and I'll iterate.
