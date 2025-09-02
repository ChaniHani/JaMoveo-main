using JaMoveo.Infrastructure.Entities;

namespace JaMoveo.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<ApplicationUser> GetByIdAsync(int id);
        Task<ApplicationUser> GetByUsernameAsync(string username);
        Task<bool> ExistsByUsernameAsync(string username);
        Task<ApplicationUser> UpdateAsync(ApplicationUser user);
        Task<bool> DeleteAsync(int id);
        Task<List<ApplicationUser>> GetAllAsync();
        Task<List<ApplicationUser>> GetActiveUsersAsync();
    }
}
