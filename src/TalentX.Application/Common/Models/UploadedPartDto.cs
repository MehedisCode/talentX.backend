namespace TalentX.Application.Common.Models;

public sealed record UploadedPartDto(
    int PartNumber,
    string ETag,
    long SizeBytes);
