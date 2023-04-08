using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using File = Google.Apis.Drive.v3.Data.File;

public class UserFeedbackUploader
{
    readonly DriveService _service;

    public UserFeedbackUploader()
    {
        var base64_encoded_json = Secrets.Get("GDRIVE_SERVICE_ACCOUNT");
        string decodedJson = Encoding.UTF8.GetString(Convert.FromBase64String(base64_encoded_json));

        var credential = GoogleCredential.FromJson(decodedJson).CreateScoped(DriveService.Scope.Drive);
        _service = new(new()
        {
            HttpClientInitializer = credential,
            ApplicationName = "LeesSimpel",
        });
    }
    
    public async Task<string> Upload(string? ocrResult, string? humanFeedback, string? summary, IFormFileCollection images)
    {
        var (thisFeedbackFolder,foldername) = await MakeFolderForThisFeedback();

        var uploads = images
            .Select(img => UploadFileForFeedback(thisFeedbackFolder, img.FileName, img.OpenReadStream(), img.ContentType))
            .ToList();
        
        if (ocrResult != null)
            uploads.Add(UploadFileForFeedback(thisFeedbackFolder, "ocrresult.txt", StreamForString(ocrResult), "text/plain"));
        if (summary != null)
            uploads.Add(UploadFileForFeedback(thisFeedbackFolder, "summary.json", StreamForString(summary), "text/plain"));
        if (humanFeedback != null)
            uploads.Add(UploadFileForFeedback(thisFeedbackFolder, "humanfeedback.txt", StreamForString(humanFeedback), "text/plain"));
        
        await Task.WhenAll(uploads);
        var upload = foldername[..6];
        return upload;
    }

    async Task<(File, string name)> MakeFolderForThisFeedback()
    {
        var name = Guid.NewGuid().ToString();
        var createFolderRequest = _service.Files.Create(new()
        {
            Name = name,
            MimeType = "application/vnd.google-apps.folder",
            Parents = new[]
            {
                (await RootFeedbackFolder()).Id
            }
        });
        createFolderRequest.Fields = "id";

        return (await createFolderRequest.ExecuteAsync(), name);
    }

    async Task<IUploadProgress> UploadFileForFeedback(File thisFeedbackFolder, string filename, Stream stream, string contentType)
    {
        try
        {
            var fileMetadata = new File()
            {
                Name = filename,
                Parents = new[]
                {
                    thisFeedbackFolder.Id
                }
            };

            var uploadRequest = _service.Files.Create(fileMetadata, stream, contentType);
            uploadRequest.Fields = "id";
            return await uploadRequest.UploadAsync();
        }
        finally
        {
            await stream.DisposeAsync();
        }
    }

    static MemoryStream StreamForString(string str)
    {
        MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(str));
        memoryStream.Position = 0;
        return memoryStream;
    }

    async Task<File> RootFeedbackFolder()
    {
        var folderRequest = _service.Files.List();
        folderRequest.Q = "mimeType='application/vnd.google-apps.folder' and trashed=false and name='LeesSimpelFeedback'";
        var folderResult = await folderRequest.ExecuteAsync();
        var folder = folderResult.Files[0];
        return folder;
    }
}