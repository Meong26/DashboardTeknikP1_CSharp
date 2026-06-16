using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DashboardTeknikP1.Models;

namespace DashboardTeknikP1.Repositories
{
    public class SparepartRepository
    {
        private readonly string _connectionString;

        // Injeksi konfigurasi dari appsettings.json
        public SparepartRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public List<SAP_Sparepart> GetAllSpareparts()
        {
            var list = new List<SAP_Sparepart>();
            
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM tbl_SAP_Sparepart";
                
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new SAP_Sparepart
                            {
                                // Pengecekan DBNull agar aplikasi tidak crash jika ada kolom kosong dari SAP
                                PlantCode = reader["PlantCode"] != DBNull.Value ? reader["PlantCode"].ToString() : null,
                                MaterialNo = reader["MaterialNo"] != DBNull.Value ? reader["MaterialNo"].ToString() : null,
                                MaterialNoDescription = reader["MaterialNoDescription"] != DBNull.Value ? reader["MaterialNoDescription"].ToString() : null,
                                MatrGroupDescription = reader["MatrGroupDescription"] != DBNull.Value ? reader["MatrGroupDescription"].ToString() : null,
                                StorBin = reader["StorBin"] != DBNull.Value ? reader["StorBin"].ToString() : null,
                                BUn = reader["BUn"] != DBNull.Value ? reader["BUn"].ToString() : "PC",
                                
                                TotalQtyStock = reader["TotalQtyStock"] != DBNull.Value ? Convert.ToDouble(reader["TotalQtyStock"]) : 0,
                                SafetyStock = reader["SafetyStock"] != DBNull.Value ? Convert.ToInt32(reader["SafetyStock"]) : 0,
                                LamaTdkBergerakDay = reader["LamaTdkBergerakDay"] != DBNull.Value ? Convert.ToInt32(reader["LamaTdkBergerakDay"]) : 0,
                                MvgAvgPriceIDR = reader["MvgAvgPriceIDR"] != DBNull.Value ? Convert.ToDecimal(reader["MvgAvgPriceIDR"]) : 0
                            });
                        }
                    }
                }
            }
            return list;
        }
    }
}