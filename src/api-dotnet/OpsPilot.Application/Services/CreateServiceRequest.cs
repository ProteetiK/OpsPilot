namespace OpsPilot.Application.Services;

public sealed record CreateServiceRequest(
    string Name,
    string Description
);