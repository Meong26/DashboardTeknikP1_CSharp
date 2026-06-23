using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        // 1. Ambil data detail breakdown downtime (YP11) secara Asinkronus
        public async Task<List<SAP_YP11>> GetDowntimeDetailsAsync()
        {
            var list = new List<SAP_YP11>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT WorkCenterPPDesc, NotificationDesc, TotalDownTimeInMinutes, 
                                        NotificationDate, DownTimeCode_ActivityCodeDesc,
                                        FunctionLocation, NotificationType, WageGroup_GroupShift, 
                                        WeekKalendarIndofood, ActivityText
                                 FROM tbl_SAP_YP11 
                                 ORDER BY NotificationDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    await conn.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
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
                                ActivityText = reader["ActivityText"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }

        // 2. Ambil data Produksi (YR21) secara Asinkronus
        public async Task<List<SAP_YR21>> GetProduksiDetailsAsync()
        {
            var list = new List<SAP_YR21>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT PostingDate, ResourceName, PlannedHour, WageGroup, WeekOfBasicFinishedDate FROM tbl_SAP_YR21";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    await conn.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
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

        // 3. Ambil data Sparepart EWS (Jika sewaktu-waktu dibutuhkan di Home)
        public async Task<List<SAP_Sparepart>> GetEwsSparepartsAsync()
        {
            var list = new List<SAP_Sparepart>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Material, MaterialDescription, CurrentStock, SafetyStock, StorLoct 
                                FROM tbl_SAP_Sparepart 
                                WHERE CurrentStock <= SafetyStock 
                                ORDER BY CurrentStock ASC";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    await conn.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new SAP_Sparepart
                            {
                                Material = reader["Material"].ToString(),
                                MaterialDescription = reader["MaterialDescription"].ToString(),
                                CurrentStock = Convert.ToDouble(reader["CurrentStock"]),
                                SafetyStock = Convert.ToInt32(reader["SafetyStock"]),
                                StorLoct = reader["StorLoct"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }
    }
}