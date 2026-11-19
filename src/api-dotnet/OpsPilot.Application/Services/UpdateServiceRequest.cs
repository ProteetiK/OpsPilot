using System.ComponentModel.DataAnnotations;

namespace OpsPilot.Application.Services;

public sealed record UpdateServiceRequest(
    [Required]
    [StringLength(200, MinimumLength = 1)]
    string Name,

    [Required]
    [StringLength(2000, MinimumLength = 1)]
    string Description
);