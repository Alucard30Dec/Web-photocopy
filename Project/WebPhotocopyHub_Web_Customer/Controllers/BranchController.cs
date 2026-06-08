using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoCopyHub.Web.Models;

namespace PhotoCopyHub.Web.Controllers;

[AllowAnonymous]
public class BranchController : Controller
{
    [HttpGet]
    public IActionResult Index(string branchSlug)
    {
        if (string.IsNullOrWhiteSpace(branchSlug))
        {
            return NotFound();
        }

        var branch = ShopBranchCatalog.Find(branchSlug);
        if (branch is null)
        {
            return NotFound();
        }

        return View(branch);
    }
}