using TalentX.Application.Common.Models;
using TalentX.Domain;
using TalentX.Domain.Enums;

namespace TalentX.Application.Common.Interfaces.Repositories;
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUserIdAsync(string userId);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(UserQueryParams queryParams, CancellationToken cancellationToken = default);
    Task<(IReadOnlyDictionary<UserRole, int> RoleCounts, IReadOnlyDictionary<AccountStatus, int> StatusCounts)> GetRoleAndStatusCountsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAdminUserIdsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    void Update(User user);
}