using TalentX.Application.Common.Interfaces.Repositories;

namespace TalentX.Application.Common.Interfaces;
public interface IUnitOfWork
{
    IUserRepository Users {get;}

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}