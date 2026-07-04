# WebPhotocopyHub.Web.Customer View Analysis Report

This report documents the structure, layouts, styling frameworks, models, and form bindings for all customer-facing Razor views (`.cshtml` files) inside the project `WebPhotocopyHub.Web.Customer` located at `e:\OneDrive - 0dpmr\WebPhotocopy\Project\WebPhotocopyHub.Web.Customer\Views`.

---

## Global and Setup Files

### 1. `_ViewImports.cshtml`
- **Customer-facing**: No (Setup/Import configuration)
- **Current Layout**: None
- **Main Bootstrap/styling classes**: None
- **Razor Models, Forms, and Bindings**:
  - Global using directives:
    - `@using WebPhotocopyHub.Web`
    - `@using WebPhotocopyHub.Web.Models`
    - `@using WebPhotocopyHub.Web.Customer.Models`
    - `@using WebPhotocopyHub.Web.Extensions`
    - `@using System.Linq`
    - `@using WebPhotocopyHub.Domain.Entities`
    - `@using WebPhotocopyHub.Domain.Enums`
    - `@using WebPhotocopyHub.Web.Customer.Helpers`
  - Global Tag Helpers: `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`

### 2. `_ViewStart.cshtml`
- **Customer-facing**: No (Configuration)
- **Current Layout**: None (Assigns default layout: `Layout = "~/Views/Shared/_BranchCustomerLayout.cshtml";`)
- **Main Bootstrap/styling classes**: None
- **Razor Models, Forms, and Bindings**: None

---

## Customer-Facing Account Views

### 3. `Account/ExternalLoginConfirmation.cshtml`
- **Customer-facing**: Yes (Confirming registration/linking accounts via external providers)
- **Current Layout**: Overrides to `~/Views/Shared/_BranchCustomerLayout.cshtml`
- **Main Bootstrap/styling classes**: `text-danger`, `mb-3`, `form-label`, `form-control`, `small`, `w-100` (alongside custom classes such as `customer-auth-page`, `customer-auth-hero`, etc.).
- **Razor Models, Forms, and Bindings**:
  - Model: `@model ExternalLoginConfirmationViewModel`
  - Form tag: `<form action="@formAction" method="post" class="customer-auth-form">` with `@Html.AntiForgeryToken()`
  - Bindings:
    - `<input type="hidden" asp-for="ReturnUrl" />`
    - `<div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>`
    - `<input asp-for="Email" type="email" class="form-control" readonly />`
    - `<input asp-for="FullName" class="form-control" />`
    - `<input asp-for="UserName" class="form-control" />`
    - `<input asp-for="PhoneNumber" class="form-control" />`
    - `<input asp-for="Address" class="form-control" />`
  - Script section rendering: `@section Scripts { <partial name="_ValidationScriptsPartial" /> }`

### 4. `Account/ForgotPassword.cshtml`
- **Customer-facing**: Yes (Password recovery)
- **Current Layout**: Inherits from `_ViewStart.cshtml` (`~/Views/Shared/_BranchCustomerLayout.cshtml`)
- **Main Bootstrap/styling classes**: `text-danger`, `mb-3`, `form-floating`, `form-control`, `form-label`, `w-100`, `btn`, `btn-lg`, `btn-primary`, `mt-4`, `text-center`.
- **Razor Models, Forms, and Bindings**:
  - Model: `@model WebPhotocopyHub.Web.Models.ForgotPasswordViewModel`
  - Form tag: `<form asp-action="ForgotPassword" asp-route-branchSlug="@ViewData["Branch"]?.GetType().GetProperty("Slug")?.GetValue(ViewData["Branch"])" method="post">`
  - Bindings:
    - `<div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>`
    - `<input asp-for="Email" class="form-control" />`
  - Script section rendering: `@section Scripts { <partial name="_ValidationScriptsPartial" /> }`

### 5. `Account/Login.cshtml`
- **Customer-facing**: Yes (Customer Portal Login)
- **Current Layout**: Overrides to `~/Views/Shared/_BranchCustomerLayout.cshtml`
- **Main Bootstrap/styling classes**: `text-danger`, `mb-2`, `small` (mainly utilizes custom scoped styling block with class prefix `.exact-`).
- **Razor Models, Forms, and Bindings**:
  - Model: `@model LoginViewModel`
  - Services: `@inject Microsoft.Extensions.Configuration.IConfiguration Configuration`
  - External Login Forms: Two distinct forms (Google/Facebook) targeting `action="@externalLoginUrl" method="post"`.
  - Credentials Form: `<form action="@formAction" method="post" novalidate>` containing:
    - `@Html.AntiForgeryToken()`
    - `<input type="hidden" asp-for="ReturnUrl" />`
    - `<div asp-validation-summary="ModelOnly" class="text-danger mb-2 exact-validation"></div>`
    - `<input asp-for="Email" class="exact-input" autocomplete="username" />`
    - `<input asp-for="Password" type="password" class="exact-input" autocomplete="current-password" />`
    - `<input asp-for="RememberMe" />` (Checkbox)
  - Script section rendering: `@section Scripts { <partial name="_ValidationScriptsPartial" /> }`

### 6. `Account/Register.cshtml`
- **Customer-facing**: Yes (New customer registration)
- **Current Layout**: Overrides to `~/Views/Shared/_BranchCustomerLayout.cshtml`
- **Main Bootstrap/styling classes**: `text-danger`, `mb-3`, `form-label`, `form-control`, `small`, `w-100`.
- **Razor Models, Forms, and Bindings**:
  - Model: `@model RegisterViewModel`
  - Services: `@inject Microsoft.Extensions.Configuration.IConfiguration Configuration`
  - Google Register Form: Form targeting `action="@externalLoginUrl" method="post"`.
  - Form tag: `<form action="@formAction" method="post" class="customer-auth-form">` containing:
    - `@Html.AntiForgeryToken()`
    - `<div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>`
    - `<input asp-for="FullName" class="form-control" />`
    - `<input asp-for="UserName" class="form-control" />`
    - `<input asp-for="Email" type="email" class="form-control" />`
    - `<input asp-for="PhoneNumber" class="form-control" />`
    - `<input asp-for="Address" class="form-control" />`
    - `<input asp-for="Password" type="password" class="form-control" />`
    - `<input asp-for="ConfirmPassword" type="password" class="form-control" />`
  - Script section rendering: `@section Scripts { <partial name="_ValidationScriptsPartial" /> }`

### 7. `Account/ResetPassword.cshtml`
- **Customer-facing**: Yes (Password reset confirmation)
- **Current Layout**: Inherits from `_ViewStart.cshtml` (`~/Views/Shared/_BranchCustomerLayout.cshtml`)
- **Main Bootstrap/styling classes**: `text-danger`, `mb-3`, `form-floating`, `form-control`, `form-label`, `w-100`, `btn`, `btn-lg`, `btn-primary`.
- **Razor Models, Forms, and Bindings**:
  - Model: `@model WebPhotocopyHub.Web.Models.ResetPasswordViewModel`
  - Form tag: `<form asp-action="ResetPassword" asp-route-branchSlug="@ViewData["Branch"]?.GetType().GetProperty("Slug")?.GetValue(ViewData["Branch"])" method="post">`
  - Bindings:
    - `<div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>`
    - `<input asp-for="Token" type="hidden" />`
    - `<input asp-for="Email" class="form-control" />`
    - `<input asp-for="Password" class="form-control" />`
    - `<input asp-for="ConfirmPassword" class="form-control" />`
  - Script section rendering: `@section Scripts { <partial name="_ValidationScriptsPartial" /> }`

---

## Home & Dashboard Views

### 8. `Branch/Index.cshtml`
- **Customer-facing**: Yes (Branch homepage and quick action index)
- **Current Layout**: Overrides to `~/Views/Shared/_BranchCustomerLayout.cshtml`
- **Main Bootstrap/styling classes**: `small`, `strong`, `em` (very minimal Bootstrap helper classes; mostly uses custom `bh-` prefixed flexbox and grid classes).
- **Razor Models, Forms, and Bindings**:
  - Model: `@model BranchHomeViewModel`
  - Key variables: URL generation via `CustomerBranchContext.ToPath(ViewContext, ...)` and `CustomerBranchContext.Home(ViewContext)`.
  - Properties: `Model.Branch.Name`, `Model.IsSignedIn`, `Model.QuickPrices`, `Model.RecentPrintJobs`, `Model.ActivePrintJobCount`.

### 9. `Dashboard/Index.cshtml`
- **Customer-facing**: Yes (Main Signed-in Customer Dashboard)
- **Current Layout**: Overrides to `~/Views/Shared/_BranchCustomerModernLayout.cshtml` (Tailwind layout)
- **Main Styling framework**: **Tailwind CSS Utility Classes** (`flex-1`, `p-md`, `grid`, `grid-cols-1`, `lg:grid-cols-12`, `bg-surface-container-low`, `rounded-xl`, `shadow-sm`, etc.) rather than Bootstrap.
- **Razor Models, Forms, and Bindings**:
  - Model: `@model DashboardViewModel`
  - Services: `@inject UserManager<ApplicationUser> UserManager`
  - Properties: `Model.PrintJobsCount`, `Model.ProductOrdersCount`, `Model.SupportOrdersCount`, `Model.PendingTopUpCount`, `Model.CurrentBalance`.
  - Key functions: uses `CustomerBranchContext.GetBranch(ViewContext)`, `CustomerBranchContext.Home(ViewContext)`, and `CustomerBranchContext.ToPath(ViewContext, ...)` to generate path-based routing.

---

## Customer Print Job Views

### 10. `PrintJobs/Create.cshtml`
- **Customer-facing**: Yes (Print job creation wizard)
- **Current Layout**: Overrides to `~/Views/Shared/_BranchCustomerLayout.cshtml`
- **Main Bootstrap/styling classes**: `alert`, `alert-danger`, `text-danger`, `btn`, `btn-sm`, `btn-outline-secondary`, `btn-primary`, `form-label`, `form-select`, `form-control`, `form-check-input`, `mt-3`, `pt-3`, `border-top`.
- **Razor Models, Forms, and Bindings**:
  - Model: `@model CreatePrintJobViewModel`
  - Form tag: `<form asp-route="shop-branch-customer" asp-route-branchSlug="@branchSlug" asp-route-controller="PrintJobs" asp-route-action="Create" method="post" enctype="multipart/form-data" id="printJobForm" data-page-count-url="..." data-office-preview-url="..." data-calculate-price-url="..." novalidate>`
  - Bindings:
    - `<input asp-for="IdempotencyKey" type="hidden" />`
    - `<div asp-validation-summary="All" class="alert alert-danger print-validation-summary" role="alert"></div>`
    - `<input asp-for="UploadFiles" class="print-file-input" type="file" multiple ... />`
    - `<select asp-for="PaperSize" class="form-select" asp-items="Html.GetEnumSelectList<PaperSize>()"></select>`
    - `<select asp-for="PrintSide" class="form-select" asp-items="Html.GetEnumSelectList<PrintSide>()"></select>`
    - `<select asp-for="ColorMode" class="form-select" asp-items="Html.GetEnumSelectList<ColorMode>()"></select>`
    - `<input asp-for="Copies" class="form-control" />`
    - `<input asp-for="IsPhoto" class="form-check-input" />`
    - `<select asp-for="DeliveryMethod" class="form-select" asp-items="Html.GetEnumSelectList<DeliveryMethod>()"></select>`
    - `<input asp-for="DeliveryAddress" class="form-control" />`
    - `<textarea asp-for="Notes" class="form-control"></textarea>`
  - Script section rendering: `@section Scripts { <partial name="_ValidationScriptsPartial" /> <script src="~/_content/WebPhotocopyHub.Web.Customer/js/printjob-create.js" asp-append-version="true"></script> }`

### 11. `PrintJobs/Details.cshtml`
- **Customer-facing**: Yes (Print job detail page)
- **Current Layout**: Overrides to `~/Views/Shared/_BranchCustomerLayout.cshtml`
- **Main Bootstrap/styling classes**: `btn`, `btn-outline-secondary`, `btn-primary`, `btn-danger`, `d-inline`, `alert`, `alert-success`, `badge`, `mt-3`.
- **Razor Models, Forms, and Bindings**:
  - Model: `@model PrintJob`
  - Cancel Form: `<form asp-action="Cancel" asp-route-id="@Model.Id" asp-route-branchSlug="@ViewContext.RouteData.Values["branchSlug"]" method="post" class="d-inline" ...>`
  - Properties: Model properties (e.g., `Model.Id`, `Model.Status`, `Model.TotalAmount`, etc.) are rendered in detail cards.
  - File preview iframe: `src="@CustomerBranchContext.ToPath(ViewContext, $"PrintJobs/PreviewFile/{Model.UploadedFileId}")"`

### 12. `PrintJobs/Files.cshtml`
- **Customer-facing**: Yes (Customer file management list)
- **Current Layout**: Overrides to `~/Views/Shared/_BranchCustomerLayout.cshtml`
- **Main Bootstrap/styling classes**: `btn`, `btn-primary`, `btn-outline-secondary`, `btn-outline-primary`, `btn-sm`, `table`, `text-end`, `fw-semibold`.
- **Razor Models, Forms, and Bindings**:
  - Model: `@model List<UploadedFileMetadata>`
  - Properties/Loops: Loop `@foreach (var item in Model)` utilizing `item.OriginalFileName`, `item.Size`, `item.ContentType`, `item.CreatedAt`, `item.Id`.

### 13. `PrintJobs/Index.cshtml`
- **Customer-facing**: Yes (List of customer print orders)
- **Current Layout**: Overrides to `~/Views/Shared/_BranchCustomerLayout.cshtml`
- **Main Bootstrap/styling classes**: `btn`, `btn-primary`, `btn-outline-secondary`, `btn-outline-primary`, `btn-sm`, `table`, `text-end`, `text-decoration-none`, `badge`, `fw-semibold`, `pagination`, `page-item`, `page-link`, `disabled`, `active`, `mt-4`, `d-flex`, `justify-content-center`.
- **Razor Models, Forms, and Bindings**:
  - Model: `@model WebPhotocopyHub.Application.DTOs.PagedResult<PrintJob>`
  - Properties/Loops: `@foreach (var item in Model.Items)` along with pagination controls (`Model.TotalPages`, `Model.HasPreviousPage`, `Model.PageNumber`, `Model.HasNextPage`, `Model.TotalCount`).

---

## Customer Product Views (Stationery)

### 14. `Products/Details.cshtml`
- **Customer-facing**: Yes (Detail of stationery order)
- **Current Layout**: Overrides to `~/Views/Shared/_BranchCustomerLayout.cshtml`
- **Main Bootstrap/styling classes**: `btn`, `btn-outline-secondary`, `btn-danger`, `d-inline`, `badge`, `table`, `text-end`, `fw-semibold`.
- **Razor Models, Forms, and Bindings**:
  - Model: `@model ProductOrder`
  - Cancel Form: `<form asp-action="Cancel" asp-route-id="@Model.Id" asp-route-branchSlug="@ViewContext.RouteData.Values["branchSlug"]" method="post" class="d-inline" ...>`
  - Properties/Loops: `@foreach (var line in Model.Items)` utilizing `line.Product.Name`, `line.UnitPrice`, `line.Quantity`, and `line.LineTotal`.

### 15. `Products/Index.cshtml`
- **Customer-facing**: Yes (Stationery purchase catalog)
- **Current Layout**: Overrides to `~/Views/Shared/_BranchCustomerLayout.cshtml`
- **Main Bootstrap/styling classes**: `btn`, `btn-outline-secondary`, `btn-primary`, `text-danger`, `form-label`, `form-control`, `form-select`, `mt-3`.
- **Razor Models, Forms, and Bindings**:
  - Model: `@model ProductCatalogViewModel`
  - Form tag: `<form asp-action="Index" method="post" class="cu-form" asp-route-branchSlug="@CustomerBranchContext.GetSlug(ViewContext)">`
  - Bindings:
    - `<input asp-for="IdempotencyKey" type="hidden" />`
    - `<div asp-validation-summary="ModelOnly" class="text-danger"></div>`
    - Catalog Items Loop: `@for (var i = 0; i < Model.Items.Count; i++)`
      - `<input asp-for="Items[i].ProductId" type="hidden" />`
      - `<input asp-for="Items[i].Name" type="hidden" />`
      - `<input asp-for="Items[i].Price" type="hidden" />`
      - `<input asp-for="Items[i].StockQuantity" type="hidden" />`
      - `<input asp-for="Items[i].Quantity" class="form-control" />`
    - `<select asp-for="DeliveryMethod" class="form-select" asp-items="Html.GetEnumSelectList<DeliveryMethod>()"></select>`
    - `<input asp-for="DeliveryAddress" class="form-control" />`
    - `<textarea asp-for="Notes" class="form-control" rows="3"></textarea>`
  - Script section rendering: `@section Scripts { <partial name="_ValidationScriptsPartial" /> }`

### 16. `Products/Orders.cshtml`
- **Customer-facing**: Yes (List of product orders)
- **Current Layout**: Overrides to `~/Views/Shared/_BranchCustomerLayout.cshtml`
- **Main Bootstrap/styling classes**: `btn`, `btn-primary`, `btn-outline-primary`, `btn-sm`, `table`, `text-end`, `fw-semibold`, `badge`, `pagination`, `page-item`, `page-link`, `disabled`, `active`, `mt-4`, `d-flex`, `justify-content-center`.
- **Razor Models, Forms, and Bindings**:
  - Model: `@model WebPhotocopyHub.Application.DTOs.PagedResult<ProductOrder>`
  - Properties/Loops: Loop `@foreach (var item in Model.Items)` along with pagination controls (`Model.TotalPages`, `Model.HasPreviousPage`, `Model.PageNumber`, `Model.HasNextPage`, `Model.TotalCount`).

---

## Customer Profile Views

### 17. `Profile/ChangePassword.cshtml`
- **Customer-facing**: Yes (Change account password form)
- **Current Layout**: Overrides to `~/Views/Shared/_BranchCustomerLayout.cshtml`
- **Main Bootstrap/styling classes**: `row`, `col-lg-6`, `text-danger`, `mb-3`, `form-label`, `form-control`, `small`, `d-flex`, `gap-2`, `mt-4`.
- **Razor Models, Forms, and Bindings**:
  - Model: `@model WebPhotocopyHub.Web.Models.ChangePasswordViewModel`
  - Form tag: `<form asp-action="ChangePassword" method="post" novalidate>` containing:
    - `<div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>`
    - `<input asp-for="CurrentPassword" class="form-control" />`
    - `<input asp-for="NewPassword" class="form-control" />`
    - `<input asp-for="ConfirmPassword" class="form-control" />`
  - Script section rendering: `@section Scripts { <partial name="_ValidationScriptsPartial" /> }`

### 18. `Profile/Index.cshtml`
- **Customer-facing**: Yes (Manage customer contact/profile information)
- **Current Layout**: Overrides to `~/Views/Shared/_BranchCustomerLayout.cshtml`
- **Main Bootstrap/styling classes**: `btn`, `btn-primary`, `btn-outline-secondary`, `btn-outline-warning`, `text-danger`, `form-label`, `form-control`, `small`, `form-text`, `text-muted`.
- **Razor Models, Forms, and Bindings**:
  - Model: `@model ProfileViewModel`
  - Form tag: `<form asp-action="Index" method="post" class="cu-form customer-profile-form" asp-route-branchSlug="@CustomerBranchContext.GetSlug(ViewContext)">`
  - Bindings:
    - `<div asp-validation-summary="ModelOnly" class="text-danger"></div>`
    - `<input asp-for="FullName" class="form-control" />`
    - `<input asp-for="UserName" class="form-control" />`
    - `<input asp-for="Email" class="form-control" readonly />` (Read-only input)
    - `<input asp-for="PhoneNumber" class="form-control" />`
    - `<textarea asp-for="Address" class="form-control" rows="4"></textarea>`
  - Script section rendering: `@section Scripts { <partial name="_ValidationScriptsPartial" /> }`

---

## Customer Support Views

### 19. `SupportOrders/Create.cshtml`
- **Customer-facing**: Yes (Submit a scan/plastic/binding support request)
- **Current Layout**: Overrides to `~/Views/Shared/_BranchCustomerLayout.cshtml`
- **Main Bootstrap/styling classes**: `btn`, `btn-outline-secondary`, `btn-primary`, `text-danger`, `form-label`, `form-select`, `form-control`, `small`.
- **Razor Models, Forms, and Bindings**:
  - Model: `@model CreateSupportOrderViewModel`
  - Form tag: `<form asp-action="Create" method="post" class="cu-form" asp-route-branchSlug="@CustomerBranchContext.GetSlug(ViewContext)">`
  - Bindings:
    - `<input asp-for="IdempotencyKey" type="hidden" />`
    - `<div asp-validation-summary="ModelOnly" class="text-danger"></div>`
    - `<select asp-for="SupportServiceId" class="form-select">` with custom option loop `@foreach (var item in Model.AvailableServices)`
    - `<input asp-for="Quantity" class="form-control" />`
    - `<textarea asp-for="Notes" class="form-control" rows="4"></textarea>`
  - Script section rendering: `@section Scripts { <partial name="_ValidationScriptsPartial" /> }`

### 20. `SupportOrders/Details.cshtml`
- **Customer-facing**: Yes (Detail of support service order)
- **Current Layout**: Overrides to `~/Views/Shared/_BranchCustomerLayout.cshtml`
- **Main Bootstrap/styling classes**: `btn`, `btn-outline-secondary`, `btn-danger`, `d-inline`, `badge`.
- **Razor Models, Forms, and Bindings**:
  - Model: `@model SupportServiceOrder`
  - Cancel Form: `<form asp-action="Cancel" asp-route-id="@Model.Id" asp-route-branchSlug="@branchSlug" method="post" class="d-inline" ...>`
  - Properties: `Model.Id`, `Model.Status`, `Model.TotalAmount`, `Model.SupportService.Name`, `Model.UnitPrice`, `Model.Quantity`, `Model.CreatedAt`, `Model.Notes`, `Model.ProcessNote`.

### 21. `SupportOrders/History.cshtml`
- **Customer-facing**: Yes (List of customer support orders)
- **Current Layout**: Overrides to `~/Views/Shared/_BranchCustomerLayout.cshtml`
- **Main Bootstrap/styling classes**: `btn`, `btn-primary`, `btn-sm`, `table`, `text-end`, `fw-semibold`, `badge`, `pagination`, `page-item`, `page-link`, `disabled`, `active`, `mt-4`, `d-flex`, `justify-content-center`.
- **Razor Models, Forms, and Bindings**:
  - Model: `@model WebPhotocopyHub.Application.DTOs.PagedResult<SupportServiceOrder>`
  - Properties/Loops: Loop `@foreach (var item in Model.Items)` along with pagination controls (`Model.TotalPages`, `Model.HasPreviousPage`, `Model.PageNumber`, `Model.HasNextPage`, `Model.TotalCount`).

---

## Customer Wallet Views

### 22. `Wallet/Index.cshtml`
- **Customer-facing**: Yes (Wallet balance, transaction history, stats cards)
- **Current Layout**: Overrides to `~/Views/Shared/_BranchCustomerLayout.cshtml`
- **Main Bootstrap/styling classes**: `btn`, `btn-primary`, `btn-outline-secondary`, `table`, `text-end`, `text-success`, `text-danger`, `pagination`, `page-item`, `page-link`, `disabled`, `active`, `mt-4`, `d-flex`, `justify-content-center`.
- **Razor Models, Forms, and Bindings**:
  - Model: `@model WalletIndexViewModel`
  - Loops: `@foreach (var tx in Model.Transactions.Items)` (renders `tx.CreatedAt`, `tx.TransactionType.GetDisplayName()`, `tx.Amount`, `tx.BalanceBefore`, `tx.BalanceAfter`, `tx.Note`).
  - Statistics/Paging: Uses `Model.CurrentBalance`, calculated sums `creditTotal`/`debitTotal`, and paging properties (`Model.Transactions.TotalCount`, etc.).

### 23. `Wallet/TopUp.cshtml`
- **Customer-facing**: Yes (Form to submit top-up transfer request)
- **Current Layout**: Overrides to `~/Views/Shared/_BranchCustomerLayout.cshtml`
- **Main Bootstrap/styling classes**: `btn`, `btn-outline-secondary`, `btn-primary`, `text-danger`, `form-label`, `form-control`, `small`.
- **Razor Models, Forms, and Bindings**:
  - Model: `@model TopUpPageViewModel`
  - Form tag: `<form asp-action="TopUp" method="post" enctype="multipart/form-data" class="cu-form" asp-route-branchSlug="@CustomerBranchContext.GetSlug(ViewContext)">`
  - Bindings:
    - `<input asp-for="Form.IdempotencyKey" type="hidden" />`
    - `<div asp-validation-summary="ModelOnly" class="text-danger"></div>`
    - `<input asp-for="Form.Amount" class="form-control" />`
    - `<input asp-for="Form.TransferContent" class="form-control" />`
    - `<input asp-for="Form.TransactionReferenceCode" class="form-control" />`
    - `<input asp-for="Form.ProofFile" class="form-control" />` (file input for deposit receipt)
  - Script section rendering: `@section Scripts { <partial name="_ValidationScriptsPartial" /> }`

### 24. `Wallet/TopUpHistory.cshtml`
- **Customer-facing**: Yes (List of submitted top-up requests)
- **Current Layout**: Overrides to `~/Views/Shared/_BranchCustomerLayout.cshtml`
- **Main Bootstrap/styling classes**: `btn`, `btn-primary`, `btn-outline-secondary`, `table`, `text-end`, `fw-semibold`, `badge`.
- **Razor Models, Forms, and Bindings**:
  - Model: `@model List<TopUpRequest>`
  - Properties/Loops: `@foreach (var item in Model)` utilizing `item.CreatedAt`, `item.Amount`, `item.TransferContent`, `item.TransactionReferenceCode`, `item.Status`, `item.ReviewNote`.

---

## Layouts & Shared Components

### 25. `Shared/Components/CustomerHeaderNotifications/Default.cshtml`
- **Customer-facing**: Yes (Notifications Dropdown View Component in header)
- **Current Layout**: Inherits from layout frame (rendered as ViewComponent)
- **Main Bootstrap/styling classes**: None (uses custom `customer-notification` styling scheme)
- **Razor Models, Forms, and Bindings**:
  - Model: `@model CustomerHeaderNotificationsViewModel`
  - Loops: `@foreach (var item in Model.Items)` (renders `item.Url`, `item.Tone`, `item.Icon`, `item.Title`, `item.Description`, `item.TimeText`).
  - Properties: `Model.AttentionCount`, `Model.AllNotificationsUrl`.

### 26. `Shared/_Alert.cshtml`
- **Customer-facing**: Yes (Shared notification alert partial)
- **Current Layout**: Inherits from host view
- **Main Bootstrap/styling classes**: `alert`, `alert-success`, `alert-dismissible`, `fade`, `show`, `btn-close`, `alert-danger`.
- **Razor Models, Forms, and Bindings**:
  - Conditions: Checks `TempData["Success"]` and `TempData["Error"]`.

### 27. `Shared/_BranchCustomerLayout.cshtml`
- **Customer-facing**: Yes (Standard Page Layout Frame)
- **Current Layout**: Top-Level Main Layout
- **Main Bootstrap/styling classes**: Loads **Bootstrap v5.3.3 CSS & JS bundle**. Uses Bootstrap classes such as `dropdown`, `dropdown-toggle`, `dropdown-menu`, `dropdown-menu-end`, `btn-close` (rest are customized theme classes prefixed with `customer-` or `cu-`).
- **Razor Models, Forms, and Bindings**:
  - Services: Injects `UserManager<ApplicationUser>`, `IBranchContext`, and `IBranchManagementService`.
  - Form tag: `<form asp-controller="Account" asp-action="Logout" asp-area="" method="post" class="customer-account-logout-form">`
  - Navigation elements: features active state detection via helper functions `InController` and `ActiveGroup`.
  - Body rendering: `@RenderBody()` and script section `@await RenderSectionAsync("Scripts", required: false)`.

### 28. `Shared/_BranchCustomerModernLayout.cshtml`
- **Customer-facing**: Yes (Tailwind CSS Page Layout Frame for modern pages)
- **Current Layout**: Top-Level Main Layout
- **Main Styling framework**: **Tailwind CSS** (configured with customized material/surface colors in a JavaScript configuration block).
- **Razor Models, Forms, and Bindings**:
  - Services: Injects `UserManager<ApplicationUser>`, `IBranchContext`, and `IBranchManagementService`.
  - View Component call: `@await Component.InvokeAsync("CustomerHeaderNotifications", new { branchSlug })`
  - Form tag: `<form asp-controller="Account" asp-action="Logout" asp-area="" method="post" class="customer-account-logout">`
  - Body rendering: `@RenderBody()` and script section `@await RenderSectionAsync("Scripts", required: false)`.

### 29. `Shared/_ValidationScriptsPartial.cshtml`
- **Customer-facing**: Yes (Shared client-side validation libraries)
- **Current Layout**: Inherits from host view
- **Main Bootstrap/styling classes**: None
- **Razor Models, Forms, and Bindings**: Loads external validation CDN scripts (jQuery, jQuery Validate, and jQuery Unobtrusive).
