namespace api.Application.Abstractions;

public interface IGoogleDriveService
{
    Task<string?> UploadExcelAsGoogleSheetAsync(
        Stream excelStream,
        string fileName,
        CancellationToken cancellationToken = default);
}
