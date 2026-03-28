namespace PhotoCopyHub.Application.Contracts;

public interface IDbInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
