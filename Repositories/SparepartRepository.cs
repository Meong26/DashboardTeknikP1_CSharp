using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DashboardTeknikP1.Models;

namespace DashboardTeknikP1.Repositories
{
    public class SparepartRepository
    {
        private readonly string _connectionString;

        public SparepartRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // PERBAIKAN: Menggunakan async Task agar proses antri di server web jauh lebih ringan
        public async Task<List<SAP_Sparepart>> GetAllSparepartsAsync()
        {
            var list = new List<SAP_Sparepart>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM tbl_SAP_Sparepart";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    await conn.OpenAsync(); // Membuka koneksi di latar belakang

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync()) // Membaca di latar belakang
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new SAP_Sparepart
                            {
                                PlantCode = reader["PlantCode"] != DBNull.Value ? reader["PlantCode"].ToString() : null,
                                MaterialNo = reader["MaterialNo"] != DBNull.Value ? reader["MaterialNo"].ToString() : null,
                                MaterialNoDescription = reader["MaterialNoDescription"] != DBNull.Value ? reader["MaterialNoDescription"].ToString() : null,
                                MatrGroupDescription = reader["MatrGroupDescription"] != DBNull.Value ? reader["MatrGroupDescription"].ToString() : null,
                                StorBin = reader["StorBin"] != DBNull.Value ? reader["StorBin"].ToString() : null,
                                BUn = reader["BUn"] != DBNull.Value ? reader["BUn"].ToString() : "PC",
                                TotalQtyStock = reader["TotalQtyStock"] != DBNull.Value ? Convert.ToDouble(reader["TotalQtyStock"]) : 0,
                                SafetyStock = reader["SafetyStock"] != DBNull.Value ? Convert.ToInt32(reader["SafetyStock"]) : 0,
                                LamaTdkBergerakDay = reader["LamaTdkBergerakDay"] != DBNull.Value ? Convert.ToInt32(reader["LamaTdkBergerakDay"]) : 0,
                                MvgAvgPriceIDR = reader["MvgAvgPriceIDR"] != DBNull.Value ? Convert.ToDecimal(reader["MvgAvgPriceIDR"]) : 0,
                                Priority = reader["Priority"] != DBNull.Value ? reader["Priority"].ToString() : null
                            });
                        }
                    }
                }
            }
            return list;
        }

        public async Task SavePrioritiesBulkAsync(List<string> priorityMaterialNos)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Reset seluruh bendera prioritas menjadi kosong/NULL
                        string resetQuery = "UPDATE tbl_SAP_Sparepart SET Priority = NULL";
                        using (SqlCommand cmd = new SqlCommand(resetQuery, conn, trans))
                        {
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // 2. Set nilai 'Y' hanya untuk nomor material yang dikirim dari antarmuka
                        if (priorityMaterialNos != null && priorityMaterialNos.Any())
                        {
                            string updateQuery = "UPDATE tbl_SAP_Sparepart SET Priority = 'Y' WHERE MaterialNo = @MatNo";
                            using (SqlCommand cmd = new SqlCommand(updateQuery, conn, trans))
                            {
                                cmd.Parameters.Add("@MatNo", System.Data.SqlDbType.VarChar, 50);
                                foreach (var matNo in priorityMaterialNos)
                                {
                                    cmd.Parameters["@MatNo"].Value = matNo;
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }
                        }
                        trans.Commit();
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}