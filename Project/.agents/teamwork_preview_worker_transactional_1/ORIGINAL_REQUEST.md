## 2026-07-03T22:43:34Z
You are a teamwork_preview_worker agent.
Your working directory is: e:\OneDrive - 0dpmr\WebPhotocopy\Project\.agents\teamwork_preview_worker_transactional_1

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A Forensic Auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Task:
Refactor the following 13 customer-facing transaction views in the WebPhotocopyHub.Web.Customer project to use Tailwind CSS and the modern layout:
- PrintJobs views: `Views/PrintJobs/Create.cshtml`, `Views/PrintJobs/Details.cshtml`, `Views/PrintJobs/Files.cshtml`, `Views/PrintJobs/Index.cshtml`
- Products views: `Views/Products/Index.cshtml`, `Views/Products/Details.cshtml`, `Views/Products/Orders.cshtml`
- Wallet views: `Views/Wallet/Index.cshtml`, `Views/Wallet/TopUp.cshtml`, `Views/Wallet/TopUpHistory.cshtml`
- SupportOrders views: `Views/SupportOrders/Create.cshtml`, `Views/SupportOrders/Details.cshtml`, `Views/SupportOrders/History.cshtml`

Instructions:
1. Read the refactoring strategy report at: `e:\OneDrive - 0dpmr\WebPhotocopy\Project\.agents\teamwork_preview_explorer_transactional_1\transactional_strategy.md`.
2. Systematically refactor each of the 13 files, replacing Bootstrap classes, legacy custom classes, legacy SVG icons, and legacy badge/pagination styles with modern Tailwind classes and Material Symbols as detailed in the strategy blueprint.
3. Set the Layout explicitly in all 13 views to: `Layout = "~/Views/Shared/_BranchCustomerModernLayout.cshtml";`
4. Strict constraint: Preserve all C# structures, Razor models (@model), forms, form actions, input helper bindings (asp-for, etc.), anti-forgery tokens, idempotency inputs, data-* attributes, JavaScript section blocks (@section Scripts), and loops/conditions.
5. Verify your changes periodically by running:
   `powershell -ExecutionPolicy Bypass -File .\verify_views.ps1`
   Ensure the build succeeds with 0 errors and the count of files with violations decreases.
6. Write a summary of changes to `changes.md` in your working directory and send a message to the orchestrator (conversation ID: 8dbb442d-5eaf-4a67-bb89-ae81058ae674) when done.
