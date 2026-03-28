using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PhotoCopyHub.Application.Common;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Constants;
using PhotoCopyHub.Web;
using PhotoCopyHub.Web.Extensions;
using PhotoCopyHub.Web.Models;

namespace PhotoCopyHub.Web.Controllers;

[Authorize(Policy = AppPolicies.CustomerPortal)]
public class WalletController : Controller
{
    private readonly IWalletService _walletService;
    private readonly ITopUpService _topUpService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IConfiguration _configuration;

    public WalletController(
        IWalletService walletService,
        ITopUpService topUpService,
        IFileStorageService fileStorageService,
        IConfiguration configuration)
    {
        _walletService = walletService;
        _topUpService = topUpService;
        _fileStorageService = fileStorageService;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var vm = new WalletIndexViewModel
        {
            CurrentBalance = await _walletService.GetCurrentBalanceAsync(userId, cancellationToken),
            Transactions = await _walletService.GetUserTransactionsAsync(userId, cancellationToken)
        };

        return View(vm);
    }

    [HttpGet]
    public IActionResult TopUp()
    {
        var vm = BuildTopUpPageViewModel(new CreateTopUpRequestViewModel());
        return View(vm);
    }

    [HttpPost]
    [EnableRateLimiting("money")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TopUp([Bind(Prefix = "Form")] CreateTopUpRequestViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(BuildTopUpPageViewModel(form));
        }

        try
        {
            Guid? proofFileId = null;
            if (form.ProofFile is { Length: > 0 })
            {
                await using var stream = form.ProofFile.OpenReadStream();
                var uploaded = await _fileStorageService.SaveAsync(new CreateUploadedFileDto
                {
                    OwnerUserId = User.GetUserId(),
                    OriginalFileName = form.ProofFile.FileName,
                    ContentType = form.ProofFile.ContentType,
                    Size = form.ProofFile.Length,
                    Content = stream,
                    IsForPrintJob = false
                }, cancellationToken);

                proofFileId = uploaded.Id;
            }

            await _topUpService.CreateRequestAsync(new CreateTopUpRequestDto
            {
                UserId = User.GetUserId(),
                Amount = form.Amount,
                TransferContent = form.TransferContent,
                TransactionReferenceCode = form.TransactionReferenceCode,
                IdempotencyKey = form.IdempotencyKey,
                ProofFileId = proofFileId
            }, cancellationToken);

            TempData["Success"] = "Đã tạo yêu cầu nạp tiền, vui lòng chờ admin duyệt.";
            return RedirectToAction(nameof(TopUpHistory));
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(BuildTopUpPageViewModel(form));
        }
    }

    [HttpGet]
    public async Task<IActionResult> TopUpHistory(CancellationToken cancellationToken)
    {
        var requests = await _topUpService.GetUserRequestsAsync(User.GetUserId(), cancellationToken);
        return View(requests);
    }

    private TopUpPageViewModel BuildTopUpPageViewModel(CreateTopUpRequestViewModel form)
    {
        return new TopUpPageViewModel
        {
            Form = form,
            BankName = _configuration["TopUpInfo:BankName"] ?? string.Empty,
            AccountNumber = _configuration["TopUpInfo:AccountNumber"] ?? string.Empty,
            AccountName = _configuration["TopUpInfo:AccountName"] ?? string.Empty,
            TransferContentPrefix = _configuration["TopUpInfo:TransferContentPrefix"] ?? string.Empty
        };
    }
}
