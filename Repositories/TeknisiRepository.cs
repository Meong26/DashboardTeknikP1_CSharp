using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DashboardTeknikP1.Models;
using Dapper;

namespace DashboardTeknikP1.Repositories
{
    public class TeknisiRepository
    {
        private readonly string _connectionString;

        public TeknisiRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<List<Teknisi>> GetAllTeknisiAsync()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT NIK, Nama, Plant FROM tbl_Teknisi ORDER BY Nama ASC";
                var result = await conn.QueryAsync<Teknisi>(query);
                return result.ToList();
            }
        }

        public async Task InsertTeknisiAsync(Teknisi teknisi)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "INSERT INTO tbl_Teknisi (NIK, Nama, Plant) VALUES (@NIK, @Nama, @Plant)";
                await conn.ExecuteAsync(query, teknisi);
            }
        }

        public async Task UpdateTeknisiAsync(Teknisi teknisi)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "UPDATE tbl_Teknisi SET Nama = @Nama, Plant = @Plant WHERE NIK = @NIK";
                await conn.ExecuteAsync(query, teknisi);
            }
        }

        public async Task DeleteTeknisiAsync(string nik)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM tbl_Teknisi WHERE NIK = @NIK";
                await conn.ExecuteAsync(query, new { NIK = nik });
            }
        }
    }
}
