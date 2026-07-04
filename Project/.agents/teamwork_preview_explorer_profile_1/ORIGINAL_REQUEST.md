## 2026-07-03T22:37:22Z
You are a teamwork_preview_explorer agent.
Your working directory is: e:\OneDrive - 0dpmr\WebPhotocopy\Project\.agents\teamwork_preview_explorer_profile_1

Task:
Analyze the Profile Razor views:
1. `WebPhotocopyHub.Web.Customer\Views\Profile\Index.cshtml`
2. `WebPhotocopyHub.Web.Customer\Views\Profile\ChangePassword.cshtml`

Your goal is to recommend a detailed refactoring strategy to Tailwind CSS (fully compatible with `_BranchCustomerModernLayout.cshtml`).
Ensure that:
- You list all the Bootstrap classes currently used in these files.
- You map each Bootstrap class or element to an equivalent modern Tailwind utility layout (e.g. wrapper card `bg-surface-container-lowest rounded-xl border border-surface-variant shadow-sm p-6`, inputs `bg-white border border-outline-variant/30 rounded-lg text-sm px-3 py-2 w-full focus:ring-1 focus:ring-primary focus:border-primary transition-all`, labels `block text-xs font-bold text-on-surface uppercase tracking-wider mb-2`, primary blue buttons `bg-primary hover:bg-primary-fixed-variant text-on-primary font-bold px-4 py-2 rounded-lg shadow-sm transition-colors`).
- You outline the final HTML/Razor structure proposed for each page, showing how all original `@model` properties, form tags, helper attributes (`asp-for`, `asp-action`, etc.), and scripts sections are preserved exactly.
- Make sure to review the existing `_BranchCustomerModernLayout.cshtml` layout to match its design tokens and styles (e.g., container width, background colors).

Write your refactoring strategy to: `e:\OneDrive - 0dpmr\WebPhotocopy\Project\.agents\teamwork_preview_explorer_profile_1\profile_strategy.md`.
Send a message to the orchestrator (conversation ID: 8dbb442d-5eaf-4a67-bb89-ae81058ae674) with the strategy file path when done.
