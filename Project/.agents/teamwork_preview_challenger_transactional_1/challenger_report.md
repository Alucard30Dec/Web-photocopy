# Challenger Verification Report — Transactional Core Views

## Challenge Summary

**Overall risk assessment**: HIGH (due to a runtime-breaking syntax error discovered in a target view, despite successful compilation and E2E verification script passing on target files).

## Challenges

### [Critical] Challenge 1: Syntax Error / Residual Markup in Wallet/Index.cshtml
- **Assumption challenged**: The assumption that successful project compilation (`dotnet build`) and zero violations reported by `verify_views.ps1` for target files means the views are correct and runtime-safe.
- **Attack scenario**: When a user accesses the "Ví của tôi" (My Wallet) page, the ASP.NET Core Razor view engine compiles the CSHTML file at runtime. Because `Wallet/Index.cshtml` contains dangling, broken markup at the end of the file, the engine will fail to parse/render the view, resulting in an unhandled exception (HTTP 500 error) for the user.
- **Blast radius**: Complete outage of the Customer Wallet dashboard. Users will be unable to view their wallet balance, transaction history, or access links to nạp tiền (top-up).
- **Mitigation**: Remove the dangling residual lines (lines 147–154) from `e:\OneDrive - 0dpmr\WebPhotocopy\Project\WebPhotocopyHub.Web.Customer\Views\Wallet\Index.cshtml`. The file should end cleanly after line 146.

### [Medium] Challenge 2: Verification Script Scopes and Exits on Non-Target Files
- **Assumption challenged**: The assumption that running `powershell -ExecutionPolicy Bypass -File .\verify_views.ps1` as-is is a reliable CI/CD gate for verifying ONLY the refactored Transactional Core views.
- **Attack scenario**: The verification script currently scans the entire `WebPhotocopyHub.Web.Customer/Views` folder, rather than targeting only the folders in-scope. Since 6 files in the `Account` and `Shared` folders still contain Bootstrap classes (such as `form-control`, `text-danger`, `alert`, `btn-primary`), the script exits with exit code 1. This blocks build pipelines even though the Transactional Core views themselves are fully compliant.
- **Blast radius**: CI/CD build pipelines will fail unless the non-target views are also refactored, or the script is modified to exclude them or target only the four specified directories.
- **Mitigation**: Update `verify_views.ps1` or run a targeted scan for the four transactional folders: `PrintJobs`, `Products`, `Wallet`, and `SupportOrders`.

---

## Stress Test Results

### 1. View Verification Script (`verify_views.ps1`)
- **Scenario**: Run `verify_views.ps1` to detect Bootstrap classes.
- **Expected Behavior**: Target views under `PrintJobs`, `Products`, `Wallet`, and `SupportOrders` do not trigger any Bootstrap class violations.
- **Actual Behavior**: No violations found in any of the 13 target files. However, 67 violations were found in 6 non-target views (`Account/ExternalLoginConfirmation.cshtml`, `Account/ForgotPassword.cshtml`, `Account/Login.cshtml`, `Account/Register.cshtml`, `Account/ResetPassword.cshtml`, `Shared/_Alert.cshtml`), causing the script to exit with code 1.
- **Result**: **PASS** (for the 13 target files, as they have zero violations), but **FAIL** (for the overall script execution).

### 2. Project Build (`dotnet build`)
- **Scenario**: Run `dotnet build` on the `WebPhotocopyHub.Web.Customer` project.
- **Expected Behavior**: The project compiles with zero errors and zero warnings.
- **Actual Behavior**: Build succeeded with 0 errors and 0 warnings.
- **Result**: **PASS**

### 3. Static Syntax Validation of Razor Views
- **Scenario**: Inspect target views for syntactical and logical correctness.
- **Expected Behavior**: All HTML, Razor directives, and C# code blocks are well-formed and close properly.
- **Actual Behavior**: `Wallet/Index.cshtml` contains dangling residual code starting at line 147:
  ```html
  </section>ctions.PageNumber + 1}")">Sau</a>
                          </li>
                      </ul>
                  </nav>
              </div>
          }
      </section>
  </section>
  ```
  This dangling code is a copy-paste/merge artifact left over from the Bootstrap pagination layout.
- **Result**: **FAIL** (due to a runtime-breaking syntax error in `Wallet/Index.cshtml`).

---

## Unchallenged Areas
- **Runtime View Engine Rendering**: Not challenged because we operate in review-only mode and do not run the full web application server or have access to a local DB context for live rendering of views.
- **Tailwind CSS Compilation**: We did not verify if all the custom Tailwind classes (e.g., `p-md`, `gap-lg`, `bg-surface-container-low`) are correctly defined in `tailwind.config.js` or if they generate the expected CSS, since CSS build configurations are out of scope.
