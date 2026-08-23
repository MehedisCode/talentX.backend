namespace TalentX.Infrastructure.FileStorage;

/// <summary>
/// Strongly-typed options for AWS S3 file storage.
/// Credentials (AccessKey / SecretKey) are NOT stored here;
/// they are supplied via environment variables (AWS_ACCESS_KEY_ID /
/// AWS_SECRET_ACCESS_KEY) and resolved automatically by the AWS SDK
/// default credential provider chain.
/// </summary>
public sealed class AwsS3Settings
{
    public const string SectionName = "FileStorage:S3";

    /// <summary>The target S3 bucket name.</summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>AWS region identifier, e.g. "ap-southeast-1".</summary>
    public string Region { get; set; } = "ap-southeast-1";

    /// <summary>
    /// Optional folder prefix inside the bucket.
    /// Defaults to "resumes" so objects are stored as "resumes/{fileName}".
    /// </summary>
    public string FolderPrefix { get; set; } = "resumes";
}
