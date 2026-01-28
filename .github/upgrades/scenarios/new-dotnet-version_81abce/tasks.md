# CardCrawler .NET 10 Upgrade Tasks

## Overview

This document tracks the execution of the repository upgrade to .NET 10 for the `CardCrawler` solution. Tasks cover prerequisites verification, applying required project/package changes, test validation, and the final commit.

**Progress**: 0/4 tasks complete (0%) ![0%](https://progress-bar.xyz/0)

---

## Tasks

### [▶] TASK-001: Verify prerequisites
**References**: Plan §Phase 0, Plan §Testing & Validation Strategy

- [▶] (1) Verify required .NET 10 SDK is installed per Plan §Phase 0 (run `dotnet --list-sdks`)
- [ ] (2) Runtime/SDK version meets minimum requirements (**Verify**)
- [ ] (3) Validate `global.json` compatibility with .NET 10 if `global.json` is present (per Plan §Phase 0)
- [ ] (4) `global.json` compatibility confirmed or no `global.json` present (**Verify**)

### [ ] TASK-002: Atomic framework and package upgrade with compilation fixes
**References**: Plan §Migration Strategy, Plan §Project-by-Project Plans, Plan §Package Update Reference, Plan §Breaking Changes Catalog

- [ ] (1) Update `TargetFramework` entries in all projects listed in Plan §Project-by-Project Plans (apply changes only if required)
- [ ] (2) Update centralized MSBuild props/imports if present (`Directory.Build.props`, `Directory.Packages.props`) per Plan §Migration Strategy
- [ ] (3) Update package references per Plan §Package Update Reference (apply only required updates)
- [ ] (4) Restore dependencies for `CardCrawler.sln` (run `dotnet restore`)
- [ ] (5) All dependencies restored successfully (**Verify**)
- [ ] (6) Build solution and fix all compilation errors referenced in Plan §Breaking Changes Catalog
- [ ] (7) Solution builds with 0 errors (**Verify**)

### [ ] TASK-003: Run test validation and remediate (if applicable)
**References**: Plan §Testing & Validation Strategy, Plan §Breaking Changes Catalog

- [ ] (1) Per Plan §Testing & Validation Strategy, confirm no test projects were discovered in the assessment; verify repository contains no test projects (**Verify**)
- [ ] (2) IF test projects exist contrary to Plan §Testing & Validation Strategy: Run tests in the specific test projects listed in Plan §Testing & Validation Strategy (run `dotnet test` for those projects)
- [ ] (3) IF tests were executed: Fix any test failures (reference Plan §Breaking Changes Catalog for common issues)
- [ ] (4) IF tests were executed: Re-run tests and ensure all tests pass with 0 failures (**Verify**)

### [ ] TASK-004: Final commit
**References**: Plan §Source Control Strategy

- [ ] (1) Commit all remaining changes with message: "TASK-004: Complete upgrade to .NET 10"
