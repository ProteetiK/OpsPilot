namespace OpsPilot.Application.Authentication;

public interface IAuthService
{
    Task<AuthenticatedUser?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default
    );
}