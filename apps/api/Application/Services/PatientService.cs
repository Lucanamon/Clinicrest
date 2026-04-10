using api.Application.Abstractions;
using api.Application.Patients;
using api.Domain.Entities;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace api.Application.Services;

public class PatientService(
    IPatientRepository repository,
    IGoogleDriveService googleDriveService,
    ILogger<PatientService> logger) : IPatientService
{
    public async Task<PatientDto> CreateAsync(
        CreatePatientRequest request,
        CancellationToken cancellationToken = default)
    {
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DateOfBirth = DateTime.SpecifyKind(request.DateOfBirth.Date, DateTimeKind.Utc),
            Gender = request.Gender.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            UnderlyingDisease = NormalizeOptional(request.UnderlyingDisease)
        };

        await repository.AddAsync(patient, cancellationToken);
        return MapToDto(patient);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await repository.DeleteAsync(id, cancellationToken);
    }

    public async Task<PatientExportResult> ExportAsync(
        PatientExportRequest request,
        CancellationToken cancellationToken = default)
    {
        var patients = await repository.GetForExportAsync(request, cancellationToken);
        var summary = await repository.GetSummaryCountsAsync(cancellationToken);

        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Patients");

        sheet.Cells["A1:F1"].Merge = true;
        sheet.Cells["A1"].Value = "CLINICREST HOSPITAL";
        sheet.Cells["A1"].Style.Font.Size = 18;
        sheet.Cells["A1"].Style.Font.Bold = true;
        sheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        sheet.Cells["A2:F2"].Merge = true;
        sheet.Cells["A2"].Value = "Patient Summary Report";
        sheet.Cells["A2"].Style.Font.Bold = true;
        sheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        sheet.Cells["A3:F3"].Merge = true;
        sheet.Cells["A3"].Value = $"Generated: {DateTime.Now:yyyy-MM-dd}";
        sheet.Cells["A3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        var header = sheet.Cells["A5:F5"];
        header.Style.Font.Bold = true;
        header.Style.Fill.PatternType = ExcelFillStyle.Solid;
        header.Style.Fill.BackgroundColor.SetColor(Color.LightGray);

        sheet.Cells[5, 1].Value = "No.";
        sheet.Cells[5, 2].Value = "Full Name";
        sheet.Cells[5, 3].Value = "Age";
        sheet.Cells[5, 4].Value = "Phone";
        sheet.Cells[5, 5].Value = "Disease";
        sheet.Cells[5, 6].Value = "Created Date";

        var row = 6;
        var number = 1;
        foreach (var p in patients)
        {
            sheet.Cells[row, 1].Value = number++;
            sheet.Cells[row, 2].Value = $"{p.FirstName} {p.LastName}".Trim();
            sheet.Cells[row, 3].Value = CalculateAge(p.DateOfBirth);
            sheet.Cells[row, 4].Value = p.PhoneNumber;
            sheet.Cells[row, 5].Value = p.UnderlyingDisease;
            sheet.Cells[row, 6].Value = p.CreatedAt.ToString("yyyy-MM-dd");
            row++;
        }

        var dataEndRow = Math.Max(row - 1, 5);
        var range = sheet.Cells[5, 1, dataEndRow, 6];
        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

        sheet.Cells.AutoFitColumns();

        var summarySheet = package.Workbook.Worksheets.Add("Summary");
        summarySheet.Cells[1, 1].Value = "Total Patients";
        summarySheet.Cells[1, 2].Value = summary.TotalPatients;
        summarySheet.Cells[2, 1].Value = "Active Patients";
        summarySheet.Cells[2, 2].Value = summary.ActivePatients;
        summarySheet.Cells[3, 1].Value = "Deleted Patients";
        summarySheet.Cells[3, 2].Value = summary.DeletedPatients;
        summarySheet.Cells.AutoFitColumns();

        var content = await package.GetAsByteArrayAsync(cancellationToken);
        var fileName = $"patients-report-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        string? googleSheetUrl = null;

        try
        {
            await using var stream = new MemoryStream(content);
            googleSheetUrl = await googleDriveService.UploadExcelAsGoogleSheetAsync(stream, fileName, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload exported patient report to Google Drive.");
        }

        return new PatientExportResult
        {
            Content = content,
            FileName = fileName,
            GoogleSheetUrl = googleSheetUrl
        };
    }

    public async Task<PagedResult<PatientDto>> GetPagedAsync(
        PatientQueryParams query,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await repository.GetPagedAsync(query, cancellationToken);

        return new PagedResult<PatientDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<PagedResult<PatientPreviewItemDto>> GetPreviewAsync(
        PatientQueryParams query,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await repository.GetPagedAsync(query, cancellationToken);

        return new PagedResult<PatientPreviewItemDto>
        {
            Items = items.Select(p => new PatientPreviewItemDto
            {
                Name = $"{p.FirstName} {p.LastName}".Trim(),
                Age = CalculateAge(p.DateOfBirth),
                Phone = p.PhoneNumber,
                Disease = p.UnderlyingDisease,
                CreatedAt = p.CreatedAt
            }).ToList(),
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<PatientDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var patient = await repository.GetByIdAsync(id, cancellationToken);
        return patient is null ? null : MapToDto(patient);
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        UpdatePatientRequest request,
        CancellationToken cancellationToken = default)
    {
        var patient = await repository.GetByIdAsync(id, cancellationToken);
        if (patient is null)
        {
            return false;
        }

        patient.FirstName = request.FirstName.Trim();
        patient.LastName = request.LastName.Trim();
        patient.DateOfBirth = DateTime.SpecifyKind(request.DateOfBirth.Date, DateTimeKind.Utc);
        patient.Gender = request.Gender.Trim();
        patient.PhoneNumber = request.PhoneNumber.Trim();
        patient.UnderlyingDisease = NormalizeOptional(request.UnderlyingDisease);

        return await repository.UpdateAsync(patient, cancellationToken);
    }

    private static PatientDto MapToDto(Patient patient)
    {
        return new PatientDto
        {
            Id = patient.Id,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            PhoneNumber = patient.PhoneNumber,
            UnderlyingDisease = patient.UnderlyingDisease,
            Age = patient.Age,
            CreatedAt = patient.CreatedAt
        };
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int CalculateAge(DateTime dob)
    {
        var today = DateTime.Today;
        var age = today.Year - dob.Year;
        if (dob.Date > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}
