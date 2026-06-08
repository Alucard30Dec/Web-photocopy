using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoCopyHub.Application.Common;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Domain.Enums;
using PhotoCopyHub.Infrastructure.Data;
using PhotoCopyHub.Infrastructure.Options;

namespace PhotoCopyHub.Infrastructure.Services;

public class PrintJobService : IPrintJobService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPricingService _pricingService;
    private readonly IWalletService _walletService;
    private readonly BusinessOptions _businessOptions;

    public PrintJobService(
        ApplicationDbContext dbContext,
        IPricingService pricingService,
        IWalletService walletService,
        IOptions<BusinessOptions> businessOptions)
    {
        _dbContext = dbContext;
        _pricingService = pricingService;
        _walletService = walletService;
        _businessOptions = businessOptions.Value;
    }

    public async Task<PrintJob> CreateAndSubmitAsync(CreatePrintJobDto request, CancellationToken cancellationToken = default)
    {
        var idempotencyKey = NormalizeIdempotencyKey(request.IdempotencyKey) ?? Guid.NewGuid().ToString("N");
        var existingJob = await _dbContext.PrintJobs
            .Include(x => x.UploadedFile)
            .FirstOrDefaultAsync(x =>
                x.UserId == request.UserId &&
                x.SubmitIdempotencyKey == idempotencyKey, cancellationToken);
        if (existingJob is not null)
        {
            if (existingJob.UploadedFileId != request.UploadedFileId
                || existingJob.Copies != request.Copies
                || existingJob.TotalPages != request.TotalPages
                || existingJob.PaperSize != request.PaperSize
                || existingJob.PrintSide != request.PrintSide
                || existingJob.ColorMode != request.ColorMode)
            {
                throw new BusinessException("Idempotency key đã được dùng cho payload khác.");
            }

            return existingJob;
        }

        var file = await _dbContext.UploadedFileMetadatas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.UploadedFileId && x.OwnerUserId == request.UserId, cancellationToken);

        if (file is null)
        {
            throw new BusinessException("File in không tồn tại hoặc không thuộc tài khoản của bạn.");
        }

        if (request.DeliveryMethod == DeliveryMethod.Shipping && string.IsNullOrWhiteSpace(request.DeliveryAddress))
        {
            throw new BusinessException("Vui lòng nhập địa chỉ giao hàng.");
        }

        var pricing = await _pricingService.CalculatePrintPriceAsync(new PricingCalculationRequestDto
        {
            PaperSize = request.PaperSize,
            PrintSide = request.PrintSide,
            ColorMode = request.ColorMode,
            IsPhoto = request.IsPhoto,
            Copies = request.Copies,
            TotalPages = request.TotalPages,
            DeliveryMethod = request.DeliveryMethod,
            ShippingFee = _businessOptions.ShippingFee
        }, cancellationToken);

        var job = new PrintJob
        {
            UserId = request.UserId,
            UploadedFileId = request.UploadedFileId,
            PaperSize = request.PaperSize,
            PrintSide = request.PrintSide,
            ColorMode = request.ColorMode,
            IsPhoto = request.IsPhoto,
            Copies = request.Copies,
            TotalPages = request.TotalPages,
            Notes = request.Notes,
            SubmitIdempotencyKey = idempotencyKey,
            DeliveryMethod = request.DeliveryMethod,
            DeliveryAddress = request.DeliveryAddress,
            UnitPrice = pricing.UnitPrice,
            SubTotal = pricing.SubTotal,
            ShippingFee = pricing.ShippingFee,
            TotalAmount = pricing.TotalAmount,
            Status = PrintJobStatus.Submitted
        };

        _dbContext.PrintJobs.Add(job);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return job;
    }

    public Task<List<PrintJob>> GetUserOrdersAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.PrintJobs
            .AsNoTracking()
            .Include(x => x.UploadedFile)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<List<PrintJob>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.PrintJobs
            .Include(x => x.User)
            .Include(x => x.UploadedFile)
            .OrderBy(x => x.Status)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<PrintJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.PrintJobs
            .Include(x => x.User)
            .Include(x => x.UploadedFile)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task UpdateStatusAsync(
        Guid id,
        PrintJobStatus status,
        string actorUserId,
        bool actorIsAdmin,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.PrintJobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new BusinessException("Không tìm thấy đơn in.");

        if (status == PrintJobStatus.Refunded)
        {
            throw new BusinessException("Vui lòng dùng chức năng hoàn tiền để chuyển sang trạng thái Refunded.");
        }

        if (!IsValidTransition(job.Status, status) && !actorIsAdmin)
        {
            throw new BusinessException($"Không thể chuyển trạng thái từ {job.Status} sang {status}.");
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        if (status == PrintJobStatus.ConfirmedByShop)
        {
            job.ConfirmedByOperatorId = actorUserId;
            job.ConfirmedAt = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(job.AssignedOperatorId))
            {
                job.AssignedOperatorId = actorUserId;
            }
        }

        if (status == PrintJobStatus.Paid && job.PaidAt is null)
        {
            var walletTxn = await _walletService.DebitAsync(new WalletOperationRequestDto
            {
                UserId = job.UserId,
                Amount = job.TotalAmount,
                TransactionType = WalletTransactionType.DebitForOrder,
                ReferenceType = nameof(PrintJob),
                ReferenceId = job.Id,
                Note = $"Thanh toán đơn in {job.Id}",
                IdempotencyKey = $"printjob-pay-{job.Id:N}",
                PerformedByAdminId = actorUserId
            }, cancellationToken);

            job.PaidAt = DateTime.UtcNow;
            job.PaidWalletTransactionId = walletTxn.Id;
        }

        job.Status = status;
        job.ProcessedByAdminId = actorUserId;
        job.LastStatusNote = note;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = "UpdatePrintJobStatus",
            EntityName = nameof(PrintJob),
            EntityId = job.Id.ToString(),
            Details = $"Status: {status}. Note: {note}"
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    public async Task RefundAsync(
        Guid id,
        string actorUserId,
        bool actorIsAdmin,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.PrintJobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new BusinessException("Không tìm thấy đơn in.");

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BusinessException("Vui lòng nhập lý do hoàn tiền.");
        }

        if (job.Status == PrintJobStatus.Refunded)
        {
            throw new BusinessException("Đơn in đã được hoàn tiền trước đó.");
        }

        if (job.Status is PrintJobStatus.Submitted or PrintJobStatus.ConfirmedByShop or PrintJobStatus.Draft or PrintJobStatus.Cancelled)
        {
            throw new BusinessException("Trạng thái hiện tại không hợp lệ để hoàn tiền.");
        }

        if (job.Status == PrintJobStatus.Completed && !actorIsAdmin)
        {
            throw new BusinessException("Chỉ Admin được hoàn tiền cho đơn đã hoàn thành.");
        }

        if (_businessOptions.RefundRequireAdminApprovalThreshold > 0
            && job.TotalAmount >= _businessOptions.RefundRequireAdminApprovalThreshold
            && !actorIsAdmin)
        {
            throw new BusinessException("Khoản hoàn vượt ngưỡng, cần Admin xác nhận.");
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        await _walletService.CreditAsync(new WalletOperationRequestDto
        {
            UserId = job.UserId,
            Amount = job.TotalAmount,
            TransactionType = WalletTransactionType.Refund,
            ReferenceType = nameof(PrintJob),
            ReferenceId = job.Id,
            Note = reason,
            IdempotencyKey = $"printjob-refund-{job.Id:N}",
            PerformedByAdminId = actorUserId
        }, cancellationToken);

        job.Status = PrintJobStatus.Refunded;
        job.ProcessedByAdminId = actorUserId;
        job.RefundedByUserId = actorUserId;
        job.RefundedAt = DateTime.UtcNow;
        job.RefundReason = reason;
        job.LastStatusNote = reason;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = "RefundPrintJob",
            EntityName = nameof(PrintJob),
            EntityId = job.Id.ToString(),
            Details = reason
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private static bool IsValidTransition(PrintJobStatus currentStatus, PrintJobStatus nextStatus)
    {
        if (currentStatus == nextStatus)
        {
            return true;
        }

        return currentStatus switch
        {
            PrintJobStatus.Submitted => nextStatus is PrintJobStatus.ConfirmedByShop or PrintJobStatus.Cancelled,
            PrintJobStatus.ConfirmedByShop => nextStatus is PrintJobStatus.Paid or PrintJobStatus.Cancelled,
            PrintJobStatus.Paid => nextStatus is PrintJobStatus.Processing or PrintJobStatus.Cancelled,
            PrintJobStatus.Processing => nextStatus is PrintJobStatus.ReadyForPickup or PrintJobStatus.Shipping or PrintJobStatus.Cancelled,
            PrintJobStatus.ReadyForPickup => nextStatus is PrintJobStatus.Completed or PrintJobStatus.Cancelled,
            PrintJobStatus.Shipping => nextStatus == PrintJobStatus.Completed,
            _ => false
        };
    }

    private static string? NormalizeIdempotencyKey(string? key)
    {
        var trimmed = key?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
