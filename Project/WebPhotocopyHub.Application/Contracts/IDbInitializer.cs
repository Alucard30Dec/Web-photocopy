namespace WebPhotocopyHub.Application.Contracts;

public interface IDbInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
