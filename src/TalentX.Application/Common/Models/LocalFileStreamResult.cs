namespace TalentX.Application.Common.Models;

public sealed record LocalFileStreamResult(
    Stream Stream,
    string ContentType,
    string FileName);
