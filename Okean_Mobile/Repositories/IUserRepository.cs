using Okean_Mobile.Models;

namespace Okean_Mobile.Repositories
{
    public interface IUserRepository
    {
        Task<User> GetByUsernameAsync(string username);
        Task AddUserAsync(User user);
        Task<User> GetByEmailAsync(string email); // Thêm phương thức mới
        Task<User> GetByIdAsync(int id);
    }
}
