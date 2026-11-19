namespace OpsPilot.Application.Services;

public sealed record UpdateServiceRequest(
    string Name,
    string Description
);