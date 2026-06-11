namespace WebPhotocopyHub.Domain.Common;

public interface IHasRowVersion
{
    byte[] RowVersion { get; set; }
}
