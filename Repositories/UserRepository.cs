using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DashboardTeknikP1.Models;
using Dapper;

namespace DashboardTeknikP1.Repositories
{
    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT u.UserID, u.NamaLengkap, u.RoleID, u.IsActive, r.RoleName 
                                 FROM tbl_Users u
                                 INNER JOIN tbl_Roles r ON u.RoleID = r.RoleID
                                 WHERE u.IsActive = 1
                                 ORDER BY u.NamaLengkap ASC";
                var result = await conn.QueryAsync<User>(query);
                return result.ToList();
            }
        }

        public async Task<User> GetUserByIdAsync(string userId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT u.UserID, u.NamaLengkap, u.RoleID, u.IsActive, r.RoleName 
                                 FROM tbl_Users u
                                 INNER JOIN tbl_Roles r ON u.RoleID = r.RoleID
                                 WHERE u.UserID = @UserID";
                var result = await conn.QueryFirstOrDefaultAsync<User>(query, new { UserID = userId });
                return result;
            }
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT RoleID, RoleName FROM tbl_Roles ORDER BY RoleName ASC";
                var result = await conn.QueryAsync<Role>(query);
                return result.ToList();
            }
        }

        public async Task<bool> AddUserAsync(User user)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO tbl_Users (UserID, NamaLengkap, PasswordHash, RoleID, IsActive)
                                 VALUES (@UserID, @NamaLengkap, @PasswordHash, @RoleID, 1)";
                try
                {
                    int rows = await conn.ExecuteAsync(query, user);
                    return rows > 0;
                }
                catch
                {
                    return false; // Constraint violation (e.g. duplicate NIK)
                }
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE tbl_Users 
                                 SET NamaLengkap = @NamaLengkap, RoleID = @RoleID 
                                 WHERE UserID = @UserID";
                int rows = await conn.ExecuteAsync(query, user);
                return rows > 0;
            }
        }

        // SOFT DELETE
        public async Task<bool> DeleteUserAsync(string userId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE tbl_Users SET IsActive = 0 WHERE UserID = @UserID";
                int rows = await conn.ExecuteAsync(query, new { UserID = userId });
                return rows > 0;
            }
        }

        public async Task<bool> ChangePasswordAsync(string userId, string hashedPassword)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE tbl_Users SET PasswordHash = @PasswordHash WHERE UserID = @UserID";
                int rows = await conn.ExecuteAsync(query, new { UserID = userId, PasswordHash = hashedPassword });
                return rows > 0;
            }
        }
    }
}
