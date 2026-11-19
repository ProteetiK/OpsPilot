
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsPilot.Application.Incidents;
using System.Security.Claims;

namespace OpsPilot.Api.Controllers;

[ApiController]
[Route("api/incidents")]
[Authorize]
public class IncidentsController : ControllerBase
{
	private readonly IIncidentService _incidentService;

	public IncidentsController(IIncidentService incidentService)
	{
		_incidentService = incidentService;
	}

	// All authenticated roles can view incidents.
	[HttpGet]
	public async Task<IActionResult> GetIncidents(
		CancellationToken cancellationToken)
	{
		var incidents = await _incidentService.GetIncidentsAsync(
			cancellationToken);

		return Ok(incidents);
	}

	[HttpGet("{id:guid}")]
	public async Task<IActionResult> GetIncident(
		Guid id,
		CancellationToken cancellationToken)
	{
		var incident = await _incidentService.GetIncidentByIdAsync(
			id, cancellationToken);

		return incident is null ? NotFound() : Ok(incident);
	}

	// Engineers and Administrators can investigate/create incidents.
	[HttpPost]
	[Authorize(Policy = "IncidentInvestigation")]
	public async Task<IActionResult> CreateIncident(
		[FromBody] CreateIncidentRequest request,
		CancellationToken cancellationToken)
	{
		try
		{
			var incident = await _incidentService.CreateIncidentAsync(
				request, cancellationToken);

			return CreatedAtAction(
				nameof(GetIncident),
				new { id = incident.Id },
				incident);
		}
		catch (ArgumentException exception)
		{
			return BadRequest(new { error = exception.Message });
		}
	}

	// Engineers and Administrators can update incident status.
	[HttpPut("{id:guid}/status")]
	[Authorize(Policy = "IncidentUpdate")]
	public async Task<IActionResult> UpdateStatus(
		Guid id,
		[FromBody] UpdateIncidentStatusRequest request,
		CancellationToken cancellationToken)
	{
		try
		{
			var updated = await _incidentService.UpdateStatusAsync(
				id, request, cancellationToken);

			return updated ? NoContent() : NotFound();
		}
		catch (ArgumentException exception)
		{
			return BadRequest(new { error = exception.Message });
		}
	}

	[HttpPut("{id:guid}/assignment")]
	[Authorize(Policy = "IncidentUpdate")]
	public async Task<IActionResult> AssignIncident(
		Guid id,
		[FromBody] AssignIncidentRequest request,
		CancellationToken cancellationToken)
	{
		try
		{
			var updated = await _incidentService.AssignIncidentAsync(
				id, request.AssignedToUserId, cancellationToken);

			return updated ? NoContent() : NotFound();
		}
		catch (ArgumentException exception)
		{
			return BadRequest(new { error = exception.Message });
		}
	}
}

public sealed record AssignIncidentRequest(Guid? AssignedToUserId);
