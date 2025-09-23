namespace ESS.Api.Infrastructure.Minio;

public sealed class MinioUploadResult
{
    public string ObjectName { get; set; }
    public string ETag { get; set; }
    public long Size { get; set; }
}
