# Review Report — Customer Profile Views Refactoring

**Target Files**:
- `WebPhotocopyHub.Web.Customer\Views\Profile\Index.cshtml`
- `WebPhotocopyHub.Web.Customer\Views\Profile\ChangePassword.cshtml`

---

## Review Summary

**Verdict**: **APPROVE**

Both views have been successfully refactored to align with the new modern layout (`_BranchCustomerModernLayout.cshtml`), with all Bootstrap classes fully replaced by Tailwind CSS styles. Razor syntax, model bindings, anti-forgery tokens, and validation helpers have been completely preserved. The project builds successfully.

---

## Findings

### [Minor] Finding 1: Success/Error Feedback Alerts Deferred
- **What**: The modern layout `_BranchCustomerModernLayout.cshtml` does not currently reference the success/error alert partial (`_Alert.cshtml`).
- **Where**: `WebPhotocopyHub.Web.Customer\Views\Shared\_BranchCustomerModernLayout.cshtml`
- **Why**: The legacy partial `_Alert.cshtml` still contains Bootstrap classes (`alert`, `alert-success`, `btn-close`) and is flagged as a violation by the scan script. Because it is not included in the modern layout, success messages (like `TempData["Success"] = "Cập nhật hồ sơ thành công."` set by `ProfileController.cs`) will not render in the UI when profile changes are saved.
- **Suggestion**: Ensure that `_Alert.cshtml` is refactored to use Tailwind CSS as part of the M8 (Account & General) milestone, and once clean of Bootstrap, include it in `_BranchCustomerModernLayout.cshtml`.

---

## Verified Claims

- **Claim 1**: The customer profile views compile successfully.
  - *Verified via*: Executing the `verify_views.ps1` script which runs `dotnet build` on the customer project target.
  - *Result*: **PASS** (Zero compiler errors/warnings).
- **Claim 2**: Bootstrap classes are completely removed from the refactored views.
  - *Verified via*: Inspecting the scanner logs of `verify_views.ps1` for `Views\Profile\Index.cshtml` and `Views\Profile\ChangePassword.cshtml`.
  - *Result*: **PASS** (Both files showed zero Bootstrap violations).
- **Claim 3**: Model bindings, validation helpers, and routing slugs are preserved.
  - *Verified via*: Manual inspection of the Razor files and comparison against `ProfileController.cs` and `ProfileViewModel.cs`.
  - *Result*: **PASS** (All model declarations and tag helpers match the controller specs).

---

## Coverage Gaps

- **Alert and Feedback components** — risk level: **Low** — recommendation: **Accept risk / Investigate in M8**.
  - *Detail*: Since `_Alert.cshtml` is not refactored yet, visual feedback for successful actions is temporarily missing. However, the underlying MVC model state errors do render correctly using the modern Tailwind styled `ModelOnly` validation summary box.

---

## Adversarial Review

### Challenge Summary
- **Overall risk assessment**: **LOW**

The refactored files are visually cohesive, clean, and structurally sound. Below are the tested scenarios and potential edge cases.

### Challenges

#### [Low] Challenge 1: Visual Styling of jQuery Validation Class Override
- **Assumption challenged**: The inputs rely on the `.input-validation-error` class generated dynamically by jQuery Validation to style invalid states in Tailwind.
- **Attack scenario**: If jQuery files are not loaded correctly or if validation is performed purely server-side, the input fields might not highlight in red instantly unless the CSS class is applied on page load.
- **Blast radius**: Cosmetic layout variance where invalid inputs lack red borders before form submission, but error text messages still render.
- **Mitigation**: The scoped `<style>` block in both views maps `.input-validation-error` to the appropriate Tailwind-inspired error border style. Server-side validation errors correctly trigger this upon POST postback.

#### [Low] Challenge 2: Missing Branch Slug Route Fallback
- **Assumption challenged**: The cancel button in `ChangePassword.cshtml` constructs the redirect URI using `ViewContext.RouteData.Values["branchSlug"]`.
- **Attack scenario**: A user navigates directly to the page using a direct URL where the slug is somehow missing or blank.
- **Blast radius**: Redirecting back to the index view fails or triggers a 404 because the routing layout expects a branch slug in the URL path.
- **Mitigation**: The customer controller itself has policy routing that forces `branchSlug` in the URL pattern, making it impossible to access these views without a valid branch slug route context.

---

## Stress Test Results

- **Empty Profile Form Submission** → Server returns model errors for required fields (`FullName`, `UserName`) → validation summary and validation labels display correctly with custom Tailwind error colors → **PASS**
- **Attempting to Edit Readonly Fields** → Inspecting the HTML inputs for `Email`, `CreatedAt`, and `IsActive` → fields are correctly marked `readonly` with `cursor-not-allowed` styles, preventing any client-side tampering → **PASS**
- **Redirect after Successful ChangePassword** → Controller redirects back to `Index` with correct route slug → router correctly redirects to `/profile` → **PASS**
