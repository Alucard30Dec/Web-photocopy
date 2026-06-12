namespace WebPhotocopyHub.Application.Common;

public class OfficePreviewUnavailableException : Exception
{
    public OfficePreviewUnavailableException(string message)
        : base(message)
    {
    }

    public OfficePreviewUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
