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
        services.AddSingleton<ILocalStoragePathProvider, LocalStoragePathProvider>();

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

            services.AddSingleton<IAmazonS3>(serviceProvider =>
            {
                return new AmazonS3Client(RegionEndpoint.GetBySystemName(region));
            });

            services.AddScoped<IFileStorageService, S3FileStorageService>();
            services.AddScoped<IPresignedUrlService, S3PresignedUrlService>();
            services.AddScoped<IMultipartStorageService, S3MultipartStorageService>();
        }
        else
        {
            services.AddScoped<IFileStorageService, LocalFileStorageService>();
            services.AddScoped<IPresignedUrlService, LocalPresignedUrlService>();
            services.AddScoped<IMultipartStorageService, LocalMultipartStorageService>();
        }

        return services;
    }
}
