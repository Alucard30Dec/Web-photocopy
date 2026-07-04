# TEST_READY

## Runner Command
To execute the E2E view validation and build verification script:
```powershell
powershell -ExecutionPolicy Bypass -File .\verify_views.ps1
```

## Coverage Summary
The verification covers the customer area views of the WebPhotocopyHub system:
- **Project Target**: `WebPhotocopyHub.Web.Customer/WebPhotocopyHub.Web.Customer.csproj` (Build validation).
- **Target Directories**: `WebPhotocopyHub.Web.Customer/Views` recursively (All view and sub-view `.cshtml` files).
- **Excluded Files**: `Views/Shared/_BranchCustomerLayout.cshtml` (the legacy layout is ignored during analysis).

## Feature Checklist
- [x] **Project Build Check**: Compiles the customer web project using dotnet build and validates the exit code.
- [x] **Bootstrap Class Scanner**: Scans all `.cshtml` files for classes matching Bootstrap's grid, layout, buttons, forms, components, colors, and font-weight utilities.
- [x] **Tailwind Exclusion**: Ignores Tailwind utility classes (such as `grid-cols-`, `col-span-`, `bg-surface-container`, `max-w-container-max`, etc.) to prevent false positives.
- [x] **Detailed Violation Listing**: Reports the filename, exact line number, violating line content, and the specific violating Bootstrap classes for every single occurrence.
- [x] **Exit Code Protocol**: Returns exit code `0` if build succeeds and no Bootstrap classes are found (success); otherwise returns exit code `1` or the specific compiler exit code.
