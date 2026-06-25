using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<List<SAP_Sparepart>> GetAllSparepartsAsync()
        {
            var list = new List<SAP_Sparepart>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM tbl_SAP_Sparepart";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    await conn.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new SAP_Sparepart
                            {
                                Plant = reader["Plant"] != DBNull.Value ? reader["Plant"].ToString() : "",
                                Material = reader["Material"] != DBNull.Value ? reader["Material"].ToString() : "",
                                MaterialDescription = reader["MaterialDescription"] != DBNull.Value ? reader["MaterialDescription"].ToString() : "",
                                UoM = reader["UoM"] != DBNull.Value ? reader["UoM"].ToString() : "PC",
                                MovingUnitPrice = reader["MovingUnitPrice"] != DBNull.Value ? Convert.ToDecimal(reader["MovingUnitPrice"]) : 0,
                                CurrentStock = reader["CurrentStock"] != DBNull.Value ? Convert.ToDouble(reader["CurrentStock"]) : 0,
                                SafetyStock = reader["SafetyStock"] != DBNull.Value ? Convert.ToInt32(reader["SafetyStock"]) : 0,
                                MatType = reader["MatType"] != DBNull.Value ? reader["MatType"].ToString() : "",
                                StorLoct = reader["StorLoct"] != DBNull.Value ? reader["StorLoct"].ToString() : "",
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
                        // 1. Simpan ke Tabel Abadi (Backup)
                        SqlCommand cmd1 = new SqlCommand("DELETE FROM tbl_Sparepart_Priority", conn, trans);
                        await cmd1.ExecuteNonQueryAsync();

                        if (priorityMaterialNos != null && priorityMaterialNos.Any())
                        {
                            string qInsert = "INSERT INTO tbl_Sparepart_Priority (Material) VALUES (@MatNo)";
                            using (SqlCommand cmd2 = new SqlCommand(qInsert, conn, trans))
                            {
                                cmd2.Parameters.Add("@MatNo", System.Data.SqlDbType.VarChar, 100);
                                foreach (var matNo in priorityMaterialNos)
                                {
                                    cmd2.Parameters["@MatNo"].Value = matNo;
                                    await cmd2.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        // 2. Sinkronkan ke Tabel Utama (Untuk UI)
                        SqlCommand cmd3 = new SqlCommand("UPDATE tbl_SAP_Sparepart SET Priority = NULL", conn, trans);
                        await cmd3.ExecuteNonQueryAsync();

                        if (priorityMaterialNos != null && priorityMaterialNos.Any())
                        {
                            string qUpdate = "UPDATE tbl_SAP_Sparepart SET Priority = 'Y' WHERE Material = @MatNo";
                            using (SqlCommand cmd4 = new SqlCommand(qUpdate, conn, trans))
                            {
                                cmd4.Parameters.Add("@MatNo", System.Data.SqlDbType.VarChar, 100);
                                foreach (var matNo in priorityMaterialNos)
                                {
                                    cmd4.Parameters["@MatNo"].Value = matNo;
                                    await cmd4.ExecuteNonQueryAsync();
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