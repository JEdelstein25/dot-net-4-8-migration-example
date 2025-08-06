using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using TaxCalculator.Core.Models;
using TaxCalculator.Data.Interfaces;

namespace TaxCalculator.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IConnectionFactory _connectionFactory;

        public UserRepository(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<User> GetUserAsync(Guid userId)
        {
            const string sql = @"
                SELECT UserId, FirstName, LastName, Email, DateOfBirth, TFN, ResidencyStatus, 
                       CreatedDate, LastModifiedDate, IsActive
                FROM Users 
                WHERE UserId = @UserId AND IsActive = 1";

            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sql, (SqlConnection)connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new User
                            {
                                UserId = reader.GetGuid(0),
                                FirstName = reader.GetString(1),
                                LastName = reader.GetString(2),
                                Email = reader.GetString(3),
                                DateOfBirth = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
                                TFN = reader.IsDBNull(5) ? null : reader.GetString(5),
                                ResidencyStatus = reader.GetString(6),
                                CreatedDate = reader.GetDateTime(7),
                                LastModifiedDate = reader.GetDateTime(8),
                                IsActive = reader.GetBoolean(9)
                            };
                        }
                    }
                }
            }
            return null;
        }

        public async Task<Guid> CreateUserAsync(User user)
        {
            user.UserId = Guid.NewGuid();
            user.CreatedDate = DateTime.UtcNow;
            user.LastModifiedDate = DateTime.UtcNow;

            const string sql = @"
                INSERT INTO Users (UserId, FirstName, LastName, Email, DateOfBirth, TFN, ResidencyStatus, CreatedDate, LastModifiedDate, IsActive)
                VALUES (@UserId, @FirstName, @LastName, @Email, @DateOfBirth, @TFN, @ResidencyStatus, @CreatedDate, @LastModifiedDate, @IsActive)";

            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sql, (SqlConnection)connection))
                {
                    command.Parameters.AddWithValue("@UserId", user.UserId);
                    command.Parameters.AddWithValue("@FirstName", user.FirstName);
                    command.Parameters.AddWithValue("@LastName", user.LastName);
                    command.Parameters.AddWithValue("@Email", user.Email);
                    command.Parameters.AddWithValue("@DateOfBirth", (object)user.DateOfBirth ?? DBNull.Value);
                    command.Parameters.AddWithValue("@TFN", (object)user.TFN ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ResidencyStatus", user.ResidencyStatus);
                    command.Parameters.AddWithValue("@CreatedDate", user.CreatedDate);
                    command.Parameters.AddWithValue("@LastModifiedDate", user.LastModifiedDate);
                    command.Parameters.AddWithValue("@IsActive", user.IsActive);

                    await command.ExecuteNonQueryAsync();
                }
            }

            return user.UserId;
        }

        public async Task UpdateUserAsync(User user)
        {
            user.LastModifiedDate = DateTime.UtcNow;

            const string sql = @"
                UPDATE Users 
                SET FirstName = @FirstName, LastName = @LastName, Email = @Email, 
                    DateOfBirth = @DateOfBirth, TFN = @TFN, ResidencyStatus = @ResidencyStatus, 
                    LastModifiedDate = @LastModifiedDate, IsActive = @IsActive
                WHERE UserId = @UserId";

            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sql, (SqlConnection)connection))
                {
                    command.Parameters.AddWithValue("@UserId", user.UserId);
                    command.Parameters.AddWithValue("@FirstName", user.FirstName);
                    command.Parameters.AddWithValue("@LastName", user.LastName);
                    command.Parameters.AddWithValue("@Email", user.Email);
                    command.Parameters.AddWithValue("@DateOfBirth", (object)user.DateOfBirth ?? DBNull.Value);
                    command.Parameters.AddWithValue("@TFN", (object)user.TFN ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ResidencyStatus", user.ResidencyStatus);
                    command.Parameters.AddWithValue("@LastModifiedDate", user.LastModifiedDate);
                    command.Parameters.AddWithValue("@IsActive", user.IsActive);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            const string sql = @"
                SELECT COUNT(*) 
                FROM Users 
                WHERE Email = @Email AND IsActive = 1";

            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sql, (SqlConnection)connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    var count = (int)await command.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }

        public async Task<List<User>> GetAllActiveUsersAsync()
        {
            const string sql = @"
                SELECT UserId, FirstName, LastName, Email, DateOfBirth, TFN, ResidencyStatus, 
                       CreatedDate, LastModifiedDate, IsActive
                FROM Users 
                WHERE IsActive = 1
                ORDER BY LastModifiedDate DESC";

            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sql, (SqlConnection)connection))
                {
                    var users = new List<User>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new User
                            {
                                UserId = reader.GetGuid(0),
                                FirstName = reader.GetString(1),
                                LastName = reader.GetString(2),
                                Email = reader.GetString(3),
                                DateOfBirth = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
                                TFN = reader.IsDBNull(5) ? null : reader.GetString(5),
                                ResidencyStatus = reader.GetString(6),
                                CreatedDate = reader.GetDateTime(7),
                                LastModifiedDate = reader.GetDateTime(8),
                                IsActive = reader.GetBoolean(9)
                            });
                        }
                    }
                    return users;
                }
            }
        }
    }
}
