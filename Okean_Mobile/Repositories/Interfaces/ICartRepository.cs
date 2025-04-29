using Okean_Mobile.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Okean_Mobile.Repositories.Interfaces
{
    public interface ICartRepository
    {
        Task<IEnumerable<CartItem>> GetCartItemsByUserIdAsync(string userId);
        Task AddCartItemAsync(CartItem cartItem);
        Task UpdateCartItemAsync(CartItem cartItem);
        Task RemoveCartItemAsync(int cartItemId);
        Task ClearCartAsync(string userId);
        Task ClearCartAsync(int userId);
        
    }
}