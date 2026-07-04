# Refactoring Strategy: Tailwind CSS Modernization for Customer Transactional Views

## Executive Summary
This document provides a detailed refactoring strategy to transition 13 legacy Razor views in the Customer Transactional Core modules from Bootstrap 5 and custom `.cu-` legacy styles to Tailwind CSS. All refactored views will be fully compatible with `_BranchCustomerModernLayout.cshtml` and follow the modern dashboard design system. 

Key goals include maintaining exact Razor bindings, model structures, CSRF tokens, client-side scripts, and AJAX event handlers, while replacing legacy utility and component classes with Tailwind CSS utility classes.

---

## 1. Analysis of Legacy Classes (Bootstrap & Custom)

### 1.1 Detected Bootstrap Classes
The following Bootstrap classes are used across the 13 views:
*   **Grid & Layout**: `d-inline`, `d-flex`, `justify-content-center`, `text-end`
*   **Spacers & Borders**: `mt-3`, `mt-4`, `pt-3`, `border-top`
*   **Alerts**: `alert`, `alert-danger`, `alert-success`
*   **Buttons**: `btn`, `btn-primary`, `btn-outline-secondary`, `btn-outline-primary`, `btn-danger`, `btn-sm`
*   **Tables**: `table`
*   **Typography**: `fw-semibold`, `text-danger`, `text-success`, `text-decoration-none`
*   **Forms**: `form-label`, `form-select`, `form-control`, `form-check-input`
*   **Pagination**: `pagination`, `page-item`, `page-link`, `active`, `disabled`

### 1.2 Legacy Custom `.cu-` Classes
The views rely on `customer-role-ui.css` selectors which need to be translated to Tailwind layout systems:
*   `cu-page`, `cu-hero`, `cu-kicker`, `cu-hero-card`, `cu-card-label`, `cu-actions`, `cu-code`
*   `cu-grid`, `cu-grid--detail`, `cu-card`, `cu-section-head`, `cu-detail-list`, `cu-detail-list__full`
*   `cu-stat-grid`, `cu-stat-card`, `cu-stat-icon`
*   `cu-table-wrap`, `cu-table`, `cu-empty`
*   `cu-pagination`
*   Specific layout elements: `print-workspace`, `print-form-shell`, `print-summary-card`, `upload-dropzone`, `quick-preset-grid`, `quick-preset`, `print-config-grid`, `delivery-grid`, `print-submit-bar`, `document-preview-dialog`.

---

## 2. Tailwind CSS Unified Mapping System

To ensure consistency across all modernized views, the following design tokens and utility class mappings must be used:

### 2.1 Core Container & Layout Mappings
| Legacy Class / Layout | Target Tailwind CSS Utility Classes | Rationale |
| :--- | :--- | :--- |
| `cu-page` | `flex-1 p-md md:p-lg overflow-y-auto max-w-container-max mx-auto w-full px-lg flex flex-col gap-lg` | Main page view wrapper matching `Dashboard/Index` |
| `cu-hero` / `page-hero` | `bg-surface-container-low rounded-xl p-lg border border-surface-variant/20 shadow-sm relative overflow-hidden flex flex-col lg:flex-row justify-between items-start lg:items-center gap-md` | Modernized page header banner card |
| `cu-kicker` | `text-primary font-bold text-xs uppercase tracking-wider mb-1 block` | Tiny uppercase lead kicker text |
| `cu-hero h1` | `text-2xl md:text-3xl font-bold text-on-surface mb-2` | Large headline text |
| `cu-hero p` | `text-sm text-on-surface-variant max-w-2xl` | Standard description text |
| `cu-actions` / `cu-actions mt-3` | `flex flex-wrap gap-3 mt-4` | Button grouping layout |
| `cu-hero-card` / `print-create-hero__note` | `bg-surface-container-lowest border border-surface-variant/20 rounded-xl p-5 shadow-sm min-w-[240px] flex flex-col gap-2 relative z-10` | Side card panel within hero layout |
| `cu-card` | `bg-surface-container-lowest border border-surface-variant/20 rounded-xl p-md md:p-lg shadow-sm flex flex-col gap-md` | Standard dashboard content card |
| `cu-section-head` | `border-b border-outline-variant/20 pb-4 mb-4 flex flex-col sm:flex-row justify-between items-start sm:items-center gap-2` | Card-internal header layout |
| `cu-code` | `font-mono text-xs font-semibold bg-surface-container px-2 py-1 rounded border border-outline-variant/30 text-primary` | Code-like reference tags |
| `cu-empty` | `flex flex-col items-center justify-center text-center p-lg gap-3` | Empty table or list placeholder state |

### 2.2 Alert Mappings
| Legacy Class | Target Tailwind CSS Utility Classes |
| :--- | :--- |
| `alert alert-danger` | `bg-error/10 border border-error/20 text-error rounded-xl p-4 text-sm mb-6 flex flex-col gap-2` |
| `alert alert-success` | `bg-green-100 border border-green-600/20 text-green-800 rounded-xl p-4 text-sm mb-6` |

### 2.3 Button Mappings
| Legacy Class | Target Tailwind CSS Utility Classes |
| :--- | :--- |
| `btn btn-primary` | `px-5 py-2.5 bg-primary hover:bg-primary-container text-white font-bold rounded-xl text-sm transition-colors inline-flex items-center justify-center gap-2 shadow-sm hover:shadow-md` |
| `btn btn-outline-secondary` | `px-5 py-2.5 bg-surface-container border border-outline-variant/30 hover:bg-surface-container-high text-on-surface-variant hover:text-on-surface font-bold rounded-xl text-sm transition-colors inline-flex items-center justify-center gap-2 shadow-sm` |
| `btn btn-outline-primary` | `px-5 py-2.5 bg-surface-container-low border border-primary/20 hover:border-primary/50 hover:bg-surface-container text-primary font-bold rounded-xl text-sm transition-colors inline-flex items-center justify-center gap-2` |
| `btn btn-danger` | `px-5 py-2.5 bg-error hover:opacity-90 text-white font-bold rounded-xl text-sm transition-colors inline-flex items-center justify-center gap-2 shadow-sm` |
| `btn-sm btn-primary` | `px-3.5 py-2 bg-primary hover:bg-primary-container text-white font-bold rounded-lg text-xs transition-colors inline-flex items-center justify-center gap-1.5 shadow-sm` |
| `btn-sm btn-outline-primary` | `px-3.5 py-2 bg-surface-container-low border border-primary/20 hover:border-primary/50 hover:bg-surface-container text-primary font-medium rounded-lg text-xs transition-colors inline-flex items-center justify-center gap-1.5` |
| `btn-sm btn-outline-secondary` | `px-3.5 py-2 bg-surface-container border border-outline-variant/30 hover:bg-surface-container-high text-on-surface-variant hover:text-on-surface font-medium rounded-lg text-xs transition-colors inline-flex items-center justify-center gap-1.5` |

### 2.4 Form & Input Mappings
| Legacy Class | Target Tailwind CSS Utility Classes |
| :--- | :--- |
| `form-label` | `block text-xs font-bold text-on-surface-variant uppercase tracking-wider mb-2` |
| `form-control` | `w-full bg-surface-container border border-outline-variant/30 rounded-xl px-4 py-3 text-sm text-on-surface placeholder-on-surface-variant/40 focus:outline-none focus:border-primary/50 transition-colors` |
| `form-select` | `w-full bg-surface-container border border-outline-variant/30 rounded-xl px-4 py-3 text-sm text-on-surface focus:outline-none focus:border-primary/50 transition-colors` |
| `form-check-input` | `w-5 h-5 rounded border-outline-variant/50 text-primary focus:ring-primary focus:ring-offset-background` |
| `text-danger` / `text-danger small` | `text-error text-xs mt-1 block font-medium` |
| `text-success` | `text-green-600 font-medium` |

### 2.5 Table Mappings
Legacy class `table cu-table` and `cu-table-wrap` must be refactored to look modern and match the following specifications:
*   **Table Container Wrapper (`cu-table-wrap`)**: `overflow-x-auto border border-outline-variant/20 rounded-xl w-full`
*   **Table Element (`table`)**: `table-auto w-full text-left text-sm border-collapse`
*   **Table Head (`thead`)**: `bg-surface-container/60 border-b border-outline-variant/20`
*   **Table Header Cell (`th`)**: `px-4 py-3 text-xs font-bold text-on-surface-variant uppercase tracking-wider`
*   **Table Row (`tr`)**: `border-b border-outline-variant/10 hover:bg-surface-container-low transition-colors duration-150`
*   **Table Data Cell (`td`)**: `px-4 py-3.5 text-on-surface align-middle`
*   **Right Align helper (`text-end`)**: `text-right`
*   **Font Weight helpers (`fw-semibold`)**: `font-semibold`

### 2.6 Pagination Mappings
Legacy pagination wrappers and sub-elements must be completely redesigned:
*   **Legacy Wrapper (`cu-pagination mt-4 d-flex justify-content-center`)**: `flex justify-center items-center gap-1.5 mt-6`
*   **Active Page Item (`page-item active`)**: `w-9 h-9 flex items-center justify-center rounded-lg bg-primary text-white font-bold text-sm shadow-sm transition-all`
*   **Inactive Page Item (`page-item` with normal `page-link`)**: `w-9 h-9 flex items-center justify-center rounded-lg bg-surface-container hover:bg-surface-container-high text-on-surface font-medium text-sm border border-outline-variant/20 transition-all`
*   **Disabled Page Item (`page-item disabled`)**: `px-3.5 h-9 flex items-center justify-center rounded-lg bg-surface-container-low text-on-surface-variant/40 font-medium text-sm border border-outline-variant/10 cursor-not-allowed opacity-50`
*   **Navigation Button (`Trước`/`Sau`)**: `px-3.5 h-9 flex items-center justify-center rounded-lg bg-surface-container hover:bg-surface-container-high text-on-surface font-medium text-sm border border-outline-variant/20 transition-all`

### 2.7 Icon Modernization Mappings
Legacy SVG icons (`<svg class="cu-svg-icon"><use href="#cu-icon-xyz"></use></svg>`) must be mapped to Material Symbol spans (`<span class="material-symbols-outlined text-[20px]">icon_name</span>`) to fit seamlessly with the modern header icons:
*   `cu-icon-printer` / `cu-icon-printer-fill` &rarr; `print`
*   `cu-icon-files` &rarr; `description`
*   `cu-icon-filetype-pdf` &rarr; `picture_as_pdf`
*   `cu-icon-clipboard-check` &rarr; `fact_check`
*   `cu-icon-sliders` &rarr; `tune`
*   `cu-icon-shop` &rarr; `store`
*   `cu-icon-chat-left-text` &rarr; `sms`
*   `cu-icon-arrow-left-circle` &rarr; `arrow_back`
*   `cu-icon-send-check` &rarr; `send`
*   `cu-icon-bag-check` &rarr; `shopping_bag`
*   `cu-icon-clock-history` &rarr; `history`
*   `cu-icon-speedometer2` &rarr; `dashboard`
*   `cu-icon-bag-heart` &rarr; `shopping_basket`
*   `cu-icon-plus-circle` &rarr; `add_circle`
*   `cu-icon-arrow-down-circle` &rarr; `arrow_downward`
*   `cu-icon-arrow-up-circle` &rarr; `arrow_upward`
*   `cu-icon-receipt` &rarr; `receipt_long`

### 2.8 Badge Modernization Mappings
The C# helper extensions `ToBadgeClass()` return Bootstrap classes (`bg-success`, `bg-danger`, etc.). The refactored views must override or adjust these outputs to map to Tailwind badge styles. A unified markup formula for badges:
```html
<span class="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-bold border leading-none shadow-xs @badgeColorClass">
    @Model.Status.GetDisplayName()
</span>
```
Where mapping classes are applied:
*   `bg-success` &rarr; `bg-green-100 text-green-800 border-green-600/20`
*   `bg-danger` &rarr; `bg-error/10 text-error border-error/20`
*   `bg-primary` &rarr; `bg-primary/10 text-primary border-primary/20`
*   `bg-info text-dark` &rarr; `bg-primary/10 text-primary border-primary/20`
*   `bg-warning text-dark` &rarr; `bg-amber-100 text-amber-800 border-amber-500/20`
*   `bg-secondary` &rarr; `bg-surface-container-high text-on-surface-variant border-outline-variant/20`

---

## 3. File-by-File Refactoring Blueprint

### 3.1 PrintJobs Views

#### 1. `PrintJobs/Create.cshtml`
*   **Layout Change**: Update layout path to `"~/Views/Shared/_BranchCustomerModernLayout.cshtml"`.
*   **Structure Refactoring**:
    *   Rewrite `.cu-page` to the standard wrapper `main` container.
    *   Convert `.print-workspace` grid into `grid grid-cols-1 lg:grid-cols-12 gap-lg`.
    *   Form wraps `lg:col-span-8 flex flex-col gap-lg` containing form-step sections styled as standard cards.
    *   The aside sidebar `.print-summary-card` becomes `lg:col-span-4 lg:sticky lg:top-24 bg-surface-container-lowest border border-surface-variant/20 rounded-xl p-md shadow-sm flex flex-col gap-4`.
    *   The presets grid `.quick-preset-grid` &rarr; `grid grid-cols-2 gap-md`.
    *   Preset buttons `.quick-preset` &rarr; `w-full text-left bg-surface-container-lowest border border-outline-variant/30 hover:border-primary/50 hover:bg-surface-container-low rounded-xl p-4 transition-all flex flex-col gap-1 focus:outline-none group`. Add data attributes exactly.
    *   Dropzone container &rarr; `flex flex-col items-center justify-center border-2 border-dashed border-outline-variant/50 rounded-xl p-8 hover:border-primary/50 transition-colors cursor-pointer bg-surface-container-low relative focus-within:ring-2 focus-within:ring-primary/50`.
    *   Validation summary: replace `alert alert-danger` with the Tailwind alert system.
    *   Toggle Switch for Photo Print:
        ```html
        <label class="flex items-start gap-3 p-4 bg-surface-container-low border border-outline-variant/20 rounded-xl cursor-pointer hover:border-primary/30 transition-all select-none">
            <input asp-for="IsPhoto" class="w-5 h-5 rounded border-outline-variant/50 text-primary focus:ring-primary mt-0.5" />
            <span class="flex flex-col">
                <strong class="text-sm font-bold text-on-surface">In ảnh chất lượng cao</strong>
                <small class="text-xs text-on-surface-variant mt-0.5">Dùng giấy ảnh hoặc yêu cầu màu chính xác.</small>
            </span>
        </label>
        ```
    *   Dialog (`<dialog class="document-preview-dialog">`): Styled with modal styling:
        - Outer backdrop styled via `<dialog class="backdrop:bg-black/60 rounded-xl border border-outline-variant/30 bg-surface-container-lowest shadow-2xl p-0 overflow-hidden w-full max-w-4xl max-h-[85vh] flex flex-col">`.
        - Inside structure uses standard flex header and preview iframe container.
    *   Preserve all scripts and `data-*` attributes (`data-page-count-url`, `data-office-preview-url`, etc.) exactly.

#### 2. `PrintJobs/Details.cshtml`
*   **Layout Change**: Update layout to `_BranchCustomerModernLayout.cshtml`.
*   **Structure Refactoring**:
    *   Outer container wrapper &rarr; standard container.
    *   Hero section &rarr; Modernized page header banner.
    *   Detail List Grid (`.cu-detail-list`): Use a CSS grid:
        - Wrapper: `grid grid-cols-1 md:grid-cols-2 gap-md p-md`
        - Line details: `flex justify-between items-center border-b border-outline-variant/10 py-2.5`
        - Full-width details: `col-span-1 md:col-span-2 flex flex-col gap-1 py-2.5`
    *   Preview Card:
        - Frame wrapper: `w-full aspect-[4/3] md:aspect-[16/10] rounded-xl border border-outline-variant/20 overflow-hidden bg-surface-container-low`
        - Iframe element: `w-full h-full border-0`
    *   Action Form & Confirm dialog: Keep `onsubmit="return confirm('...')"`, method, action, and anti-forgery tokens intact. Use Tailwind class for danger button.

#### 3. `PrintJobs/Files.cshtml`
*   **Layout Change**: Update layout to `_BranchCustomerModernLayout.cshtml`.
*   **Structure Refactoring**:
    *   Hero container &rarr; Page header banner.
    *   Table section: Replace legacy table tags with Tailwind table specs.
    *   Empty files section &rarr; flex-col with `cloud_off` Material Symbol.
    *   Action links: Convert Bootstrap button classes to small Tailwind button variants.

#### 4. `PrintJobs/Index.cshtml`
*   **Layout Change**: Update layout to `_BranchCustomerModernLayout.cshtml`.
*   **Structure Refactoring**:
    *   Hero container &rarr; Page header banner.
    *   Table section &rarr; Modernized table structure.
    *   Badges: Refactor badge element using the custom mapping.
    *   Pagination: Map legacy pagination to the Tailwind flex page layout preserving exact loops (`@for (int i = 1; i <= Model.TotalPages; i++)`).

---

### 3.2 Products Views

#### 5. `Products/Index.cshtml`
*   **Layout Change**: Update layout to `_BranchCustomerModernLayout.cshtml`.
*   **Structure Refactoring**:
    *   Hero container &rarr; Page header banner.
    *   Form and Product Grid &rarr; Card layout container.
    *   Product Cards (`.cu-product-card`):
        - Container: `bg-surface-container-lowest border border-surface-variant/20 rounded-xl p-md shadow-sm hover:shadow-md transition-shadow flex flex-col justify-between gap-md`
        - Input and Label &rarr; Tailwind inline styles.
    *   Delivery form grid: `grid grid-cols-1 md:grid-cols-2 gap-md`.
    *   Submit actions &rarr; Primary buttons.

#### 6. `Products/Details.cshtml`
*   **Layout Change**: Update layout to `_BranchCustomerModernLayout.cshtml`.
*   **Structure Refactoring**:
    *   Hero container &rarr; Page header banner.
    *   Items Table &rarr; Modern table specs.
    *   Convert status badge using the mapped styles.

#### 7. `Products/Orders.cshtml`
*   **Layout Change**: Update layout to `_BranchCustomerModernLayout.cshtml`.
*   **Structure Refactoring**:
    *   Hero container &rarr; Page header banner.
    *   Orders Table &rarr; Modern table specs.
    *   Pagination &rarr; Modern pagination container.

---

### 3.3 Wallet Views

#### 8. `Wallet/Index.cshtml`
*   **Layout Change**: Update layout to `_BranchCustomerModernLayout.cshtml`.
*   **Structure Refactoring**:
    *   Hero container &rarr; Page header banner.
    *   Stats Cards Grid (`.cu-stat-grid`):
        - Grid container: `grid grid-cols-1 md:grid-cols-3 gap-md mb-lg`
        - Card: `bg-surface-container-lowest border border-surface-variant/20 rounded-xl p-md shadow-sm flex items-center gap-4`
        - Icon container: `w-12 h-12 rounded-full flex items-center justify-center shrink-0`
          - Green (`is-green`): `bg-green-100 text-green-800`
          - Rose (`is-rose`): `bg-error/10 text-error`
          - Blue (`is-blue`): `bg-primary/10 text-primary`
    *   Transaction Table &rarr; Modern table specs.
    *   Ensure to dynamically map `tx.Amount >= 0 ? "text-success" : "text-danger"` &rarr; `tx.Amount >= 0 ? "text-green-600 font-bold" : "text-error font-bold"`.
    *   Pagination &rarr; Modern pagination container.

#### 9. `Wallet/TopUp.cshtml`
*   **Layout Change**: Update layout to `_BranchCustomerModernLayout.cshtml`.
*   **Structure Refactoring**:
    *   Hero container &rarr; Page header banner.
    *   Grid workspace: `grid grid-cols-1 lg:grid-cols-12 gap-lg`.
    *   Bank Transfer Card (Aside): `lg:col-span-5 bg-surface-container-lowest border border-surface-variant/20 rounded-xl p-md shadow-sm flex flex-col gap-4`.
    *   Form Card (Main): `lg:col-span-7 bg-surface-container-lowest border border-surface-variant/20 rounded-xl p-md shadow-sm flex flex-col gap-4`.
    *   Form elements and inputs &rarr; Form control mappings. Keep `multipart/form-data` and anti-forgery helper `ModelOnly` alert intact.

#### 10. `Wallet/TopUpHistory.cshtml`
*   **Layout Change**: Update layout to `_BranchCustomerModernLayout.cshtml`.
*   **Structure Refactoring**:
    *   Hero container &rarr; Page header banner.
    *   Table &rarr; Modern table specs.
    *   Badges &rarr; Badge mappings.

---

### 3.4 SupportOrders Views

#### 11. `SupportOrders/Create.cshtml`
*   **Layout Change**: Update layout to `_BranchCustomerModernLayout.cshtml`.
*   **Structure Refactoring**:
    *   Hero container &rarr; Page header banner.
    *   Card Form container &rarr; Standard modern card styling.
    *   Dropdown and select &rarr; Form control mapping.
    *   Textarea and numeric fields &rarr; Form control mapping.

#### 12. `SupportOrders/Details.cshtml`
*   **Layout Change**: Update layout to `_BranchCustomerModernLayout.cshtml`.
*   **Structure Refactoring**:
    *   Hero container &rarr; Page header banner.
    *   Details List Grid (`.cu-detail-list`) &rarr; CSS grid with flex list item rows.
    *   Badge &rarr; Badge mapping.

#### 13. `SupportOrders/History.cshtml`
*   **Layout Change**: Update layout to `_BranchCustomerModernLayout.cshtml`.
*   **Structure Refactoring**:
    *   Hero container &rarr; Page header banner.
    *   Table &rarr; Modern table specs.
    *   Pagination &rarr; Modern pagination container.

---

## 4. Key Preservation Safeguards

To prevent breaking business logic during the refactoring process, the following details must not be modified:
1.  **Idempotency Keys**: Inputs like `<input asp-for="IdempotencyKey" type="hidden" />` or `<input asp-for="Form.IdempotencyKey" type="hidden" />` must remain unchanged inside forms.
2.  **ASP Validation Attributes**: Do not remove or change helper validation tags like `<span asp-validation-for="..." class="..."></span>`. Change only the layout wrapper classes (`text-danger` &rarr; `text-error text-xs mt-1 block`).
3.  **Scripts Section bindings**: Keep all `@section Scripts { ... }` blocks unchanged, including files `printjob-create.js`.
4.  **Form actions and URLs**: Ensure `asp-route-*`, `asp-controller`, `asp-action`, `method`, `enctype`, and custom HTML5 data attributes (e.g. `data-page-count-url`) on `<form>` tags are exactly preserved.
5.  **C# Helpers & Loops**: Do not modify any `@foreach`, `@for`, or condition tags. E.g., `Model.Items.Count`, `item.Status.ToBadgeClass()`, and display text formatters like `.ToString("N0")`.
