using ESS.Api.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace ESS.Api.Database.Minio;

public sealed class MinioService(
    IMinioClient minioClient,
    IOptions<MinioConfiguration> options,
    ILogger<MinioService> logger) : IMinioService
{
    public async Task<MinioUploadResult> UploadFileAsync(
            Stream fileStream,
            string objectName,
            string contentType)
    {
        try
        {
            await EnsureBucketExistsAsync();

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(options.Value.BucketName)
                .WithObject(objectName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType);

            var response = await minioClient.PutObjectAsync(putObjectArgs);

            logger.LogInformation("File uploaded successfully: {ObjectName}", objectName);

            return new MinioUploadResult
            {
                ObjectName = objectName,
                ETag = response.Etag,
                Size = response.Size
            };
        }
        catch (MinioException ex)
        {
            logger.LogError(ex, "Error uploading file to MinIO: {ObjectName}", objectName);
            throw new ApplicationException($"Failed to upload file: {ex.Message}", ex);
        }
    }
    public async Task<string> GetPresignedUrlAsync(string objectName, int expiryInSeconds = 3600)
    {
        try
        {
            var presignedGetObjectArgs = new PresignedGetObjectArgs()
                .WithBucket(options.Value.BucketName)
                .WithObject(objectName)
                .WithExpiry(expiryInSeconds);

            var url = await minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);

            logger.LogDebug("Generated presigned URL for: {ObjectName}", objectName);

            return url;
        }
        catch (MinioException ex)
        {
            logger.LogError(ex, "Error generating presigned URL: {ObjectName}", objectName);
            throw new ApplicationException($"Failed to generate presigned URL: {ex.Message}", ex);
        }
    }
    public async Task DeleteFileAsync(string objectName)
    {
        try
        {
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(options.Value.BucketName)
                .WithObject(objectName);

            await minioClient.RemoveObjectAsync(removeObjectArgs);

            logger.LogInformation("File deleted successfully: {ObjectName}", objectName);
        }
        catch (MinioException ex)
        {
            logger.LogError(ex, "Error deleting file from MinIO: {ObjectName}", objectName);
            throw new ApplicationException($"Failed to delete file: {ex.Message}", ex);
        }
    }
    private async Task EnsureBucketExistsAsync()
    {
        try
        {
            var bucketExistsArgs = new BucketExistsArgs()
                .WithBucket(options.Value.BucketName);

            var exists = await minioClient.BucketExistsAsync(bucketExistsArgs);

            if (!exists)
            {
                var makeBucketArgs = new MakeBucketArgs()
                    .WithBucket(options.Value.BucketName);

                await minioClient.MakeBucketAsync(makeBucketArgs);

                logger.LogInformation("Created MinIO bucket: {BucketName}", options.Value.BucketName);
            }
        }
        catch (MinioException ex)
        {
            logger.LogError(ex, "Error ensuring bucket exists: {BucketName}", options.Value.BucketName);
            throw new ApplicationException($"Failed to ensure bucket exists: {ex.Message}", ex);
        }
    }
    public async Task<(Stream Stream, string ContentType, string FileName)> GetObjectAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(options.Value.BucketName)
                .WithObject(objectKey);

            var response = new MemoryStream();
            string fileName = Path.GetFileName(objectKey);

            getObjectArgs = getObjectArgs.WithCallbackStream((stream) =>
            {
                stream.CopyTo(response);
            });

            await minioClient.GetObjectAsync(getObjectArgs, cancellationToken);

            response.Position = 0;

            string extension = Path.GetExtension(objectKey);
            string contentType = FileValidationOptions.GetContentType(extension);

            logger.LogDebug("Retrieved object: {ObjectKey}", objectKey);
            return (response, contentType, fileName);
        }
        catch (ObjectNotFoundException ex)
        {
            logger.LogWarning(ex, "Object not found: {ObjectKey}", objectKey);
            throw new FileNotFoundException($"Object not found: {objectKey}");
        }
        catch (MinioException ex)
        {
            logger.LogError(ex, "Error retrieving object from MinIO: {ObjectKey}", objectKey);
            throw new ApplicationException($"Failed to retrieve object: {ex.Message}", ex);
        }
    }

}
