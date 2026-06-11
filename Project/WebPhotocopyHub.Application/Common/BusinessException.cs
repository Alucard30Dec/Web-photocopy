namespace WebPhotocopyHub.Application.Common;

public class BusinessException : Exception
{
    public BusinessException(string message) : base(message)
    {
    }
}
