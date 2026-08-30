using TalentX.Domain;

namespace TalentX.Application.Common.Interfaces.Repositories;
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUserIdAsync(string userId);
    void Update(User user);
}