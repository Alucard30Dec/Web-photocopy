## 2026-07-04T05:48:16Z
Review the refactored Transactional Core views:
- PrintJobs: Create.cshtml, Details.cshtml, Files.cshtml, Index.cshtml
- Products: Index.cshtml, Details.cshtml, Orders.cshtml
- Wallet: Index.cshtml, TopUp.cshtml, TopUpHistory.cshtml
- SupportOrders: Create.cshtml, Details.cshtml, History.cshtml

Your review should verify:
- Correctness, completeness, and layout/aesthetic alignment with the modern layout _BranchCustomerModernLayout.cshtml and design system classes.
- Successful compilation and lack of Bootstrap classes by running the verification test script:
  `powershell -ExecutionPolicy Bypass -File .\verify_views.ps1`
- Complete preservation of @model directives, forms, form helper attributes, anti-forgery tokens, script blocks, and loops.

Write your review report to: `e:\OneDrive - 0dpmr\WebPhotocopy\Project\.agents\teamwork_preview_reviewer_transactional_1\review.md`.
Send a message to the orchestrator (conversation ID: 8dbb442d-5eaf-4a67-bb89-ae81058ae674) with the review file path when done.
