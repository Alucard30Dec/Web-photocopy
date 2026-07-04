# BRIEFING — 2026-07-04T05:45:00+07:00

## Mission
Analyze selected Customer Transactional Core Razor views and propose a Tailwind CSS refactoring strategy matching the modern layout.

## 🔒 My Identity
- Archetype: teamwork_preview_explorer
- Roles: Read-only investigator, Teamwork explorer
- Working directory: e:\OneDrive - 0dpmr\WebPhotocopy\Project\.agents\teamwork_preview_explorer_transactional_1
- Original parent: 8dbb442d-5eaf-4a67-bb89-ae81058ae674
- Milestone: Customer Transactional Razor Views Modernization

## 🔒 Key Constraints
- Read-only investigation — do NOT implement (only write strategy/reports to the agents directory)
- Must list all Bootstrap classes used in these files
- Must map Bootstrap classes to Tailwind CSS utility classes and modern layout concepts matching the dashboard aesthetic
- Pay close attention to tables, pagination controls, form inputs and validation, preserving all Razor bindings/logics exactly.

## Current Parent
- Conversation ID: 8dbb442d-5eaf-4a67-bb89-ae81058ae674
- Updated: 2026-07-04T05:45:00+07:00

## Investigation State
- **Explored paths**:
  - `WebPhotocopyHub.Web.Customer/Views/Shared/_BranchCustomerModernLayout.cshtml` (Layout)
  - `WebPhotocopyHub.Web.Customer/wwwroot/css/customer-dashboard-modern.css` (Styles)
  - `WebPhotocopyHub.Web.Customer/Views/PrintJobs/` (Create, Details, Files, Index)
  - `WebPhotocopyHub.Web.Customer/Views/Products/` (Details, Index, Orders)
  - `WebPhotocopyHub.Web.Customer/Views/Wallet/` (Index, TopUp, TopUpHistory)
  - `WebPhotocopyHub.Web.Customer/Views/SupportOrders/` (Create, Details, History)
  - `WebPhotocopyHub.Application/WebShared/Extensions/EnumViewExtensions.cs` (Badge class mappings)
- **Key findings**:
  - Identified 30 Bootstrap classes and custom `.cu-` styles that need replacement.
  - Formulated a standard utility-class configuration compatible with `customer-dashboard-modern.css`.
  - Defined details for pagination, tables, form inputs, validation fields, and modern badges.
- **Unexplored areas**:
  - None. All 13 target files and related dependencies have been investigated.

## Key Decisions Made
- Recommending to transition view layouts to `_BranchCustomerModernLayout.cshtml`.
- Replacing all legacy inline SVG icons with Material Icon symbols for dashboard aesthetic alignment.
- Overriding/altering the output from `ToBadgeClass()` C# extension helper directly in razor file badge wrapper classes.

## Artifact Index
- e:\OneDrive - 0dpmr\WebPhotocopy\Project\.agents\teamwork_preview_explorer_transactional_1\transactional_strategy.md — Strategy and mapping document for the implementation phase.
