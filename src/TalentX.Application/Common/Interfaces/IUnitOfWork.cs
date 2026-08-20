using TalentX.Application.Common.Interfaces.Repositories;

namespace TalentX.Application.Common.Interfaces;
public interface IUnitOfWork
{
    IJobRepository Jobs {get;}

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}