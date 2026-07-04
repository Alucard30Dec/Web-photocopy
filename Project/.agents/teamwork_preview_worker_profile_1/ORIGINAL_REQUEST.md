## 2026-07-03T22:39:02Z

You are a teamwork_preview_worker agent.
Your working directory is: e:\OneDrive - 0dpmr\WebPhotocopy\Project\.agents\teamwork_preview_worker_profile_1

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A Forensic Auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Task:
Refactor the following Customer Profile view files to use Tailwind CSS and the modern layout:
1. `WebPhotocopyHub.Web.Customer\Views\Profile\Index.cshtml`
2. `WebPhotocopyHub.Web.Customer\Views\Profile\ChangePassword.cshtml`

Instructions:
1. Read the refactoring strategy report at: `e:\OneDrive - 0dpmr\WebPhotocopy\Project\.agents\teamwork_preview_explorer_profile_1\profile_strategy.md`.
2. Apply the proposed Razor/HTML structures for these two files.
3. Ensure that you preserve all:
   - `@model` directives and types.
   - Form attributes (`asp-action`, `asp-controller`, `asp-route-*`, `method`, `enctype`, etc.).
   - Helper attributes (`asp-for`, `readonly`, `autocomplete`, etc.).
   - Anti-forgery tokens (`@Html.AntiForgeryToken()`).
   - Scripts sections rendering (`@section Scripts { ... }`).
4. Set the Layout explicitly in both files to: `Layout = "~/Views/Shared/_BranchCustomerModernLayout.cshtml";`
5. Ensure all Bootstrap-specific classes (e.g. `row`, `col-lg-6`, `text-danger`, `mb-3`, `form-label`, `form-control`, `small`, `d-flex`, `gap-2`, `mt-4`, etc.) are removed and replaced with their Tailwind equivalents.
6. Verify your changes by running the test script from a PowerShell prompt:
   `powershell -ExecutionPolicy Bypass -File .\verify_views.ps1`
   Verify that:
   - The build succeeds.
   - There are no Bootstrap violations reported in these two views (`Profile/Index.cshtml` and `Profile/ChangePassword.cshtml`).
7. Save a summary of changes to `changes.md` in your working directory and send a message to the orchestrator (conversation ID: 8dbb442d-5eaf-4a67-bb89-ae81058ae674) when completed.
