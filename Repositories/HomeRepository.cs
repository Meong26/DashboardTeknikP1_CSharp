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

        // 1. Ambil data detail breakdown downtime (YP11)
       public List<SAP_YP11> GetDowntimeDetails()
        {
            var list = new List<SAP_YP11>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // Kolom ActivityText sekarang dimasukkan ke dalam SELECT
                string query = @"SELECT WorkCenterPPDesc, NotificationDesc, TotalDownTimeInMinutes, 
                                        NotificationDate, DownTimeCode_ActivityCodeDesc,
                                        FunctionLocation, NotificationType, WageGroup_GroupShift, 
                                        WeekKalendarIndofood, ActivityText
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
                                FunctionLocation = reader["FunctionLocation"].ToString(),
                                NotificationType = reader["NotificationType"].ToString(),
                                WageGroup_GroupShift = reader["WageGroup_GroupShift"].ToString(),
                                WeekKalendarIndofood = reader["WeekKalendarIndofood"].ToString(),
                                // Mapping data baru
                                ActivityText = reader["ActivityText"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }
        // 2. Ambil data Produksi (YR21) untuk perhitungan persentase jam kerja
        public List<SAP_YR21> GetProduksiDetails()
        {
            var list = new List<SAP_YR21>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // Kueri diperkaya dengan WageGroup (Shift) dan Week
                string query = "SELECT PostingDate, ResourceName, PlannedHour, WageGroup, WeekOfBasicFinishedDate FROM tbl_SAP_YR21";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new SAP_YR21
                            {
                                PostingDate = Convert.ToDateTime(reader["PostingDate"]),
                                ResourceName = reader["ResourceName"].ToString(),
                                PlannedHour = Convert.ToDouble(reader["PlannedHour"]),
                                WageGroup = reader["WageGroup"].ToString(),
                                WeekOfBasicFinishedDate = reader["WeekOfBasicFinishedDate"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }

        // 3. Ambil data Sparepart EWS (Tetap kita simpan di sini untuk halaman EWS terpisah nanti)
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