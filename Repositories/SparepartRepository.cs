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
                        string resetQuery = "UPDATE tbl_SAP_Sparepart SET Priority = NULL";
                        using (SqlCommand cmd = new SqlCommand(resetQuery, conn, trans))
                        {
                            await cmd.ExecuteNonQueryAsync();
                        }

                        if (priorityMaterialNos != null && priorityMaterialNos.Any())
                        {
                            string updateQuery = "UPDATE tbl_SAP_Sparepart SET Priority = 'Y' WHERE Material = @MatNo";
                            using (SqlCommand cmd = new SqlCommand(updateQuery, conn, trans))
                            {
                                cmd.Parameters.Add("@MatNo", System.Data.SqlDbType.VarChar, 100);
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