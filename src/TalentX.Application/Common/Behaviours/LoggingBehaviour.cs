using MediatR;
using Microsoft.Extensions.Logging;

namespace TalentX.Application.Common.Behaviours;

public sealed class LoggingBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    public LoggingBehaviour(
        ILogger<LoggingBehaviour<TRequest, TResponse>> logger
    )
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation(
            "Handling request {RequestName}",
            typeof(TRequest).Name
        );

        var response = await next();

        _logger.LogInformation(
            "Completed request {RequestName}",
            typeof(TRequest).Name
        );

        return response;
    }
}