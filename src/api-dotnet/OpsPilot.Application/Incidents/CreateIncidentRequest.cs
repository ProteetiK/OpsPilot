
using System.ComponentModel.DataAnnotations;

namespace OpsPilot.Application.Incidents;

public sealed record CreateIncidentRequest(
    [Required]
    [StringLength(200, MinimumLength = 1)]
    string Title,

    [Required]
    [StringLength(5000, MinimumLength = 1)]
    string Description,

    [Required]
    [RegularExpression(
        "^(Low|Medium|High|Critical)$",
        ErrorMessage = "Severity must be Low, Medium, High, or Critical.")]
    string Severity,

    Guid? ServiceId
);
