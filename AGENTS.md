# AGENTS.md — OdooJsonRpcClient (.NET Standard library)

Project truth only. Fleet conventions for this stack: `~/Nextcloud/ClaudeObsidian/Agents/fleet/DOTNET.md` (read it when you touch build or test plumbing) — but see invariant 1: this is a vendored upstream fork, not a `Dave.*` service, so the fleet code style does **not** get retrofitted onto existing files. Global rules: `~/.claude/CLAUDE.md`. If `.swarm/ROSTER.md` exists you are in a crew; your seat file tells you how to work.

## What this is

A vendored fork of the MIT-licensed OSS library `PortaCapena.OdooJsonRpcClient` (author Patryk Bujalla), re-packaged for the fleet as the NuGet **`Dave.OdooJsonRpcClient`**. It is the low-level Odoo JSON-RPC transport that sits *beneath* `dave.odoo`: typed client, repository, query builder, domain-filter DSL and model mapping. Pure library — no `Program.cs`, no host, no REST/Blazor/Temporal/MCP surface anywhere. Origin is GitHub (`bsandmann/OdooJsonRpcClient`), not the internal GitLab.

## Layout

- `PortaCapena.OdooJsonRpcClient/` — the library (netstandard2.0): `OdooClient.cs`, `OdooRepository`, `OdooQueryBuilder<T>`, `OdooResult<T>`/`OdooError`, `OdooDictionaryModel`, `Attributes/`, `Consts/`, `Converters/`, `Extensions/`, `Models/`, `Request/`, `Result/`, `Utils/`.
- `PortaCapena.OdooJsonRpcClient.Shared/` — netstandard2.1, shared create-models.
- `PortaCapena.OdooJsonRpcClient.Tests/` — xUnit (net6.0): `OdooConfigTests`, `OdooContextTests`, `OdooDictionaryModelTests`, `OdooModelMapperTests`, `OdooQueryOfTTests`, `OdooRequestModelTests`.
- `PortaCapena.OdooJsonRpcClient.Example/` — sample console usage; its models are version-specific, do not copy them.
- `nupkg/`, `nupkg2/` — checked-in artifacts: `PortaCapena.OdooJsonRpcClient.1.0.0.nupkg` and `Dave.OdooJsonRpcClient.1.0.0.nupkg` (the one the fleet consumes).
- `.github/workflows/` — upstream GitHub Actions: `pr_build.yml`, `release.yml`, `codeql-analysis.yml`, `code-coverage-badge.yml`.

## Build, test, gate

**The gate — run before every commit:**

```bash
cd /home/bjoern/work/OdooJsonRpcClient
dotnet build PortaCapena.OdooJsonRpcClient.sln --configuration Release
dotnet test PortaCapena.OdooJsonRpcClient.sln --configuration Release
```

`GeneratePackageOnBuild=true`, so a Release build of the main project already produces a nupkg. The `Dave.OdooJsonRpcClient` package is built **out of band** and checked into `nupkg2/`; no CI here produces it.

## Runtime and infrastructure

| Thing | Value |
|---|---|
| Git remote | `https://github.com/bsandmann/OdooJsonRpcClient.git`, branch `master` |
| Upstream | `Intechnity-com/OdooJsonRpcClient` / `patricoos/PortaCapena.OdooJsonRpcClient`, MIT |
| Target frameworks | library `netstandard2.0`, Shared `netstandard2.1`, tests `net6.0` (GitHub Actions uses `dotnet-version: 6.0.x`) |
| In-repo version | `<Version>1.0.20</Version>` (upstream package id); the fleet package is `Dave.OdooJsonRpcClient` 1.0.0 |
| Dependencies | `Newtonsoft.Json` 13.0.4 — the sole `PackageReference`. Tests: xunit 2.4.2, FluentAssertions 6.8.0, coverlet.collector 3.2.0, Microsoft.NET.Test.Sdk 16.8.3. |
| Fleet feed | consumed from the internal GitLab NuGet feed, **project 9**, via the `Dave.*` `packageSourceMapping` pattern |
| Odoo endpoint | Not configured here. `OdooConfig(apiUrl, dbName, userName, password)` is built by the caller; `ApiUrlJson => ApiUrl + "/jsonrpc"`, services `common` (version/login) and `object` (execute/execute_kw). The concrete address, db `test_db` and credentials come from `dave.odoo`'s `OdooClientFactory` (Vault `kv/dave-odoo/shared`). |

## Invariants and rules

1. **This is upstream code.** Keep the existing style (Newtonsoft attributes, public setters, no FluentResults, no MediatR). Do not retrofit `DOTNET.md` conventions onto these files — that would make future upstream merges impossible. New fleet-only code belongs in `dave.odoo`, not here.
2. **Package id / namespace split, load-bearing for the fleet join:** consumers reference the package id `Dave.OdooJsonRpcClient` but write `using PortaCapena.OdooJsonRpcClient`. The shipped assembly is `lib/netstandard2.0/PortaCapena.OdooJsonRpcClient.dll`. Renaming either half breaks restore or compilation in `dave.odoo`.
3. The committed `release.yml` still packs and pushes the **upstream** id `PortaCapena.OdooJsonRpcClient` to nuget.org. Never run it for a fleet change.
4. Public contract invariants — operation strings, operator vocabulary, the JSON-RPC envelope, date formats, the `AccessDenied` retry, and the `OdooResult` success/error shape — are what `dave.odoo` binds to. Changing one is a co-deploy.
5. Consumers: `dave.odoo` is the only direct `PackageReference` (`Dave.Odoo/Dave.Odoo.csproj`, restored via `dave.odoo/nuget.config`). `dave.fibu` (`OdooQueryHelper.cs`) and `dave.customer` (`OdooStateCodeResolver.cs`) use the namespace **transitively** through `Dave.Odoo` — compile-time leaks, so a namespace change breaks them too.
6. Login is implicit: `OdooClient` and `OdooRepository` log in in the background; `LoginAsync()` is only needed for a connectivity check.
7. Odoo models are per-installation. Generate them with `OdooClient.GetModelAsync(table)` + `OdooModelMapper.GetDotNetModel(...)`; never reuse the Example project's models.

## Where to look

- `README.md` — upstream usage guide, query/filter examples, the raw JSON-RPC request/result shapes.
- `docs/` (present on the working tree, **untracked**) — fleet documentation substrate: `catalog.yaml`, `rules/odoo-jsonrpc-contract.rules.yaml`, `arc42/`. Decide with Björn before committing it.
- Consumer side: `dave.odoo` (`OdooClientFactory`), auto-memory `project_dave_odoo_vault_config.md`, `project_dave_odoo_multiple_delivery_addresses.md`, `feedback_dave_odoo_float_breaks_ci_nu1605.md`.
