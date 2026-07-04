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
    public class SettingRepository
    {
        private readonly string _connectionString;

        public SettingRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<List<SystemSetting>> GetAllSettingsAsync()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM tbl_Settings ORDER BY SettingKey ASC";
                var result = await conn.QueryAsync<SystemSetting>(query);
                return result.ToList();
            }
        }

        public async Task<SystemSetting> GetSettingByKeyAsync(string key)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM tbl_Settings WHERE SettingKey = @SettingKey";
                var result = await conn.QueryFirstOrDefaultAsync<SystemSetting>(query, new { SettingKey = key });
                return result;
            }
        }

        public async Task<string> GetSettingValueAsync(string key, string defaultValue = "")
        {
            var setting = await GetSettingByKeyAsync(key);
            return setting != null ? setting.SettingValue : defaultValue;
        }

        public async Task<bool> UpdateSettingAsync(string key, string value)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    UPDATE tbl_Settings 
                    SET SettingValue = @SettingValue, LastUpdated = GETDATE()
                    WHERE SettingKey = @SettingKey";
                
                int rowsAffected = await conn.ExecuteAsync(query, new { SettingKey = key, SettingValue = value });
                return rowsAffected > 0;
            }
        }
    }
}
