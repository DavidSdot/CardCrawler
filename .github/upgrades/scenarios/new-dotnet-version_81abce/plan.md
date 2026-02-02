# .NET Version Upgrade Plan

- Table of Contents
  - Executive Summary
  - Migration Strategy
  - Detailed Dependency Analysis
  - Project-by-Project Plans
  - Package Update Reference
  - Breaking Changes Catalog
  - Testing & Validation Strategy
  - Risk Management
  - Complexity & Effort Assessment
  - Source Control Strategy
  - Success Criteria

---

## Executive Summary

- Selected Strategy: **All-At-Once Strategy** — All projects upgraded simultaneously in a single atomic operation.
- Rationale: Small solution (3 projects), homogeneous SDK-style projects, all projects already target `net10.0-windows7.0`, no package compatibility issues or security vulnerabilities reported.
- Scope: `CardCrawler.sln` — projects:
  - `CardCrawler\CardCrawler.csproj` (DotNetCoreApp) — current target `net10.0-windows7.0`
  - `CardCrawler.CardMarket\CardCrawler.Cardmarket.csproj` (ClassLibrary) — current target `net10.0-windows7.0`
  - `CardCrawler.Browser\CardCrawler.Browser.csproj` (ClassLibrary) — current target `net10.0-windows7.0`
- Complexity classification: **Simple** — 3 projects, shallow dependency depth, no high-risk findings.
- Iterations performed to build this plan: 3 (Discovery, Classification, Strategy)
- Immediate recommendation: Create a verification branch and run the following checks: SDK presence, global.json compatibility, CI build validation. If all pass, either record a verification commit or apply minimal package updates as desired.

## Migration Strategy

- Approach: **All-At-Once** — perform atomic repository-wide update and verification. Even though assessment shows projects already target `net10.0`, the plan documents the steps to (a) confirm SDK/tooling, (b) apply framework/package updates in a single coordinated change if requested, and (c) validate.
- When to use this plan as-is: repository is small, homogeneous, and non-problematic. Use All-At-Once to minimize churn.
- Phases (organized for clarity; atomic upgrade is a single operation):
  - Phase 0: Preparation — SDK validation, branch creation, stash/commit handling.
  - Phase 1: Atomic Upgrade — update project TargetFramework elements and package references across all projects in one pass (if any change is required).
  - Phase 2: Test Validation — restore, build solution, run tests, remediate any issues discovered during the atomic pass.

- Important implementation notes for executors (planning-only guidance):
  - Check for MSBuild import files that affect TargetFramework or PackageReferences: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`. Update those files atomically if they centralize TFM or package versions.
  - Look for conditional logic in project files (e.g., `Condition=" '$(TargetFramework)' == 'net...' "`) and ensure conditions remain correct for `net10.0`.
  - If projects use multi-targeting, prefer appending the new TFM (e.g., `<TargetFrameworks>net7.0;net10.0</TargetFrameworks>`) rather than replacing, unless a full migration is desired.
  - If no changes are required (projects already target `net10.0`), create a verification commit branch to record validation steps and CI checks.

## Detailed Dependency Analysis

- Topological order (leaf → root):
  1. `CardCrawler.Browser\CardCrawler.Browser.csproj`
  2. `CardCrawler.Cardmarket\CardCrawler.Cardmarket.csproj`
  3. `CardCrawler\CardCrawler.csproj`

- Dependency graph summary:
  - `CardCrawler.Browser` — leaf library, no internal project dependencies.
  - `CardCrawler.CardMarket` — depends on `CardCrawler.Browser` (inferred by analysis order).
  - `CardCrawler` — application, depends on `CardCrawler.CardMarket`.

- Circular dependencies: none detected.
- Critical path: `CardCrawler.Browser` → `CardCrawler.CardMarket` → `CardCrawler`.

- Migration grouping: All projects will be updated simultaneously (All-At-Once). The topological list is provided so verification steps can inspect dependency order if needed during build.

## Project-by-Project Plans

### Project Inventory

| Project | Current TFM | Proposed TFM | Project Type | LOC | Risk |
|---|---:|---:|---|---:|---:|
| `CardCrawler.Browser` | `net10.0-windows7.0` | `net10.0-windows7.0` | ClassLibrary | 103 | Low |
| `CardCrawler.CardMarket` | `net10.0-windows7.0` | `net10.0-windows7.0` | ClassLibrary | 213 | Low |
| `CardCrawler` | `net10.0-windows7.0` | `net10.0-windows7.0` | App | 299 | Low |

### Package details per project

#### `CardCrawler.Browser`

| Package | Current | Suggested | Notes |
|---|---:|---|---|
| `PuppeteerSharp` | `20.2.5` | (no change) | Compatible with net10.0 per assessment |

Risk & Complexity: Low — single package, small LOC.

#### `CardCrawler.CardMarket`

| Package | Current | Suggested | Notes |
|---|---:|---|---|
| `HtmlAgilityPack` | `1.12.4` | (no change) | Compatible with net10.0 per assessment |

Risk & Complexity: Low — standard library usage.

#### `CardCrawler`

| Package | Current | Suggested | Notes |
|---|---:|---|---|
| (none reported) |  |  |  |

Risk & Complexity: Low — application logic only, few external packages.

## Risk Management (detailed)

- High-level: Low overall. No security vulnerabilities or breaking changes detected.
- Residual risks:
  - Platform assumption risk in `CardCrawler.Browser` due to `-windows7.0` TFM; confirm any P/Invoke or OS-specific APIs.
  - If CI agents are using older SDKs, build may fail; Phase 0 ensures SDK validation.

Mitigation actions:
- Verify .NET 10 SDK on all CI and developer machines.
- Run a dependency vulnerability scan post-restore.
- Run the full solution build and tests on CI using the upgrade/verification branch before merging.

## Project-by-Project Plans

### Project: `CardCrawler\CardCrawler.csproj`
- Current State: `net10.0-windows7.0` (SDK-style, DotNetCoreApp)
- Target State: `net10.0-windows7.0` (no change unless a different variant is requested)
- Packages referenced (assessment): none reported other than solution-level items
- Migration Steps (atomic operation - included here for executor guidance):
  1. Verify SDK and global.json compatibility (see Phase 0).
  2. If target change required, update `TargetFramework` to desired TFM (replace existing value). If multitargeting desired, append new TFM instead of replacing.
  3. Update any `PackageReference` versions per Package Update Reference (none mandatory in assessment).
  4. Restore and compile as part of the atomic pass.
  5. Address compilation/test failures as part of the single atomic fix pass.
- Expected Breaking Changes: none identified in assessment.
- Validation: builds clean with 0 errors; unit/integration tests (if present) pass.

### Project: `CardCrawler.CardMarket\CardCrawler.Cardmarket.csproj`
- Current State: `net10.0-windows7.0` (SDK-style, ClassLibrary)
- Target State: `net10.0-windows7.0` (no change)
- Key packages: `HtmlAgilityPack` 1.12.4 (compatible)
- Migration Steps: same atomic steps as above.
- Expected Breaking Changes: none identified.
- Validation: builds clean; libraries referenced by `CardCrawler` compile successfully.

### Project: `CardCrawler.Browser\CardCrawler.Browser.csproj`
- Current State: `net10.0-windows7.0` (SDK-style, ClassLibrary)
- Target State: `net10.0-windows7.0` (no change)
- Key packages: `PuppeteerSharp` 20.2.5 (compatible)
- Special considerations: Windows-specific runtime identifier in TFM (`-windows7.0`) exists. If cross-platform runtime is desired, adjust TFM accordingly — document separately.
- Migration Steps: same atomic steps as above.
- Validation: builds clean and any browser automation tests (if present) run successfully.

## Package Update Reference

- Assessment found all packages compatible. No mandatory package updates flagged.

Common packages (from assessment):
- `PuppeteerSharp` — current: `20.2.5` — Projects: `CardCrawler.Browser` — Status: Compatible
- `HtmlAgilityPack` — current: `1.12.4` — Projects: `CardCrawler.CardMarket` — Status: Compatible

Notes:
- If security or feature-driven package updates are desired, include the updated versions here and apply them during the atomic pass. Any package updates should be included in the single atomic commit.

## Breaking Changes Catalog

- Assessment reports no breaking changes detected for this target upgrade.
- Generic checks to include during the atomic pass (executor to verify):
  - Obsolete API usage and compiler warnings escalated to errors if repository policy enforces it.
  - Windows-specific APIs used by `CardCrawler.Browser` that might be affected by runtime change.
  - Any reflection/assembly-binding assumptions that depend on old runtime behavior.

## Testing & Validation Strategy

- Phase 0 validations (pre-upgrade):
  - Confirm .NET 10 SDK is installed on build agents/developer machines (`dotnet --list-sdks`).
  - Validate `global.json` (if present) is compatible with chosen SDK.
- Atomic Upgrade validation (part of single operation):
  1. Run `dotnet restore` for the solution.
  2. Build the entire solution and ensure 0 compilation errors.
  3. Run all discovered test projects (none discovered in assessment; if present, run them).
  4. Verify no new package vulnerabilities have been introduced (run dependency scan if available).
- Acceptance criteria: solution builds with 0 errors; all automated tests pass; no outstanding security vulnerabilities.

## Testing & Validation Checklist

Pre-upgrade checks (Phase 0):
- [ ] Working tree clean and changes committed.
- [ ] Upgrade/verification branch created.
- [ ] .NET 10 SDK installed on local/CI agents.
- [ ] `global.json` reviewed for SDK pinning and updated if necessary.

Atomic task checks (Phase 1 & 2):
- [ ] Update project TFMs or confirm no change required.
- [ ] Update centralized MSBuild props if used.
- [ ] Run `dotnet restore` successfully for the solution.
- [ ] `dotnet build` completes with 0 errors.
- [ ] All discovered test projects executed and pass.
- [ ] Dependency vulnerability scan completed and no critical findings remain.

Post-upgrade checks:
- [ ] Create PR with assessment and plan linked.
- [ ] CI runs and build/tests succeed.
- [ ] Merge and monitor for runtime regressions.

Test projects to run:
- Assessment did not discover test projects. If tests exist, include them in the verification run:
  - Unit tests (any test project under solution)
  - Integration tests (if present)

Validation criteria:
- Solution builds with 0 errors.
- Tests pass.
- No unresolved security vulnerabilities.

## Risk Management

- Overall risk: **Low** — small codebase, tested libraries, no incompatibilities flagged.
- Project risk levels:
  - `CardCrawler` — Low
  - `CardCrawler.CardMarket` — Low
  - `CardCrawler.Browser` — Low (note: Windows-specific runtime flag; verify any platform APIs)

Mitigations:
- Use a disposable upgrade branch for the atomic change.
- Include all updates in a single commit to simplify rollbacks.
- Keep a clear rollback plan: if build/tests fail post-merge, revert commit and investigate.
- If any package shows vulnerabilities later, address them as part of a follow-up change.

## Complexity & Effort Assessment

- Solution classification: **Simple** (3 projects, shallow dependency graph, no issues reported).
- Per-project complexity: Low — small LOC counts, few dependencies.
- No time estimates provided (per constraints). Use relative complexity only.

## Source Control Strategy (detailed)

- Start branch: `main` (current branch).
- Upgrade/verification branch naming convention:
  - Verification-only (no changes): `verify/dotnet-10-<YYYYMMDD-HHMM>`
  - Upgrade/apply changes: `upgrade/dotnet-10-<YYYYMMDD-HHMM>`
- Commit style:
  - Single atomic commit capturing all changes.
  - Commit message example: `Atomic: upgrade repo to .NET 10 (confirm TFMs and package compatibility)`
- Pull Request:
  - Single PR from upgrade branch to `main`.
  - PR description should include link to assessment.md and this plan.md and summary of changes.

## Follow-up Modernization Options

If you want to continue modernization beyond framework verification, consider these prioritized actions:
1. Dependency security sweep — run a scanner (Dependabot, dotnet list package --vulnerable) and include fixes in a follow-up PR.
2. CI/CD improvements — pin SDK versions in CI, add matrix builds for OS/TFM if multi-targeting is later desired.
3. Performance features — evaluate trimming, single-file deployment, and AOT where applicable (requires targeted testing).
4. Observability — centralize logging, add structured tracing, export metrics to chosen platform.
5. Code-quality toolchain — add Roslyn analyzers, enforce nullability and code-style rules across solution.

## Plan Completion

All sections populated. No placeholder text remains.

**Plan generation complete.**

Opening `plan.md` for review.
