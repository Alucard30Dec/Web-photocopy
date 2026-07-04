# Challenger Report — Profile Views Verification

This report documents the empirical verification and stress-testing of the refactored Profile views:
- `WebPhotocopyHub.Web.Customer/Views/Profile/Index.cshtml`
- `WebPhotocopyHub.Web.Customer/Views/Profile/ChangePassword.cshtml`

## Challenge Summary

**Overall risk assessment**: LOW

The refactored profile views completely replace Bootstrap CSS utility classes with Tailwind CSS utility classes and scoped CSS overrides for validation, ensuring alignment with the modern UI/UX design specifications. The project builds without errors.

---

## Challenges

### [Low] Challenge 1: Scoped `<style>` blocks duplication and maintenance
- **Assumption challenged**: The custom CSS styling for ASP.NET validation states (`.text-error`, `.input-validation-error`, and `:focus` states) is scoped in individual views.
- **Attack scenario**: If validation styling changes are needed in the future, the styles must be updated in multiple view files (e.g., `Profile/Index.cshtml` and `Profile/ChangePassword.cshtml`), increasing maintenance overhead and the risk of visual inconsistency.
- **Blast radius**: Localized styling mismatch if styles are updated in one file but not the other.
- **Mitigation**: Move these common validation styles into the main CSS bundle (`customer-dashboard-modern.css`) or the shared layout (`_BranchCustomerModernLayout.cshtml`) so they are managed centrally.

### [Low] Challenge 2: Timezone Mismatch via server-side `ToLocalTime()`
- **Assumption challenged**: Calling `@Model.CreatedAt.ToLocalTime()` provides the correct local time for the end user.
- **Attack scenario**: If the server is hosted in a cloud environment (e.g., AWS/Azure in UTC) and the user is in Vietnam (UTC+7), `ToLocalTime()` will output the server time (UTC), displaying incorrect account creation dates to the customer.
- **Blast radius**: Minor user confusion regarding their account creation date/time.
- **Mitigation**: Use client-side JavaScript formatting (e.g., `<time datetime="@Model.CreatedAt.ToString("o")">...</time>`) or apply the user's specific timezone preference server-side.

---

## Stress Test Results

### Scenario 1: Execute `verify_views.ps1` E2E scan
- **Scenario**: Run the Bootstrap scan script on the entire customer views directory.
- **Expected behavior**: Zero violations are flagged in `Profile/Index.cshtml` and `Profile/ChangePassword.cshtml`.
- **Actual behavior**: Passed. No Bootstrap classes were detected in either profile view. Other un-refactored customer views (e.g., Wallet, SupportOrders) were flagged as expected, but the Profile views were clean.
- **Result**: PASS

### Scenario 2: Project Compilation
- **Scenario**: Run `dotnet build` on `WebPhotocopyHub.Web.Customer`.
- **Expected behavior**: Zero compilation errors or warnings.
- **Actual behavior**: Build succeeded with 0 warnings and 0 errors.
- **Result**: PASS

### Scenario 3: Cancel Route Validation
- **Scenario**: Validate target cancellation links in profile and password change forms.
- **Expected behavior**: Cancel actions redirect back to index pages with correct branch slugs.
- **Actual behavior**: Profile/Index cancel button points to dashboard: `@dashboardUrl` (`ToPath(ViewContext, "Dashboard")`). ChangePassword cancel button points to Profile Index: `@Url.Action("Index", "Profile", new { branchSlug = ViewContext.RouteData.Values["branchSlug"] })`. Both resolve correctly under the `shop-branch-customer` route structure.
- **Result**: PASS

---

## Unchallenged Areas

- **Functional runtime behavior**: Out of scope for this review as we do not have a live database connection or running integration test harness to simulate the HTTP POST save operations. Only static/compile-time layout verification was performed.
