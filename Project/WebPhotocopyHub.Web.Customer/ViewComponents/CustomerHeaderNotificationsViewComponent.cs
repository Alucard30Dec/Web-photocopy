using Microsoft.AspNetCore.Mvc;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Domain.Enums;
using WebPhotocopyHub.Web.Customer.Models;
using WebPhotocopyHub.Web.Extensions;

namespace WebPhotocopyHub.Web.Customer.ViewComponents;

public sealed class CustomerHeaderNotificationsViewComponent : ViewComponent
{
    private readonly IPrintJobService _printJobService;
    private readonly IProductOrderService _productOrderService;
    private readonly ISupportServiceOrderService _supportServiceOrderService;
    private readonly IWalletService _walletService;
    private readonly ITopUpService _topUpService;

    public CustomerHeaderNotificationsViewComponent(
        IPrintJobService printJobService,
        IProductOrderService productOrderService,
        ISupportServiceOrderService supportServiceOrderService,
        IWalletService walletService,
        ITopUpService topUpService)
    {
        _printJobService = printJobService;
        _productOrderService = productOrderService;
        _supportServiceOrderService = supportServiceOrderService;
        _walletService = walletService;
        _topUpService = topUpService;
    }

    public async Task<IViewComponentResult> InvokeAsync(string branchSlug)
    {
        var userId = UserClaimsPrincipal.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return View(new CustomerHeaderNotificationsViewModel());
        }

        var cancellationToken = HttpContext.RequestAborted;

        // Codex 2026-07-04: Build customer header notifications from existing order and wallet services without adding a new table.
        var printJobs = (await _printJobService.GetUserOrdersAsync(userId, 1, 5, cancellationToken)).Items;
        var productOrders = (await _productOrderService.GetUserOrdersAsync(userId, 1, 5, cancellationToken)).Items;
        var supportOrders = (await _supportServiceOrderService.GetUserOrdersAsync(userId, 1, 5, cancellationToken)).Items;
        var walletTransactions = (await _walletService.GetUserTransactionsAsync(userId, 1, 5, cancellationToken)).Items;
        var topUpRequests = await _topUpService.GetUserRequestsAsync(userId, cancellationToken);

        var model = new CustomerHeaderNotificationsViewModel
        {
            AllNotificationsUrl = BuildBranchPath(branchSlug, "Dashboard")
        };

        model.Items.AddRange(printJobs.Select(item => BuildPrintJobItem(branchSlug, item)));
        model.Items.AddRange(productOrders.Select(item => BuildProductOrderItem(branchSlug, item)));
        model.Items.AddRange(supportOrders.Select(item => BuildSupportOrderItem(branchSlug, item)));
        model.Items.AddRange(topUpRequests.OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).Take(5).Select(item => BuildTopUpItem(branchSlug, item)));
        model.Items.AddRange(walletTransactions.Select(item => BuildWalletItem(branchSlug, item)));

        model.Items = model.Items
            .OrderByDescending(x => x.SortTime)
            .Take(8)
            .ToList();

        model.AttentionCount = model.Items.Count(x => x.NeedsAttention);

        return View(model);
    }

    private static CustomerHeaderNotificationItemViewModel BuildPrintJobItem(string branchSlug, PrintJob item)
    {
        var needsAttention = item.Status is not PrintJobStatus.Completed and not PrintJobStatus.Cancelled and not PrintJobStatus.Refunded;
        return new CustomerHeaderNotificationItemViewModel
        {
            Title = $"Đơn in {item.Status.GetDisplayName()}",
            Description = $"Tổng tiền {item.TotalAmount:N0} đ, {item.TotalPages:N0} trang.",
            TimeText = ToRelativeTime(item.UpdatedAt ?? item.CreatedAt),
            Url = BuildBranchPath(branchSlug, $"PrintJobs/Details/{item.Id}"),
            Icon = item.Status == PrintJobStatus.ReadyForPickup ? "task_alt" : "print",
            Tone = item.Status == PrintJobStatus.ReadyForPickup ? "green" : "blue",
            NeedsAttention = needsAttention,
            SortTime = item.UpdatedAt ?? item.CreatedAt
        };
    }

    private static CustomerHeaderNotificationItemViewModel BuildProductOrderItem(string branchSlug, ProductOrder item)
    {
        var needsAttention = item.Status is not OrderStatus.Completed and not OrderStatus.Cancelled and not OrderStatus.Refunded;
        return new CustomerHeaderNotificationItemViewModel
        {
            Title = $"Đơn sản phẩm {item.Status.GetDisplayName()}",
            Description = $"Giá trị đơn {item.TotalAmount:N0} đ.",
            TimeText = ToRelativeTime(item.UpdatedAt ?? item.CreatedAt),
            Url = BuildBranchPath(branchSlug, $"Products/Details/{item.Id}"),
            Icon = "inventory_2",
            Tone = "purple",
            NeedsAttention = needsAttention,
            SortTime = item.UpdatedAt ?? item.CreatedAt
        };
    }

    private static CustomerHeaderNotificationItemViewModel BuildSupportOrderItem(string branchSlug, SupportServiceOrder item)
    {
        var needsAttention = item.Status is not OrderStatus.Completed and not OrderStatus.Cancelled and not OrderStatus.Refunded;
        return new CustomerHeaderNotificationItemViewModel
        {
            Title = $"Yêu cầu hỗ trợ {item.Status.GetDisplayName()}",
            Description = $"Chi phí dịch vụ {item.TotalAmount:N0} đ.",
            TimeText = ToRelativeTime(item.UpdatedAt ?? item.CreatedAt),
            Url = BuildBranchPath(branchSlug, $"SupportOrders/Details/{item.Id}"),
            Icon = "support_agent",
            Tone = "teal",
            NeedsAttention = needsAttention,
            SortTime = item.UpdatedAt ?? item.CreatedAt
        };
    }

    private static CustomerHeaderNotificationItemViewModel BuildTopUpItem(string branchSlug, TopUpRequest item)
    {
        var needsAttention = item.Status is TopUpStatus.Pending or TopUpStatus.PendingAdminApproval;
        return new CustomerHeaderNotificationItemViewModel
        {
            Title = $"Nạp tiền {item.Status.GetDisplayName()}",
            Description = $"Số tiền {item.Amount:N0} đ.",
            TimeText = ToRelativeTime(item.UpdatedAt ?? item.CreatedAt),
            Url = BuildBranchPath(branchSlug, "Wallet/TopUpHistory"),
            Icon = needsAttention ? "pending_actions" : "payments",
            Tone = needsAttention ? "amber" : "green",
            NeedsAttention = needsAttention,
            SortTime = item.UpdatedAt ?? item.CreatedAt
        };
    }

    private static CustomerHeaderNotificationItemViewModel BuildWalletItem(string branchSlug, WalletTransaction item)
    {
        var isCredit = item.TransactionType is WalletTransactionType.TopUpApproved or WalletTransactionType.Refund or WalletTransactionType.ManualAdjustment && item.Amount > 0;
        return new CustomerHeaderNotificationItemViewModel
        {
            Title = item.TransactionType.GetDisplayName(),
            Description = $"{(item.Amount >= 0 ? "+" : string.Empty)}{item.Amount:N0} đ, số dư {item.BalanceAfter:N0} đ.",
            TimeText = ToRelativeTime(item.CreatedAt),
            Url = BuildBranchPath(branchSlug, "Wallet"),
            Icon = isCredit ? "add_circle" : "receipt_long",
            Tone = isCredit ? "green" : "blue",
            NeedsAttention = false,
            SortTime = item.CreatedAt
        };
    }

    private static string BuildBranchPath(string branchSlug, string path)
    {
        var normalizedSlug = string.IsNullOrWhiteSpace(branchSlug) ? "toanphotocopy" : branchSlug.Trim('/');
        return "/" + normalizedSlug + "/" + path.TrimStart('/');
    }

    private static string ToRelativeTime(DateTime value)
    {
        var localValue = DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime();
        var span = DateTime.Now - localValue;

        if (span.TotalMinutes < 1)
        {
            return "Vừa xong";
        }

        if (span.TotalHours < 1)
        {
            return $"{Math.Max(1, (int)span.TotalMinutes)} phút trước";
        }

        if (span.TotalDays < 1)
        {
            return $"{Math.Max(1, (int)span.TotalHours)} giờ trước";
        }

        if (span.TotalDays < 7)
        {
            return $"{Math.Max(1, (int)span.TotalDays)} ngày trước";
        }

        return localValue.ToString("dd/MM/yyyy HH:mm");
    }
}
