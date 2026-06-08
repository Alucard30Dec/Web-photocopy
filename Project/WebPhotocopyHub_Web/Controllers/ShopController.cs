using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoCopyHub.Web.Models;

namespace PhotoCopyHub.Web.Controllers;

[AllowAnonymous]
public class ShopController : Controller
{
    public IActionResult Index()
    {
        return View(new ShopDirectoryViewModel
        {
            Branches = ShopBranchCatalog.All
        });
    }

    public IActionResult Details(string branchSlug)
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

        return RedirectToAction("Index", "Branch", new { branchSlug = branch.Slug });
    }
}
