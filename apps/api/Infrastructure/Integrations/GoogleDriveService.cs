using api.Application.Abstractions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace api.Infrastructure.Integrations;

public class GoogleDriveService(IConfiguration configuration, ILogger<GoogleDriveService> logger) : IGoogleDriveService
{
    private const string GoogleSheetsMimeType = "application/vnd.google-apps.spreadsheet";
    private const string ExcelMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private static readonly string[] Scopes = [DriveService.Scope.DriveFile];

    public async Task<string?> UploadExcelAsGoogleSheetAsync(
        Stream excelStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var clientId = configuration["GoogleDrive:ClientId"];
        var clientSecret = configuration["GoogleDrive:ClientSecret"];
        var refreshToken = configuration["GoogleDrive:RefreshToken"];

        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret) ||
            string.IsNullOrWhiteSpace(refreshToken))
        {
            logger.LogWarning("Google Drive upload skipped because OAuth credentials are not configured.");
            return null;
        }

        var service = await CreateDriveServiceAsync(clientId, clientSecret, refreshToken, cancellationToken);

        var metadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = fileName,
            MimeType = GoogleSheetsMimeType
        };

        var folderId = configuration["GoogleDrive:FolderId"];
        if (!string.IsNullOrWhiteSpace(folderId))
        {
            metadata.Parents = [folderId];
        }

        if (excelStream.CanSeek)
        {
            excelStream.Position = 0;
        }

        var request = service.Files.Create(metadata, excelStream, ExcelMimeType);
        request.Fields = "id";
        await request.UploadAsync(cancellationToken);

        var file = request.ResponseBody;
        if (string.IsNullOrWhiteSpace(file?.Id))
        {
            logger.LogWarning("Google Drive upload completed without a file id.");
            return null;
        }

        return $"https://docs.google.com/spreadsheets/d/{file.Id}/edit";
    }

    private async Task<DriveService> CreateDriveServiceAsync(
        string clientId,
        string clientSecret,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var token = new TokenResponse
        {
            RefreshToken = refreshToken
        };

        var credentials = new UserCredential(
            new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes = Scopes,
                DataStore = new NullDataStore()
            }),
            "clinicrest-google-drive",
            token);

        await credentials.RefreshTokenAsync(cancellationToken);

        var appName = configuration["GoogleDrive:ApplicationName"];
        if (string.IsNullOrWhiteSpace(appName))
        {
            appName = "Clinicrest";
        }

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credentials,
            ApplicationName = appName
        });
    }
}
