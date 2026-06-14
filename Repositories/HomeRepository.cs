using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DashboardTeknikP1.Models;

namespace DashboardTeknikP1.Repositories
{
    public class HomeRepository
    {
        private readonly string _connectionString;

        public HomeRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // 1. Ambil data agregat Downtime per Work Center untuk grafik utama
        public List<Dictionary<string, object>> GetDowntimeSummary()
        {
            var data = new List<Dictionary<string, object>>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT WorkCenterPPDesc, SUM(TotalDownTimeInMinutes) as TotalMinutes 
                                FROM tbl_SAP_YP11 
                                GROUP BY WorkCenterPPDesc 
                                ORDER BY TotalMinutes DESC";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            data.Add(new Dictionary<string, object>
                            {
                                { "WorkCenter", reader["WorkCenterPPDesc"].ToString() },
                                { "TotalMinutes", Convert.ToDouble(reader["TotalMinutes"]) }
                            });
                        }
                    }
                }
            }
            return data;
        }

        // 2. Ambil data detail breakdown downtime untuk fitur drill-down
        public List<SAP_YP11> GetDowntimeDetails()
        {
            var list = new List<SAP_YP11>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // Kueri diperkaya dengan FunctionLocation (Mesin), Type, Shift, dan Week
                string query = @"SELECT WorkCenterPPDesc, NotificationDesc, TotalDownTimeInMinutes, 
                                        NotificationDate, DownTimeCode_ActivityCodeDesc,
                                        FunctionLocation, NotificationType, WageGroup_GroupShift, WeekKalendarIndofood
                                 FROM tbl_SAP_YP11 
                                 ORDER BY NotificationDate DESC";
                                 
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new SAP_YP11
                            {
                                WorkCenterPPDesc = reader["WorkCenterPPDesc"].ToString(),
                                NotificationDesc = reader["NotificationDesc"].ToString(),
                                TotalDownTimeInMinutes = Convert.ToDouble(reader["TotalDownTimeInMinutes"]),
                                NotificationDate = Convert.ToDateTime(reader["NotificationDate"]),
                                DownTimeCode_ActivityCodeDesc = reader["DownTimeCode_ActivityCodeDesc"].ToString(),
                                
                                // Meta Data Tambahan untuk Filter
                                FunctionLocation = reader["FunctionLocation"].ToString(),
                                NotificationType = reader["NotificationType"].ToString(),
                                WageGroup_GroupShift = reader["WageGroup_GroupShift"].ToString(),
                                WeekKalendarIndofood = reader["WeekKalendarIndofood"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }

        // 3. Ambil data Sparepart yang kritis (Stok <= SafetyStock)
        public List<SAP_Sparepart> GetEwsSpareparts()
        {
            var list = new List<SAP_Sparepart>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT MaterialNo, MaterialNoDescription, TotalQtyStock, SafetyStock, SLoc 
                                FROM tbl_SAP_Sparepart 
                                WHERE TotalQtyStock <= SafetyStock 
                                ORDER BY TotalQtyStock ASC";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new SAP_Sparepart
                            {
                                MaterialNo = reader["MaterialNo"].ToString(),
                                MaterialNoDescription = reader["MaterialNoDescription"].ToString(),
                                TotalQtyStock = Convert.ToDouble(reader["TotalQtyStock"]),
                                SafetyStock = Convert.ToInt32(reader["SafetyStock"]),
                                SLoc = reader["SLoc"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }
    }
}