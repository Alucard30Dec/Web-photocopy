## 2026-07-03T22:42:14Z

Analyze the following Customer Transactional Core Razor views:
- PrintJobs views: `Create.cshtml`, `Details.cshtml`, `Files.cshtml`, `Index.cshtml`
- Products views: `Index.cshtml`, `Details.cshtml`, `Orders.cshtml`
- Wallet views: `Index.cshtml`, `TopUp.cshtml`, `TopUpHistory.cshtml`
- SupportOrders views: `Create.cshtml`, `Details.cshtml`, `History.cshtml`

Your goal is to recommend a detailed refactoring strategy to Tailwind CSS (fully compatible with `_BranchCustomerModernLayout.cshtml`).
Ensure that you:
- List all Bootstrap classes used in these files.
- Map them to modern Tailwind CSS utility classes and layout concepts matching the modern dashboard aesthetic (e.g. Card structures `bg-surface-container-lowest border border-surface-variant/20 rounded-xl p-6 shadow-sm`, lists, forms, buttons).
- Pay close attention to tables: how to style tables in Tailwind (e.g. using `table-auto w-full text-left text-sm`, `thead` with `bg-surface-container`, text headers, row hover effects, borders, cell padding).
- Pay close attention to pagination controls: how to style pagination in Tailwind (e.g. replacing `pagination`/`page-item`/`page-link` with a simple flex container containing styled `<a>` elements for pages and status badges).
- Pay close attention to form inputs and helpers (such as `form-select`, `form-control`, validation fields).
- Ensure that all Razor bindings, loops, model properties, form actions, anti-forgery tokens, and scripts are preserved exactly.

Write your refactoring strategy to: `e:\OneDrive - 0dpmr\WebPhotocopy\Project\.agents\teamwork_preview_explorer_transactional_1\transactional_strategy.md`.
Send a message to the orchestrator (conversation ID: 8dbb442d-5eaf-4a67-bb89-ae81058ae674) with the strategy file path when done.
