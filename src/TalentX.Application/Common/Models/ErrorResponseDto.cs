namespace TalentX.Application.Common.Models;

public sealed record UserResponse(
    string Message,
    string? ResourceId = null,
    IReadOnlyList<string>? Errors = null);

public sealed record DevResponse(
    string Message,
    string? Details = null,
    string? ExceptionType = null,
    string? StackTrace = null);

public sealed record ErrorResponseDto(
    UserResponse UserResponse,
    DevResponse DevResponse);
