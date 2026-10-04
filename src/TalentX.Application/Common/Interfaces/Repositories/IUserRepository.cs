using TalentX.Application.Common.Models;
using TalentX.Domain;
using TalentX.Domain.Enums;

namespace TalentX.Application.Common.Interfaces.Repositories;
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    // Case-insensitive: PostgreSQL equality is case-sensitive while account emails
    // are stored as typed, but callers (e.g. guest apply) normalize to lower-case.
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetByUserIdsAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(UserQueryParams queryParams, CancellationToken cancellationToken = default);
    Task<(IReadOnlyDictionary<UserRole, int> RoleCounts, IReadOnlyDictionary<AccountStatus, int> StatusCounts)> GetRoleAndStatusCountsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAdminUserIdsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    void Update(User user);
    void Delete(User user);
}