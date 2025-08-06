using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaxCalculator.Core.Models;

namespace TaxCalculator.Data.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetUserAsync(Guid userId);
        Task<Guid> CreateUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task<bool> UserExistsAsync(string email);
        Task<List<User>> GetAllActiveUsersAsync();
    }
}
