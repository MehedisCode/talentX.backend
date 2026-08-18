using FluentAssertions;
using TalentX.Application.Common.Exceptions;

namespace TalentX.Api.Tests.Application.Common.Exceptions;

public class ExceptionsTests
{
    [Fact]
    public void ValidationException_ExposesTheGivenMessage()
    {
        var exception = new ValidationException("Title is required.");

        exception.Message.Should().Be("Title is required.");
    }

    [Fact]
    public void NotFoundException_ExposesTheGivenMessage()
    {
        var exception = new NotFoundException("Job 'x' was not found.");

        exception.Message.Should().Be("Job 'x' was not found.");
    }

    [Fact]
    public void ConflictException_ExposesTheGivenMessage()
    {
        var exception = new ConflictException("Job already published.");

        exception.Message.Should().Be("Job already published.");
    }

    [Fact]
    public void UnauthorizedException_ExposesTheGivenMessage()
    {
        var exception = new UnauthorizedException("Invalid credentials.");

        exception.Message.Should().Be("Invalid credentials.");
    }

    [Fact]
    public void ForbiddenException_HasAFixedDefaultMessage()
    {
        var exception = new ForbiddenException();

        exception.Message.Should().Be("You are not allowed to perform this action.");
    }

    [Fact]
    public void AllCustomExceptions_DeriveFromException()
    {
        new ValidationException("m").Should().BeAssignableTo<Exception>();
        new NotFoundException("m").Should().BeAssignableTo<Exception>();
        new ConflictException("m").Should().BeAssignableTo<Exception>();
        new UnauthorizedException("m").Should().BeAssignableTo<Exception>();
        new ForbiddenException().Should().BeAssignableTo<Exception>();
    }
}
