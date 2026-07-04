## 2026-07-04T05:34:03Z

You are a teamwork_preview_worker agent.
Your working directory is: e:\OneDrive - 0dpmr\WebPhotocopy\Project\.agents\teamwork_preview_worker_e2e_tests_1

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A Forensic Auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Task:
Create an E2E verification script `verify_views.ps1` (PowerShell) at the project root `e:\OneDrive - 0dpmr\WebPhotocopy\Project`.

The script must:
1. Run `dotnet build WebPhotocopyHub.Web.Customer/WebPhotocopyHub.Web.Customer.csproj` and check the exit code. If build fails, return non-zero exit code.
2. Scan all `.cshtml` files under `WebPhotocopyHub.Web.Customer/Views` to check for the presence of Bootstrap classes.
   - Ignore `Views/Shared/_BranchCustomerLayout.cshtml` (the old layout) during scans, but scan all other views.
   - Specify patterns that match Bootstrap classes specifically, and avoid matching Tailwind classes (such as `max-w-container-max`, `bg-surface-container`, `grid-cols-`, `col-span-`, etc.).
   - Explicitly list which file contains any violations and the line contents.
3. Return exit code 0 if build succeeds and no Bootstrap classes are found. Otherwise, return exit code 1.
4. Run the script on the current codebase to verify it works (it should report build status and list the Bootstrap violations on the non-refactored pages).
5. Write a `TEST_READY.md` at the project root following the format in the Project Pattern instructions (e.g. detailing runner command, coverage summary, and feature checklist).
6. Report your findings and handoff in your working directory. Send a message to the orchestrator (conversation ID: 8dbb442d-5eaf-4a67-bb89-ae81058ae674) when done.
