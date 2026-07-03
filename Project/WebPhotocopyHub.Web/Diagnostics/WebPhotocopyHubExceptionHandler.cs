using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebPhotocopyHub.Application.Common;

namespace WebPhotocopyHub.Web.Diagnostics;

public sealed class WebPhotocopyHubExceptionHandler : IExceptionHandler
{
    private const int ClientClosedRequestStatusCode = 499;

    private readonly ILogger<WebPhotocopyHubExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public WebPhotocopyHubExceptionHandler(
        ILogger<WebPhotocopyHubExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        var v_strCorrelationId = CorrelationIdContext.GetOrCreate(httpContext);
        var v_objError = MapException(exception, httpContext);

        LogException(httpContext, exception, v_objError, v_strCorrelationId);

        httpContext.Response.Clear();
        httpContext.Response.StatusCode = v_objError.StatusCode;
        httpContext.Response.Headers[CorrelationIdContext.HeaderName] = v_strCorrelationId;

        if (v_objError.StatusCode == ClientClosedRequestStatusCode)
        {
            return true;
        }

        if (!CorrelationIdContext.IsApiRequest(httpContext))
        {
            var v_strErrorPath = "/Home/Error?correlationId=" + Uri.EscapeDataString(v_strCorrelationId);
            httpContext.Response.Redirect(v_strErrorPath);
            return true;
        }

        var v_objProblemDetails = new ProblemDetails
        {
            Status = v_objError.StatusCode,
            Title = v_objError.Title,
            Type = v_objError.Type,
            Detail = v_objError.Detail,
            Instance = httpContext.Request.Path
        };

        v_objProblemDetails.Extensions["code"] = v_objError.Code;
        v_objProblemDetails.Extensions["correlationId"] = v_strCorrelationId;
        v_objProblemDetails.Extensions["traceId"] =
            Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        if (!string.IsNullOrWhiteSpace(v_objError.FieldName))
        {
            v_objProblemDetails.Extensions["field"] = v_objError.FieldName;
        }

        var v_bWritten = await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = v_objProblemDetails
        });

        if (v_bWritten)
        {
            return true;
        }

        httpContext.Response.ContentType = "application/problem+json; charset=utf-8";
        await httpContext.Response.WriteAsJsonAsync(v_objProblemDetails, cancellationToken);
        return true;
    }

    private static ErrorDescriptor MapException(Exception exception, HttpContext httpContext)
    {
        if (IsClientAbort(exception, httpContext))
        {
            return new ErrorDescriptor(
                ClientClosedRequestStatusCode,
                "client_closed_request",
                "Yêu cầu đã bị hủy.",
                "Client closed request.",
                "/problems/client-closed-request",
                "Request bị hủy bởi client.",
                null);
        }

        if (exception is BusinessException businessException)
        {
            return new ErrorDescriptor(
                businessException.HttpStatus,
                businessException.Code,
                "Yêu cầu không hợp lệ.",
                businessException.UserMessage,
                "/problems/business-error",
                businessException.UserMessage,
                businessException.FieldName);
        }

        if (exception is BadHttpRequestException badHttpRequestException)
        {
            var v_iStatusCode = badHttpRequestException.StatusCode is >= 400 and <= 599
                ? badHttpRequestException.StatusCode
                : StatusCodes.Status400BadRequest;

            return new ErrorDescriptor(
                v_iStatusCode,
                v_iStatusCode == StatusCodes.Status413PayloadTooLarge ? "payload_too_large" : "bad_request",
                v_iStatusCode == StatusCodes.Status413PayloadTooLarge ? "File hoặc request quá dung lượng." : "Request không hợp lệ.",
                v_iStatusCode == StatusCodes.Status413PayloadTooLarge
                    ? "File hoặc request vượt quá giới hạn cho phép."
                    : "Request không hợp lệ.",
                v_iStatusCode == StatusCodes.Status413PayloadTooLarge ? "/problems/payload-too-large" : "/problems/bad-request",
                "Bad HTTP request.",
                null);
        }

        if (exception is UnauthorizedAccessException)
        {
            return new ErrorDescriptor(
                StatusCodes.Status403Forbidden,
                "forbidden",
                "Không có quyền truy cập.",
                "Bạn không có quyền thực hiện thao tác này.",
                "/problems/forbidden",
                "Access denied.",
                null);
        }

        if (exception is KeyNotFoundException or FileNotFoundException)
        {
            return new ErrorDescriptor(
                StatusCodes.Status404NotFound,
                "not_found",
                "Không tìm thấy dữ liệu.",
                "Không tìm thấy dữ liệu được yêu cầu.",
                "/problems/not-found",
                "Resource not found.",
                null);
        }

        if (exception is DbUpdateConcurrencyException)
        {
            return new ErrorDescriptor(
                StatusCodes.Status409Conflict,
                "concurrency_conflict",
                "Dữ liệu đã thay đổi.",
                "Dữ liệu đã được cập nhật bởi thao tác khác. Vui lòng tải lại trước khi tiếp tục.",
                "/problems/concurrency-conflict",
                "Concurrency conflict.",
                null);
        }

        if (exception is DbUpdateException)
        {
            return new ErrorDescriptor(
                StatusCodes.Status500InternalServerError,
                "database_error",
                "Không thể xử lý dữ liệu.",
                "Hệ thống chưa thể xử lý yêu cầu. Vui lòng thử lại sau.",
                "/problems/database-error",
                "Database update failed.",
                null);
        }

        if (exception is InvalidDataException)
        {
            return new ErrorDescriptor(
                StatusCodes.Status400BadRequest,
                "invalid_file",
                "File không hợp lệ.",
                "File không hợp lệ hoặc không đúng định dạng được hỗ trợ.",
                "/problems/invalid-file",
                "Invalid file data.",
                null);
        }

        if (exception is TimeoutException)
        {
            return new ErrorDescriptor(
                StatusCodes.Status504GatewayTimeout,
                "timeout",
                "Dịch vụ phản hồi quá lâu.",
                "Hệ thống chưa nhận được phản hồi kịp thời. Vui lòng thử lại sau.",
                "/problems/timeout",
                "Operation timed out.",
                null);
        }

        if (exception is IOException)
        {
            return new ErrorDescriptor(
                StatusCodes.Status503ServiceUnavailable,
                "storage_unavailable",
                "Tạm thời chưa thể xử lý file.",
                "Tài nguyên lưu trữ tạm thời chưa sẵn sàng. Vui lòng thử lại sau.",
                "/problems/storage-unavailable",
                "Storage I/O failure.",
                null);
        }

        return new ErrorDescriptor(
            StatusCodes.Status500InternalServerError,
            "unexpected_error",
            "Có lỗi xảy ra.",
            "Hệ thống gặp lỗi không mong muốn. Vui lòng cung cấp correlation ID khi cần hỗ trợ.",
            "/problems/unexpected-error",
            "Unhandled exception.",
            null);
    }

    private void LogException(
        HttpContext httpContext,
        Exception exception,
        ErrorDescriptor error,
        string correlationId)
    {
        if (error.StatusCode == ClientClosedRequestStatusCode)
        {
            _logger.LogDebug(
                "Request canceled by client. Method={Method} Path={Path} CorrelationId={CorrelationId}",
                httpContext.Request.Method,
                httpContext.Request.Path.Value,
                correlationId);
            return;
        }

        if (exception is BusinessException)
        {
            _logger.LogWarning(
                "Business exception handled. StatusCode={StatusCode} Code={Code} Method={Method} Path={Path} CorrelationId={CorrelationId}",
                error.StatusCode,
                error.Code,
                httpContext.Request.Method,
                httpContext.Request.Path.Value,
                correlationId);
            return;
        }

        _logger.LogError(
            exception,
            "Unhandled exception handled at web boundary. StatusCode={StatusCode} Code={Code} Method={Method} Path={Path} CorrelationId={CorrelationId}",
            error.StatusCode,
            error.Code,
            httpContext.Request.Method,
            httpContext.Request.Path.Value,
            correlationId);
    }

    private static bool IsClientAbort(Exception exception, HttpContext httpContext)
    {
        if (!httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        return exception is OperationCanceledException or TaskCanceledException;
    }

    private sealed record ErrorDescriptor(
        int StatusCode,
        string Code,
        string Title,
        string Detail,
        string Type,
        string LogMessage,
        string? FieldName);
}
