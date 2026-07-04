# BRIEFING — 2026-07-04T05:39:00+07:00

## Mission
Analyze Profile Razor views (Index.cshtml, ChangePassword.cshtml) and recommend a Tailwind CSS refactoring strategy compatible with layout _BranchCustomerModernLayout.cshtml.

## 🔒 My Identity
- Archetype: teamwork_preview_explorer
- Roles: read-only explorer, analyst
- Working directory: e:\OneDrive - 0dpmr\WebPhotocopy\Project\.agents\teamwork_preview_explorer_profile_1
- Original parent: 8dbb442d-5eaf-4a67-bb89-ae81058ae674
- Milestone: Refactoring Profile Views to Tailwind CSS

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Must list all Bootstrap classes used in target views
- Must map Bootstrap classes to Tailwind CSS layout utility classes compatible with _BranchCustomerModernLayout.cshtml
- Must outline the final HTML/Razor structure proposed for each page, preserving all model properties, form tags, helper attributes, and script sections
- Write strategy to e:\OneDrive - 0dpmr\WebPhotocopy\Project\.agents\teamwork_preview_explorer_profile_1\profile_strategy.md

## Current Parent
- Conversation ID: 8dbb442d-5eaf-4a67-bb89-ae81058ae674
- Updated: not yet

## Investigation State
- **Explored paths**:
  - `WebPhotocopyHub.Web.Customer\Views\Shared\_BranchCustomerModernLayout.cshtml`
  - `WebPhotocopyHub.Web.Customer\Views\Profile\Index.cshtml`
  - `WebPhotocopyHub.Web.Customer\Views\Profile\ChangePassword.cshtml`
  - `WebPhotocopyHub.Web.Customer\Views\Dashboard\Index.cshtml`
  - `WebPhotocopyHub.Web.Customer\Views\Wallet\TopUp.cshtml`
  - `WebPhotocopyHub.Web.Customer\wwwroot\css\customer-dashboard-modern.css`
  - `WebPhotocopyHub.Web.Customer\wwwroot\css\customer-role-ui.css`
- **Key findings**:
  - `customer-dashboard-modern.css` only has compiled Tailwind classes, missing standard styling like `form-control`, `form-label`, `text-danger`, or input focus rings.
  - Spanning grid layout columns (like for Address textarea) can be elegantly solved by moving the textarea wrapper outside the 2-column grid container instead of relying on non-compiled `col-span-2` utility.
  - A small, view-scoped `<style>` block is required to map standard validation classes (`.input-validation-error` and `.text-error`) and custom focus states (`input:focus`) to M3 CSS variables (`--cd-primary` and `--cd-error`).
- **Unexplored areas**: None. Complete investigation of target files and dependencies is finished.

## Key Decisions Made
- Structured both views inside `<main class="flex-1 p-md md:p-lg overflow-y-auto max-w-container-max mx-auto w-full px-lg">` to match layout container structure.
- Developed vertical stack layout using `<div class="flex flex-col gap-2">` to render labels above inputs without block utility dependencies.

## Artifact Index
- e:\OneDrive - 0dpmr\WebPhotocopy\Project\.agents\teamwork_preview_explorer_profile_1\profile_strategy.md — Tailwind CSS refactoring strategy for Profile views
