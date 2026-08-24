using Amazon;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.FileStorage;

public static class FileStorageStartup
{
    public static IServiceCollection AddFileStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = Environment.GetEnvironmentVariable("FILE_STORAGE_PROVIDER")
            ?? configuration["FILE_STORAGE_PROVIDER"]
            ?? configuration["FileStorage:Provider"]
            ?? "Local";

        if (provider.Equals("S3", StringComparison.OrdinalIgnoreCase))
        {
            var bucketName = Environment.GetEnvironmentVariable("FILE_STORAGE_S3_BUCKET_NAME")
                ?? Environment.GetEnvironmentVariable("AWS_S3_BUCKET_NAME")
                ?? configuration["FILE_STORAGE_S3_BUCKET_NAME"]
                ?? configuration["FileStorage:S3:BucketName"]
                ?? "talentx-s3-staging";

            var region = Environment.GetEnvironmentVariable("FILE_STORAGE_S3_REGION")
                ?? Environment.GetEnvironmentVariable("AWS_S3_REGION")
                ?? Environment.GetEnvironmentVariable("AWS_REGION")
                ?? configuration["FILE_STORAGE_S3_REGION"]
                ?? configuration["FileStorage:S3:Region"]
                ?? "ap-southeast-1";

            var folderPrefix = Environment.GetEnvironmentVariable("FILE_STORAGE_S3_FOLDER_PREFIX")
                ?? Environment.GetEnvironmentVariable("AWS_S3_FOLDER_PREFIX")
                ?? configuration["FILE_STORAGE_S3_FOLDER_PREFIX"]
                ?? configuration["FileStorage:S3:FolderPrefix"]
                ?? "resumes";

            services.Configure<AwsS3Settings>(options =>
            {
                options.BucketName = bucketName;
                options.Region = region;
                options.FolderPrefix = folderPrefix;
            });

            // Register IAmazonS3 as singleton — it owns the HTTP connection pool
            // and is thread-safe. Credentials come from the AWS SDK default
            // credential provider chain (env vars → ~/.aws → IAM role).
            services.AddSingleton<IAmazonS3>(sp =>
            {
                return new AmazonS3Client(RegionEndpoint.GetBySystemName(region));
            });

            services.AddScoped<IFileStorageService, S3FileStorageService>();
        }
        else
        {
            // Default: local disk storage (development / fallback)
            services.AddScoped<IFileStorageService, LocalFileStorageService>();
        }

        return services;
    }
}
