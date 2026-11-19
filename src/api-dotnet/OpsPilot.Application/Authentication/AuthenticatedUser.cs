namespace OpsPilot.Application.Authentication;

public sealed record AuthenticatedUser(
	Guid Id,
	string Email
);