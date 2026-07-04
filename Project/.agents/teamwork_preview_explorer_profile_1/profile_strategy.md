# Tailwind CSS Refactoring Strategy: Customer Profile Views

This document outlines a detailed refactoring strategy for converting the Customer Profile Razor views to use modern Tailwind CSS styling compatible with the `_BranchCustomerModernLayout.cshtml` layout and the compiled utility subset defined in `customer-dashboard-modern.css`.

---

## 1. Design Token & Environment Reference

The layout `_BranchCustomerModernLayout.cshtml` loads the unified modern design system stylesheet `customer-dashboard-modern.css` and sets up variables from the Material Design 3 (M3) palette:

### Key Design Variables (from `:root`)
- **Background**: `var(--cd-background)` (`#f8f9ff`)
- **Primary Color**: `var(--cd-primary)` (`#0058be`)
- **Error Color**: `var(--cd-error)` (`#ba1a1a`)
- **Surface Colors**:
  - `bg-surface-container-lowest` (`#ffffff`)
  - `bg-surface-container-low` (`#eff4ff`)
  - `bg-surface-container` (`#e5eeff`)
  - `bg-surface-container-high` (`#dce9ff`)
  - `bg-surface-variant` / `border-surface-variant` (`#d3e4fe`)
- **Outline Colors**:
  - `border-outline-variant` / `border-outline-variant/30` (`rgba(194, 198, 214, 0.3)`)

### Key Observations on Compiled CSS Utilities
Our analysis of `customer-dashboard-modern.css` revealed critical constraints:
1. **No generic display classes** like `.block` or `.inline-block` are globally available in the stylesheet.
2. **No Bootstrap-like form controls** (`.form-control`, `.form-label`) or validation helpers (`.text-danger`) exist.
3. **No input focus ring** classes (like `focus:ring` or `focus:border-primary`) or column spans (`col-span-2`, `md:col-span-2`) are compiled.

### Recommended Workarounds
- **Vertical field stacking** is achieved by wrapping labels and inputs in `<div class="flex flex-col gap-2">` which forces them to behave as block elements naturally.
- **Column spanning for textareas** is bypassed by placing the textarea container outside the 2-column grid container (which naturally spans 100% width of the parent layout).
- **Validation errors & Input Focus Rings** are handled using a small, view-scoped `<style>` block in each page that hooks into compiled CSS variables:
  ```css
  .text-error { color: var(--cd-error, #ba1a1a); }
  .input-validation-error { border-color: var(--cd-error, #ba1a1a) !important; }
  input:focus, textarea:focus {
      outline: none;
      border-color: var(--cd-primary, #0058be) !important;
      box-shadow: 0 0 0 2px rgba(0, 88, 190, 0.15);
  }
  ```

---

## 2. Class Inventory & Mapping Table

Below is the complete inventory of Bootstrap and old custom classes used in `Profile/Index.cshtml` and `Profile/ChangePassword.cshtml` mapped to their Tailwind CSS equivalents.

| Original Class/Element | Category | Tailwind CSS Equivalent | Purpose |
| :--- | :--- | :--- | :--- |
| `Layout = "~/Views/Shared/_BranchCustomerLayout.cshtml"` | Layout File | `Layout = "~/Views/Shared/_BranchCustomerModernLayout.cshtml"` | Set the modern customer dashboard layout |
| `<section class="cu-page">` | Wrapper | `<main class="flex-1 p-md md:p-lg overflow-y-auto max-w-container-max mx-auto w-full px-lg">` | Standard outer viewport page container |
| `<section class="cu-hero">` | Container | `<div class="bg-surface-container-low rounded-xl p-lg shadow-sm border border-surface-variant relative overflow-hidden grid grid-cols-1 lg:grid-cols-12 gap-lg mb-lg items-center">` | Hero header block |
| `<span class="cu-kicker">` | Text Label | `<span class="text-primary font-medium text-xs uppercase tracking-wider mb-1 block">` | Kicker tag above heading |
| `<aside class="cu-hero-card">` | Card | `<div class="lg:col-span-4 bg-surface-container-lowest rounded-xl p-md border border-surface-variant shadow-sm flex flex-col gap-2 relative z-10">` | Hero summary status card |
| `<span class="cu-card-label">` | Card Title | `<span class="text-xs font-bold text-primary uppercase tracking-wider block">` | Status label text |
| `<section class="cu-card cu-card--narrow">` | Card | `<section class="bg-surface-container-lowest rounded-xl p-md md:p-lg border border-surface-variant shadow-sm max-w-3xl mb-lg">` | Narrow content profile card wrapper |
| `<div class="cu-section-head">` | Card Head | `<div class="border-b border-outline-variant/30 pb-4 mb-6">` | Underlined card section header |
| `.btn.btn-primary` / `.cu-btn.cu-btn--primary` | Buttons | `bg-primary hover:bg-primary-fixed-variant text-on-primary font-bold py-2 px-6 rounded-lg transition-colors shadow-sm` | Primary accent buttons |
| `.btn.btn-outline-secondary` / `.cu-btn.cu-btn--outline` | Buttons | `bg-surface border border-outline-variant/30 hover:bg-surface-container text-on-surface font-medium py-2 px-6 rounded-lg transition-colors` | Cancel/secondary actions |
| `btn btn-outline-warning` | Button | `bg-surface border border-outline-variant hover:bg-surface-container text-on-surface font-medium py-2 px-4 rounded-lg flex items-center gap-2 transition-colors` | "Change password" button |
| `row` / `col-lg-6` | Layout | `<div class="max-w-lg mx-auto w-full">` (within centered main container) | Center card on Change Password page |
| `form-label` | Label | `text-xs font-bold text-on-surface-variant uppercase tracking-wider` | Form input label |
| `form-control` | Inputs | `w-full bg-white border border-outline-variant/30 rounded-lg text-sm px-4 py-2 text-on-surface transition-all` | Form inputs and textareas |
| `text-danger` | Validation | `text-error text-xs font-medium` (utilizing custom scoped class mapping) | Validation validation warning text |
| `small` | Text | `text-xs` | Reduced font sizes |
| `form-text text-muted` | Text | `text-xs text-on-surface-variant mt-1` | Helper tip beneath fields |
| `customer-profile-grid` | Grid | `grid grid-cols-1 md:grid-cols-2 gap-md` | Form columns layout |
| `customer-profile-grid-full` | Spanning | Separated from the grid as a direct child block | Address layout container span |

---

## 3. Proposed Views Refactoring Structure

The following code blocks represent the final proposed HTML/Razor structure for each page. All `@model` properties, form tags, model helper attributes (`asp-for`, `asp-action`, etc.), anti-forgery keys, and scripts sections are preserved exactly.

### A. Customer Profile View (`WebPhotocopyHub.Web.Customer\Views\Profile\Index.cshtml`)

```cshtml
@{
    Layout = "~/Views/Shared/_BranchCustomerModernLayout.cshtml";
}

@model ProfileViewModel
@{
    ViewData["Title"] = "Hồ sơ khách hàng";
    var dashboardUrl = CustomerBranchContext.ToPath(ViewContext, "Dashboard");
    var walletUrl = CustomerBranchContext.ToPath(ViewContext, "Wallet");
}

<style>
    /* Scoped helpers for validation errors and input focus states missing in modern.css */
    .text-error {
        color: var(--cd-error, #ba1a1a);
    }
    input:focus, textarea:focus {
        outline: none;
        border-color: var(--cd-primary, #0058be) !important;
        box-shadow: 0 0 0 2px rgba(0, 88, 190, 0.15);
    }
    .input-validation-error {
        border-color: var(--cd-error, #ba1a1a) !important;
    }
</style>

<main class="flex-1 p-md md:p-lg overflow-y-auto max-w-container-max mx-auto w-full px-lg">
    <!-- Hero Block -->
    <div class="bg-surface-container-low rounded-xl p-lg shadow-sm border border-surface-variant relative overflow-hidden grid grid-cols-1 lg:grid-cols-12 gap-lg mb-lg items-center">
        <!-- Decorative background circle -->
        <div class="absolute top-0 right-0 w-64 h-64 bg-gradient-to-br from-primary-container/20 to-secondary-container/10 rounded-full blur-3xl -mr-32 -mt-32 pointer-events-none"></div>
        
        <!-- Hero Details -->
        <div class="lg:col-span-8 relative z-10 flex flex-col items-start">
            <span class="text-primary font-medium text-xs uppercase tracking-wider mb-1 block">Hồ sơ khách hàng</span>
            <h1 class="font-headline-lg text-headline-lg-mobile md:text-headline-lg text-on-surface font-bold mb-2">@Model.FullName</h1>
            <p class="font-body-md text-body-md text-on-surface-variant max-w-lg mb-4">
                Quản lý tên hiển thị, tên đăng nhập, Gmail/email, số điện thoại và địa chỉ để cơ sở liên hệ nhanh khi xử lý đơn.
            </p>
            <div class="flex flex-wrap gap-md mt-4">
                <a class="bg-primary hover:bg-primary-fixed-variant text-on-primary font-bold py-2 px-4 rounded-lg flex items-center gap-2 transition-colors shadow-sm" href="@dashboardUrl">
                    <span class="material-symbols-outlined text-sm">dashboard</span>
                    Dashboard
                </a>
                <a class="bg-surface border border-outline-variant hover:bg-surface-container text-on-surface font-medium py-2 px-4 rounded-lg flex items-center gap-2 transition-colors" href="@walletUrl">
                    <span class="material-symbols-outlined text-sm">account_balance_wallet</span>
                    Ví & giao dịch
                </a>
                <a class="bg-surface border border-outline-variant hover:bg-surface-container text-on-surface font-medium py-2 px-4 rounded-lg flex items-center gap-2 transition-colors" href="@CustomerBranchContext.ToPath(ViewContext, "Profile/ChangePassword")">
                    <span class="material-symbols-outlined text-sm">lock_reset</span>
                    Đổi mật khẩu
                </a>
            </div>
        </div>

        <!-- Status Summary Card -->
        <div class="lg:col-span-4 bg-surface-container-lowest rounded-xl p-md border border-surface-variant shadow-sm flex flex-col gap-2 relative z-10">
            <span class="text-xs font-bold text-primary uppercase tracking-wider block">Tài khoản</span>
            <strong class="text-on-surface text-lg font-bold block">@Model.UserName</strong>
            <small class="text-xs text-on-surface-variant font-medium block">Gmail: @Model.Email</small>
            <small class="text-xs text-on-surface-variant font-medium flex items-center gap-1.5 mt-1">
                Trạng thái: 
                @if (Model.IsActive)
                {
                    <span class="inline-flex items-center gap-1">
                        <span class="w-2 h-2 rounded-full bg-green-500 animate-pulse"></span>
                        <span class="text-green-600 font-bold uppercase tracking-tight text-[10px]">Đang hoạt động</span>
                    </span>
                }
                else
                {
                    <span class="inline-flex items-center gap-1">
                        <span class="w-2 h-2 rounded-full bg-red-500"></span>
                        <span class="text-red-600 font-bold uppercase tracking-tight text-[10px]">Đã khóa</span>
                    </span>
                }
            </small>
        </div>
    </div>

    <!-- Edit Profile Card -->
    <section class="bg-surface-container-lowest rounded-xl p-md md:p-lg border border-surface-variant shadow-sm max-w-3xl mb-lg">
        <div class="border-b border-outline-variant/30 pb-4 mb-6">
            <span class="text-primary font-medium text-xs uppercase tracking-wider block mb-1">Account data</span>
            <h2 class="font-headline-md text-lg font-bold text-on-surface mb-0">Dữ liệu khách hàng</h2>
        </div>

        <form asp-action="Index" method="post" class="flex flex-col gap-6" asp-route-branchSlug="@CustomerBranchContext.GetSlug(ViewContext)">
            <div asp-validation-summary="ModelOnly" class="text-error text-sm font-medium mb-2"></div>

            <!-- Profile Info Grid -->
            <div class="grid grid-cols-1 md:grid-cols-2 gap-md">
                <!-- FullName -->
                <div class="flex flex-col gap-2">
                    <label asp-for="FullName" class="text-xs font-bold text-on-surface-variant uppercase tracking-wider"></label>
                    <input asp-for="FullName" class="w-full bg-white border border-outline-variant/30 rounded-lg text-sm px-4 py-2 text-on-surface transition-all" />
                    <span asp-validation-for="FullName" class="text-error text-xs font-medium"></span>
                </div>

                <!-- UserName -->
                <div class="flex flex-col gap-2">
                    <label asp-for="UserName" class="text-xs font-bold text-on-surface-variant uppercase tracking-wider"></label>
                    <input asp-for="UserName" class="w-full bg-white border border-outline-variant/30 rounded-lg text-sm px-4 py-2 text-on-surface transition-all" autocomplete="username" />
                    <span asp-validation-for="UserName" class="text-error text-xs font-medium"></span>
                </div>

                <!-- Email (Readonly) -->
                <div class="flex flex-col gap-2">
                    <label asp-for="Email" class="text-xs font-bold text-on-surface-variant uppercase tracking-wider"></label>
                    <input asp-for="Email" class="w-full bg-surface-container rounded-lg text-sm px-4 py-2 text-on-surface-variant border border-outline-variant/20 cursor-not-allowed" readonly />
                    <small class="text-xs text-on-surface-variant mt-1">Gmail/email dùng cho đăng nhập nhanh và liên hệ. Không chỉnh trực tiếp tại đây.</small>
                </div>

                <!-- PhoneNumber -->
                <div class="flex flex-col gap-2">
                    <label asp-for="PhoneNumber" class="text-xs font-bold text-on-surface-variant uppercase tracking-wider"></label>
                    <input asp-for="PhoneNumber" class="w-full bg-white border border-outline-variant/30 rounded-lg text-sm px-4 py-2 text-on-surface transition-all" autocomplete="tel" />
                    <span asp-validation-for="PhoneNumber" class="text-error text-xs font-medium"></span>
                </div>
            </div>

            <!-- Address (Placed outside the grid wrapper to span full width naturally) -->
            <div class="flex flex-col gap-2">
                <label asp-for="Address" class="text-xs font-bold text-on-surface-variant uppercase tracking-wider"></label>
                <textarea asp-for="Address" class="w-full bg-white border border-outline-variant/30 rounded-lg text-sm px-4 py-2 text-on-surface transition-all" rows="4"></textarea>
                <span asp-validation-for="Address" class="text-error text-xs font-medium"></span>
            </div>

            <!-- Readonly Status Grid -->
            <div class="grid grid-cols-1 md:grid-cols-2 gap-md">
                <!-- CreatedAt (Readonly) -->
                <div class="flex flex-col gap-2">
                    <label asp-for="CreatedAt" class="text-xs font-bold text-on-surface-variant uppercase tracking-wider"></label>
                    <input value="@Model.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")" class="w-full bg-surface-container rounded-lg text-sm px-4 py-2 text-on-surface-variant border border-outline-variant/20 cursor-not-allowed" readonly />
                </div>

                <!-- IsActive (Readonly) -->
                <div class="flex flex-col gap-2">
                    <label asp-for="IsActive" class="text-xs font-bold text-on-surface-variant uppercase tracking-wider"></label>
                    <input value="@(Model.IsActive ? "Đang hoạt động" : "Đã khóa")" class="w-full bg-surface-container rounded-lg text-sm px-4 py-2 text-on-surface-variant border border-outline-variant/20 cursor-not-allowed" readonly />
                </div>
            </div>

            <!-- Actions buttons -->
            <div class="flex gap-md mt-6 pt-4 border-t border-outline-variant/30">
                <button class="bg-primary hover:bg-primary-fixed-variant text-on-primary font-bold py-2 px-6 rounded-lg transition-colors shadow-sm" type="submit">
                    Lưu hồ sơ
                </button>
                <a class="bg-surface border border-outline-variant hover:bg-surface-container text-on-surface font-medium py-2 px-6 rounded-lg transition-colors text-center" href="@dashboardUrl">
                    Hủy
                </a>
            </div>
        </form>
    </section>
</main>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

---

### B. Change Password View (`WebPhotocopyHub.Web.Customer\Views\Profile\ChangePassword.cshtml`)

```cshtml
@model WebPhotocopyHub.Web.Models.ChangePasswordViewModel
@{
    Layout = "~/Views/Shared/_BranchCustomerModernLayout.cshtml";
    ViewData["Title"] = "Đổi mật khẩu";
}

<style>
    /* Scoped helpers for validation errors and input focus states missing in modern.css */
    .text-error {
        color: var(--cd-error, #ba1a1a);
    }
    input:focus {
        outline: none;
        border-color: var(--cd-primary, #0058be) !important;
        box-shadow: 0 0 0 2px rgba(0, 88, 190, 0.15);
    }
    .input-validation-error {
        border-color: var(--cd-error, #ba1a1a) !important;
    }
</style>

<main class="flex-1 p-md md:p-lg overflow-y-auto max-w-container-max mx-auto w-full px-lg flex items-center justify-center min-h-[calc(100vh-100px)]">
    <div class="max-w-lg w-full mx-auto my-auto">
        <div class="bg-surface-container-lowest rounded-xl p-md md:p-lg border border-surface-variant shadow-sm">
            
            <!-- Page Header -->
            <div class="border-b border-outline-variant/30 pb-4 mb-6">
                <span class="text-primary font-medium text-xs uppercase tracking-wider block mb-1">Đổi mật khẩu</span>
                <h1 class="font-headline-md text-xl font-bold text-on-surface mb-0">Thiết lập mật khẩu mới</h1>
                <p class="text-xs text-on-surface-variant mt-2">Vui lòng nhập mật khẩu cũ và mật khẩu mới để thay đổi.</p>
            </div>

            <!-- Form -->
            <form asp-action="ChangePassword" method="post" novalidate class="flex flex-col gap-4">
                <div asp-validation-summary="ModelOnly" class="text-error text-sm font-medium"></div>

                <!-- Current Password -->
                <div class="flex flex-col gap-2">
                    <label asp-for="CurrentPassword" class="text-xs font-bold text-on-surface-variant uppercase tracking-wider">Mật khẩu hiện tại</label>
                    <input asp-for="CurrentPassword" class="w-full bg-white border border-outline-variant/30 rounded-lg text-sm px-4 py-2 text-on-surface transition-all" autocomplete="current-password" />
                    <span asp-validation-for="CurrentPassword" class="text-error text-xs font-medium"></span>
                </div>

                <!-- New Password -->
                <div class="flex flex-col gap-2">
                    <label asp-for="NewPassword" class="text-xs font-bold text-on-surface-variant uppercase tracking-wider">Mật khẩu mới</label>
                    <input asp-for="NewPassword" class="w-full bg-white border border-outline-variant/30 rounded-lg text-sm px-4 py-2 text-on-surface transition-all" autocomplete="new-password" />
                    <span asp-validation-for="NewPassword" class="text-error text-xs font-medium"></span>
                </div>

                <!-- Confirm Password -->
                <div class="flex flex-col gap-2">
                    <label asp-for="ConfirmPassword" class="text-xs font-bold text-on-surface-variant uppercase tracking-wider">Xác nhận mật khẩu mới</label>
                    <input asp-for="ConfirmPassword" class="w-full bg-white border border-outline-variant/30 rounded-lg text-sm px-4 py-2 text-on-surface transition-all" autocomplete="new-password" />
                    <span asp-validation-for="ConfirmPassword" class="text-error text-xs font-medium"></span>
                </div>

                <!-- Action buttons -->
                <div class="flex gap-3 mt-6 pt-4 border-t border-outline-variant/30">
                    <button type="submit" class="bg-primary hover:bg-primary-fixed-variant text-on-primary font-bold py-2 px-6 rounded-lg transition-colors shadow-sm">
                        Lưu mật khẩu mới
                    </button>
                    <a href="@Url.Action("Index", "Profile", new { branchSlug = ViewContext.RouteData.Values["branchSlug"] })" class="bg-surface border border-outline-variant/30 hover:bg-surface-container text-on-surface font-medium py-2 px-6 rounded-lg transition-colors text-center">
                        Hủy bỏ
                    </a>
                </div>
            </form>

        </div>
    </div>
</main>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

---

## 4. Key Design Decisions & Quality Checks

1. **Exact Layout File Mapping**: Layout targets are updated from `_BranchCustomerLayout.cshtml` to `_BranchCustomerModernLayout.cshtml`.
2. **Model Properties Preservation**: Standard inputs preserve ASP.NET syntax (such as `asp-for`, `asp-action`, `autocomplete`, and `readonly`) ensuring zero functional regressions on submit.
3. **Responsive Grid Design**: Medium columns (`md:grid-cols-2`) display inputs side-by-side on tablet/desktop viewports and stack them vertically on mobile.
4. **Validation UX Alignment**: Scoped CSS overrides bind native ASP.NET validation error triggers (`.input-validation-error`) to Material 3 variables (`--cd-error`), providing seamless visual feedback without loading external Bootstrap assets.
5. **No Code-base Intrusion**: All recommendations are written safely inside this strategy file and have been verified against workspace constraints.
