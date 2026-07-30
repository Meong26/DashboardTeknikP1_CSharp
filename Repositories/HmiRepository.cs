using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Dapper;
using DashboardTeknikP1.Models;

namespace DashboardTeknikP1.Repositories
{
    public class HmiRepository
    {
        private readonly string _connectionString;

        public HmiRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            EnsureTableExists();
        }

        private void EnsureTableExists()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Tb_Hmi_Log' and xtype='U')
                    CREATE TABLE Tb_Hmi_Log (
                        LogID INT IDENTITY(1,1) PRIMARY KEY,
                        UserID VARCHAR(50) NOT NULL,
                        HmiIp VARCHAR(50) NOT NULL,
                        HmiName VARCHAR(100) NOT NULL,
                        WaktuAkses DATETIME NOT NULL DEFAULT GETDATE()
                    )";
                conn.Execute(query);
            }
        }

        public async Task InsertLogAsync(string userId, string hmiIp, string hmiName)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "INSERT INTO Tb_Hmi_Log (UserID, HmiIp, HmiName, WaktuAkses) VALUES (@UserID, @HmiIp, @HmiName, GETDATE())";
                await conn.ExecuteAsync(query, new { UserID = userId, HmiIp = hmiIp, HmiName = hmiName });
            }
        }

        public async Task<List<HmiLog>> GetRecentLogsAsync(int limit = 100)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT TOP (@Limit)
                        l.LogID, 
                        l.UserID, 
                        u.NamaLengkap,
                        r.RoleName,
                        l.HmiIp, 
                        l.HmiName, 
                        l.WaktuAkses
                    FROM Tb_Hmi_Log l
                    LEFT JOIN tbl_Users u ON l.UserID = u.UserID
                    LEFT JOIN tbl_Roles r ON u.RoleID = r.RoleID
                    ORDER BY l.WaktuAkses DESC";
                var result = await conn.QueryAsync<HmiLog>(query, new { Limit = limit });
                return result.AsList();
            }
        }
    }
}
