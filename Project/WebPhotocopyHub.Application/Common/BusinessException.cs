namespace WebPhotocopyHub.Application.Common;

public class BusinessException : Exception
{
    public const string DefaultCode = "business_error";

    public BusinessException(string message) : base(message)
    {
        Code = DefaultCode;
        UserMessage = message;
        HttpStatus = 400;
        Metadata = new Dictionary<string, object?>();
    }

    public BusinessException(
        string code,
        string userMessage,
        int httpStatus = 400,
        string? fieldName = null,
        IReadOnlyDictionary<string, object?>? metadata = null,
        Exception? innerException = null) : base(userMessage, innerException)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Business error code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(userMessage))
        {
            throw new ArgumentException("Business user message is required.", nameof(userMessage));
        }

        Code = code.Trim();
        UserMessage = userMessage.Trim();
        HttpStatus = httpStatus is >= 400 and <= 599 ? httpStatus : 400;
        FieldName = string.IsNullOrWhiteSpace(fieldName) ? null : fieldName.Trim();
        Metadata = metadata ?? new Dictionary<string, object?>();
    }

    public string Code { get; }

    public string UserMessage { get; }

    public int HttpStatus { get; }

    public string? FieldName { get; }

    public IReadOnlyDictionary<string, object?> Metadata { get; }
}
