
using System.ComponentModel.DataAnnotations;

namespace OpsPilot.Application.Common.Pagination;

public sealed record PaginationParameters(
    [Range(1, int.MaxValue)]
    int Page = 1,

    [Range(1, 100)]
    int PageSize = 20
);
