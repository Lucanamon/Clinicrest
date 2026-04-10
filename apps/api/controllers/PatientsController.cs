using api.Application.Abstractions;
using api.Application.Patients;
using api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController(IPatientService patientService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = Roles.ClinicalAll)]
    public async Task<ActionResult<PagedResult<PatientDto>>> GetPatients(
        [FromQuery] PatientQueryParams query,
        CancellationToken cancellationToken = default)
    {
        if (query.PageNumber < 1)
        {
            query.PageNumber = 1;
        }

        if (query.PageSize < 1)
        {
            query.PageSize = 10;
        }

        if (query.PageSize > 100)
        {
            query.PageSize = 100;
        }

        var result = await patientService.GetPagedAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.ClinicalAll)]
    public async Task<ActionResult<PatientDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var patient = await patientService.GetByIdAsync(id, cancellationToken);
        if (patient is null)
        {
            return NotFound();
        }

        return Ok(patient);
    }

    [HttpPost]
    [Authorize(Roles = Roles.ClinicalAll)]
    public async Task<ActionResult<PatientDto>> Create(
        [FromBody] CreatePatientRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var created = await patientService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.ClinicalAll)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePatientRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var updated = await patientService.UpdateAsync(id, request, cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.RootAdmin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await patientService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("export")]
    [Authorize(Roles = Roles.ClinicalAll)]
    public async Task<IActionResult> ExportPatients(
        [FromBody] PatientExportRequest? filter,
        CancellationToken cancellationToken)
    {
        var export = await patientService.ExportAsync(filter ?? new PatientExportRequest(), cancellationToken);
        var reportsDir = Path.Combine("wwwroot", "reports");
        Directory.CreateDirectory(reportsDir);

        var filePath = Path.Combine(reportsDir, export.FileName);
        await System.IO.File.WriteAllBytesAsync(filePath, export.Content, cancellationToken);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var downloadUrl = $"{baseUrl}/reports/{export.FileName}";
        var encodedDownloadUrl = WebUtility.UrlEncode(downloadUrl);
        var viewUrl = $"https://view.officeapps.live.com/op/view.aspx?src={encodedDownloadUrl}";

        return Ok(new
        {
            downloadUrl,
            viewUrl,
            googleSheetUrl = export.GoogleSheetUrl
        });
    }

    [HttpGet("preview")]
    [Authorize(Roles = Roles.ClinicalAll)]
    public async Task<ActionResult<PagedResult<PatientPreviewItemDto>>> PreviewPatients(
        [FromQuery] PatientQueryParams query,
        CancellationToken cancellationToken)
    {
        if (query.PageNumber < 1)
        {
            query.PageNumber = 1;
        }

        if (query.PageSize < 1)
        {
            query.PageSize = 10;
        }

        if (query.PageSize > 100)
        {
            query.PageSize = 100;
        }

        var result = await patientService.GetPreviewAsync(query, cancellationToken);
        return Ok(result);
    }
}
