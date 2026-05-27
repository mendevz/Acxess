using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using Amazon.S3;
using Amazon.S3.Model;
using ImageMagick;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace Acxess.Infrastructure.Services;

public partial class CloudflareR2StorageService(
    IAmazonS3 s3Client,
    IConfiguration config) : IImageStorageService
{

    private readonly string _bucketName = config["CloudflareR2:BucketName"]!;
    private readonly string _publicDomain = config["CloudflareR2:PublicDomain"]!; 

    public async Task<Result> DeleteImageAsync(string photoUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(photoUrl))
            return Result.Success();

        var uri = new Uri(photoUrl);
        var key = uri.AbsolutePath.TrimStart('/');

        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        };

        await s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<string>> SaveImageAsync(string base64Image, string fileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(base64Image))
            return string.Empty;

        var match = HeadBase64Regex().Match(base64Image);
        var base64Data = match.Success ? match.Groups["data"].Value : base64Image;
        var imageBytes = Convert.FromBase64String(base64Data);


        using var memoryStream = new MemoryStream();

        using (var image = new MagickImage(imageBytes))
        {
            var geometry = new MagickGeometry(800, 800)
            {
                IgnoreAspectRatio = false
            };
            image.Resize(geometry);

            image.Format = MagickFormat.WebP;
            image.Quality = 80;

            image.Write(memoryStream);
        }
        memoryStream.Position = 0;

        var uniqueFileName = $"members/{Guid.NewGuid()}_{fileName}.webp";

        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = uniqueFileName,
            InputStream = memoryStream,
            ContentType = "image/webp",
            DisablePayloadSigning = true
        };

        await s3Client.PutObjectAsync(putRequest, cancellationToken);
        return $"{_publicDomain}/{uniqueFileName}";
    }

    [GeneratedRegex(@"data:image/(?<type>.+?);base64,(?<data>.+)")]
    private static partial Regex HeadBase64Regex();
}
