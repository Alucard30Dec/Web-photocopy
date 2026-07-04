# Project: WebPhotocopyHub Customer View Refactoring

## Architecture
- Target Project: `WebPhotocopyHub.Web.Customer`
- Tech Stack: ASP.NET Core 8.0 MVC (Razor Views)
- Objective: Replace Bootstrap with Tailwind CSS styling, utilizing the new `_BranchCustomerModernLayout.cshtml`.

## Code Layout
- Customer Views: `WebPhotocopyHub.Web.Customer/Views/`
- Layouts: `WebPhotocopyHub.Web.Customer/Views/Shared/`
- ViewStart: `WebPhotocopyHub.Web.Customer/Views/_ViewStart.cshtml`

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| 1 | M1: Analysis | Investigate existing customer-facing views for layout, forms, model bindings | None | DONE |
| 2 | M2: E2E Test Suite | Create test scripts to verify lack of Bootstrap classes and successful dotnet build | M1 | DONE |
| 3 | M3: Refactor Profile Views | Change layout and styles of Profile Index & ChangePassword views | M1 | DONE |
| 4 | M4: Refactor Transactional Core | Refactor PrintJobs, Products, Wallet, and SupportOrders views | M3 | PLANNED |
| 5 | M5: Refactor Auth & General | Refactor Account Login, Register, Forgot, Reset views + Branch Index + ViewStart + Alert | M4 | PLANNED |
| 6 | M6: Final E2E Pass | Run E2E verification script and compile project to 100% success | M5 | PLANNED |

## Interface Contracts
### Razor Views ↔ Controller Model Bindings
- All `@model` types must remain identical.
- All form helper attributes (`asp-for`, `asp-action`, `asp-controller`, `asp-route-*`) must remain unchanged.
- Form submit action behaviors must not be altered.
