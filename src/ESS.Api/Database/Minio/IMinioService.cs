namespace ESS.Api.Database.Minio;

public interface IMinioService
{
    Task<MinioUploadResult> UploadFileAsync(Stream fileStream, string objectName, string contentType);
    Task<string> GetPresignedUrlAsync(string objectName, int expiryInSeconds = 3600);
    Task DeleteFileAsync(string objectName);
    Task<(Stream Stream, string ContentType, string FileName)> GetObjectAsync(string objectKey, CancellationToken cancellationToken = default);

}
