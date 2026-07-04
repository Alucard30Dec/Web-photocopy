# BRIEFING — 2026-07-04T05:37:00+07:00

## Mission
Create and verify `verify_views.ps1` E2E script and compile the project to ensure no Bootstrap classes exist in the views (except the ignored layout).

## 🔒 My Identity
- Archetype: teamwork_preview_worker
- Roles: implementer, qa, specialist
- Working directory: e:\OneDrive - 0dpmr\WebPhotocopy\Project\.agents\teamwork_preview_worker_e2e_tests_1
- Original parent: 8dbb442d-5eaf-4a67-bb89-ae81058ae674
- Milestone: E2E Verification Script for Views

## 🔒 Key Constraints
- CODE_ONLY network mode. No external HTTP clients/curl/wget/etc.
- DO NOT CHEAT. All implementations must be genuine.
- PowerShell script `verify_views.ps1` at project root.
- Build the specific customer csproj project and check exit code.
- Scan `.cshtml` files under `WebPhotocopyHub.Web.Customer/Views` ignoring `Views/Shared/_BranchCustomerLayout.cshtml`.
- Exclude Tailwind classes but match Bootstrap classes specifically.
- Return exit code 0 if build succeeds and no Bootstrap classes are found, otherwise return 1.
- Write `TEST_READY.md` at project root.
- Report findings and handoff in working directory.

## Current Parent
- Conversation ID: 8dbb442d-5eaf-4a67-bb89-ae81058ae674
- Updated: 2026-07-04T05:37:00+07:00

## Task Summary
- **What to build**: E2E verification script `verify_views.ps1` in PowerShell, testing view files for Bootstrap class presence and project buildability.
- **Success criteria**: Script compiles and runs successfully, returning exit code 0 if build is successful and no Bootstrap violations are found (except in the ignored file), otherwise 1. Prints out files and lines where Bootstrap violations are found.
- **Interface contracts**: `verify_views.ps1` script output formats and exit codes.
- **Code layout**: Root directory for script, `WebPhotocopyHub.Web.Customer/Views` for scanning target.

## Key Decisions Made
- Tokenized lines containing `class=` to identify Bootstrap class usages.
- Refined Bootstrap patterns to specifically match form control classes (like `form-control`, `form-label`) instead of a broad `form-` prefix, avoiding false positives like `form-data`.
- Excluded Tailwind classes starting with `col-` (like `col-span-`, `col-start-`, `col-end-`) while capturing Bootstrap `col-lg-6`, `col-md-4`, etc.

## Artifact Index
- `verify_views.ps1` — E2E view validation script.
- `TEST_READY.md` — Test specification and execution instructions.

## Change Tracker
- **Files modified**: `verify_views.ps1` (new), `TEST_READY.md` (new)
- **Build status**: PASS
- **Pending issues**: None

## Quality Status
- **Build/test result**: Build passes, scan finds 245 violations (expected on non-refactored pages)
- **Lint status**: PASS
- **Tests added/modified**: new script `verify_views.ps1`

## Loaded Skills
None
