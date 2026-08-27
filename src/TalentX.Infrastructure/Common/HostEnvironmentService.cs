using Microsoft.Extensions.Hosting;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.Common;

public sealed class HostEnvironmentService : IHostEnvironmentService
{
    private readonly IHostEnvironment _hostEnvironment;

    public HostEnvironmentService(IHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
    }

    public bool IsDevelopment => _hostEnvironment.IsDevelopment();
}
