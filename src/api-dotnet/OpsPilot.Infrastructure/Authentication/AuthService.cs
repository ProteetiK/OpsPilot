using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpsPilot.Application.Authentication;
using OpsPilot.Domain.Entities;
using OpsPilot.Infrastructure.Persistence;

namespace OpsPilot.Infrastructure.Authentication;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthService(
        AppDbContext dbContext,
        IPasswordHasher<User> passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthenticatedUser?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrEmpty(password))
        {
            return null;
        }

        var normalizedEmail = email.Trim().ToUpperInvariant();

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(
                u => u.NormalizedEmail == normalizedEmail,
                cancellationToken);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (verificationResult ==
            PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                password);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new AuthenticatedUser(
            user.Id,
            user.Email,
            user.Role
        );
    }
}