using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsPilot.Application.Services;
using OpsPilot.Application.Common.Pagination;

namespace OpsPilot.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/services")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly IServiceManagement _serviceManagement;

    public ServicesController(IServiceManagement serviceManagement)
    {
        _serviceManagement = serviceManagement;
    }

    // All authenticated users can view services.
    [HttpGet]
    public async Task<IActionResult> GetServices(
    [FromQuery] PaginationParameters pagination,
    [FromQuery] string? search,
    CancellationToken cancellationToken)
    {
        var services = await _serviceManagement.GetServicesAsync(
            pagination,
            search,
            cancellationToken);

        return Ok(services);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetService(
        Guid id,
        CancellationToken cancellationToken)
    {
        var service = await _serviceManagement.GetServiceByIdAsync(
            id, cancellationToken);

        return service is null ? NotFound() : Ok(service);
    }

    // Only Administrators can create services.
    [HttpPost]
    [Authorize(Policy = "ManageServices")]
    public async Task<IActionResult> CreateService(
        [FromBody] CreateServiceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var service = await _serviceManagement.CreateServiceAsync(
                request, cancellationToken);

            return CreatedAtAction(
                nameof(GetService),
                new { id = service.Id },
                service);
        }
        catch (ArgumentException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request",
                detail: exception.Message);
        }
    }

    // Only Administrators can modify service configuration.
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ManageServices")]
    public async Task<IActionResult> UpdateService(
        Guid id,
        [FromBody] UpdateServiceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _serviceManagement.UpdateServiceAsync(
                id, request, cancellationToken);

            return updated ? NoContent() : NotFound();
        }
        catch (ArgumentException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request",
                detail: exception.Message);
        }
    }
}
