# Challenger Report — Verification of Transactional Core Views

## Challenge Summary

**Overall risk assessment**: LOW

All 13 Transactional Core views (PrintJobs, Products, Wallet, and SupportOrders) compile with zero errors and conform completely to the Bootstrap-free styling policy. 
- **Compilation Check**: `dotnet build WebPhotocopyHub.Web/WebPhotocopyHub.Web.csproj` (which builds all dependencies including `Web.Customer`) compiles successfully with 0 warnings and 0 errors.
- **Bootstrap Scanner**: The verification script `verify_views.ps1` was executed. None of the 13 target views were flagged with any Bootstrap class violations. 
- **Verification Target Coverage**:
  - **PrintJobs**: `Create.cshtml`, `Details.cshtml`, `Files.cshtml`, `Index.cshtml` (4 files)
  - **Products**: `Details.cshtml`, `Index.cshtml`, `Orders.cshtml` (3 files)
  - **Wallet**: `Index.cshtml`, `TopUp.cshtml`, `TopUpHistory.cshtml` (3 files)
  - **SupportOrders**: `Create.cshtml`, `Details.cshtml`, `History.cshtml` (3 files)
  - **Total**: 13 views.

---

## Challenges

### [Low] Challenge 1: Static Regex Limitations in View Verification Script

- **Assumption challenged**: The E2E script `verify_views.ps1` perfectly scans and detects all Bootstrap classes.
- **Attack scenario**: If a developer writes a Bootstrap class conditionally within a C# block on a line that does not contain the word `class =` (for example, storing a list of classes in a string variable first, or dynamically formatting `"text-" + "danger"`), the script's regex `class\s*=` check on line 73 will skip the line completely.
- **Blast radius**: Low. A small amount of Bootstrap styling could slip through if dynamic classes are declared on separate lines before being applied.
- **Mitigation**: Standardize code formatting so class attributes are always on the same line as the `class` declaration, or expand the regex scanner to analyze all string literals in `.cshtml` files for Bootstrap keyword patterns.

### [Low] Challenge 2: Runtime Route and Parameter Bindings

- **Assumption challenged**: Zero build errors guarantees that the views are fully functional at runtime.
- **Attack scenario**: Razor views bind dynamically to routes and controller helper contexts (e.g. `CustomerBranchContext.GetSlug(ViewContext)`). If a controller action's signature or parameter names change, or if routing rules change, compilation will succeed but pages will throw runtime binding errors.
- **Blast radius**: Medium. User forms could fail to submit or display incorrect links if slugs or dynamic paths resolve incorrectly.
- **Mitigation**: Implement integration tests or light E2E testing using Playwright/Selenium to verify route loading and form submission workflows.

---

## Stress Test Results

- **Run E2E Verification Script (`verify_views.ps1`)** → Scans all customer views for Bootstrap classes → **PASS** (Zero violations found for the 13 target views. Violations were only flagged in 6 files belonging to the next milestone `M5: Auth & General`).
- **Build Target Project (`WebPhotocopyHub.Web.Customer`)** → Compiles project and output assemblies → **PASS** (Build succeeded with 0 warnings and 0 errors).
- **Build Main Web Project (`WebPhotocopyHub.Web`)** → Full solution compilation → **PASS** (Build succeeded with 0 warnings and 0 errors).

---

## Unchallenged Areas

- **Controller Bindings and Dynamic Routing** — Insufficient context to run the web server and perform runtime integration checks. Scope is limited to build and styling validation.
- **Legacy Layout File (`_BranchCustomerLayout.cshtml`)** — Explicitly ignored by the scanner as per `TEST_READY.md` specs.
