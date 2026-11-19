using System.ComponentModel.DataAnnotations;

namespace OpsPilot.Application.Incidents;

public sealed record UpdateIncidentStatusRequest(
    [Required]
    [RegularExpression(
        "^(Open|Investigating|Mitigated|Resolved|Closed)$",
        ErrorMessage = "Status must be Open, Investigating, Mitigated, Resolved, or Closed.")]
    string Status
);