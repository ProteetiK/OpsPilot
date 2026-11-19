using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpsPilot.Domain.Entities;
using OpsPilot.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OpsPilot.Infrastructure.Authentication;

public static class DemoUserSeeder
{
	public static async Task SeedAsync(
		IServiceProvider services,
		IConfiguration configuration)
	{
		var email = configuration["DemoUser:Email"];
		var password = configuration["DemoUser:Password"];

		if (string.IsNullOrWhiteSpace(email) &&
			string.IsNullOrWhiteSpace(password))
		{
			Console.WriteLine(
				"Demo user seeding skipped: credentials not configured.");
			return;
		}

		if (string.IsNullOrWhiteSpace(email) ||
			string.IsNullOrWhiteSpace(password))
		{
			throw new InvalidOperationException(
				"Both DemoUser:Email and DemoUser:Password must be configured.");
		}

		if (password.Length < 12)
		{
			throw new InvalidOperationException(
				"Demo user password must contain at least 12 characters.");
		}

		using var scope = services.CreateScope();

		var dbContext = scope.ServiceProvider
			.GetRequiredService<AppDbContext>();

		var passwordHasher = scope.ServiceProvider
			.GetRequiredService<IPasswordHasher<User>>();

		var normalizedEmail = email.Trim().ToUpperInvariant();

		var existingUser = await dbContext.Users
			.SingleOrDefaultAsync(
				user => user.NormalizedEmail == normalizedEmail);

		if (existingUser is not null)
		{
			Console.WriteLine("Demo user already exists. No changes made.");
			return;
		}

		var user = new User
		{
			Id = Guid.NewGuid(),
			Email = email.Trim(),
			NormalizedEmail = normalizedEmail,
			IsActive = true,
			CreatedAtUtc = DateTime.UtcNow
		};

		user.PasswordHash = passwordHasher.HashPassword(
			user,
			password);

		dbContext.Users.Add(user);

		await dbContext.SaveChangesAsync();

		Console.WriteLine("Development demo user created successfully.");
	}
}